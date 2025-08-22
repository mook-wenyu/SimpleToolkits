using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SimpleToolkits.DialogueKit
{
    /// <summary>
    /// TextMeshProUGUI 扩展方法
    /// 提供便捷的打字机效果调用接口
    /// </summary>
    public static class TypeTextExtensions
    {
        /// <summary>
        /// 开始打字机效果
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <param name="text">要显示的文本</param>
        /// <param name="speed">打字速度</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>UniTask</returns>
        public static async UniTask TypeTextAsync(this TextMeshProUGUI textComponent, string text,
            float speed = 0.05f, Action onComplete = null)
        {
            var typeText = textComponent.GetOrAddTypeTextComponent();
            await typeText.StartTypingAsync(text, speed, onComplete);
        }

        /// <summary>
        /// 跳过当前打字效果
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <returns>是否成功跳过</returns>
        public static async UniTask<bool> SkipTypeTextAsync(this TextMeshProUGUI textComponent)
        {
            var typeText = textComponent.GetComponent<TypeTextComponent>();
            if (typeText != null && typeText.IsSkippable)
            {
                await typeText.SkipTypingAsync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否可以跳过打字效果
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <returns>是否可跳过</returns>
        public static bool IsTypeTextSkippable(this TextMeshProUGUI textComponent)
        {
            var typeText = textComponent.GetComponent<TypeTextComponent>();
            return typeText != null && typeText.IsSkippable;
        }

        /// <summary>
        /// 检查是否正在打字
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <returns>是否正在打字</returns>
        public static bool IsTypeTextTyping(this TextMeshProUGUI textComponent)
        {
            var typeText = textComponent.GetComponent<TypeTextComponent>();
            return typeText != null && typeText.IsTyping;
        }

        /// <summary>
        /// 停止打字效果
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <returns>UniTask</returns>
        public static async UniTask StopTypeTextAsync(this TextMeshProUGUI textComponent)
        {
            var typeText = textComponent.GetComponent<TypeTextComponent>();
            if (typeText != null)
            {
                await typeText.StopTypingAsync();
            }
        }

        /// <summary>
        /// 立即设置文本（无动画）
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <param name="text">要设置的文本</param>
        public static void SetTextInstant(this TextMeshProUGUI textComponent, string text)
        {
            var typeText = textComponent.GetComponent<TypeTextComponent>();
            if (typeText != null)
                typeText.SetTextInstant(text);
            else
                textComponent.text = text;
        }


        /// <summary>
        /// 验证文本格式
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <param name="text">要验证的文本</param>
        /// <returns>验证结果</returns>
        public static TagValidationResult ValidateTypeText(this TextMeshProUGUI textComponent, string text)
        {
            var typeText = textComponent.GetOrAddTypeTextComponent();
            return typeText.ValidateText(text);
        }

        /// <summary>
        /// 获取或添加打字机组件
        /// </summary>
        /// <param name="textComponent">TextMeshProUGUI 组件</param>
        /// <returns>TypeTextComponent 实例</returns>
        public static TypeTextComponent GetOrAddTypeTextComponent(this TextMeshProUGUI textComponent)
        {
            var typeText = textComponent.GetComponent<TypeTextComponent>();
            if (typeText == null)
            {
                typeText = textComponent.gameObject.AddComponent<TypeTextComponent>();
            }
            return typeText;
        }
    }

    /// <summary>
    /// 打字机工具类
    /// 提供静态工具方法和标签处理功能
    /// </summary>
    public static class TypeTextUtility
    {
        /// <summary>
        /// 注册自定义标签处理器
        /// </summary>
        /// <param name="processor">标签处理器</param>
        public static void RegisterTagProcessor(ITagProcessor processor)
        {
            TagProcessorRegistry.Instance.RegisterProcessor(processor);
        }

        /// <summary>
        /// 批量注册标签处理器
        /// </summary>
        /// <param name="processors">标签处理器数组</param>
        public static void RegisterTagProcessors(params ITagProcessor[] processors)
        {
            TagProcessorRegistry.Instance.RegisterProcessors(processors);
        }

        /// <summary>
        /// 注销标签处理器
        /// </summary>
        /// <param name="tagName">标签名称</param>
        public static void UnregisterTagProcessor(string tagName)
        {
            TagProcessorRegistry.Instance.UnregisterProcessor(tagName);
        }

        /// <summary>
        /// 检查标签是否已注册
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>是否已注册</returns>
        public static bool HasTagProcessor(string tagName)
        {
            return TagProcessorRegistry.Instance.HasProcessor(tagName);
        }

        /// <summary>
        /// 获取已注册的标签名称列表
        /// </summary>
        /// <returns>标签名称数组</returns>
        public static string[] GetRegisteredTagNames()
        {
            return TagProcessorRegistry.Instance.GetRegisteredTagNames();
        }

        /// <summary>
        /// 验证文本格式
        /// </summary>
        /// <param name="text">要验证的文本</param>
        /// <returns>验证结果</returns>
        public static TagValidationResult ValidateTextFormat(string text)
        {
            var engine = new TagParsingEngine();
            return engine.ValidateText(text);
        }

        /// <summary>
        /// 清理文本，移除控制标签但保留样式标签
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>清理后的文本</returns>
        public static string CleanText(string text)
        {
            var engine = new TagParsingEngine();
            return engine.PreprocessText(text);
        }

        /// <summary>
        /// 获取文本中使用的标签
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>使用的标签集合</returns>
        public static System.Collections.Generic.HashSet<string> GetUsedTags(string text)
        {
            var engine = new TagParsingEngine();
            return engine.GetUsedTags(text);
        }

        /// <summary>
        /// 获取标签统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public static string GetTagStatistics()
        {
            return TagProcessorRegistry.Instance.GetStatistics();
        }

        /// <summary>
        /// 重置到内置标签
        /// </summary>
        public static void ResetToBuiltinTags()
        {
            TagProcessorRegistry.Instance.ResetToBuiltins();
        }

        /// <summary>
        /// 清空所有标签处理器
        /// </summary>
        public static void ClearAllTagProcessors()
        {
            TagProcessorRegistry.Instance.Clear();
        }
    }

    /// <summary>
    /// 自定义标签处理器示例
    /// 开发者可以参考此类创建自定义标签
    /// </summary>
    public class CustomTagProcessorExample : BaseTagProcessor
    {
        public override string TagName => "example";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 示例：处理 [example=parameter] 标签
            Debug.Log($"Processing example tag with parameter: {tagContent}");

            // 返回处理结果
            return new TagProcessResult(true, 0, true); // 处理成功，跳过显示
        }

        // 使用示例：
        // TypeTextUtility.RegisterTagProcessor(new CustomTagProcessorExample());
        // 然后在文本中使用 [example=hello] 标签
    }
}