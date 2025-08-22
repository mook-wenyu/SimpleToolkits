using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits.DialogueKit
{
    /// <summary>
    /// 标签处理结果
    /// </summary>
    public struct TagProcessResult
    {
        public bool isProcessed;
        public int charactersConsumed;
        public bool shouldSkipDisplay;
        public object data;

        public TagProcessResult(bool isProcessed, int charactersConsumed, bool shouldSkipDisplay = false, object data = null)
        {
            this.isProcessed = isProcessed;
            this.charactersConsumed = charactersConsumed;
            this.shouldSkipDisplay = shouldSkipDisplay;
            this.data = data;
        }
    }

    /// <summary>
    /// 标签处理上下文
    /// </summary>
    public class TagProcessContext
    {
        public string text;
        public int currentIndex;
        public float currentSpeed;
        public TypeTextComponent typeTextComponent;
        public Dictionary<string, object> variables;

        public TagProcessContext(string text, int currentIndex, float currentSpeed, TypeTextComponent component)
        {
            this.text = text;
            this.currentIndex = currentIndex;
            this.currentSpeed = currentSpeed;
            this.typeTextComponent = component;
            this.variables = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 标签处理器接口
    /// </summary>
    public interface ITagProcessor
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        string TagName { get; }


        /// <summary>
        /// 是否需要闭合标签
        /// </summary>
        bool RequiresClosingTag { get; }

        /// <summary>
        /// 处理标签
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <param name="tagContent">标签内容（不包含括号）</param>
        /// <returns>处理结果</returns>
        TagProcessResult ProcessTag(TagProcessContext context, string tagContent);

        /// <summary>
        /// 处理闭合标签（如果需要）
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <returns>处理结果</returns>
        TagProcessResult ProcessClosingTag(TagProcessContext context);

        /// <summary>
        /// 预处理标签，用于从最终显示文本中移除
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="startIndex">标签开始位置</param>
        /// <returns>是否应该从显示文本中移除</returns>
        bool ShouldRemoveFromDisplayText(string text, int startIndex);
    }


    /// <summary>
    /// 标签信息
    /// </summary>
    public struct TagInfo
    {
        public string tagName;
        public string parameter;
        public string fullTag;
        public int startIndex;
        public int endIndex;
        public int length;
        public bool isClosingTag;

        public TagInfo(string tagName, string parameter, string fullTag, int startIndex, int endIndex, 
                      bool isClosingTag)
        {
            this.tagName = tagName;
            this.parameter = parameter;
            this.fullTag = fullTag;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.length = endIndex - startIndex + 1;
            this.isClosingTag = isClosingTag;
        }
    }

    /// <summary>
    /// 异步标签处理器接口（用于需要等待的标签，如暂停、延迟等）
    /// </summary>
    public interface IAsyncTagProcessor : ITagProcessor
    {
        /// <summary>
        /// 异步处理标签
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <param name="tagContent">标签内容</param>
        /// <returns>异步处理结果</returns>
        UniTask<TagProcessResult> ProcessTagAsync(TagProcessContext context, string tagContent);
    }

    /// <summary>
    /// 标签处理器基类
    /// </summary>
    public abstract class BaseTagProcessor : ITagProcessor
    {
        public abstract string TagName { get; }
        public virtual bool RequiresClosingTag => false;

        public abstract TagProcessResult ProcessTag(TagProcessContext context, string tagContent);

        public virtual TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            return new TagProcessResult(false, 0);
        }

        public virtual bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true; // 默认方括号标签从显示文本中移除
        }

        /// <summary>
        /// 解析参数值
        /// </summary>
        /// <param name="parameter">参数字符串</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>解析结果</returns>
        protected T ParseParameter<T>(string parameter, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(parameter))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(parameter, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 解析多个参数（用逗号分隔）
        /// </summary>
        /// <param name="parameter">参数字符串</param>
        /// <returns>参数数组</returns>
        protected string[] ParseMultipleParameters(string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return new string[0];

            return parameter.Split(',');
        }
    }
}