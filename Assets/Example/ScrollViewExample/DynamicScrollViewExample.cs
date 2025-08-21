using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleToolkits;

namespace SimpleToolkits.ScrollViewExample
{
    /// <summary>
    /// 动态ScrollView示例 - 使用新的BaseVariableSizeAdapter系统
    /// 演示基本的纵向动态聊天功能
    /// </summary>
    public class DynamicScrollViewExample : MonoBehaviour
    {
        [Header("UI组件")]
        private ScrollView _scrollView;
        [SerializeField] private RectTransform _messageTemplate;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _scrollToBottomButton;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TMP_InputField _messageInput;

        [Header("设置")]
        [SerializeField] private float _minHeight = 60f;   // 纵向列表最小高度
        [SerializeField] private float _maxHeight = 300f;  // 纵向列表最大高度
        [SerializeField] private float _fixedWidth = 300f; // 纵向列表固定宽度

        private readonly System.Collections.Generic.List<Models.ChatMessage> _messages = new();
        private StandardVariableSizeAdapter _adapter;

        private void Awake()
        {
            _scrollView = GetComponentInChildren<ScrollView>();
            ValidateComponents();
            InitializeUI();
            // 延迟初始化，确保所有组件都准备就绪
            StartCoroutine(DelayedInitialization());
        }

        private System.Collections.IEnumerator DelayedInitialization()
        {
            // 等待一帧，确保所有组件都完全初始化
            yield return null;
            
            InitializeComponents();
            BindEvents();
            AddInitialMessages();
        }

        private void ValidateComponents()
        {
            if (_scrollView == null) Debug.LogError("ScrollView未设置！", this);
            if (_messageTemplate == null) Debug.LogError("消息模板未设置！", this);
            if (_addButton == null) Debug.LogError("添加按钮未设置！", this);
        }

        private void InitializeUI()
        {
            if (_messageInput != null) _messageInput.text = "这是一条测试消息";
            UpdateCountText();
        }

        private void InitializeComponents()
        {
            // 确保ScrollView有正确的ScrollRect引用
            if (_scrollView != null)
            {
                var scrollRect = _scrollView.GetComponent<ScrollRect>();
                if (scrollRect != null && scrollRect.content == null)
                {
                    Debug.LogError("ScrollView的ScrollRect组件缺少Content引用！", this);
                    return;
                }
            }

            // 创建消息绑定器
            var messageBinder = new Binds.ChatMessageBinder(_messages);

            // 使用增强的 StandardVariableSizeAdapter（合并了LayoutAutoSizeProvider功能）
            // 纵向列表：固定宽度，自适应高度
            _adapter = StandardVariableSizeAdapter.CreateForVertical(
                prefab: _messageTemplate,
                countGetter: () => _messages.Count,
                dataGetter: index => index >= 0 && index < _messages.Count ? _messages[index] : null,
                binder: messageBinder,
                templateBinder: (rt, obj) =>
                {
                    // 为测量写入必要文本
                    var senderTMP = rt.Find<TextMeshProUGUI>("SenderText");
                    var contentTMP = rt.Find<TextMeshProUGUI>("ContentText");
                    var timeTMP = rt.Find<TextMeshProUGUI>("TimeText");

                    if (contentTMP == null)
                        contentTMP = rt.GetComponentInChildren<TextMeshProUGUI>(true);

                    if (obj is Models.ChatMessage msg)
                    {
                        if (senderTMP != null) senderTMP.text = msg.Sender ?? string.Empty;
                        if (contentTMP != null) contentTMP.text = msg.Content ?? string.Empty;
                        if (timeTMP != null) timeTMP.text = msg.Time ?? string.Empty;
                    }
                    else if (obj is string s)
                    {
                        if (contentTMP != null) contentTMP.text = s;
                    }
                },
                fixedWidth: _fixedWidth,
                minHeight: _minHeight,
                maxHeight: _maxHeight,
                enableCache: true,
                maxCacheSize: 1000
            );

            // 创建布局策略 - 纵向列表
            var content = _scrollView.GetComponentInChildren<ScrollRect>()?.content;
            if (content != null)
            {
                // 检查是否已有布局组件，如果没有则报错并返回
                if (!content.gameObject.TryGetComponent<IScrollLayout>(out var layout))
                {
                    Debug.LogError("无法找到 IScrollLayout 组件！请在 Content 对象上手动添加布局组件（如 VerticalLayout、ScrollHorizontalLayout 或 ScrollGridLayout）。", this);
                    return;
                }
            }
            else
            {
                Debug.LogError("无法找到 ScrollView 的 Content 对象！", this);
                return;
            }

            // 初始化ScrollView
            _scrollView.Initialize(_adapter);
            
            // 验证初始化是否成功
            if (!_scrollView.Initialized)
            {
                Debug.LogError("ScrollView初始化失败！", this);
            }
        }

        private void BindEvents()
        {
            if (_addButton != null) _addButton.onClick.AddListener(AddMessage);
            if (_clearButton != null) _clearButton.onClick.AddListener(ClearAllMessages);
            if (_scrollToBottomButton != null) _scrollToBottomButton.onClick.AddListener(ScrollToBottom);
        }

        private void AddInitialMessages()
        {
            var initialMessages = new[]
            {
                Models.ChatMessage.CreateSystem("欢迎使用基于BaseVariableSizeAdapter的聊天系统！"),
                Models.ChatMessage.CreateNormal("开发者", "这个示例使用新的BaseVariableSizeAdapter自动计算尺寸。"),
                Models.ChatMessage.CreateNormal("开发者", "优点：开发效率高、维护简单、灵活性强！"),
                Models.ChatMessage.CreateSuccess("所有组件工作正常。"),
                Models.ChatMessage.CreateWarning("长消息会自动换行并调整高度。")
            };

            // 清空现有消息
            _messages.Clear();
            
            // 添加初始消息
            foreach (var message in initialMessages) 
            {
                _messages.Add(message);
            }
            
            // 延迟刷新，确保ScrollView完全初始化
            StartCoroutine(DelayedRefresh());
        }

        private System.Collections.IEnumerator DelayedRefresh()
        {
            // 等待一帧，确保ScrollView完全初始化
            yield return null;
            
            // 清理缓存，确保重新计算尺寸
            _adapter?.ClearSizeCache();
            
            // 强制刷新ScrollView
            RefreshScrollView();
            
            // 再等待一帧，确保UI更新完成
            yield return null;
            
            // 预热缓存
            PreheatCache();
            
            // 滚动到底部
            ScrollToBottom();
        }

        private void AddMessage()
        {
            if (string.IsNullOrWhiteSpace(_messageInput?.text))
            {
                Debug.Log("消息内容不能为空！");
                return;
            }

            var message = Models.ChatMessage.CreateUser("用户", _messageInput.text);
            _messages.Add(message);

            if (_messageInput != null) _messageInput.text = string.Empty;

            RefreshScrollView();
            ScrollToBottomAsync().Forget();
        }

        private void ClearAllMessages()
        {
            _messages.Clear();
            RefreshScrollView();

            // 清理缓存
            _adapter?.ClearSizeCache();
        }

        private void ScrollToBottom()
        {
            if (_scrollView != null && _scrollView.Initialized)
            {
                _scrollView.ScrollToBottom();
            }
        }

        private async UniTaskVoid ScrollToBottomAsync()
        {
            await UniTask.Yield();
            await UniTask.Yield();
            ScrollToBottom();
        }

        private void RefreshScrollView()
        {
            if (_scrollView != null && _scrollView.Initialized)
            {
                // 强制重建所有尺寸（解决尺寸计算问题）
                _scrollView.InvalidateAllSizes(false);
                
                // 刷新ScrollView
                _scrollView.Refresh();
            }
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            if (_countText != null)
            {
                _countText.text = $"消息数量: {_messages.Count}";
            }
        }

        /// <summary>
        /// 预热缓存（性能优化）
        /// </summary>
        private void PreheatCache()
        {
            if (_adapter != null && _scrollView != null && _scrollView.Initialized)
            {
                // 获取当前布局信息
                var layout = _scrollView.GetType().GetProperty("Layout")?.GetValue(_scrollView) as IScrollLayout;
                var scrollRect = _scrollView.GetComponent<ScrollRect>();
                var viewportSize = scrollRect?.viewport.rect.size ?? Vector2.zero;

                if (layout != null && viewportSize != Vector2.zero && _messages.Count > 0)
                {
                    // 清理现有缓存，确保重新计算
                    _adapter.ClearSizeCache();
                    
                    // 预热所有消息的缓存
                    _adapter.PreheatCache(layout, viewportSize, 0, _messages.Count);

                    // 输出缓存统计
                    Debug.Log($"[DynamicScrollViewExample] 缓存预热完成: {_messages.Count} 条消息");
                }
            }
        }

        /// <summary>
        /// 测试性能（调试用）
        /// </summary>
        private void TestPerformance()
        {
            // 性能测试功能已集成到StandardVariableSizeAdapter中
            Debug.Log("[DynamicScrollViewExample] 性能测试功能已集成到StandardVariableSizeAdapter中");
        }

        private void OnDestroy()
        {
            if (_addButton != null) _addButton.onClick.RemoveListener(AddMessage);
            if (_clearButton != null) _clearButton.onClick.RemoveListener(ClearAllMessages);
            if (_scrollToBottomButton != null) _scrollToBottomButton.onClick.RemoveListener(ScrollToBottom);
        }

        #region 调试方法
        [ContextMenu("显示诊断信息")]
        private void ShowDiagnostics()
        {
            if (_adapter != null)
            {
                Debug.Log($"[DynamicScrollViewExample] 当前消息数量: {_messages.Count}");
            }
        }

        [ContextMenu("测试性能")]
        private void RunPerformanceTest()
        {
            TestPerformance();
        }

        [ContextMenu("预热缓存")]
        private void RunCachePreheat()
        {
            PreheatCache();
        }

        [ContextMenu("添加测试消息")]
        private void AddTestMessage()
        {
            var testMessages = new[]
            {
                "这是一条短消息。",
                "这是一条中等长度的消息，用于测试自动换行功能。",
                "这是一条很长的消息，用于测试长文本的显示效果和自动换行功能。这条消息包含很多内容，应该能够自动换行并调整高度以适应内容的长度。"
            };

            var random = new System.Random();
            var message = testMessages[random.Next(testMessages.Length)];

            var chatMessage = Models.ChatMessage.CreateNormal("测试用户", message);
            _messages.Add(chatMessage);

            RefreshScrollView();
            ScrollToBottomAsync().Forget();
        }
        #endregion
    }
}
