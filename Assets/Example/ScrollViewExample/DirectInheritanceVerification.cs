using UnityEngine;
using SimpleToolkits;

/// <summary>
/// 验证布局系统直接继承Component和IScrollLayout接口
/// </summary>
public class DirectInheritanceVerification : MonoBehaviour
{
    [ContextMenu("验证直接继承")]
    public void VerifyDirectInheritance()
    {
        Debug.Log("=== 开始验证布局系统直接继承 ===");
        
        // 验证VerticalLayout
        VerifyVerticalLayout();
        
        // 验证HorizontalLayout
        VerifyHorizontalLayout();
        
        // 验证GridLayout
        VerifyGridLayout();
        
        // 验证接口实现
        VerifyInterfaceImplementation();
        
        Debug.Log("=== 布局系统直接继承验证完成 ===");
    }
    
    private void VerifyVerticalLayout()
    {
        Debug.Log("验证VerticalLayout直接继承...");
        
        var go = new GameObject("TestVerticalLayout");
        var verticalLayout = go.AddComponent<VerticalLayout>();
        
        // 验证继承关系
        bool isComponent = verticalLayout is Component;
        bool isIScrollLayout = verticalLayout is IScrollLayout;
        bool isNotScrollLayout = !(verticalLayout is ScrollLayout); // 应该为false，因为ScrollLayout已被删除
        
        Debug.Log($"VerticalLayout继承Component: {isComponent}");
        Debug.Log($"VerticalLayout实现IScrollLayout: {isIScrollLayout}");
        Debug.Log($"VerticalLayout不是ScrollLayout: {isNotScrollLayout}");
        
        // 验证接口属性
        var scrollLayout = verticalLayout as IScrollLayout;
        if (scrollLayout != null)
        {
            Debug.Log($"IScrollLayout.IsVertical: {scrollLayout.IsVertical}");
            Debug.Log($"IScrollLayout.ConstraintCount: {scrollLayout.ConstraintCount}");
            Debug.Log($"IScrollLayout.Spacing: {scrollLayout.Spacing}");
            Debug.Log($"IScrollLayout.ControlChildWidth: {scrollLayout.ControlChildWidth}");
            Debug.Log($"IScrollLayout.ControlChildHeight: {scrollLayout.ControlChildHeight}");
            Debug.Log($"IScrollLayout.Reverse: {scrollLayout.Reverse}");
            Debug.Log($"IScrollLayout.Padding: {scrollLayout.Padding}");
        }
        
        DestroyImmediate(verticalLayout);
        DestroyImmediate(go);
        
        Debug.Log("VerticalLayout验证完成");
    }
    
    private void VerifyHorizontalLayout()
    {
        Debug.Log("验证HorizontalLayout直接继承...");
        
        var go = new GameObject("TestHorizontalLayout");
        var horizontalLayout = go.AddComponent<HorizontalLayout>();
        
        // 验证继承关系
        bool isComponent = horizontalLayout is Component;
        bool isIScrollLayout = horizontalLayout is IScrollLayout;
        
        Debug.Log($"HorizontalLayout继承Component: {isComponent}");
        Debug.Log($"HorizontalLayout实现IScrollLayout: {isIScrollLayout}");
        
        // 验证特有属性
        Debug.Log($"HorizontalLayout.spacing: {horizontalLayout.spacing}");
        Debug.Log($"HorizontalLayout.controlChildWidth: {horizontalLayout.controlChildWidth}");
        Debug.Log($"HorizontalLayout.controlChildHeight: {horizontalLayout.controlChildHeight}");
        
        DestroyImmediate(horizontalLayout);
        DestroyImmediate(go);
        
        Debug.Log("HorizontalLayout验证完成");
    }
    
    private void VerifyGridLayout()
    {
        Debug.Log("验证GridLayout直接继承...");
        
        var go = new GameObject("TestGridLayout");
        var gridLayout = go.AddComponent<GridLayout>();
        
        // 验证继承关系
        bool isComponent = gridLayout is Component;
        bool isIScrollLayout = gridLayout is IScrollLayout;
        
        Debug.Log($"GridLayout继承Component: {isComponent}");
        Debug.Log($"GridLayout实现IScrollLayout: {isIScrollLayout}");
        
        // 验证特有属性
        Debug.Log($"GridLayout.isVertical: {gridLayout.isVertical}");
        Debug.Log($"GridLayout.constraintCount: {gridLayout.constraintCount}");
        Debug.Log($"GridLayout.spacingX: {gridLayout.spacingX}");
        Debug.Log($"GridLayout.spacingY: {gridLayout.spacingY}");
        
        DestroyImmediate(gridLayout);
        DestroyImmediate(go);
        
        Debug.Log("GridLayout验证完成");
    }
    
    private void VerifyInterfaceImplementation()
    {
        Debug.Log("验证接口实现...");
        
        var go = new GameObject("TestInterface");
        
        // 测试所有布局类都正确实现IScrollLayout接口
        var verticalLayout = go.AddComponent<VerticalLayout>();
        var horizontalLayout = go.AddComponent<HorizontalLayout>();
        var gridLayout = go.AddComponent<GridLayout>();
        
        var layouts = new IScrollLayout[] { verticalLayout, horizontalLayout, gridLayout };
        
        foreach (var layout in layouts)
        {
            Debug.Log($"布局类型 {layout.GetType().Name}:");
            Debug.Log($"  IsVertical: {layout.IsVertical}");
            Debug.Log($"  ConstraintCount: {layout.ConstraintCount}");
            Debug.Log($"  Spacing: {layout.Spacing}");
            Debug.Log($"  ControlChildWidth: {layout.ControlChildWidth}");
            Debug.Log($"  ControlChildHeight: {layout.ControlChildHeight}");
            Debug.Log($"  Reverse: {layout.Reverse}");
            Debug.Log($"  Padding: {layout.Padding}");
        }
        
        DestroyImmediate(verticalLayout);
        DestroyImmediate(horizontalLayout);
        DestroyImmediate(gridLayout);
        DestroyImmediate(go);
        
        Debug.Log("接口实现验证完成");
    }
    
    [ContextMenu("测试布局功能")]
    public void TestLayoutFunctionality()
    {
        Debug.Log("=== 测试布局功能 ===");
        
        var go = new GameObject("TestFunctionality");
        
        // 测试VerticalLayout功能
        var verticalLayout = go.AddComponent<VerticalLayout>();
        var cellSize = new Vector2(100f, 50f);
        var viewportSize = new Vector2(300f, 200f);
        
        var contentSize = verticalLayout.ComputeContentSize(5, cellSize, viewportSize);
        Debug.Log($"VerticalLayout内容尺寸(5个项目): {contentSize}");
        
        // 测试可见范围计算
        verticalLayout.GetVisibleRange(0.5f, 5, viewportSize, cellSize, out int first, out int last);
        Debug.Log($"VerticalLayout可见范围(0.5f位置): {first}-{last}");
        
        // 测试位置计算
        var position = verticalLayout.GetItemAnchoredPosition(2, 5, cellSize);
        Debug.Log($"VerticalLayout第3个项目位置: {position}");
        
        DestroyImmediate(verticalLayout);
        DestroyImmediate(go);
        
        Debug.Log("=== 布局功能测试完成 ===");
    }
}