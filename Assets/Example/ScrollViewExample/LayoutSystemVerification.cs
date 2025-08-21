using UnityEngine;
using SimpleToolkits;

/// <summary>
/// 验证布局系统重构成功
/// </summary>
public class LayoutSystemVerification : MonoBehaviour
{
    [ContextMenu("验证布局系统")]
    public void VerifyLayoutSystem()
    {
        Debug.Log("=== 开始验证布局系统重构 ===");
        
        // 验证ScrollLayout抽象基类
        VerifyScrollLayoutBase();
        
        // 验证具体布局类
        VerifyConcreteLayouts();
        
        // 验证接口实现
        VerifyInterfaceImplementation();
        
        // 验证继承关系
        VerifyInheritance();
        
        Debug.Log("=== 布局系统重构验证完成 ===");
    }
    
    private void VerifyScrollLayoutBase()
    {
        Debug.Log("验证ScrollLayout抽象基类...");
        
        // 创建测试对象
        var go = new GameObject("TestLayout");
        
        // 测试VerticalLayout
        var verticalLayout = go.AddComponent<VerticalLayout>();
        Debug.Log($"VerticalLayout继承ScrollLayout: {verticalLayout is ScrollLayout}");
        Debug.Log($"VerticalLayout实现IScrollLayout: {verticalLayout is IScrollLayout}");
        
        // 测试基类字段
        Debug.Log($"VerticalLayout.padding: {verticalLayout.padding}");
        Debug.Log($"VerticalLayout.controlChildWidth: {verticalLayout.controlChildWidth}");
        Debug.Log($"VerticalLayout.controlChildHeight: {verticalLayout.controlChildHeight}");
        Debug.Log($"VerticalLayout.reverse: {verticalLayout.reverse}");
        
        // 清理
        DestroyImmediate(verticalLayout);
        
        // 测试HorizontalLayout
        var horizontalLayout = go.AddComponent<HorizontalLayout>();
        Debug.Log($"HorizontalLayout继承ScrollLayout: {horizontalLayout is ScrollLayout}");
        Debug.Log($"HorizontalLayout实现IScrollLayout: {horizontalLayout is IScrollLayout}");
        
        // 清理
        DestroyImmediate(horizontalLayout);
        
        // 测试GridLayout
        var gridLayout = go.AddComponent<GridLayout>();
        Debug.Log($"GridLayout继承ScrollLayout: {gridLayout is ScrollLayout}");
        Debug.Log($"GridLayout实现IScrollLayout: {gridLayout is IScrollLayout}");
        
        // 清理
        DestroyImmediate(gridLayout);
        DestroyImmediate(go);
        
        Debug.Log("ScrollLayout抽象基类验证完成");
    }
    
    private void VerifyConcreteLayouts()
    {
        Debug.Log("验证具体布局类...");
        
        var go = new GameObject("TestLayout");
        
        // 测试VerticalLayout
        var verticalLayout = go.AddComponent<VerticalLayout>();
        Debug.Log($"VerticalLayout.IsVertical: {verticalLayout.IsVertical}");
        Debug.Log($"VerticalLayout.ConstraintCount: {verticalLayout.ConstraintCount}");
        Debug.Log($"VerticalLayout.Spacing: {verticalLayout.Spacing}");
        Debug.Log($"VerticalLayout.ControlChildWidth: {verticalLayout.ControlChildWidth}");
        Debug.Log($"VerticalLayout.ControlChildHeight: {verticalLayout.ControlChildHeight}");
        Debug.Log($"VerticalLayout.Reverse: {verticalLayout.Reverse}");
        
        DestroyImmediate(verticalLayout);
        
        // 测试HorizontalLayout
        var horizontalLayout = go.AddComponent<HorizontalLayout>();
        Debug.Log($"HorizontalLayout.IsVertical: {horizontalLayout.IsVertical}");
        Debug.Log($"HorizontalLayout.ConstraintCount: {horizontalLayout.ConstraintCount}");
        Debug.Log($"HorizontalLayout.Spacing: {horizontalLayout.Spacing}");
        Debug.Log($"HorizontalLayout.ControlChildWidth: {horizontalLayout.ControlChildWidth}");
        Debug.Log($"HorizontalLayout.ControlChildHeight: {horizontalLayout.ControlChildHeight}");
        Debug.Log($"HorizontalLayout.Reverse: {horizontalLayout.Reverse}");
        
        DestroyImmediate(horizontalLayout);
        
        // 测试GridLayout
        var gridLayout = go.AddComponent<GridLayout>();
        Debug.Log($"GridLayout.IsVertical: {gridLayout.IsVertical}");
        Debug.Log($"GridLayout.ConstraintCount: {gridLayout.ConstraintCount}");
        Debug.Log($"GridLayout.Spacing: {gridLayout.Spacing}");
        Debug.Log($"GridLayout.ControlChildWidth: {gridLayout.ControlChildWidth}");
        Debug.Log($"GridLayout.ControlChildHeight: {gridLayout.ControlChildHeight}");
        Debug.Log($"GridLayout.Reverse: {gridLayout.Reverse}");
        
        DestroyImmediate(gridLayout);
        DestroyImmediate(go);
        
        Debug.Log("具体布局类验证完成");
    }
    
    private void VerifyInterfaceImplementation()
    {
        Debug.Log("验证接口实现...");
        
        var go = new GameObject("TestLayout");
        
        // 测试VerticalLayout接口实现
        var verticalLayout = go.AddComponent<VerticalLayout>();
        var scrollLayout = verticalLayout as IScrollLayout;
        
        if (scrollLayout != null)
        {
            Debug.Log("VerticalLayout正确实现IScrollLayout接口");
            Debug.Log($"IsVertical: {scrollLayout.IsVertical}");
            Debug.Log($"ConstraintCount: {scrollLayout.ConstraintCount}");
            Debug.Log($"Spacing: {scrollLayout.Spacing}");
            Debug.Log($"Padding: {scrollLayout.Padding}");
            Debug.Log($"ControlChildWidth: {scrollLayout.ControlChildWidth}");
            Debug.Log($"ControlChildHeight: {scrollLayout.ControlChildHeight}");
            Debug.Log($"Reverse: {scrollLayout.Reverse}");
        }
        else
        {
            Debug.LogError("VerticalLayout未正确实现IScrollLayout接口");
        }
        
        DestroyImmediate(verticalLayout);
        DestroyImmediate(go);
        
        Debug.Log("接口实现验证完成");
    }
    
    private void VerifyInheritance()
    {
        Debug.Log("验证继承关系...");
        
        var go = new GameObject("TestLayout");
        
        // 测试继承层次
        var verticalLayout = go.AddComponent<VerticalLayout>();
        
        // 检查继承链
        bool isScrollLayout = verticalLayout is ScrollLayout;
        bool isComponent = verticalLayout is Component;
        bool isIScrollLayout = verticalLayout is IScrollLayout;
        
        Debug.Log($"VerticalLayout继承链检查:");
        Debug.Log($"  - 继承ScrollLayout: {isScrollLayout}");
        Debug.Log($"  - 继承Component: {isComponent}");
        Debug.Log($"  - 实现IScrollLayout: {isIScrollLayout}");
        
        // 验证所有布局类都有相同的基类
        var horizontalLayout = go.AddComponent<HorizontalLayout>();
        var gridLayout = go.AddComponent<GridLayout>();
        
        bool allInheritFromScrollLayout = 
            verticalLayout is ScrollLayout && 
            horizontalLayout is ScrollLayout && 
            gridLayout is ScrollLayout;
        
        Debug.Log($"所有布局类都继承自ScrollLayout: {allInheritFromScrollLayout}");
        
        DestroyImmediate(verticalLayout);
        DestroyImmediate(horizontalLayout);
        DestroyImmediate(gridLayout);
        DestroyImmediate(go);
        
        Debug.Log("继承关系验证完成");
    }
}