using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;

namespace SimpleToolkits.DialogueKit.Examples
{
    /// <summary>
    /// 打字机效果使用示例
    /// 演示如何使用标签系统和各种功能
    /// </summary>
    public class TypeTextExamples : MonoBehaviour
    {
        [Header("UI 组件")]
        [SerializeField] private TextMeshProUGUI _exampleText;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("示例文本")]
        [SerializeField] private string[] _exampleTexts = new string[]
        {
            "Hello World!",
            "Hello [color=red]Red[/color] World!",
            "[speed=0.02]Slow typing... [speed=0.1]Fast typing!",
            "[speedregion=0.02]This region is slow[/speedregion] Back to normal speed!",
            "Normal [speedregion=0.01]very slow region[/speedregion] and [speed=0.1]permanently fast!",
            "[b]Bold[/b] and [i]Italic[/i] text.",
            "Wait for it... [pause=2.0]Done!",
            "Simple text example",
            "[action=click]Action trigger! [br]New line here.",
            "[color=yellow][size=30]Big Yellow Text[/size][/color]",
            "Complex: [speedregion=0.02][color=red]Slow red text[/color][/speedregion] [speed=0.1]Fast normal text"
        };

        private int _currentExampleIndex = 0;
        private TypeTextComponent _typeTextComponent;

        private void Start()
        {
            InitializeExample();
        }

        private void Update()
        {
            // 空格键切换示例
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextExample().Forget();
            }

            // 回车键跳过当前打字
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SkipCurrentTyping().Forget();
            }

            // ESC键重置
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResetExamples();
            }
        }

        /// <summary>
        /// 初始化示例
        /// </summary>
        private void InitializeExample()
        {
            if (_exampleText == null)
            {
                Debug.LogError("Example text component is not assigned!");
                return;
            }

            // 获取或添加打字机组件
            _typeTextComponent = _exampleText.GetOrAddTypeTextComponent();


            // 显示使用说明
            UpdateStatus("按 [Space] 切换示例，[Enter] 跳过打字，[ESC] 重置");

            // 开始第一个示例
            ShowCurrentExample().Forget();
        }

        /// <summary>
        /// 显示当前示例
        /// </summary>
        private async UniTask ShowCurrentExample()
        {
            if (_currentExampleIndex >= _exampleTexts.Length)
            {
                _currentExampleIndex = 0;
            }

            var text = _exampleTexts[_currentExampleIndex];
            UpdateStatus($"示例 {_currentExampleIndex + 1}/{_exampleTexts.Length}: {text}");

            // 验证文本格式
            var validation = _typeTextComponent.ValidateText(text);
            if (!validation.isValid)
            {
                Debug.LogWarning($"Text validation failed: {validation}");
            }

            try
            {
                await _typeTextComponent.StartTypingAsync(text, 0.05f);
                UpdateStatus($"示例 {_currentExampleIndex + 1} 完成！按 [Space] 继续下一个");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in typing example: {ex.Message}");
                UpdateStatus($"示例执行出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换到下一个示例
        /// </summary>
        private async UniTask NextExample()
        {
            if (_typeTextComponent.IsTyping)
            {
                await _typeTextComponent.StopTypingAsync();
            }

            _currentExampleIndex = (_currentExampleIndex + 1) % _exampleTexts.Length;
            await ShowCurrentExample();
        }

        /// <summary>
        /// 跳过当前打字
        /// </summary>
        private async UniTask SkipCurrentTyping()
        {
            if (_typeTextComponent.IsSkippable)
            {
                await _typeTextComponent.SkipTypingAsync();
                UpdateStatus($"示例 {_currentExampleIndex + 1} 已跳过！按 [Space] 继续下一个");
            }
        }

        /// <summary>
        /// 重置示例
        /// </summary>
        private void ResetExamples()
        {
            _currentExampleIndex = 0;
            _typeTextComponent.StopTypingAsync().Forget();
            _exampleText.text = "";
            UpdateStatus("示例已重置，按 [Space] 开始");
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
            Debug.Log($"[TypeTextExample] {status}");
        }

#if UNITY_EDITOR
        [ContextMenu("Show Tag Statistics")]
        private void ShowTagStatistics()
        {
            Debug.Log(TypeTextUtility.GetTagStatistics());
        }

        [ContextMenu("Test Custom Tag")]
        private void TestCustomTag()
        {
            // 注册示例自定义标签
            TypeTextUtility.RegisterTagProcessor(new CustomTagProcessorExample());
            
            var testText = "This is a [example=test]custom tag[/example] example.";
            ShowCustomText(testText).Forget();
        }

        private async UniTask ShowCustomText(string text)
        {
            await _typeTextComponent.StartTypingAsync(text, 0.05f);
        }
#endif
    }

    /// <summary>
    /// 自定义震动标签处理器示例
    /// 实现文字震动效果
    /// </summary>
    public class ShakeTagProcessor : BaseTagProcessor
    {
        public override string TagName => "shake";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 将方括号标签转换为 TextMeshPro 的震动标签
            var shakeTag = "<shake>";
            return new TagProcessResult(true, shakeTag.Length, false, shakeTag);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</shake>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true; // 移除方括号版本，替换为角括号版本
        }
    }

    /// <summary>
    /// 自定义渐变标签处理器示例
    /// 实现颜色渐变效果
    /// </summary>
    public class GradientTagProcessor : BaseTagProcessor
    {
        public override string TagName => "gradient";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 解析渐变参数，如 [gradient=red,blue]
            var colors = ParseMultipleParameters(tagContent);
            if (colors.Length >= 2)
            {
                var gradientTag = $"<gradient tint=1 from=\"{colors[0]}\" to=\"{colors[1]}\">";
                return new TagProcessResult(true, gradientTag.Length, false, gradientTag);
            }
            
            return new TagProcessResult(false, 0);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</gradient>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true;
        }
    }

    /// <summary>
    /// 高级标签使用示例
    /// </summary>
    public class AdvancedTagExample : MonoBehaviour
    {
        [Header("高级示例")]
        [SerializeField] private TextMeshProUGUI _advancedText;

        private void Start()
        {
            RegisterAdvancedTags();
            ShowAdvancedExample().Forget();
        }

        /// <summary>
        /// 注册高级标签
        /// </summary>
        private void RegisterAdvancedTags()
        {
            TypeTextUtility.RegisterTagProcessors(
                new ShakeTagProcessor(),
                new GradientTagProcessor()
            );
        }

        /// <summary>
        /// 显示高级示例
        /// </summary>
        private async UniTask ShowAdvancedExample()
        {
            var advancedText = 
                "[gradient=red,blue]Gradient Text[/gradient]\n" +
                "[shake]Shaking Text[/shake]\n" +
                "[color=yellow][shake]Yellow Shaking![/shake][/color]";

            await _advancedText.TypeTextAsync(advancedText, 0.1f);
        }
    }
}