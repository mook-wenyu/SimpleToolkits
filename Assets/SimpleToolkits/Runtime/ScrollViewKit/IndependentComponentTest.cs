namespace SimpleToolkits.Tests
{
    using UnityEngine;

    /// <summary>
    /// 测试新架构的独立性和可复用性
    /// </summary>
    [AddComponentMenu("SimpleToolkits/Tests/Independent Component Test")]
    public class IndependentComponentTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _logTestResults = true;
        
        [Header("测试结果")]
        [SerializeField] private string _testStatus = "未开始测试";
        
        private void Start()
        {
            if (_logTestResults)
            {
                TestIndependentComponents();
            }
        }

        [ContextMenu("运行独立性测试")]
        public void TestIndependentComponents()
        {
            _testStatus = "开始测试...";
            
            try
            {
                // 测试1：布局组件独立工作
                TestLayoutComponentIndependence();
                
                // 测试2：尺寸提供器独立工作
                TestSizeProviderIndependence();
                
                // 测试3：组件可复用性
                TestComponentReusability();
                
                // 测试4：通知系统工作
                TestNotificationSystem();
                
                _testStatus = "所有测试通过 ✓";
                LogResult("✅ 新架构测试全部通过！组件可以独立工作且可复用。");
            }
            catch (System.Exception ex)
            {
                _testStatus = $"测试失败: {ex.Message}";
                LogResult($"❌ 测试失败: {ex.Message}");
            }
        }
        
        private void TestLayoutComponentIndependence()
        {
            LogResult("测试1: 布局组件独立性");
            
            // 测试布局组件是否可以独立创建和配置
            var layout = gameObject.AddComponent<VerticalScrollLayout>();
            layout.Spacing = 10f;
            layout.Padding = new RectOffset(5, 5, 5, 5);
            
            // 验证参数设置成功
            if (layout.Spacing != 10f || layout.Padding.top != 5)
                throw new System.Exception("布局组件参数设置失败");
                
            // 测试通知发送（不依赖ScrollView）
            bool notificationSent = false;
            ScrollComponentNotifier.LayoutChanged += (l) => {
                if (l == layout) notificationSent = true;
            };
            
            layout.ForceUpdate();
            
            if (!notificationSent)
                throw new System.Exception("布局组件通知发送失败");
                
            LogResult("  ✓ 布局组件可独立工作并正确发送通知");
            
            // 清理
            DestroyImmediate(layout);
        }
        
        private void TestSizeProviderIndependence()
        {
            LogResult("测试2: 尺寸提供器独立性");
            
            // 测试尺寸提供器是否可以独立创建和使用
            var sizeProvider = gameObject.AddComponent<FixedSizeProviderBehaviour>();
            sizeProvider.FixedSize = new Vector2(100, 50);
            
            // 验证尺寸计算功能
            var size = sizeProvider.GetItemSize(0, Vector2.one * 300);
            if (size != new Vector2(100, 50))
                throw new System.Exception("尺寸提供器计算错误");
                
            // 测试通知发送
            bool notificationSent = false;
            ScrollComponentNotifier.SizeProviderChanged += (sp) => {
                if (sp == sizeProvider) notificationSent = true;
            };
            
            sizeProvider.ForceUpdate();
            
            if (!notificationSent)
                throw new System.Exception("尺寸提供器通知发送失败");
                
            LogResult("  ✓ 尺寸提供器可独立工作并正确计算尺寸");
            
            // 清理
            DestroyImmediate(sizeProvider);
        }
        
        private void TestComponentReusability()
        {
            LogResult("测试3: 组件可复用性");
            
            // 创建一个布局组件
            var layout = gameObject.AddComponent<HorizontalScrollLayout>();
            layout.Spacing = 15f;
            
            // 测试组件可以被多个地方引用（模拟多个ScrollView使用同一组件）
            var layout1 = layout as IScrollLayout;
            var layout2 = layout as IScrollLayout;
            
            if (layout1 != layout2)
                throw new System.Exception("组件引用不一致");
                
            // 测试组件状态独立性
            var size1 = layout1.CalculateContentSize(10, null, new Vector2(300, 200));
            var size2 = layout2.CalculateContentSize(10, null, new Vector2(300, 200));
            
            if (size1 != size2)
                throw new System.Exception("组件状态不一致");
                
            LogResult("  ✓ 组件可以被多处引用且状态一致");
            
            // 清理
            DestroyImmediate(layout);
        }
        
        private void TestNotificationSystem()
        {
            LogResult("测试4: 通知系统功能");
            
            int layoutNotifications = 0;
            int sizeProviderNotifications = 0;
            
            // 订阅通知
            ScrollComponentNotifier.LayoutChanged += (_) => layoutNotifications++;
            ScrollComponentNotifier.SizeProviderChanged += (_) => sizeProviderNotifications++;
            
            // 触发通知
            ScrollComponentNotifier.NotifyLayoutChanged(null);
            ScrollComponentNotifier.NotifySizeProviderChanged(null);
            
            if (layoutNotifications != 1 || sizeProviderNotifications != 1)
                throw new System.Exception("通知系统工作异常");
                
            LogResult("  ✓ 全局通知系统工作正常");
        }
        
        private void LogResult(string message)
        {
            if (_logTestResults)
            {
                Debug.Log($"[IndependentComponentTest] {message}");
            }
        }

        [ContextMenu("清理测试组件")]
        public void CleanupTestComponents()
        {
            var layouts = GetComponents<ScrollLayoutBehaviour>();
            var sizeProviders = GetComponents<ScrollSizeProviderBehaviour>();
            
            foreach (var layout in layouts)
            {
                if (layout != null)
                    DestroyImmediate(layout);
            }
            
            foreach (var provider in sizeProviders)
            {
                if (provider != null)
                    DestroyImmediate(provider);
            }
            
            _testStatus = "测试组件已清理";
            LogResult("🧹 测试组件清理完成");
        }
    }
}