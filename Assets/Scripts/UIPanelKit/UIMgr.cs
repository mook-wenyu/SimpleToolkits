using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using UnityEngine.Pool;

/// <summary>
/// UI管理器，负责管理所有UI面板的生命周期
/// </summary>
public class UIMgr : MonoSingleton<UIMgr>
{
    // UI Canvas
    private Canvas _uiCanvas;

    // 各层级的父节点
    private readonly Dictionary<UILayerType, Transform> _layerDict = new();

    // 当前打开的UI面板实例（使用UniqueId作为key）
    private readonly Dictionary<string, UIPanelBase> _openedPanelDict = new();

    // UI预制体缓存
    private readonly Dictionary<string, GameObject> _uiPrefabCache = new();

    // UI面板配置信息存储（面板类型名称 -> 配置信息）
    private readonly Dictionary<string, UIPanelInfo> _panelConfigs = new();

    // UI栈(用于管理UI层级关系和返回逻辑)
    private readonly Stack<UIPanelBase> _uiStack = new();

    // 是否正在执行UI动画（用于防止动画过程中重复操作）
    private bool _isPlayingAnim = false;

    #region 初始化
    /// <summary>
    /// 初始化UI管理器
    /// </summary>
    public async UniTask Init()
    {
        InitializeCanvas();
        InitLayers();
        await InitMaskPrefabAsync();
    }

    /// <summary>
    /// 初始化Canvas
    /// </summary>
    private void InitializeCanvas()
    {
        if (_uiCanvas)
        {
            return;
        }

        // 如果没有找到，则创建新的
        _uiCanvas = gameObject.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 100;

        // 添加CanvasScaler组件
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // 设置参考分辨率
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // 添加GraphicRaycaster组件
        gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 初始化UI层级
    /// </summary>
    private void InitLayers()
    {
        // 确保Canvas已初始化
        if (!_uiCanvas)
        {
            Debug.LogError("Canvas未初始化，无法创建UI层级");
            return;
        }

        // 清空现有层级字典
        _layerDict.Clear();

        // 获取枚举长度并使用for循环遍历
        var layerTypes = (UILayerType[])Enum.GetValues(typeof(UILayerType));
        foreach (var layer in layerTypes)
        {
            // 检查是否已存在该层级
            var existingLayer = _uiCanvas.transform.Find($"Layer_{layer.ToString()}");
            if (existingLayer)
            {
                _layerDict.Add(layer, existingLayer as RectTransform);
                continue;
            }

            // 创建新层级
            var layerObj = new GameObject($"Layer_{layer.ToString()}");
            var rect = layerObj.AddComponent<RectTransform>();
            rect.SetParent(_uiCanvas.transform);

            // 设置铺满
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            // 设置层级顺序
            rect.SetSiblingIndex((int)layer);

            _layerDict.Add(layer, rect);
        }
    }

    /// <summary>
    /// 初始化遮罩预制体
    /// </summary>
    private async UniTask InitMaskPrefabAsync()
    {
        // 加载并缓存遮罩预制体
        var maskPrefab = await LoadAndCachePrefab<UIMaskPanel>();

        // 创建遮罩对象池
        GetOrCreateUIPool<UIMaskPanel>(maskPrefab);
    }
    #endregion

    #region UI面板管理
    /// <summary>
    /// 预注册面板（提前创建实例并放入对象池）
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="preCreateCount">预创建数量</param>
    /// <param name="layer">UI层级</param>
    /// <param name="allowMultiple">是否允许多实例</param>
    /// <param name="fullscreen">是否全屏面板</param>
    /// <param name="needMask">是否需要背景遮罩</param>
    /// <param name="closeByOutside">是否可以点击外部关闭</param>
    /// <param name="animType">面板动画类型</param>
    public async UniTask<bool> RegisterPanel<T>(int preCreateCount = 1, UILayerType layer = UILayerType.Panel,
        bool allowMultiple = false, bool fullscreen = false, bool needMask = false,
        bool closeByOutside = false, UIPanelAnimType animType = UIPanelAnimType.None) where T : UIPanelBase
    {
        // 获取面板名称
        string panelName = typeof(T).Name;

        if (preCreateCount <= 0)
        {
            Debug.LogWarning($"预注册面板 {panelName} 失败：preCreateCount <= 0");
            return false;
        }

        // 创建并存储面板配置信息
        var panelInfo = new UIPanelInfo
        {
            PanelType = typeof(T),
            Layer = layer,
            AllowMultiple = allowMultiple,
            Fullscreen = fullscreen,
            NeedMask = needMask,
            CloseByOutside = closeByOutside,
            AnimType = animType
        };

        // 存储面板配置
        _panelConfigs[panelName] = panelInfo;

        // 加载并缓存预制体
        var prefab = await LoadAndCachePrefab<T>();
        if (prefab == null)
        {
            Debug.LogError($"预注册面板失败，无法加载预制体: {panelName}");
            return false;
        }

        // 创建对象池并预创建实例（所有面板都使用对象池）
        var pool = GetOrCreateUIPool<T>(prefab);

        // 预创建指定数量的实例
        var tempPanels = new GameObject[preCreateCount];
        for (var i = 0; i < preCreateCount; i++)
        {
            tempPanels[i] = pool.Get();
        }

        // 立即释放回对象池
        for (var i = 0; i < preCreateCount; i++)
        {
            pool.Release(tempPanels[i]);
        }

        Debug.Log($"预注册面板 {panelName} 成功，预创建了 {preCreateCount} 个实例");

        return true;
    }

    /// <summary>
    /// 打开UI面板（使用注册时的配置）
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="args">传递给面板的参数</param>
    /// <returns>面板实例</returns>
    public async UniTask<T> OpenPanel<T>(object args = null) where T : UIPanelBase
    {
        // 获取面板配置信息
        var panelInfo = GetPanelConfig<T>();

        // 使用配置信息打开面板
        return await OpenPanelWithConfig<T>(args, panelInfo);
    }

    /// <summary>
    /// 获取面板配置信息
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    private UIPanelInfo GetPanelConfig<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;

        if (_panelConfigs.TryGetValue(panelName, out var config))
        {
            return config;
        }

        // 如果面板未注册，返回默认配置并给出警告
        Debug.LogWarning($"面板 {panelName} 未注册，使用默认配置。建议先调用RegisterPanel进行注册。");
        return new UIPanelInfo
        {
            PanelType = typeof(T),
            Layer = UILayerType.Panel,
            AllowMultiple = false,
            Fullscreen = false,
            NeedMask = false,
            CloseByOutside = false,
            AnimType = UIPanelAnimType.None
        };
    }

    /// <summary>
    /// 使用配置信息打开UI面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="args">传递给面板的参数</param>
    /// <param name="panelInfo">面板配置信息</param>
    private async UniTask<T> OpenPanelWithConfig<T>(object args, UIPanelInfo panelInfo) where T : UIPanelBase
    {
        // 如果正在播放动画，则忽略重复操作
        if (_isPlayingAnim)
        {
            Debug.Log($"正在播放UI动画，忽略打开面板请求: {typeof(T).Name}");
            return null;
        }

        string panelName = typeof(T).Name;

        // 检查面板是否已打开（如果不允许多实例）
        if (!panelInfo.AllowMultiple)
        {
            // 查找是否已有同类型的面板在显示
            foreach (var kvp in _openedPanelDict)
            {
                if (kvp.Value.PanelName == panelName)
                {
                    // 如果已经打开并不允许多实例，则刷新并返回现有面板
                    kvp.Value.Refresh(args);
                    return kvp.Value as T;
                }
            }
        }

        UIPanelBase panel = null;

        // 优先从对象池获取面板实例（所有面板都使用对象池）
        var pooledObject = GetFromUIPool<T>();
        if (pooledObject != null)
        {
            panel = pooledObject.GetComponent<T>();
            Debug.Log($"从对象池获取面板: {panelName}");
        }

        // 如果对象池中没有可用实例，则创建新实例
        if (panel == null)
        {
            panel = await CreatePanelInstance<T>(panelInfo.Layer, panelInfo.Fullscreen);
            if (panel == null)
            {
                Debug.LogError($"创建面板失败: {panelName}");
                return null;
            }
        }
        else
        {
            // 重新设置父对象和位置（对象池中的面板可能位置不正确）
            var layerTrans = _layerDict[panelInfo.Layer];
            panel.transform.SetParent(layerTrans, false);

            var rectTrans = panel.GetComponent<RectTransform>();
            if (panelInfo.Fullscreen)
            {
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                rectTrans.offsetMin = Vector2.zero;
                rectTrans.offsetMax = Vector2.zero;
            }

            rectTrans.localScale = Vector3.one;
        }

        // 添加到正在显示的面板字典
        _openedPanelDict[panel.UniqueId] = panel;

        // 创建背景遮罩
        if (panelInfo.NeedMask)
        {
            CreatePanelMask(panel, panelInfo.CloseByOutside);
        }

        // 播放打开动画
        await PlayPanelAnimation(panel, panelInfo.AnimType, true);

        // 显示面板
        panel.Show(args);

        // 管理UI栈（默认添加到栈中）
        _uiStack.Push(panel);

        Debug.Log($"面板 {panel.PanelName}({panel.UniqueId}) 已显示");

        return panel as T;
    }

    /// <summary>
    /// 关闭UI面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="destroy">是否强制销毁面板，默认false（回收到对象池）</param>
    public async UniTask ClosePanel<T>(bool destroy = false) where T : UIPanelBase
    {
        string panelName = typeof(T).Name;

        // 查找第一个匹配类型的面板
        UIPanelBase targetPanel = null;
        foreach (var kvp in _openedPanelDict)
        {
            if (kvp.Value.PanelName == panelName)
            {
                targetPanel = kvp.Value;
                break;
            }
        }

        if (targetPanel != null)
        {
            await ClosePanelInternal(targetPanel, destroy);
        }
    }

    /// <summary>
    /// 关闭指定的UI面板实例
    /// </summary>
    /// <param name="panel">要关闭的面板</param>
    /// <param name="destroy">是否强制销毁面板，默认false（回收到对象池）</param>
    public async UniTask ClosePanel(UIPanelBase panel, bool destroy = false)
    {
        await ClosePanelInternal(panel, destroy);
    }

    /// <summary>
    /// 内部关闭面板逻辑
    /// </summary>
    /// <param name="panel">要关闭的面板</param>
    /// <param name="destroy">是否强制销毁面板</param>
    private async UniTask ClosePanelInternal(UIPanelBase panel, bool destroy)
    {
        if (panel == null) return;

        // 如果正在播放动画，则忽略重复操作
        if (_isPlayingAnim)
        {
            Debug.Log($"正在播放UI动画，忽略关闭面板请求: {panel.PanelName}");
            return;
        }

        // 获取面板配置信息（用于获取动画类型）
        string panelName = panel.PanelName;
        var animType = UIPanelAnimType.None;

        if (_panelConfigs.TryGetValue(panelName, out var config))
        {
            animType = config.AnimType;
        }

        // 播放关闭音效
        AudioMgr.Instance.PlaySound("UI_关闭");

        // 从UI栈中移除
        if (_uiStack.Count > 0 && _uiStack.Peek() == panel)
        {
            _uiStack.Pop();
        }

        // 播放关闭动画
        await PlayPanelAnimation(panel, animType, false);

        // 隐藏面板（这会自动从_openedPanelDict中移除）
        panel.HideInternal();

        // 移除背景遮罩
        RemovePanelMask(panel);

        if (destroy)
        {
            // 强制销毁面板
            Destroy(panel.gameObject);
        }
        else
        {
            // 默认回收到对象池
            RecycleToUIPool(panel.gameObject, panelName);
        }
    }

    /// <summary>
    /// 返回上一个UI
    /// </summary>
    public async UniTask GoBack()
    {
        if (_uiStack.Count <= 0) return;

        // 如果正在播放动画，则忽略重复操作
        if (_isPlayingAnim)
        {
            Debug.Log("正在播放UI动画，忽略返回操作");
            return;
        }

        var currentPanel = _uiStack.Pop();
        await ClosePanel(currentPanel);

        // 显示栈顶的面板（如果存在）
        if (_uiStack.Count > 0)
        {
            var topPanel = _uiStack.Peek();

            // 重新显示栈顶面板
            _openedPanelDict[topPanel.UniqueId] = topPanel;
            topPanel.gameObject.SetActive(true);
            topPanel.Show();

            Debug.Log($"返回到面板 {topPanel.PanelName}({topPanel.UniqueId})");
        }
    }

    /// <summary>
    /// 创建面板实例
    /// </summary>
    private async UniTask<UIPanelBase> CreatePanelInstance<T>(UILayerType layer, bool fullscreen) where T : UIPanelBase
    {
        // 加载并缓存预制体
        var prefab = await LoadAndCachePrefab<T>();
        if (prefab == null)
        {
            Debug.LogError($"加载UI预制体失败: {typeof(T).Name}");
            return null;
        }

        // 创建新实例（所有面板都使用对象池）
        var pool = GetOrCreateUIPool<T>(prefab);
        var panelGo = pool.Get();

        // 设置父对象和位置
        var layerTrans = _layerDict[layer];
        panelGo.transform.SetParent(layerTrans, false);

        // 设置RectTransform
        var rectTrans = panelGo.GetComponent<RectTransform>();

        // 根据Fullscreen属性决定是否铺满
        if (fullscreen)
        {
            // 铺满整个父容器
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.offsetMin = Vector2.zero;
            rectTrans.offsetMax = Vector2.zero;
        }

        // 统一设置缩放
        rectTrans.localScale = Vector3.one;

        // 获取或添加面板组件
        var panel = panelGo.GetComponent<T>();
        if (panel == null)
        {
            panel = panelGo.AddComponent<T>();
        }

        // 初始化面板
        panel.Init(this);

        return panel;
    }

    /// <summary>
    /// 加载并缓存UI预制体
    /// </summary>
    /// <returns>预制体GameObject</returns>
    private async UniTask<GameObject> LoadAndCachePrefab<T>()
    {
        string prefabPath = typeof(T).Name;
        string uiPanelPath = ResMgr.Instance.Settings.UIPanelPath;
        if (!string.IsNullOrEmpty(uiPanelPath))
        {
            prefabPath = $"{uiPanelPath}/{prefabPath}";
        }

        // 检查缓存
        if (_uiPrefabCache.TryGetValue(prefabPath, out var cachedPrefab))
        {
            return cachedPrefab;
        }

        // 加载预制体
        var prefab = await ResMgr.Instance.LoadAssetAsync<GameObject>(prefabPath);

        // 缓存预制体（所有预制体都缓存）
        if (prefab != null)
        {
            _uiPrefabCache[prefabPath] = prefab;
        }

        return prefab;
    }
    #endregion

    #region UI动画与遮罩
    /// <summary>
    /// 播放面板动画
    /// </summary>
    private async UniTask PlayPanelAnimation(UIPanelBase panel, UIPanelAnimType animType, bool isOpen)
    {
        if (animType == UIPanelAnimType.None || panel == null) return;

        _isPlayingAnim = true;
        var canvasGroup = panel.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        }

        var rect = panel.GetComponent<RectTransform>();
        var originalPos = rect.localPosition;
        var originalScale = rect.localScale;
        float originalAlpha = canvasGroup.alpha;

        // 设置初始状态
        if (isOpen)
        {
            switch (animType)
            {
                case UIPanelAnimType.Fade:
                    canvasGroup.alpha = 0;
                    break;
                case UIPanelAnimType.Scale:
                    rect.localScale = Vector3.zero;
                    break;
                case UIPanelAnimType.SlideFromTop:
                    rect.localPosition = new Vector3(0, Screen.height, 0);
                    break;
                case UIPanelAnimType.SlideFromBottom:
                    rect.localPosition = new Vector3(0, -Screen.height, 0);
                    break;
                case UIPanelAnimType.SlideFromLeft:
                    rect.localPosition = new Vector3(-Screen.width, 0, 0);
                    break;
                case UIPanelAnimType.SlideFromRight:
                    rect.localPosition = new Vector3(Screen.width, 0, 0);
                    break;
            }
        }

        // 动画时间
        var duration = 0.3f;

        // 根据动画类型执行不同的动画
        switch (animType)
        {
            case UIPanelAnimType.Fade:
                float fromAlpha = isOpen ? 0 : 1;
                float toAlpha = isOpen ? 1 : 0;
                await Tween.Custom(fromAlpha, toAlpha, duration,
                    onValueChange: value => canvasGroup.alpha = value).ToYieldInstruction();
                break;

            case UIPanelAnimType.Scale:
                var fromScale = isOpen ? Vector3.zero : Vector3.one;
                var toScale = isOpen ? Vector3.one : Vector3.zero;
                await Tween.Scale(rect, toScale, duration).ToYieldInstruction();
                break;

            case UIPanelAnimType.SlideFromTop:
            case UIPanelAnimType.SlideFromBottom:
            case UIPanelAnimType.SlideFromLeft:
            case UIPanelAnimType.SlideFromRight:
                var fromPos = isOpen ? rect.localPosition : Vector3.zero;
                var toPos = isOpen ? Vector3.zero : originalPos;
                await Tween.LocalPosition(rect, toPos, duration).ToYieldInstruction();
                break;
        }

        _isPlayingAnim = false;
    }

    /// <summary>
    /// 创建面板背景遮罩（使用统一对象池）
    /// </summary>
    private void CreatePanelMask(UIPanelBase panel, bool closeByOutside)
    {
        if (panel == null) return;

        // 从统一对象池获取遮罩
        var maskObj = GetFromUIPool<UIMaskPanel>();
        if (maskObj == null)
        {
            Debug.LogError("无法从对象池获取遮罩对象，请确保已初始化遮罩预制体");
            return;
        }

        // 获取UIMask组件
        var maskPanel = maskObj.GetComponent<UIMaskPanel>();
        if (maskPanel == null)
        {
            Debug.LogError("遮罩对象缺少UIMask组件");
            return;
        }

        maskObj.name = "Mask_" + panel.PanelName;

        // 初始化遮罩面板
        maskPanel.Init(this);

        // 设置父对象为面板所在层的父对象
        maskObj.transform.SetParent(panel.transform.parent, false);
        maskObj.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

        // 设置铺满
        var rectTrans = maskObj.GetComponent<RectTransform>();
        rectTrans.anchorMin = Vector2.zero;
        rectTrans.anchorMax = Vector2.one;
        rectTrans.offsetMin = Vector2.zero;
        rectTrans.offsetMax = Vector2.zero;

        // 添加点击事件
        if (closeByOutside)
        {
            var btn = maskObj.GetComponent<Button>();
            btn.onClick.AddListener(() => ClosePanel(panel).Forget());
        }

        // 确保遮罩在面板之前（下方）显示
        maskObj.transform.SetAsFirstSibling();

        // 显示遮罩
        maskPanel.Show();

        // 将遮罩加入UI栈管理（所有面板都加入栈）
        _uiStack.Push(maskPanel);
    }

    /// <summary>
    /// 移除面板背景遮罩（回收到统一对象池）
    /// </summary>
    private void RemovePanelMask(UIPanelBase panel)
    {
        if (panel == null) return;

        string maskName = "Mask_" + panel.PanelName;
        var parent = panel.transform.parent;

        if (parent != null)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == maskName)
                {
                    var maskPanel = child.GetComponent<UIMaskPanel>();
                    if (maskPanel != null)
                    {
                        // 从UI栈中移除遮罩
                        if (_uiStack.Count > 0 && _uiStack.Peek() == maskPanel)
                        {
                            _uiStack.Pop();
                        }

                        // 隐藏遮罩
                        maskPanel.Hide();
                    }

                    // 回收到统一对象池
                    RecycleToUIPool(child.gameObject, nameof(UIMaskPanel));
                    break;
                }
            }
        }
    }
    #endregion

    #region UI对象池管理（委托给PoolMgr）
    /// <summary>
    /// 获取或创建UI对象池
    /// </summary>
    private ObjectPool<GameObject> GetOrCreateUIPool<T>(GameObject prefab) where T : UIPanelBase
    {
        string panelName = typeof(T).Name;

        return PoolMgr.Instance.GetOrCreatePool<GameObject>(
            poolName: panelName,
            createFunc: () => CreateUIPooledObject(prefab),
            actionOnGet: OnGetFromUIPool,
            actionOnRelease: OnReleaseToUIPool,
            actionOnDestroy: OnDestroyUIPooledObject,
            defaultCapacity: 1,
            maxSize: 100
        );
    }

    /// <summary>
    /// 创建UI池化对象
    /// </summary>
    private GameObject CreateUIPooledObject(GameObject prefab)
    {
        var obj = Instantiate(prefab, transform, true);
        obj.SetActive(false);
        return obj;
    }

    /// <summary>
    /// 从UI对象池获取对象时的回调
    /// </summary>
    private void OnGetFromUIPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(true);
        }
    }

    /// <summary>
    /// 释放对象到UI对象池时的回调
    /// </summary>
    private void OnReleaseToUIPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// 销毁UI池化对象时的回调
    /// </summary>
    private void OnDestroyUIPooledObject(GameObject obj)
    {
        if (obj != null)
        {
            Destroy(obj);
        }
    }

    /// <summary>
    /// 从UI对象池获取对象
    /// </summary>
    private GameObject GetFromUIPool<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;
        return PoolMgr.Instance.GetFromPool<GameObject>(panelName);
    }

    /// <summary>
    /// 回收对象到UI对象池
    /// </summary>
    private void RecycleToUIPool(GameObject obj, string panelName)
    {
        if (obj == null) return;
        PoolMgr.Instance.RecycleToPool<GameObject>(obj, panelName);
    }

    /// <summary>
    /// 清空UI对象池
    /// </summary>
    public void ClearUIPool(string panelName = null)
    {
        PoolMgr.Instance.ClearPool(panelName);
    }

    /// <summary>
    /// 从正在显示的面板字典中移除面板
    /// </summary>
    internal void RemoveFromOpenedPanels(UIPanelBase panel)
    {
        if (panel != null && _openedPanelDict.Remove(panel.UniqueId))
        {
            Debug.Log($"面板 {panel.PanelName}({panel.UniqueId}) 已从显示列表中移除");
        }
    }

    /// <summary>
    /// 获取所有正在显示的面板
    /// </summary>
    public UIPanelBase[] GetAllOpenedPanels()
    {
        var panels = new UIPanelBase[_openedPanelDict.Count];
        var index = 0;
        foreach (var panel in _openedPanelDict.Values)
        {
            panels[index++] = panel;
        }
        return panels;
    }

    /// <summary>
    /// 获取指定类型的所有正在显示的面板
    /// </summary>
    public T[] GetAllPanels<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;
        var matchingPanels = new List<T>();

        foreach (var kvp in _openedPanelDict)
        {
            if (kvp.Value.PanelName == panelName)
            {
                matchingPanels.Add(kvp.Value as T);
            }
        }

        return matchingPanels.ToArray();
    }

    /// <summary>
    /// 获取正在显示的面板数量
    /// </summary>
    public int GetOpenedPanelCount()
    {
        return _openedPanelDict.Count;
    }

    /// <summary>
    /// 获取指定类型正在显示的面板数量
    /// </summary>
    public int GetOpenedPanelCount<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;
        var count = 0;

        foreach (var kvp in _openedPanelDict)
        {
            if (kvp.Value.PanelName == panelName)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 关闭所有指定类型的面板
    /// </summary>
    public async UniTask CloseAllPanels<T>(bool destroy = false) where T : UIPanelBase
    {
        var panels = GetAllPanels<T>();
        foreach (var panel in panels)
        {
            await ClosePanel(panel, destroy);
        }
    }
    #endregion

    #region 其他功能
    /// <summary>
    /// 销毁所有UI
    /// </summary>
    public void DestroyAllUI()
    {
        foreach (var panel in _openedPanelDict.Values)
        {
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }

        _openedPanelDict.Clear();
        _uiStack.Clear();
    }

    /// <summary>
    /// 获取已打开的面板
    /// </summary>
    public T GetPanel<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;

        // 查找第一个匹配类型的面板
        foreach (var kvp in _openedPanelDict)
        {
            if (kvp.Value.PanelName == panelName)
            {
                return kvp.Value as T;
            }
        }

        return null;
    }

    /// <summary>
    /// 检查面板是否打开
    /// </summary>
    public bool IsPanelOpen<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;

        // 查找是否有匹配类型的面板在显示
        foreach (var kvp in _openedPanelDict)
        {
            if (kvp.Value.PanelName == panelName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 刷新面板
    /// </summary>
    public void RefreshPanel<T>(object args = null) where T : UIPanelBase
    {
        var panel = GetPanel<T>();
        if (panel != null)
        {
            panel.Refresh(args);
        }
    }

    /// <summary>
    /// 刷新所有指定类型的面板
    /// </summary>
    public void RefreshAllPanels<T>(object args = null) where T : UIPanelBase
    {
        string panelName = typeof(T).Name;

        foreach (var kvp in _openedPanelDict)
        {
            if (kvp.Value.PanelName == panelName)
            {
                kvp.Value.Refresh(args);
            }
        }
    }

    /// <summary>
    /// 隐藏所有UI
    /// </summary>
    public void HideAllUI()
    {
        foreach (var panel in _openedPanelDict.Values)
        {
            panel.Hide();
        }
    }

    /// <summary>
    /// 显示所有UI
    /// </summary>
    public void ShowAllUI()
    {
        foreach (var panel in _openedPanelDict.Values)
        {
            panel.Show();
        }
    }

    /// <summary>
    /// 获取面板配置信息
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <returns>面板配置信息，如果未注册则返回null</returns>
    public UIPanelInfo GetPanelInfo<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;
        return _panelConfigs.GetValueOrDefault(panelName, null);
    }

    /// <summary>
    /// 检查面板是否已注册
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <returns>是否已注册</returns>
    public bool IsPanelRegistered<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;
        return _panelConfigs.ContainsKey(panelName);
    }

    /// <summary>
    /// 获取所有已注册的面板配置
    /// </summary>
    /// <returns>面板配置字典的副本</returns>
    public Dictionary<string, UIPanelInfo> GetAllPanelConfigs()
    {
        return new Dictionary<string, UIPanelInfo>(_panelConfigs);
    }

    /// <summary>
    /// 移除面板注册配置
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <returns>是否成功移除</returns>
    public bool UnregisterPanel<T>() where T : UIPanelBase
    {
        string panelName = typeof(T).Name;
        bool removed = _panelConfigs.Remove(panelName);

        if (removed)
        {
            Debug.Log($"面板 {panelName} 配置已移除");

            // 同时清理对应的UI对象池
            ClearUIPool(panelName);
        }

        return removed;
    }

    protected override void OnDestroy()
    {
        DestroyAllUI();

        // 清理面板配置
        _panelConfigs.Clear();

        base.OnDestroy();
    }
    #endregion
}
