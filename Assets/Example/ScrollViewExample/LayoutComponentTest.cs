using UnityEngine;
using SimpleToolkits;

namespace SimpleToolkits.Tests
{
    /// <summary>
    /// 验证布局组件修复是否成功
    /// </summary>
    public class LayoutComponentTest : MonoBehaviour
    {
        [ContextMenu("测试添加布局组件")]
        public void TestAddLayoutComponents()
        {
            Debug.Log("=== 测试添加布局组件 ===");
            
            // 测试添加 VerticalLayout
            try
            {
                var verticalLayout = gameObject.AddComponent<VerticalLayout>();
                Debug.Log("✅ VerticalLayout 添加成功");
                
                // 测试配置
                verticalLayout.spacing = 10f;
                verticalLayout.padding = new RectOffset(5, 5, 5, 5);
                verticalLayout.controlChildWidth = true;
                verticalLayout.controlChildHeight = false;
                verticalLayout.reverse = false;
                
                Debug.Log($"✅ VerticalLayout 配置成功: spacing={verticalLayout.spacing}, padding={verticalLayout.padding}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ VerticalLayout 添加失败: {e.Message}");
            }
            
            // 测试添加 HorizontalLayout
            try
            {
                var horizontalLayout = gameObject.AddComponent<HorizontalLayout>();
                Debug.Log("✅ HorizontalLayout 添加成功");
                
                // 测试配置
                horizontalLayout.spacing = 15f;
                horizontalLayout.padding = new RectOffset(10, 10, 10, 10);
                horizontalLayout.controlChildWidth = false;
                horizontalLayout.controlChildHeight = true;
                horizontalLayout.reverse = false;
                
                Debug.Log($"✅ HorizontalLayout 配置成功: spacing={horizontalLayout.spacing}, padding={horizontalLayout.padding}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ HorizontalLayout 添加失败: {e.Message}");
            }
            
            // 测试添加 GridLayout
            try
            {
                var gridLayout = gameObject.AddComponent<GridLayout>();
                Debug.Log("✅ GridLayout 添加成功");
                
                // 测试配置
                gridLayout.spacingX = 8f;
                gridLayout.spacingY = 8f;
                gridLayout.constraintCount = 3;
                gridLayout.isVertical = true;
                gridLayout.padding = new RectOffset(20, 20, 20, 20);
                
                Debug.Log($"✅ GridLayout 配置成功: spacingX={gridLayout.spacingX}, spacingY={gridLayout.spacingY}, constraintCount={gridLayout.constraintCount}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ GridLayout 添加失败: {e.Message}");
            }
            
            // 清理测试组件
            CleanupTestComponents();
            
            Debug.Log("=== 测试完成 ===");
        }
        
        [ContextMenu("测试接口实现")]
        public void TestInterfaceImplementation()
        {
            Debug.Log("=== 测试接口实现 ===");
            
            // 添加组件并测试接口
            var verticalLayout = gameObject.AddComponent<VerticalLayout>();
            var horizontalLayout = gameObject.AddComponent<HorizontalLayout>();
            var gridLayout = gameObject.AddComponent<GridLayout>();
            
            // 测试 VerticalLayout 接口
            TestIScrollLayoutInterface(verticalLayout, "VerticalLayout");
            
            // 测试 HorizontalLayout 接口
            TestIScrollLayoutInterface(horizontalLayout, "HorizontalLayout");
            
            // 测试 GridLayout 接口
            TestIScrollLayoutInterface(gridLayout, "GridLayout");
            
            // 清理测试组件
            CleanupTestComponents();
            
            Debug.Log("=== 接口测试完成 ===");
        }
        
        private void TestIScrollLayoutInterface(IScrollLayout layout, string layoutName)
        {
            Debug.Log($"测试 {layoutName} 接口实现:");
            
            try
            {
                // 测试基本属性
                Debug.Log($"  - IsVertical: {layout.IsVertical}");
                Debug.Log($"  - ConstraintCount: {layout.ConstraintCount}");
                Debug.Log($"  - Spacing: {layout.Spacing}");
                Debug.Log($"  - ControlChildWidth: {layout.ControlChildWidth}");
                Debug.Log($"  - ControlChildHeight: {layout.ControlChildHeight}");
                Debug.Log($"  - Reverse: {layout.Reverse}");
                Debug.Log($"  - Padding: {layout.Padding}");
                
                // 测试方法
                var viewportSize = new Vector2(400, 600);
                var cellSize = new Vector2(100, 50);
                var itemCount = 10;
                
                var contentSize = layout.ComputeContentSize(itemCount, cellSize, viewportSize);
                Debug.Log($"  - ComputeContentSize: {contentSize}");
                
                layout.GetVisibleRange(0.5f, itemCount, viewportSize, cellSize, out int first, out int last);
                Debug.Log($"  - GetVisibleRange: {first} to {last}");
                
                var position = layout.GetItemAnchoredPosition(5, itemCount, cellSize);
                Debug.Log($"  - GetItemAnchoredPosition: {position}");
                
                Debug.Log($"  ✅ {layoutName} 接口实现正常");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"  ❌ {layoutName} 接口实现失败: {e.Message}");
            }
        }
        
        [ContextMenu("清理测试组件")]
        public void CleanupTestComponents()
        {
            var components = GetComponents<IScrollLayout>();
            foreach (var component in components)
            {
                if (component is MonoBehaviour behaviour)
                {
                    DestroyImmediate(behaviour);
                }
            }
            Debug.Log("清理完成");
        }
    }
}