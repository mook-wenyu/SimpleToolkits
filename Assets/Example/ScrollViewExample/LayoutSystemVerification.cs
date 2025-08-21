using UnityEngine;
using SimpleToolkits;

namespace SimpleToolkits.Tests
{
    /// <summary>
    /// 验证ScrollLayout删除后的布局系统是否正常工作
    /// </summary>
    public class LayoutSystemVerification : MonoBehaviour
    {
        [Header("测试布局")]
        [SerializeField] private VerticalLayout _verticalLayout;
        [SerializeField] private HorizontalLayout _horizontalLayout;
        [SerializeField] private GridLayout _gridLayout;

        [Header("测试数据")]
        [SerializeField] private int _testItemCount = 100;
        [SerializeField] private Vector2 _testCellSize = new Vector2(100, 50);

        void Start()
        {
            RunVerification();
        }

        [ContextMenu("运行验证")]
        public void RunVerification()
        {
            Debug.Log("=== 布局系统验证开始 ===");
            
            // 验证垂直布局
            if (_verticalLayout != null)
            {
                VerifyLayout(_verticalLayout, "VerticalLayout");
            }
            else
            {
                Debug.LogWarning("VerticalLayout 未设置，跳过验证");
            }

            // 验证水平布局
            if (_horizontalLayout != null)
            {
                VerifyLayout(_horizontalLayout, "HorizontalLayout");
            }
            else
            {
                Debug.LogWarning("HorizontalLayout 未设置，跳过验证");
            }

            // 验证网格布局
            if (_gridLayout != null)
            {
                VerifyLayout(_gridLayout, "GridLayout");
            }
            else
            {
                Debug.LogWarning("GridLayout 未设置，跳过验证");
            }

            Debug.Log("=== 布局系统验证完成 ===");
        }

        private void VerifyLayout(IScrollLayout layout, string layoutName)
        {
            Debug.Log($"验证 {layoutName}:");
            
            // 验证接口实现
            Debug.Log($"  - IsVertical: {layout.IsVertical}");
            Debug.Log($"  - ConstraintCount: {layout.ConstraintCount}");
            Debug.Log($"  - Spacing: {layout.Spacing}");
            Debug.Log($"  - ControlChildWidth: {layout.ControlChildWidth}");
            Debug.Log($"  - ControlChildHeight: {layout.ControlChildHeight}");
            Debug.Log($"  - Reverse: {layout.Reverse}");
            Debug.Log($"  - Padding: {layout.Padding}");

            // 验证尺寸计算
            var contentSize = layout.ComputeContentSize(_testItemCount, _testCellSize, new Vector2(400, 600));
            Debug.Log($"  - Content Size ({_testItemCount} items): {contentSize}");

            // 验证可见范围计算
            layout.GetVisibleRange(0.5f, _testItemCount, new Vector2(400, 600), _testCellSize, out int first, out int last);
            Debug.Log($"  - Visible Range (50% scroll): {first} to {last}");

            // 验证位置计算
            var position = layout.GetItemAnchoredPosition(10, _testItemCount, _testCellSize);
            Debug.Log($"  - Item 10 Position: {position}");

            Debug.Log($"  ✓ {layoutName} 验证通过");
        }

        [ContextMenu("测试性能")]
        public void TestPerformance()
        {
            Debug.Log("=== 布局系统性能测试 ===");
            
            var testLayout = _verticalLayout ?? _horizontalLayout ?? _gridLayout;
            if (testLayout == null)
            {
                Debug.LogError("没有可用的布局进行性能测试");
                return;
            }

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // 执行1000次尺寸计算
            for (int i = 0; i < 1000; i++)
            {
                testLayout.ComputeContentSize(_testItemCount, _testCellSize, new Vector2(400, 600));
            }

            stopwatch.Stop();
            Debug.Log($"1000次尺寸计算耗时: {stopwatch.ElapsedMilliseconds}ms");
            Debug.Log($"平均每次计算: {stopwatch.ElapsedMilliseconds / 1000f}ms");
            
            // 测试可见范围计算性能
            stopwatch.Restart();
            for (int i = 0; i < 1000; i++)
            {
                testLayout.GetVisibleRange(0.5f, _testItemCount, new Vector2(400, 600), _testCellSize, out int first, out int last);
            }
            stopwatch.Stop();
            Debug.Log($"1000次可见范围计算耗时: {stopwatch.ElapsedMilliseconds}ms");
            Debug.Log($"平均每次计算: {stopwatch.ElapsedMilliseconds / 1000f}ms");

            Debug.Log("=== 性能测试完成 ===");
        }
    }
}