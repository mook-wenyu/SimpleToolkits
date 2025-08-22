using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits.DialogueKit
{
    /// <summary>
    /// 标签解析引擎
    /// 负责解析文本中的标签并调用相应的处理器
    /// </summary>
    public class TagParsingEngine
    {
        private readonly TagProcessorRegistry _registry;

        public TagParsingEngine()
        {
            _registry = TagProcessorRegistry.Instance;
        }


        /// <summary>
        /// 解析文本中的方括号标签信息（使用 ReadOnlySpan 优化性能）
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>标签信息列表</returns>
        public List<TagInfo> ParseTags(string text)
        {
            var tags = new List<TagInfo>();
            if (string.IsNullOrEmpty(text)) return tags;

            var span = text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                // 只解析方括号标签 [ ]
                if (span[i] == '[')
                {
                    var tagInfo = ParseSquareBracketTag(span, i);
                    if (tagInfo.length > 0)
                    {
                        tags.Add(tagInfo);
                        i = tagInfo.endIndex; // 跳过已解析的标签
                    }
                }
            }

            return tags;
        }


        /// <summary>
        /// 解析方括号标签（使用 ReadOnlySpan 优化性能）
        /// </summary>
        private TagInfo ParseSquareBracketTag(ReadOnlySpan<char> text, int startIndex)
        {
            var endIndex = text.Slice(startIndex).IndexOf(']');
            if (endIndex == -1) return default;
            
            endIndex += startIndex; // 转为绝对位置
            
            var fullTagSpan = text.Slice(startIndex, endIndex - startIndex + 1);
            var contentSpan = text.Slice(startIndex + 1, endIndex - startIndex - 1);
            
            var isClosing = contentSpan.Length > 0 && contentSpan[0] == '/';
            var tagNameSpan = isClosing ? contentSpan.Slice(1) : contentSpan;
            
            var parameter = "";
            var tagName = "";
            
            // 解析参数
            var equalIndex = tagNameSpan.IndexOf('=');
            if (equalIndex != -1 && !isClosing)
            {
                parameter = tagNameSpan.Slice(equalIndex + 1).ToString();
                tagName = tagNameSpan.Slice(0, equalIndex).ToString();
            }
            else
            {
                tagName = tagNameSpan.ToString();
            }
            
            var fullTag = fullTagSpan.ToString();
            return new TagInfo(tagName, parameter, fullTag, startIndex, endIndex, isClosing);
        }

        /// <summary>
        /// 预处理文本，移除应该被移除的标签
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>预处理后的文本</returns>
        public string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var tags = ParseTags(text);
            var result = new StringBuilder(text.Length);
            var lastIndex = 0;

            foreach (var tag in tags)
            {
                var processor = _registry.GetProcessor(tag.tagName);
                
                if (processor != null && processor.ShouldRemoveFromDisplayText(text, tag.startIndex))
                {
                    // 添加标签前的文本
                    result.Append(text, lastIndex, tag.startIndex - lastIndex);
                    lastIndex = tag.endIndex + 1;
                }
            }

            // 添加剩余文本
            if (lastIndex < text.Length)
            {
                result.Append(text, lastIndex, text.Length - lastIndex);
            }

            return result.ToString();
        }

        /// <summary>
        /// 处理文本中的标签（同步版本）
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <param name="tagInfo">标签信息</param>
        /// <returns>处理结果</returns>
        public TagProcessResult ProcessTag(TagProcessContext context, TagInfo tagInfo)
        {
            var processor = _registry.GetProcessor(tagInfo.tagName);
            if (processor == null)
            {
                Debug.LogWarning($"No processor found for tag: {tagInfo.fullTag}");
                return new TagProcessResult(false, 0);
            }


            try
            {
                if (tagInfo.isClosingTag && processor.RequiresClosingTag)
                {
                    return processor.ProcessClosingTag(context);
                }
                else
                {
                    return processor.ProcessTag(context, tagInfo.parameter);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing tag {tagInfo.fullTag}: {ex.Message}");
                return new TagProcessResult(false, 0);
            }
        }

        /// <summary>
        /// 处理文本中的标签（异步版本）
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <param name="tagInfo">标签信息</param>
        /// <returns>异步处理结果</returns>
        public async UniTask<TagProcessResult> ProcessTagAsync(TagProcessContext context, TagInfo tagInfo)
        {
            var processor = _registry.GetProcessor(tagInfo.tagName);
            if (processor == null)
            {
                Debug.LogWarning($"No processor found for tag: {tagInfo.fullTag}");
                return new TagProcessResult(false, 0);
            }


            try
            {
                // 优先使用异步处理器
                if (processor is IAsyncTagProcessor asyncProcessor)
                {
                    if (tagInfo.isClosingTag && processor.RequiresClosingTag)
                    {
                        return processor.ProcessClosingTag(context);
                    }
                    else
                    {
                        return await asyncProcessor.ProcessTagAsync(context, tagInfo.parameter);
                    }
                }
                else
                {
                    // 回退到同步处理
                    return ProcessTag(context, tagInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing tag {tagInfo.fullTag}: {ex.Message}");
                return new TagProcessResult(false, 0);
            }
        }

        /// <summary>
        /// 验证文本中的标签
        /// </summary>
        /// <param name="text">要验证的文本</param>
        /// <returns>验证结果</returns>
        public TagValidationResult ValidateText(string text)
        {
            var result = new TagValidationResult { isValid = true };
            
            if (string.IsNullOrEmpty(text))
                return result;

            var tags = ParseTags(text);
            var tagStack = new Stack<TagInfo>();

            foreach (var tag in tags)
            {
                var processor = _registry.GetProcessor(tag.tagName);
                
                // 检查处理器是否存在
                if (processor == null)
                {
                    result.errors.Add($"Unknown tag: {tag.fullTag}");
                    result.isValid = false;
                    continue;
                }

                // 检查标签配对
                if (processor.RequiresClosingTag)
                {
                    if (tag.isClosingTag)
                    {
                        if (tagStack.Count == 0 || tagStack.Peek().tagName != tag.tagName)
                        {
                            result.errors.Add($"Mismatched closing tag: {tag.fullTag}");
                            result.isValid = false;
                        }
                        else
                        {
                            tagStack.Pop();
                        }
                    }
                    else
                    {
                        tagStack.Push(tag);
                    }
                }
            }

            // 检查未闭合的标签
            if (tagStack.Count > 0)
            {
                foreach (var unclosedTag in tagStack)
                {
                    result.errors.Add($"Unclosed tag: {unclosedTag.fullTag}");
                }
                result.isValid = false;
            }

            return result;
        }


        /// <summary>
        /// 获取文本中使用的所有标签名称
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>标签名称集合</returns>
        public HashSet<string> GetUsedTags(string text)
        {
            var usedTags = new HashSet<string>();
            var tags = ParseTags(text);
            
            foreach (var tag in tags)
            {
                usedTags.Add($"{tag.tagName}");
            }
            
            return usedTags;
        }
    }

    /// <summary>
    /// 标签验证结果
    /// </summary>
    public class TagValidationResult
    {
        public bool isValid;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Validation Result: {(isValid ? "Valid" : "Invalid")}");
            
            if (errors.Count > 0)
            {
                sb.AppendLine("Errors:");
                foreach (var error in errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }
            
            if (warnings.Count > 0)
            {
                sb.AppendLine("Warnings:");
                foreach (var warning in warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }
            
            return sb.ToString();
        }
    }
}