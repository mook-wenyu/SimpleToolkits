namespace SimpleToolkits.Examples
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// 动态ScrollView示例 - 使用全新v4.0 API
    /// 
    /// 功能：
    /// - 完全自定义布局系统，无Unity布局依赖
    /// - 高性能对象池和虚拟化滚动
    /// - 动态尺寸计算和消息增删
    /// - 极简API设计，易于使用
    /// </summary>
    public class DynamicScrollViewExample : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform messagePrefab;
        [SerializeField] private Button addButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button scrollToBottomButton;
        [SerializeField] private Button scrollToTopButton;

        private ScrollView _scrollViewComponent;
        private List<MessageData> _messages = new List<MessageData>();

        [System.Serializable]
        public class MessageData
        {
            public string content;
            public string sender;
            public bool isLongMessage;

            public MessageData(string content, string sender = "用户", bool isLongMessage = false)
            {
                this.content = content;
                this.sender = sender;
                this.isLongMessage = isLongMessage;
            }
        }

        void Start()
        {
            InitializeScrollView();
            BindEvents();
            AddInitialMessages();
        }

        private void InitializeScrollView()
        {
            if (scrollView == null)
            {
                Debug.LogError("ScrollRect 组件未分配！", this);
                return;
            }

            if (messagePrefab == null)
            {
                Debug.LogError("消息预制体未分配！", this);
                return;
            }

            // 使用新的v4.0 API - 极简设计，高性能
            _scrollViewComponent = ScrollView.Create(scrollView)
                .SetData(_messages, messagePrefab, OnBindMessage)
                .SetVerticalLayout(spacing: 4f, padding: new RectOffset(8, 8, 8, 8))
                .SetDynamicSize(CalculateMessageSize, defaultSize: new Vector2(300, 60), maxCacheSize: 500)
                .SetPoolSize(15)
                .Build();

            // 绑定事件
            _scrollViewComponent.OnVisibleRangeChanged += OnVisibleRangeChanged;
            _scrollViewComponent.OnScrollPositionChanged += OnScrollPositionChanged;

            Debug.Log("ScrollView v4.0 初始化完成 - 高性能自定义布局系统");
        }

        /// <summary>
        /// 消息数据绑定 - 纯数据绑定，无布局依赖
        /// </summary>
        private void OnBindMessage(int index, RectTransform cell, MessageData messageData)
        {
            // 查找文本组件
            var senderText = cell.Find("SenderText")?.GetComponent<TextMeshProUGUI>();
            var contentText = cell.Find("ContentText")?.GetComponent<TextMeshProUGUI>();

            if (senderText != null)
            {
                senderText.text = messageData.sender;
                senderText.color = messageData.isLongMessage ? Color.red : Color.gray;
            }

            if (contentText != null)
            {
                contentText.text = messageData.content;
                contentText.color = Color.white;
            }

            // 如果没有找到TMP组件，尝试传统Text组件
            if (senderText == null || contentText == null)
            {
                var legacyText = cell.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = $"{messageData.sender}: {messageData.content}";
                    legacyText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 动态尺寸计算 - 基于内容长度智能计算高度
        /// </summary>
        private Vector2 CalculateMessageSize(int index, Vector2 viewportSize)
        {
            if (index < 0 || index >= _messages.Count)
                return new Vector2(viewportSize.x - 16f, 60f);

            var message = _messages[index];
            var contentLength = message.content.Length;
            
            // 基于内容长度计算高度
            float baseHeight = 60f;
            float additionalHeight = 0f;

            if (contentLength > 50)
            {
                // 长消息：每50个字符增加20像素高度
                additionalHeight = ((contentLength - 50) / 50) * 20f;
            }

            // 限制最大高度
            float finalHeight = Mathf.Clamp(baseHeight + additionalHeight, 60f, 200f);
            
            return new Vector2(viewportSize.x - 16f, finalHeight);
        }

        private void BindEvents()
        {
            if (addButton != null) addButton.onClick.AddListener(AddRandomMessage);
            if (clearButton != null) clearButton.onClick.AddListener(ClearAllMessages);
            if (scrollToBottomButton != null) scrollToBottomButton.onClick.AddListener(ScrollToBottom);
            if (scrollToTopButton != null) scrollToTopButton.onClick.AddListener(ScrollToTop);
        }

        private void AddInitialMessages()
        {
            var initialMessages = new[]
            {
                new MessageData("欢迎使用ScrollView v4.0！", "系统", false),
                new MessageData("全新自定义布局系统，完全摆脱Unity布局组件。", "系统", false),
                new MessageData("特点：高性能、零依赖、极简API、完全可控！", "系统", false),
                new MessageData("所有组件都使用纯手动布局计算。", "用户", false),
                new MessageData("这是一条超长消息，用来测试动态尺寸计算功能。它会根据内容长度自动调整高度，提供更好的用户体验。支持多行文本显示和智能换行，确保所有内容都能正确显示。", "用户", true)
            };

            _messages.Clear();
            _messages.AddRange(initialMessages);
            
            if (_scrollViewComponent != null && _scrollViewComponent.IsInitialized)
            {
                _scrollViewComponent.Refresh();
                StartCoroutine(ScrollToBottomDelayed());
            }
        }

        public void AddRandomMessage()
        {
            var randomMessages = new[]
            {
                new MessageData("这是一条普通消息", "用户", false),
                new MessageData("这是一条很长很长很长很长很长很长很长很长很长很长的消息，它会自动调整高度来适应内容长度。", "用户", true),
                new MessageData("短消息", "好友", false),
                new MessageData("包含换行\n的消息", "系统", false),
                new MessageData("🎉 这是一条带表情的消息！", "用户", false),
                new MessageData("系统通知：有新用户加入聊天室", "系统", false),
                new MessageData("警告：请注意网络安全", "系统", false),
                new MessageData("这是一条超长的测试消息，用来验证动态尺寸计算的准确性。它包含了大量的文本内容，应该会被系统自动识别为长消息并分配更大的显示空间。", "用户", true)
            };

            var randomMessage = randomMessages[UnityEngine.Random.Range(0, randomMessages.Length)];
            _messages.Add(randomMessage);

            if (_scrollViewComponent != null && _scrollViewComponent.IsInitialized)
            {
                _scrollViewComponent.Refresh();
                StartCoroutine(ScrollToBottomDelayed());
            }
        }

        public void ClearAllMessages()
        {
            _messages.Clear();
            
            if (_scrollViewComponent != null && _scrollViewComponent.IsInitialized)
            {
                _scrollViewComponent.Refresh();
            }
        }

        public void ScrollToBottom()
        {
            _scrollViewComponent?.ScrollToBottom(immediate: false);
        }

        public void ScrollToTop()
        {
            _scrollViewComponent?.ScrollToTop(immediate: false);
        }

        private IEnumerator ScrollToBottomDelayed()
        {
            yield return new WaitForSeconds(0.1f);
            ScrollToBottom();
        }

        #region 事件回调
        private void OnVisibleRangeChanged(int first, int last)
        {
            Debug.Log($"可见范围变化: {first} - {last}");
        }

        private void OnScrollPositionChanged(Vector2 position)
        {
            // 可以在这里添加滚动位置相关的逻辑
        }
        #endregion

        #region 键盘快捷键
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddRandomMessage();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearAllMessages();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                ScrollToBottom();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                ScrollToTop();
            }
        }
        #endregion
    }
}