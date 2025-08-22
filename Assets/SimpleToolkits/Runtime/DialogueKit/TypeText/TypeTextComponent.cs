using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace SimpleToolkits.DialogueKit
{
    /// <summary>
    /// 高性能打字机效果组件
    /// 基于 UniTask 和 TextMeshProUGUI 构建，支持完整的标签系统
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TypeTextComponent : MonoBehaviour
    {
        [Header("打字机设置")]
        [SerializeField] private float _defaultSpeed = 0.05f;
        [SerializeField] private bool _enableRichText = true;
        [SerializeField] private bool _showCursor = false;
        [SerializeField] private string _cursorChar = "|";

        // 组件引用
        private TextMeshProUGUI _textComponent;
        
        // 状态管理
        private bool _isTyping = false;
        private CancellationTokenSource _cancellationTokenSource;
        
        // 文本处理
        private StringBuilder _displayTextBuilder;
        private string _originalText;
        private string _processedText;

        // 标签系统
        private TagParsingEngine _tagEngine;

        // 事件委托
        public event Action OnComplete;
        public event Action<char> OnCharTyped;
        public event Action OnStarted;

        #region Unity 生命周期

        private void Awake()
        {
            InitializeComponent();
        }

        private void OnDestroy()
        {
            StopTypingAsync().Forget();
            _cancellationTokenSource?.Dispose();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            _displayTextBuilder = new StringBuilder(256);
            _textComponent.richText = _enableRichText;

            // 初始化标签引擎
            _tagEngine = new TagParsingEngine();

            // 使用默认内置标签
            TagProcessorRegistry.Instance.ResetToBuiltins();
        }

        #endregion

        #region 公共 API

        /// <summary>
        /// 开始打字机效果
        /// </summary>
        /// <param name="text">要显示的文本</param>
        /// <param name="customSpeed">自定义速度（可选）</param>
        /// <param name="onComplete">完成回调（可选）</param>
        public async UniTask StartTypingAsync(string text, float customSpeed = -1f, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            await StopTypingAsync();

            _originalText = text;
            _processedText = _tagEngine.PreprocessText(text);
            OnComplete = onComplete;

            var speed = customSpeed > 0 ? customSpeed : _defaultSpeed;

            _cancellationTokenSource = new CancellationTokenSource();
            _isTyping = true;

            try
            {
                OnStarted?.Invoke();

                await ExecuteTypingWithTagsAsync(speed, _cancellationTokenSource.Token);

                OnComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // 打字被取消
            }
            finally
            {
                _isTyping = false;
            }
        }

        /// <summary>
        /// 立即跳过打字效果
        /// </summary>
        public async UniTask SkipTypingAsync()
        {
            if (!_isTyping)
                return;

            await StopTypingAsync();
            _textComponent.text = _processedText;
            
            OnComplete?.Invoke();
        }

        /// <summary>
        /// 停止打字效果
        /// </summary>
        public async UniTask StopTypingAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _isTyping = false;
            await UniTask.Yield();
        }

        /// <summary>
        /// 立即设置文本（无动画）
        /// </summary>
        public void SetTextInstant(string text)
        {
            StopTypingAsync().Forget();
            _originalText = text;
            _processedText = _tagEngine.PreprocessText(text);
            _textComponent.text = _processedText;
        }


        /// <summary>
        /// 验证文本格式
        /// </summary>
        /// <param name="text">要验证的文本</param>
        /// <returns>验证结果</returns>
        public TagValidationResult ValidateText(string text)
        {
            return _tagEngine.ValidateText(text);
        }

        /// <summary>
        /// 是否正在打字
        /// </summary>
        public bool IsTyping => _isTyping;

        /// <summary>
        /// 是否可以跳过
        /// </summary>
        public bool IsSkippable => _isTyping;

        #endregion

        #region 核心打字逻辑

        /// <summary>
        /// 执行支持标签的打字效果
        /// </summary>
        private async UniTask ExecuteTypingWithTagsAsync(float speed, CancellationToken cancellationToken)
        {
            _displayTextBuilder.Clear();

            var context = new TagProcessContext(_originalText, 0, speed, this);
            var tags = _tagEngine.ParseTags(_originalText);
            var tagIndex = 0;

            for (int i = 0; i < _originalText.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                context.currentIndex = i;

                // 检查是否遇到标签
                if (tagIndex < tags.Count && tags[tagIndex].startIndex == i)
                {
                    var tag = tags[tagIndex];
                    var result = await _tagEngine.ProcessTagAsync(context, tag);

                    if (result.isProcessed)
                    {
                        // 如果标签处理产生了要显示的内容
                        if (result.data is string displayText && !string.IsNullOrEmpty(displayText))
                        {
                            _displayTextBuilder.Append(displayText);
                        }

                        // 跳过标签字符
                        i = tag.endIndex;
                        tagIndex++;

                        // 更新速度（如果标签修改了速度）- 这里是关键更新
                        speed = context.currentSpeed;

                        // 如果标签需要跳过显示，继续下一个字符
                        if (result.shouldSkipDisplay)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        tagIndex++;
                    }
                }
                else
                {
                    // 普通字符处理
                    var currentChar = _originalText[i];
                    
                    // 添加字符
                    _displayTextBuilder.Append(currentChar);

                    // 触发事件
                    OnCharTyped?.Invoke(currentChar);
                }

                // 更新显示
                UpdateDisplay();

                // 等待
                if (speed > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(speed), cancellationToken: cancellationToken);
                }
            }

            // 移除光标
            if (_showCursor)
            {
                _textComponent.text = _displayTextBuilder.ToString();
            }
        }

        /// <summary>
        /// 更新显示文本
        /// </summary>
        private void UpdateDisplay()
        {
            var displayText = _displayTextBuilder.ToString();
            if (_showCursor && _isTyping)
                displayText += _cursorChar;
                
            _textComponent.text = displayText;
        }

        #endregion

        #region Inspector 辅助方法

#if UNITY_EDITOR
        [ContextMenu("Validate Current Text")]
        private void ValidateCurrentText()
        {
            if (_textComponent != null && !string.IsNullOrEmpty(_textComponent.text))
            {
                var result = ValidateText(_textComponent.text);
                Debug.Log($"Text validation result:\n{result}");
            }
            else
            {
                Debug.Log("No text to validate");
            }
        }

        [ContextMenu("Show Tag Statistics")]
        private void ShowTagStatistics()
        {
            Debug.Log(TagProcessorRegistry.Instance.GetStatistics());
        }

        [ContextMenu("Test Text Processing")]
        private void TestTextProcessing()
        {
            var testText = "Hello [color=red]World[/color]! [pause=1.0][speed=0.02]This is slow text.";
            Debug.Log($"Original: {testText}");
            Debug.Log($"Processed: {_tagEngine.PreprocessText(testText)}");
            
            var usedTags = _tagEngine.GetUsedTags(testText);
            Debug.Log($"Used tags: {string.Join(", ", usedTags)}");
        }
#endif

        #endregion
    }
}