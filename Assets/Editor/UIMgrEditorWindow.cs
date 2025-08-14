using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI管理器编辑器窗口
/// 用于在Unity编辑器中监控和管理UI面板状态
/// </summary>
public class UIMgrEditorWindow : EditorWindow
{
    /// <summary>
    /// 在Tools菜单中添加打开窗口的选项
    /// </summary>
    [MenuItem("Simple Toolkit/UI Manager Inspector")]
    public static void ShowWindow()
    {
        var window = GetWindow<UIMgrEditorWindow>("UI Manager Inspector");
        window.minSize = new Vector2(640, 400);
        window.Show();
    }

    private Vector2 _scrollPosition;
    private bool _autoRefresh = true;
    private double _lastRefreshTime;
    private const double Refresh_Interval = 0.5; // 自动刷新间隔（秒）

    // GUI样式
    private GUIStyle _headerStyle;
    private GUIStyle _panelBoxStyle;
    private GUIStyle _statusLabelStyle;
    private bool _stylesInitialized;

    private void OnEnable()
    {
        _lastRefreshTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > Refresh_Interval)
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    private void OnGUI()
    {
        InitializeStyles();

        EditorGUILayout.BeginVertical();

        DrawHeader();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        if (Application.isPlaying)
        {
            DrawPlayModeContent();
        }

        EditorGUILayout.EndScrollView();

        DrawFooter();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        _panelBoxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, 2, 2)
        };

        _statusLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            richText = true
        };

        _stylesInitialized = true;
    }

    /// <summary>
    /// 绘制窗口头部
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("UI Manager Inspector", _headerStyle);

        GUILayout.FlexibleSpace();

        // 自动刷新开关
        _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh, GUILayout.Width(100));

        // 手动刷新按钮
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    /// <summary>
    /// 绘制Play模式下的内容
    /// </summary>
    private void DrawPlayModeContent()
    {
        var uiMgr = UIMgr.Instance;

        if (!uiMgr)
        {
            EditorGUILayout.HelpBox("UIMgr instance not found. Make sure UIMgr is initialized in the scene.", MessageType.Warning);
            return;
        }
        
        // 控制按钮
        DrawControlButtons(uiMgr);
        
        EditorGUILayout.Space();

        // 统计信息
        DrawStatistics(uiMgr);

        EditorGUILayout.Space();

        // 已注册面板配置
        DrawRegisteredPanels(uiMgr);

        EditorGUILayout.Space();

        // 当前打开的面板
        DrawOpenedPanels(uiMgr);

        EditorGUILayout.Space();

        // UI层级信息
        DrawLayerInfo(uiMgr);
    }
    
    /// <summary>
    /// 绘制控制按钮
    /// </summary>
    private void DrawControlButtons(UIMgr uiMgr)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Go Back", GUILayout.Width(100)))
        {
            uiMgr.GoBack().Forget();
        }

        foreach (var panel in uiMgr.GetAllPanelConfigs())
        {
            if (GUILayout.Button(panel.Key, GUILayout.Width(100)))
            {
                OpenPanelByName(uiMgr, panel.Key);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 通过面板名称动态打开UI面板
    /// </summary>
    /// <param name="uiMgr">UI管理器实例</param>
    /// <param name="panelName">面板名称</param>
    private void OpenPanelByName(UIMgr uiMgr, string panelName)
    {
        try
        {
            // 通过反射查找面板类型
            Type panelType = FindPanelType(panelName);
            if (panelType == null)
            {
                Debug.LogError($"找不到面板类型: {panelName}");
                EditorUtility.DisplayDialog("错误", $"找不到面板类型: {panelName}", "确定");
                return;
            }

            // 验证类型是否继承自UIPanelBase
            if (!typeof(UIPanelBase).IsAssignableFrom(panelType))
            {
                Debug.LogError($"面板类型 {panelName} 不继承自 UIPanelBase");
                EditorUtility.DisplayDialog("错误", $"面板类型 {panelName} 不继承自 UIPanelBase", "确定");
                return;
            }

            // 获取UIMgr的OpenPanel<T>方法
            MethodInfo openPanelMethod = typeof(UIMgr).GetMethod("OpenPanel", new Type[] { typeof(object) });
            if (openPanelMethod == null)
            {
                Debug.LogError("找不到 OpenPanel 方法");
                return;
            }

            // 创建泛型方法
            MethodInfo genericOpenPanelMethod = openPanelMethod.MakeGenericMethod(panelType);

            // 调用方法
            object result = genericOpenPanelMethod.Invoke(uiMgr, new object[] { null });

            // 如果返回的是UniTask，调用Forget()
            if (result != null && result.GetType().Name.Contains("UniTask"))
            {
                MethodInfo forgetMethod = result.GetType().GetMethod("Forget");
                forgetMethod?.Invoke(result, null);
            }

            Debug.Log($"成功打开面板: {panelName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"打开面板 {panelName} 时发生错误: {ex.Message}");
            EditorUtility.DisplayDialog("错误", $"打开面板 {panelName} 时发生错误:\n{ex.Message}", "确定");
        }
    }

    /// <summary>
    /// 通过面板名称查找对应的类型
    /// </summary>
    /// <param name="panelName">面板名称</param>
    /// <returns>找到的类型，如果未找到则返回null</returns>
    private Type FindPanelType(string panelName)
    {
        try
        {
            // 首先尝试在当前程序集中查找
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type panelType = currentAssembly.GetType(panelName);
            if (panelType != null && typeof(UIPanelBase).IsAssignableFrom(panelType))
            {
                return panelType;
            }

            // 在所有已加载的程序集中查找
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    // 尝试通过完整名称查找
                    panelType = assembly.GetType(panelName);
                    if (panelType != null && typeof(UIPanelBase).IsAssignableFrom(panelType))
                    {
                        return panelType;
                    }

                    // 尝试通过简单名称查找
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        if (type.Name == panelName && typeof(UIPanelBase).IsAssignableFrom(type))
                        {
                            return type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 忽略无法加载的程序集
                    continue;
                }
                catch (Exception)
                {
                    // 忽略其他异常
                    continue;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"查找面板类型 {panelName} 时发生错误: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 绘制统计信息
    /// </summary>
    private void DrawStatistics(UIMgr uiMgr)
    {
        EditorGUILayout.BeginVertical(_panelBoxStyle);

        int registeredCount = uiMgr.GetAllPanelConfigs().Count;
        int openedCount = uiMgr.GetOpenedPanelCount();

        EditorGUILayout.LabelField($"已注册的面板: {registeredCount}");
        EditorGUILayout.LabelField($"已打开的面板: {openedCount}");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制已注册面板配置
    /// </summary>
    private void DrawRegisteredPanels(UIMgr uiMgr)
    {
        var configs = uiMgr.GetAllPanelConfigs();

        if (configs.Count == 0)
        {
            EditorGUILayout.HelpBox("没有已注册的面板！", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(_panelBoxStyle);

        foreach (var kvp in configs)
        {
            string panelName = kvp.Key;
            var config = kvp.Value;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(panelName, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField($"层级: {config.layer}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"允许多个: {GetBoolDisplayText(config.allowMultiple)}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"遮罩: {GetBoolDisplayText(config.needMask)}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"点击关闭: {GetBoolDisplayText(config.closeByOutside)}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"动画: {config.animType}", GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 安全地获取BoolType的显示文本
    /// </summary>
    private string GetBoolDisplayText(BoolType boolType)
    {
        return boolType.HasValue() ? boolType.ToBool().ToString() : "Default";
    }

    /// <summary>
    /// 绘制当前打开的面板
    /// </summary>
    private void DrawOpenedPanels(UIMgr uiMgr)
    {
        var openedPanels = uiMgr.GetAllOpenedPanels();

        if (openedPanels.Length == 0)
        {
            EditorGUILayout.HelpBox("当前没有打开的面板。", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(_panelBoxStyle);

        foreach (var panel in openedPanels)
        {
            if (!panel) continue;

            EditorGUILayout.BeginHorizontal();

            // 面板名称
            EditorGUILayout.LabelField(panel.PanelName, EditorStyles.boldLabel, GUILayout.Width(150));

            // 状态
            string stateColor = panel.GetState() == UIPanelStateType.Showing ? "green" : "orange";
            EditorGUILayout.LabelField($"<color={stateColor}>{panel.GetState()}</color>", _statusLabelStyle, GUILayout.Width(80));

            // 唯一ID（缩短显示）
            string shortId = panel.UniqueId.Length > 8 ? panel.UniqueId.Substring(0, 8) + "..." : panel.UniqueId;
            EditorGUILayout.LabelField($"ID: {shortId}", GUILayout.Width(100));

            // 激活状态
            string activeColor = panel.gameObject.activeInHierarchy ? "green" : "red";
            EditorGUILayout.LabelField($"<color={activeColor}>激活: {panel.gameObject.activeInHierarchy}</color>", _statusLabelStyle, GUILayout.Width(100));

            // 操作按钮
            if (GUILayout.Button("隐藏", GUILayout.Width(50)))
            {
                uiMgr.HidePanel(panel).Forget();
            }
            
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                Selection.activeGameObject = panel.gameObject;
                EditorGUIUtility.PingObject(panel.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制UI层级信息
    /// </summary>
    private void DrawLayerInfo(UIMgr uiMgr)
    {
        EditorGUILayout.BeginVertical(_panelBoxStyle);

        // 显示各个UI层级的信息
        var layerTypes = System.Enum.GetValues(typeof(UILayerType)).Cast<UILayerType>().Where(l => l != UILayerType.None);

        foreach (var layer in layerTypes)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"层级 {layer}:", EditorStyles.boldLabel, GUILayout.Width(100));

            // 统计该层级的面板数量
            int panelsInLayer = CountPanelsInLayer(uiMgr, layer);

            EditorGUILayout.LabelField($"面板数: {panelsInLayer}", GUILayout.Width(80));

            // 显示层级状态
            string layerColor = panelsInLayer > 0 ? "green" : "gray";
            EditorGUILayout.LabelField($"<color={layerColor}>{(panelsInLayer > 0 ? "有面板" : "空")}</color>", _statusLabelStyle, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 统计指定层级的面板数量
    /// </summary>
    private int CountPanelsInLayer(UIMgr uiMgr, UILayerType layer)
    {
        try
        {
            var configs = uiMgr.GetAllPanelConfigs();
            var openedPanels = uiMgr.GetAllOpenedPanels();

            return openedPanels.Count(panel =>
            {
                if (!panel) return false;

                // 查找面板配置
                if (configs.TryGetValue(panel.PanelName, out var config))
                {
                    return config.layer == layer;
                }

                return false;
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"统计层级 {layer} 面板数量时出错: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 绘制窗口底部
    /// </summary>
    private void DrawFooter()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        var uiMgr = UIMgr.Instance;
        if (!uiMgr) return;

        // 添加一些额外的信息
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"刷新间隔: {Refresh_Interval}秒", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"上次刷新: {System.DateTime.Now:HH:mm:ss}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }
}