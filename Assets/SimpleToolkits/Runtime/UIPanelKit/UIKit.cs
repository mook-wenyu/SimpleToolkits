using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace SimpleToolkits
{
    /// <summary>
    /// UI管理器，负责管理所有UI面板的生命周期
    /// </summary>
    public class UIKit : MonoBehaviour
    {
        // UI Canvas
        private Canvas _uiCanvas;
        // 各层级的父节点
        private readonly Dictionary<UILayerType, Transform> _layerDict = new();
        // 当前打开的UI面板实例（使用UniqueId作为key）
        private readonly Dictionary<string, UIPanelBase> _openedPanelDict = new();
        // UI面板配置信息存储（面板类型名称 -> 配置信息）
        private readonly Dictionary<string, UIPanelInfo> _panelConfigs = new();
        // UI栈(用于管理UI层级关系和返回逻辑)
        private readonly Stack<UIPanelBase> _uiStack = new();
        // 正在隐藏的面板集合（用于防止重复隐藏）
        private readonly HashSet<string> _hidingPanels = new();
        // 对象池管理器
        private PoolManager _poolManager;
        // 是否正在执行UI动画（用于防止动画过程中重复操作）
        private bool _isPlayingAnim;
        // 是否正在执行GoBack操作（用于防止重复的GoBack调用）
        private bool _isGoingBack;

        #region 初始化
        private void Awake()
        {
            _poolManager = GSMgr.Instance.GetServiceObject<PoolManagerService, PoolManager>();
            if (_poolManager == null)
            {
                Debug.LogError("PoolManager未初始化，无法创建UI管理器");
                return;
            }

            InitializeCanvas();
            InitLayers();
            InitMaskPool();
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
            _uiCanvas.sortingOrder = 200;
            _uiCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            _uiCanvas.vertexColorAlwaysGammaSpace = true;

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
                // 跳过无效层级
                if (layer == UILayerType.None) continue;

                // 检查是否已存在该层级
                var existingLayer = _uiCanvas.transform.Find($"Layer_{layer.ToString()}");
                if (existingLayer)
                {
                    _layerDict[layer] = existingLayer as RectTransform;
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

                _layerDict[layer] = rect;
            }
        }
        #endregion

        #region UI面板管理
        /// <summary>
        /// 确保面板资源已准备（加载预制体、缓存管理并创建对象池）
        /// </summary>
        /// <returns>资源准备是否成功</returns>
        private async UniTask<bool> EnsurePanelResourcesAsync<T>() where T : UIPanelBase
        {
            // 构建预制体路径
            var prefabPath = typeof(T).Name;

            // 加载预制体
            var prefab = await GSMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogError($"加载UI预制体失败: {typeof(T).Name}, 路径: {prefabPath}");
                return false;
            }

            // 创建对象池
            var pool = GetOrCreateUIPool<T>(prefab);
            if (pool) return true;

            Debug.LogError($"创建对象池失败: {typeof(T).Name}");
            return false;
        }

        /// <summary>
        /// 注册面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="preCreateCount">预创建数量</param>
        /// <param name="layer">UI层级</param>
        /// <param name="allowMultiple">是否允许多实例</param>
        /// <param name="needMask">是否需要背景遮罩</param>
        /// <param name="closeByOutside">是否可以点击外部隐藏</param>
        /// <param name="animType">面板动画类型</param>
        public async UniTask<bool> RegisterPanel<T>(UILayerType layer = UILayerType.Panel,
            bool allowMultiple = false, bool needMask = false,
            bool closeByOutside = false, UIPanelAnimType animType = UIPanelAnimType.None,
            int preCreateCount = 1) where T : UIPanelBase
        {
            // 获取面板名称
            var panelName = typeof(T).Name;

            if (preCreateCount <= 0)
            {
                Debug.LogWarning($"预注册面板 {panelName} 失败：preCreateCount <= 0");
                return false;
            }

            // 创建并存储面板配置信息
            var panelInfo = new UIPanelInfo
            {
                panelType = typeof(T),
                layer = layer,
                allowMultiple = allowMultiple.ToBoolType(),
                needMask = needMask.ToBoolType(),
                closeByOutside = closeByOutside.ToBoolType(),
                animType = animType
            };

            // 存储面板配置
            _panelConfigs[panelName] = panelInfo;

            // 确保面板资源已准备（加载预制体、缓存并创建对象池）
            var success = await EnsurePanelResourcesAsync<T>();
            if (!success)
            {
                Debug.LogError($"预注册面板失败，无法加载预制体: {panelName}");
                return false;
            }

            // 预创建指定数量的实例并确保正确初始化
            var tempPanels = new GameObject[preCreateCount];
            for (var i = 0; i < preCreateCount; i++)
            {
                // 从对象池获取GameObject
                tempPanels[i] = GetFromUIPool<T>();

                // 初始化面板
                tempPanels[i].GetComponent<T>().Init(this);
            }

            // 立即释放回对象池
            for (var i = 0; i < preCreateCount; i++)
            {
                RecycleToUIPool(tempPanels[i], panelName);
            }
#if UNITY_EDITOR
            Debug.Log($"预注册面板 {panelName} 成功，预创建了 {preCreateCount} 个已初始化的实例");
#endif
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
            return await OpenPanelInternal<T>(args, panelInfo);
        }

        /// <summary>
        /// 打开UI面板（使用部分自定义配置，未指定的参数使用默认配置）
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="args">传递给面板的参数</param>
        /// <param name="info">部分配置信息，只需要设置需要自定义的属性</param>
        /// <returns>面板实例</returns>
        public async UniTask<T> OpenPanel<T>(object args, UIPanelInfo info) where T : UIPanelBase
        {
            // 获取默认配置
            var defaultConfig = GetPanelConfig<T>();

            // 创建合并后的配置
            var mergedConfig = defaultConfig.Clone();
            mergedConfig.MergeFrom(in info);

            // 使用合并后的配置打开面板
            return await OpenPanelInternal<T>(args, mergedConfig);
        }

        /// <summary>
        /// 获取面板配置信息
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        private UIPanelInfo GetPanelConfig<T>() where T : UIPanelBase
        {
            var panelName = typeof(T).Name;

            if (_panelConfigs.TryGetValue(panelName, out var config))
            {
                return config;
            }

            // 如果面板未注册，返回默认配置并给出警告
            Debug.LogWarning($"面板 {panelName} 未注册，使用默认配置。建议先调用RegisterPanel进行注册。");
            return new UIPanelInfo
            {
                panelType = typeof(T),
                layer = UILayerType.Panel,
                allowMultiple = BoolType.False,
                needMask = BoolType.False,
                closeByOutside = BoolType.False,
                animType = UIPanelAnimType.None
            };
        }

        /// <summary>
        /// 内部打开UI面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="args">传递给面板的参数</param>
        /// <param name="panelInfo">面板配置信息</param>
        private async UniTask<T> OpenPanelInternal<T>(object args, UIPanelInfo panelInfo) where T : UIPanelBase
        {
            // 如果正在播放动画，则忽略重复操作
            if (_isPlayingAnim)
            {
                Debug.Log($"正在播放UI动画，忽略打开面板请求: {typeof(T).Name}");
                return null;
            }

            var panelName = typeof(T).Name;

            // TODO: 音效

            // 检查面板是否已打开（如果不允许多实例）
            if (!panelInfo.allowMultiple.ToBool())
            {
                // 查找是否已有同类型的面板在显示
                foreach (var kvp in _openedPanelDict)
                {
                    if (kvp.Value.PanelName != panelName) continue;

                    // 如果已经打开并不允许多实例，则刷新并返回现有面板
                    kvp.Value.Refresh(args);
#if UNITY_EDITOR
                    Debug.Log("面板已打开，刷新并返回现有面板");
#endif
                    return kvp.Value as T;
                }
            }

            UIPanelBase panel = null;

            var pooledObject = GetFromUIPool<T>();
            if (pooledObject)
            {
                panel = pooledObject.GetComponent<T>();
            }

            if (panel)
            {
                // 重新设置父对象和位置（对象池中的面板可能位置不正确）
                var layerTrans = _layerDict[panelInfo.layer];
                panel.transform.SetParent(layerTrans, false);
                panel.transform.SetAsLastSibling();

                // 初始化面板
                panel.Init(this);
            }
            else
            {
                Debug.LogError($"无法创建面板实例: {panelName}");
                return null;
            }

            // 添加到正在显示的面板字典
            _openedPanelDict[panel.UniqueId] = panel;

            // 创建背景遮罩
            if (panelInfo.needMask.ToBool())
            {
                CreatePanelMask(panel, panelInfo.closeByOutside.ToBool());
            }

            // 播放打开动画
            await PlayPanelAnimation(panel, panelInfo.animType, true);

            // 显示面板
            panel.Show(args);

            // 管理UI栈（默认添加到栈中）
            _uiStack.Push(panel);
#if UNITY_EDITOR
            Debug.Log($"面板 {panel.PanelName}({panel.UniqueId}) 已显示");
#endif
            return (T)panel;
        }

        /// <summary>
        /// 隐藏指定的UI面板实例
        /// </summary>
        /// <param name="panel">要隐藏的面板</param>
        /// <param name="destroy">是否强制销毁面板，默认false（回收到对象池）</param>
        public async UniTask HidePanel(UIPanelBase panel, bool destroy = false)
        {
            await HidePanelInternal(panel, destroy);
        }

        /// <summary>
        /// 内部隐藏面板逻辑
        /// </summary>
        /// <param name="panel">要隐藏的面板</param>
        /// <param name="destroy">是否强制销毁面板</param>
        private async UniTask HidePanelInternal(UIPanelBase panel, bool destroy)
        {
            if (!panel) return;
            if (_hidingPanels.Contains(panel.UniqueId))
            {
                Debug.Log($"面板已在隐藏过程中，忽略重复隐藏请求: {panel.PanelName}({panel.UniqueId})");
                return;
            }
            if (!_openedPanelDict.ContainsKey(panel.UniqueId))
            {
                Debug.Log($"面板已隐藏，忽略重复隐藏请求: {panel.PanelName}({panel.UniqueId})");
                return;
            }
            if (_isPlayingAnim)
            {
                Debug.Log($"正在播放UI动画，忽略隐藏面板请求: {panel.PanelName}");
                return;
            }
            // 标记面板为正在隐藏状态
            _hidingPanels.Add(panel.UniqueId);

            // 获取面板配置信息（用于获取动画类型）
            var panelName = panel.PanelName;
            var animType = UIPanelAnimType.None;

            if (_panelConfigs.TryGetValue(panelName, out var config))
            {
                animType = config.animType;
            }

            // TODO: 音效

            // 从UI栈中移除
            if (_uiStack.Count > 0 && _uiStack.Peek() == panel)
            {
                _uiStack.Pop();
            }

            // 播放隐藏动画
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

            // 移除隐藏标记
            _hidingPanels.Remove(panel.UniqueId);
        }

        /// <summary>
        /// 返回上一个UI
        /// </summary>
        public async UniTask GoBack()
        {
            if (_isGoingBack)
            {
                Debug.Log("正在执行返回操作，忽略重复的GoBack请求");
                return;
            }
            if (_uiStack.Count <= 0)
            {
                Debug.Log("UI栈为空，无法执行返回操作");
                return;
            }
            if (_isPlayingAnim)
            {
                Debug.Log("正在播放UI动画，忽略返回操作");
                return;
            }
            // 标记正在执行GoBack操作
            _isGoingBack = true;

            try
            {
                // 再次检查栈状态（防止在等待期间栈被修改）
                if (_uiStack.Count <= 0)
                {
                    Debug.Log("UI栈在执行过程中变为空，取消返回操作");
                    return;
                }

                var currentPanel = _uiStack.Peek();

                // 检查面板是否已经在隐藏过程中
                if (_hidingPanels.Contains(currentPanel.UniqueId))
                {
                    Debug.Log($"面板已在隐藏过程中，取消返回操作: {currentPanel.PanelName}");
                    return;
                }

                await HidePanel(currentPanel);
            }
            finally
            {
                // 确保在任何情况下都清除GoBack标记
                _isGoingBack = false;
            }
        }
        #endregion

        #region UI动画与遮罩
        /// <summary>
        /// 播放面板动画
        /// </summary>
        private async UniTask PlayPanelAnimation(UIPanelBase panel, UIPanelAnimType animType, bool isOpen)
        {
            if (animType == UIPanelAnimType.None || !panel) return;

            _isPlayingAnim = true;
            var canvasGroup = panel.gameObject.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }

            var rect = panel.GetComponent<RectTransform>();
            var originalPos = rect.localPosition;

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
            else
            {
                switch (animType)
                {
                    case UIPanelAnimType.SlideFromTop:
                        originalPos = new Vector3(0, Screen.height, 0);
                        break;
                    case UIPanelAnimType.SlideFromBottom:
                        originalPos = new Vector3(0, -Screen.height, 0);
                        break;
                    case UIPanelAnimType.SlideFromLeft:
                        originalPos = new Vector3(-Screen.width, 0, 0);
                        break;
                    case UIPanelAnimType.SlideFromRight:
                        originalPos = new Vector3(Screen.width, 0, 0);
                        break;
                }
            }

            // 动画时间
            const float duration = 0.3f;

            // 根据动画类型执行不同的动画
            switch (animType)
            {
                case UIPanelAnimType.Fade:
                    float toAlpha = isOpen ? 1 : 0;
                    await Tween.Alpha(canvasGroup, toAlpha, duration);
                    break;

                case UIPanelAnimType.Scale:
                    var toScale = isOpen ? Vector3.one : Vector3.zero;
                    await Tween.Scale(rect, toScale, duration);
                    break;

                case UIPanelAnimType.SlideFromTop:
                case UIPanelAnimType.SlideFromBottom:
                case UIPanelAnimType.SlideFromLeft:
                case UIPanelAnimType.SlideFromRight:
                    var toPos = isOpen ? Vector3.zero : originalPos;
                    await Tween.LocalPosition(rect, toPos, duration);
                    break;
            }

            _isPlayingAnim = false;
        }

        /// <summary>
        /// 初始化遮罩对象池
        /// </summary>
        private void InitMaskPool()
        {
            // 创建遮罩对象池，使用代码创建的遮罩对象
            // 遮罩可能同时需要多个（多层弹窗场景），所以预创建2个，最大10个
            var success = CreatePoolWithConfig(
                poolName: nameof(UIMaskPanel),
                createFunc: CreateMaskObject,
                defaultCapacity: 2,
                maxSize: 10
            );

            if (!success)
            {
                Debug.LogError("创建遮罩对象池失败");
                return;
            }

            // 预热遮罩对象池，预先创建2个遮罩对象
            _poolManager.Prewarm<GameObject>(nameof(UIMaskPanel), 2);

#if UNITY_EDITOR
            Debug.Log("遮罩对象池初始化完成，已预热2个遮罩对象");
#endif
        }

        /// <summary>
        /// 创建遮罩对象
        /// </summary>
        private GameObject CreateMaskObject()
        {
            // 创建遮罩GameObject
            var maskObj = new GameObject("UIMaskPanel");

            // 添加RectTransform组件
            var rectTransform = maskObj.AddComponent<RectTransform>();

            // 设置为UI Canvas的子对象（临时设置，后续会重新设置父对象）
            rectTransform.SetParent(transform, false);

            // 设置RectTransform属性，铺满整个父容器
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            // 添加Image组件作为遮罩背景
            var image = maskObj.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f); // 半透明黑色遮罩
            image.raycastTarget = true;                // 允许接收射线检测

            // 添加Button组件用于点击事件
            var button = maskObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None; // 不需要按钮过渡效果

            // 添加UIMaskPanel脚本组件
            maskObj.AddComponent<UIMaskPanel>();

            // 初始状态设为非活跃
            maskObj.SetActive(false);

            return maskObj;
        }

        /// <summary>
        /// 创建面板背景遮罩
        /// </summary>
        private void CreatePanelMask(UIPanelBase panel, bool closeByOutside)
        {
            if (!panel) return;

            // 从遮罩对象池获取遮罩
            var maskObj = _poolManager.Get<GameObject>(nameof(UIMaskPanel));
            if (!maskObj)
            {
                Debug.LogError("无法从对象池获取遮罩对象，请确保已初始化遮罩预制体");
                return;
            }

            // 获取UIMaskPanel组件
            var maskPanel = maskObj.GetComponent<UIMaskPanel>();
            if (!maskPanel)
            {
                Debug.LogError("遮罩对象缺少UIMaskPanel组件");
                return;
            }

            maskObj.name = "Mask_" + panel.UniqueId;

            // 设置父对象为面板所在层的父对象
            maskObj.transform.SetParent(panel.transform.parent, false);
            var panelIndex = panel.transform.GetSiblingIndex();
            if (panelIndex > maskObj.transform.GetSiblingIndex())
            {
                maskObj.transform.SetSiblingIndex(panelIndex - 1);
            }
            else
            {
                maskObj.transform.SetSiblingIndex(panelIndex);
            }

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
                // 使用一次性事件监听，避免重复触发
                btn.onClick.AddListener(() =>
                {
                    if (!_hidingPanels.Contains(panel.UniqueId))
                    {
                        HidePanel(panel).Forget();
                    }
                });
            }

            // 显示遮罩
            maskPanel.Show();
        }

        /// <summary>
        /// 移除面板背景遮罩（回收到统一对象池）
        /// </summary>
        private void RemovePanelMask(UIPanelBase panel)
        {
            if (!panel) return;

            var maskName = "Mask_" + panel.UniqueId;
            var parent = panel.transform.parent;

            if (!parent) return;

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                if (child.name != maskName) continue;

                var maskPanel = child.GetComponent<UIMaskPanel>();
                if (maskPanel)
                {
                    // 直接隐藏并清理资源，不触发面板隐藏流程
                    maskPanel.Hide();
                }

                // 回收到遮罩对象池
                RecycleToUIPool(child.gameObject, nameof(UIMaskPanel));
                break;
            }
        }
        #endregion

        #region UI对象池管理（使用PoolManager）
        /// <summary>
        /// 通用对象池创建辅助方法
        /// 封装对象池创建的通用逻辑，减少代码重复
        /// </summary>
        /// <param name="poolName">对象池名称</param>
        /// <param name="createFunc">对象创建函数</param>
        /// <param name="defaultCapacity">默认容量</param>
        /// <param name="maxSize">最大容量</param>
        /// <returns>是否成功创建对象池</returns>
        private bool CreatePoolWithConfig(string poolName, Func<GameObject> createFunc,
            int defaultCapacity, int maxSize)
        {
            return _poolManager.CreateOrGetPool<GameObject>(
                poolName: poolName,
                createFunc: createFunc,
                onGet: OnGetFromUIPool,
                onRelease: OnReleaseToUIPool,
                onDestroy: OnDestroyUIPooledObject,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        /// <summary>
        /// 根据UI层级类型获取优化的对象池配置
        /// </summary>
        /// <param name="layerType">UI层级类型</param>
        /// <returns>返回(defaultCapacity, maxSize)元组</returns>
        private (int defaultCapacity, int maxSize) GetOptimizedPoolConfig(UILayerType layerType)
        {
            return layerType switch
            {
                // 常驻面板：通常只需要1个实例
                UILayerType.Bottom => (1, 2),

                // 普通面板：可能需要少量实例
                UILayerType.Panel => (1, 5),

                // 对话框：可能需要多个同时显示
                UILayerType.Popup => (2, 10),

                // 飞行提示：可能需要多个同时显示
                UILayerType.FlyTip => (3, 15),

                // 加载界面：通常只需要1个
                UILayerType.Loading => (1, 2),

                // 网络错误提示：通常只需要1个，但可能需要少量备用
                UILayerType.NetError => (1, 3),

                // 默认配置
                _ => (1, 5)
            };
        }

        /// <summary>
        /// 获取或创建UI对象池
        /// </summary>
        private bool GetOrCreateUIPool<T>(GameObject prefab) where T : UIPanelBase
        {
            // 获取面板配置以确定UI层级
            var panelConfig = GetPanelConfig<T>();
            var (defaultCapacity, maxSize) = GetOptimizedPoolConfig(panelConfig.layer);

            // 使用通用辅助方法创建对象池
            return CreatePoolWithConfig(
                poolName: typeof(T).Name,
                createFunc: () => CreateUIPooledObject(prefab),
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        /// <summary>
        /// 创建UI池化对象
        /// </summary>
        private GameObject CreateUIPooledObject(GameObject prefab)
        {
            var obj = Instantiate(prefab, transform, worldPositionStays: false);
            obj.SetActive(false);
            return obj;
        }

        /// <summary>
        /// 从UI对象池获取对象时的回调
        /// </summary>
        private void OnGetFromUIPool(GameObject obj)
        {
            if (!obj) return;
            obj.SetActive(true);
        }

        /// <summary>
        /// 释放对象到UI对象池时的回调
        /// </summary>
        private void OnReleaseToUIPool(GameObject obj)
        {
            if (!obj) return;
            obj.SetActive(false);
        }

        /// <summary>
        /// 销毁UI池化对象时的回调
        /// </summary>
        private void OnDestroyUIPooledObject(GameObject obj)
        {
            if (!obj) return;
            Destroy(obj);
        }

        /// <summary>
        /// 从UI对象池获取对象
        /// </summary>
        private GameObject GetFromUIPool<T>() where T : UIPanelBase
        {
            var panelName = typeof(T).Name;
            return _poolManager.Get<GameObject>(panelName);
        }

        /// <summary>
        /// 回收对象到UI对象池
        /// </summary>
        private void RecycleToUIPool(GameObject obj, string panelName)
        {
            if (!obj) return;
            _poolManager.Release<GameObject>(obj, panelName);
        }

        /// <summary>
        /// 清空UI对象池
        /// </summary>
        public void ClearUIPool(string panelName = null)
        {
            _poolManager.Clear(panelName);
        }

        /// <summary>
        /// 手动预热指定面板的对象池
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="count">预热数量</param>
        public void PrewarmUIPool<T>(int count) where T : UIPanelBase
        {
            var panelName = typeof(T).Name;
            _poolManager.Prewarm<GameObject>(panelName, count);

#if UNITY_EDITOR
            Debug.Log($"已预热面板 {panelName} 对象池，数量: {count}");
#endif
        }

        /// <summary>
        /// 从正在显示的面板字典中移除面板
        /// </summary>
        internal bool RemoveFromOpenedPanels(UIPanelBase panel)
        {
            return panel && _openedPanelDict.Remove(panel.UniqueId);
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
        /// 获取正在显示的面板数量
        /// </summary>
        public int GetOpenedPanelCount()
        {
            return _openedPanelDict.Count;
        }
        #endregion

        #region 其他功能
        /// <summary>
        /// 获取已打开的面板
        /// </summary>
        public T GetPanel<T>() where T : UIPanelBase
        {
            var panelName = typeof(T).Name;

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
        /// 获取面板配置信息
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <returns>面板配置信息，如果未注册则返回null</returns>
        public UIPanelInfo? GetPanelInfo<T>() where T : UIPanelBase
        {
            var panelName = typeof(T).Name;
            return _panelConfigs.TryGetValue(panelName, out var config) ? config : null;
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
            var panelName = typeof(T).Name;
            var removed = _panelConfigs.Remove(panelName);

            if (removed)
            {
                Debug.Log($"面板 {panelName} 配置已移除");

                // 同时清理对应的UI对象池
                ClearUIPool(panelName);
            }

            return removed;
        }

        private void OnDestroy()
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
            _hidingPanels.Clear();
            _panelConfigs.Clear();
            _isGoingBack = false;
        }
        #endregion
    }
}
