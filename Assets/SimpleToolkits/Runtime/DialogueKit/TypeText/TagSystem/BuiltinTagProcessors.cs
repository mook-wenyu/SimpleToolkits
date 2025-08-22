using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits.DialogueKit
{
    /// <summary>
    /// 速度标签处理器 [speed=0.1]
    /// </summary>
    public class SpeedTagProcessor : BaseTagProcessor
    {
        public override string TagName => "speed";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var speed = ParseParameter<float>(tagContent, 0.05f);
            context.currentSpeed = speed;
            
            return new TagProcessResult(true, 0, true); // 不显示字符，跳过显示
        }
    }

    /// <summary>
    /// 暂停标签处理器 [pause=1.0]
    /// </summary>
    public class PauseTagProcessor : BaseTagProcessor, IAsyncTagProcessor
    {
        public override string TagName => "pause";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 同步版本，返回未处理，让异步版本处理
            return new TagProcessResult(false, 0);
        }

        public async UniTask<TagProcessResult> ProcessTagAsync(TagProcessContext context, string tagContent)
        {
            var duration = ParseParameter<float>(tagContent, 1.0f);
            
            if (duration > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
            }
            
            return new TagProcessResult(true, 0, true);
        }
    }

    /// <summary>
    /// 清屏标签处理器 [clear]
    /// </summary>
    public class ClearTagProcessor : BaseTagProcessor
    {
        public override string TagName => "clear";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 清空已显示的文本
            if (context.typeTextComponent != null)
            {
                context.typeTextComponent.GetComponent<TMPro.TextMeshProUGUI>().text = "";
            }
            
            return new TagProcessResult(true, 0, true);
        }
    }

    /// <summary>
    /// 换行标签处理器 [br] 或 [n]
    /// </summary>
    public class LineBreakTagProcessor : BaseTagProcessor
    {
        public override string TagName => "br";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 返回换行符，会被添加到显示文本中
            return new TagProcessResult(true, 1, false, "\n");
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true; // 从显示文本中移除，但在处理时会添加换行符
        }
    }

    /// <summary>
    /// 新行标签处理器 [n] - 换行的别名
    /// </summary>
    public class NewLineTagProcessor : LineBreakTagProcessor
    {
        public override string TagName => "n";
    }

    /// <summary>
    /// 颜色标签处理器 [color=red] 和 [/color]
    /// 转换为 TextMeshPro 的 <color> 标签
    /// </summary>
    public class ColorTagProcessor : BaseTagProcessor
    {
        public override string TagName => "color";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var colorTag = $"<color={tagContent}>";
            return new TagProcessResult(true, colorTag.Length, false, colorTag);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</color>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true; // 移除方括号版本，替换为角括号版本
        }
    }

    /// <summary>
    /// 字体大小标签处理器 [size=20] 和 [/size]
    /// </summary>
    public class SizeTagProcessor : BaseTagProcessor
    {
        public override string TagName => "size";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var sizeTag = $"<size={tagContent}>";
            return new TagProcessResult(true, sizeTag.Length, false, sizeTag);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</size>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true;
        }
    }

    /// <summary>
    /// 粗体标签处理器 [b] 和 [/b]
    /// </summary>
    public class BoldTagProcessor : BaseTagProcessor
    {
        public override string TagName => "b";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var boldTag = "<b>";
            return new TagProcessResult(true, boldTag.Length, false, boldTag);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</b>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true;
        }
    }

    /// <summary>
    /// 斜体标签处理器 [i] 和 [/i]
    /// </summary>
    public class ItalicTagProcessor : BaseTagProcessor
    {
        public override string TagName => "i";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var italicTag = "<i>";
            return new TagProcessResult(true, italicTag.Length, false, italicTag);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</i>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true;
        }
    }

    /// <summary>
    /// 下划线标签处理器 [u] 和 [/u]
    /// </summary>
    public class UnderlineTagProcessor : BaseTagProcessor
    {
        public override string TagName => "u";
        public override bool RequiresClosingTag => true;

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var underlineTag = "<u>";
            return new TagProcessResult(true, underlineTag.Length, false, underlineTag);
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            var closeTag = "</u>";
            return new TagProcessResult(true, closeTag.Length, false, closeTag);
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true;
        }
    }

    /// <summary>
    /// 动作标签处理器 [action=id]
    /// 支持通过全局委托调用自定义动作
    /// </summary>
    public class ActionTagProcessor : BaseTagProcessor
    {
        /// <summary>
        /// 全局动作委托，可以注册自定义动作处理器
        /// </summary>
        public static event Action<string> OnAction;
        
        public override string TagName => "action";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            // 调用全局动作委托
            OnAction?.Invoke(tagContent);
            return new TagProcessResult(true, 0, true);
        }
        
        /// <summary>
        /// 清空所有动作监听器（用于清理资源）
        /// </summary>
        public static void ClearAllActions()
        {
            OnAction = null;
        }
    }

    /// <summary>
    /// 速度区域标签处理器 [speedregion=0.1]文本[/speedregion]
    /// 在指定区域内使用特定速度，区域结束后恢复之前的速度
    /// </summary>
    public class SpeedRegionTagProcessor : BaseTagProcessor
    {
        public override string TagName => "speedregion";
        public override bool RequiresClosingTag => true;

        private const string SPEED_STACK_KEY = "__speedStack";

        public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
        {
            var newSpeed = ParseParameter<float>(tagContent, 0.05f);
            
            // 获取或创建速度栈
            if (!context.variables.ContainsKey(SPEED_STACK_KEY))
            {
                context.variables[SPEED_STACK_KEY] = new System.Collections.Generic.Stack<float>();
            }
            
            var speedStack = (System.Collections.Generic.Stack<float>)context.variables[SPEED_STACK_KEY];
            
            // 保存当前速度到栈中
            speedStack.Push(context.currentSpeed);
            
            // 设置新速度
            context.currentSpeed = newSpeed;
            
            return new TagProcessResult(true, 0, true); // 处理成功，跳过显示
        }

        public override TagProcessResult ProcessClosingTag(TagProcessContext context)
        {
            // 获取速度栈
            if (context.variables.TryGetValue(SPEED_STACK_KEY, out var stackObj) && 
                stackObj is System.Collections.Generic.Stack<float> speedStack && 
                speedStack.Count > 0)
            {
                // 恢复之前的速度
                context.currentSpeed = speedStack.Pop();
            }
            else
            {
                Debug.LogWarning("SpeedRegion closing tag found without matching opening tag");
            }
            
            return new TagProcessResult(true, 0, true); // 处理成功，跳过显示
        }

        public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
        {
            return true; // 从显示文本中移除
        }
    }

}