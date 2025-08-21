using UnityEngine;
using SimpleToolkits;

/// <summary>
/// 测试布局系统重构后的功能
/// </summary>
public class LayoutSystemTest : MonoBehaviour
{
    [Header("测试设置")]
    public RectTransform content;
    public ScrollRect scrollRect;
    
    void Start()
    {
        TestLayoutInheritance();
        TestLayoutFunctionality();
    }
    
    /// <summary>
    /// 测试布局继承关系
    /// </summary>
    private void TestLayoutInheritance()
    {
        Debug.Log("=== 测试布局继承关系 ===");
        
        // 测试 VerticalLayout
        var verticalLayout = content.gameObject.AddComponent<VerticalLayout>();
        Debug.Log($"VerticalLayout 继承测试: {verticalLayout is ScrollLayout}");
        Debug.Log($"VerticalLayout 是否为 IScrollLayout: {verticalLayout is IScrollLayout}");
        Debug.Log($"VerticalLayout.IsVertical: {verticalLayout.IsVertical}");
        Debug.Log($"VerticalLayout.ConstraintCount: {verticalLayout.ConstraintCount}");
        
        // 清理
        DestroyImmediate(verticalLayout);
        
        // 测试 HorizontalLayout
        var horizontalLayout = content.gameObject.AddComponent<HorizontalLayout>();
        Debug.Log($"HorizontalLayout 继承测试: {horizontalLayout is ScrollLayout}");
        Debug.Log($"HorizontalLayout 是否为 IScrollLayout: {horizontalLayout is IScrollLayout}");
        Debug.Log($"HorizontalLayout.IsVertical: {horizontalLayout.IsVertical}");
        Debug.Log($"HorizontalLayout.ConstraintCount: {horizontalLayout.ConstraintCount}");
        
        // 清理
        DestroyImmediate(horizontalLayout);
        
        // 测试 GridLayout
        var gridLayout = content.gameObject.AddComponent<GridLayout>();
        Debug.Log($"GridLayout 继承测试: {gridLayout is ScrollLayout}");
        Debug.Log($"GridLayout 是否为 IScrollLayout: {gridLayout is IScrollLayout}");
        Debug.Log($"GridLayout.IsVertical: {gridLayout.IsVertical}");
        Debug.Log($"GridLayout.ConstraintCount: {gridLayout.ConstraintCount}");
        
        // 清理
        DestroyImmediate(gridLayout);
        
        Debug.Log("=== 布局继承关系测试完成 ===\n");
    }
    
    /// <summary>
    /// 测试布局功能
    /// </summary>
    private void TestLayoutFunctionality()
    {
        Debug.Log("=== 测试布局功能 ===");
        
        // 测试 VerticalLayout 功能
        var verticalLayout = content.gameObject.AddComponent<VerticalLayout>();
        verticalLayout.spacing = 10f;
        verticalLayout.padding = new RectOffset(5, 5, 5, 5);
        verticalLayout.controlChildWidth = true;
        verticalLayout.controlChildHeight = false;
        verticalLayout.reverse = false;
        
        var cellSize = new Vector2(100f, 50f);
        var viewportSize = new Vector2(300f, 200f);
        var contentSize = verticalLayout.ComputeContentSize(5, cellSize, viewportSize);
        
        Debug.Log($"VerticalLayout 内容尺寸 (5个项目): {contentSize}");
        Debug.Log($"VerticalLayout Spacing: {verticalLayout.Spacing}");
        Debug.Log($"VerticalLayout ControlChildWidth: {verticalLayout.ControlChildWidth}");
        Debug.Log($"VerticalLayout Reverse: {verticalLayout.Reverse}");
        
        // 清理
        DestroyImmediate(verticalLayout);
        
        // 测试 HorizontalLayout 功能
        var horizontalLayout = content.gameObject.AddComponent<HorizontalLayout>();
        horizontalLayout.spacing = 15f;
        horizontalLayout.padding = new RectOffset(10, 10, 10, 10);
        horizontalLayout.controlChildWidth = false;
        horizontalLayout.controlChildHeight = true;
        horizontalLayout.reverse = false;
        
        contentSize = horizontalLayout.ComputeContentSize(3, cellSize, viewportSize);
        Debug.Log($"HorizontalLayout 内容尺寸 (3个项目): {contentSize}");
        Debug.Log($"HorizontalLayout Spacing: {horizontalLayout.Spacing}");
        
        // 清理
        DestroyImmediate(horizontalLayout);
        
        // 测试 GridLayout 功能
        var gridLayout = content.gameObject.AddComponent<GridLayout>();
        gridLayout.spacingX = 8f;
        gridLayout.spacingY = 12f;
        gridLayout.constraintCount = 3;
        gridLayout.isVertical = true;
        
        contentSize = gridLayout.ComputeContentSize(6, cellSize, viewportSize);
        Debug.Log($"GridLayout 内容尺寸 (6个项目, 3列): {contentSize}");
        Debug.Log($"GridLayout Spacing: {gridLayout.Spacing}");
        Debug.Log($"GridLayout ConstraintCount: {gridLayout.ConstraintCount}");
        
        // 清理
        DestroyImmediate(gridLayout);
        
        Debug.Log("=== 布局功能测试完成 ===\n");
    }
    
    /// <summary>
    /// 在编辑器中运行的测试菜单
    /// </summary>
    [UnityEditor.CustomEditor(typeof(LayoutSystemTest))]
    public class LayoutSystemTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("运行布局系统测试"))
            {
                var test = (LayoutSystemTest)target;
                test.Start();
            }
        }
    }
}