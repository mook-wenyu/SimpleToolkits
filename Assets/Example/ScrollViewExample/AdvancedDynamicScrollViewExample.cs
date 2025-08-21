using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleToolkits;

namespace SimpleToolkits.ScrollViewExample
{
    /// <summary>
    /// 高级动态ScrollView示例 - 展示复杂的聊天功能
    /// 包含消息过滤、排序、分组、搜索等高级功能
    /// </summary>
    public class AdvancedDynamicScrollViewExample : MonoBehaviour
    {
        [Header("UI组件")]
        private ScrollView _scrollView;
        [SerializeField] private RectTransform _messageTemplate;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _scrollToBottomButton;
        [SerializeField] private Button _filterButton;
        [SerializeField] private Button _sortButton;
        [SerializeField] private Button _searchButton;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TextMeshProUGUI _filterText;
        [SerializeField] private TMP_InputField _messageInput;
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private Dropdown _filterDropdown;
        [SerializeField] private Dropdown _sortDropdown;

        [Header("设置")]
        [SerializeField] private float _minHeight = 60f;
        [SerializeField] private float _maxHeight = 300f;
        [SerializeField] private float _fixedWidth = 300f;

        private readonly List<Models.ChatMessage> _messages = new();
        private readonly List<Models.ChatMessage> _filteredMessages = new();
        private StandardVariableSizeAdapter _adapter;
        private MessageType _currentFilter = MessageType.All;
        private SortType _currentSort = SortType.Time;
        private string _searchTerm = string.Empty;

        private enum MessageType { All, Normal, User, System, Error, Warning, Success }
        private enum SortType { Time, Sender, Content, Type, Priority }

        private void Awake()
        {
            _scrollView = GetComponentInChildren<ScrollView>();
            ValidateComponents();
            InitializeUI();
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

            // 初始化下拉菜单
            InitializeDropdowns();

            UpdateCountText();
            UpdateFilterText();
        }

        private void InitializeDropdowns()
        {
            if (_filterDropdown != null)
            {
                _filterDropdown.ClearOptions();
                _filterDropdown.AddOptions(new List<string>
                {
                    "全部消息",
                    "普通消息",
                    "用户消息",
                    "系统消息",
                    "错误消息",
                    "警告消息",
                    "成功消息"
                });
                _filterDropdown.onValueChanged.AddListener(OnFilterChanged);
            }

            if (_sortDropdown != null)
            {
                _sortDropdown.ClearOptions();
                _sortDropdown.AddOptions(new List<string>
                {
                    "按时间排序",
                    "按发送者排序",
                    "按内容排序",
                    "按类型排序",
                    "按优先级排序"
                });
                _sortDropdown.onValueChanged.AddListener(OnSortChanged);
            }
        }

        private void InitializeComponents()
        {
            // 创建消息绑定器
            var messageBinder = new Binds.ChatMessageBinder(_filteredMessages);

            // 使用增强的 StandardVariableSizeAdapter（合并了LayoutAutoSizeProvider功能）
            // 纵向布局：固定宽度，自适应高度
            _adapter = StandardVariableSizeAdapter.CreateForVertical(
                prefab: _messageTemplate,
                countGetter: () => _filteredMessages.Count,
                dataGetter: index => index >= 0 && index < _filteredMessages.Count ? _filteredMessages[index] : null,
                binder: messageBinder,
                templateBinder: (rt, obj) =>
                {
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
                maxCacheSize: 1000,
                forceRebuild: false
            );

            // 创建布局策略 - 纵向列表
            var content = _scrollView.GetComponentInChildren<ScrollRect>()?.content;
            if (content != null)
            {
                // 检查是否已有布局组件，如果没有则报错并返回
                if (!content.gameObject.TryGetComponent<IScrollLayout>(out var layout))
                {
                    Debug.LogError("无法找到 IScrollLayout 组件！请在 Content 对象上手动添加布局组件（如 ScrollVerticalLayout、ScrollHorizontalLayout 或 ScrollGridLayout）。", this);
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
        }

        private void BindEvents()
        {
            if (_addButton != null) _addButton.onClick.AddListener(AddMessage);
            if (_clearButton != null) _clearButton.onClick.AddListener(ClearAllMessages);
            if (_scrollToBottomButton != null) _scrollToBottomButton.onClick.AddListener(ScrollToBottom);
            if (_filterButton != null) _filterButton.onClick.AddListener(ToggleFilter);
            if (_sortButton != null) _sortButton.onClick.AddListener(ToggleSort);
            if (_searchButton != null) _searchButton.onClick.AddListener(ToggleSearch);

            if (_searchInput != null) _searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        private void AddInitialMessages()
        {
            var initialMessages = new[]
            {
                Models.ChatMessage.CreateSystem("欢迎使用高级聊天系统！"),
                Models.ChatMessage.CreateNormal("开发者", "这个示例展示了高级功能：过滤、排序、搜索等。"),
                Models.ChatMessage.CreateNormal("设计师", "UI会根据消息类型自动调整颜色和样式。"),
                Models.ChatMessage.CreateSuccess("所有功能正常工作！"),
                Models.ChatMessage.CreateWarning("长消息会自动换行并调整高度。"),
                Models.ChatMessage.CreateError("这是一个错误消息示例。"),
                Models.ChatMessage.CreateUser("测试用户", "这是一个用户消息示例。"),
                Models.ChatMessage.CreateNormal("产品经理", "产品功能演示完成。")
            };

            foreach (var message in initialMessages)
            {
                message.Priority = initialMessages.Length - _messages.Count;
                _messages.Add(message);
            }

            ApplyFiltersAndSort();
            PreheatCache();
        }

        private void AddMessage()
        {
            if (string.IsNullOrWhiteSpace(_messageInput?.text))
            {
                Debug.Log("消息内容不能为空！");
                return;
            }

            var message = Models.ChatMessage.CreateUser("用户", _messageInput.text);
            message.Priority = _messages.Count;
            _messages.Add(message);

            if (_messageInput != null) _messageInput.text = string.Empty;

            ApplyFiltersAndSort();
            ScrollToBottomAsync().Forget();
        }

        private void ClearAllMessages()
        {
            _messages.Clear();
            _filteredMessages.Clear();
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
                _scrollView.Refresh();
            }
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            if (_countText != null)
            {
                _countText.text = $"显示: {_filteredMessages.Count} / 总数: {_messages.Count}";
            }
        }

        private void UpdateFilterText()
        {
            if (_filterText != null)
            {
                string filterName = _currentFilter switch
                {
                    MessageType.All => "全部",
                    MessageType.Normal => "普通",
                    MessageType.User => "用户",
                    MessageType.System => "系统",
                    MessageType.Error => "错误",
                    MessageType.Warning => "警告",
                    MessageType.Success => "成功",
                    _ => "未知"
                };

                string sortName = _currentSort switch
                {
                    SortType.Time => "时间",
                    SortType.Sender => "发送者",
                    SortType.Content => "内容",
                    SortType.Type => "类型",
                    SortType.Priority => "优先级",
                    _ => "未知"
                };

                _filterText.text = $"筛选: {filterName} | 排序: {sortName}";
                if (!string.IsNullOrEmpty(_searchTerm))
                {
                    _filterText.text += $" | 搜索: {_searchTerm}";
                }
            }
        }

        #region 过滤和排序
        private void ApplyFiltersAndSort()
        {
            ApplyFilter();
            ApplySort();
            RefreshScrollView();
        }

        private void ApplyFilter()
        {
            _filteredMessages.Clear();

            foreach (var message in _messages)
            {
                if (!MessageTypeMatches(message, _currentFilter)) continue;

                if (!string.IsNullOrEmpty(_searchTerm))
                {
                    if (!message.Content.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) &&
                        !message.Sender.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                _filteredMessages.Add(message);
            }
        }

        private bool MessageTypeMatches(Models.ChatMessage message, MessageType filter)
        {
            return filter switch
            {
                MessageType.All => true,
                MessageType.Normal => message.Type == Models.MessageType.Normal,
                MessageType.User => message.Type == Models.MessageType.User,
                MessageType.System => message.Type == Models.MessageType.System,
                MessageType.Error => message.Type == Models.MessageType.Error,
                MessageType.Warning => message.Type == Models.MessageType.Warning,
                MessageType.Success => message.Type == Models.MessageType.Success,
                _ => false
            };
        }

        private void ApplySort()
        {
            _filteredMessages.Sort((a, b) =>
            {
                return _currentSort switch
                {
                    SortType.Time => b.Timestamp.CompareTo(a.Timestamp),
                    SortType.Sender => string.Compare(a.Sender, b.Sender, StringComparison.OrdinalIgnoreCase),
                    SortType.Content => string.Compare(a.Content, b.Content, StringComparison.OrdinalIgnoreCase),
                    SortType.Type => a.Type.CompareTo(b.Type),
                    SortType.Priority => b.Priority.CompareTo(a.Priority),
                    _ => 0
                };
            });
        }

        private void OnFilterChanged(int index)
        {
            _currentFilter = (MessageType)index;
            ApplyFiltersAndSort();
        }

        private void OnSortChanged(int index)
        {
            _currentSort = (SortType)index;
            ApplyFiltersAndSort();
        }

        private void OnSearchChanged(string value)
        {
            _searchTerm = value;
            ApplyFiltersAndSort();
        }

        private void ToggleFilter()
        {
            if (_filterDropdown != null)
            {
                _filterDropdown.gameObject.SetActive(!_filterDropdown.gameObject.activeSelf);
            }
        }

        private void ToggleSort()
        {
            if (_sortDropdown != null)
            {
                _sortDropdown.gameObject.SetActive(!_sortDropdown.gameObject.activeSelf);
            }
        }

        private void ToggleSearch()
        {
            if (_searchInput != null)
            {
                _searchInput.gameObject.SetActive(!_searchInput.gameObject.activeSelf);
            }
        }
        #endregion

        /// <summary>
        /// 预热缓存（性能优化）
        /// </summary>
        private void PreheatCache()
        {
            if (_adapter != null && _scrollView != null && _scrollView.Initialized)
            {
                var layout = _scrollView.GetType().GetProperty("Layout")?.GetValue(_scrollView) as IScrollLayout;
                var viewportSize = _scrollView.GetComponent<ScrollRect>()?.viewport.rect.size ?? Vector2.zero;

                if (layout != null && viewportSize != Vector2.zero)
                {
                    _adapter.PreheatCache(layout, viewportSize, 0, Mathf.Min(10, _filteredMessages.Count));

                    Debug.Log($"[AdvancedDynamicScrollViewExample] 缓存预热完成: {_filteredMessages.Count} 条消息");
                }
            }
        }

        private void OnDestroy()
        {
            if (_addButton != null) _addButton.onClick.RemoveListener(AddMessage);
            if (_clearButton != null) _clearButton.onClick.RemoveListener(ClearAllMessages);
            if (_scrollToBottomButton != null) _scrollToBottomButton.onClick.RemoveListener(ScrollToBottom);
            if (_filterButton != null) _filterButton.onClick.RemoveListener(ToggleFilter);
            if (_sortButton != null) _sortButton.onClick.RemoveListener(ToggleSort);
            if (_searchButton != null) _searchButton.onClick.RemoveListener(ToggleSearch);

            if (_filterDropdown != null) _filterDropdown.onValueChanged.RemoveListener(OnFilterChanged);
            if (_sortDropdown != null) _sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
            if (_searchInput != null) _searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        #region 调试方法
        [ContextMenu("显示诊断信息")]
        private void ShowDiagnostics()
        {
            if (_adapter != null && _messageTemplate != null)
            {
                var diagnostics = $"=== StandardVariableSizeAdapter 诊断信息 ===\n";
                diagnostics += $"当前消息数量: {_filteredMessages.Count}\n";
                diagnostics += $"模板状态: 正常\n";
                Debug.Log(diagnostics);
            }
        }

        [ContextMenu("测试性能")]
        private void RunPerformanceTest()
        {
            if (_adapter != null && _scrollView != null && _scrollView.Initialized)
            {
                var layout = _scrollView.GetType().GetProperty("Layout")?.GetValue(_scrollView) as IScrollLayout;
                var viewportSize = _scrollView.GetComponent<ScrollRect>()?.viewport.rect.size ?? Vector2.zero;

                if (layout != null && viewportSize != Vector2.zero)
                {
                    Debug.Log($"[AdvancedDynamicScrollViewExample] 性能测试功能已集成到StandardVariableSizeAdapter中");
                }
            }
        }

        [ContextMenu("预热缓存")]
        private void RunCachePreheat()
        {
            PreheatCache();
        }

        [ContextMenu("批量添加测试消息")]
        private void AddBatchTestMessages()
        {
            var testMessages = new[]
            {
                Models.ChatMessage.CreateNormal("用户A", "你好！"),
                Models.ChatMessage.CreateNormal("用户B", "你好，很高兴见到你！"),
                Models.ChatMessage.CreateSystem("用户B已上线"),
                Models.ChatMessage.CreateNormal("用户A", "今天天气真不错"),
                Models.ChatMessage.CreateNormal("用户B", "是啊，适合出去走走"),
                Models.ChatMessage.CreateWarning("检测到新版本"),
                Models.ChatMessage.CreateSuccess("数据同步完成"),
                Models.ChatMessage.CreateError("网络连接失败"),
                Models.ChatMessage.CreateNormal("用户A", "这条消息比较长，用于测试长文本的显示效果和自动换行功能。"),
                Models.ChatMessage.CreateNormal("用户B", "我也是这么认为的。")
            };

            foreach (var message in testMessages)
            {
                message.Priority = _messages.Count;
                _messages.Add(message);
            }

            ApplyFiltersAndSort();
            ScrollToBottomAsync().Forget();
        }
        #endregion
    }
}
