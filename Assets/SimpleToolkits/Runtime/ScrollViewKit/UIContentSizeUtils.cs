namespace SimpleToolkits
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;

    /// <summary>
    /// UI 内容尺寸测量工具（无分配或极少分配）。
    /// - 提供基于 TMP_Text 与 Text 的首选尺寸计算
    /// - 不修改传入组件的 text，使用 GetPreferredValues 计算
    /// </summary>
    public static class UIContentSizeUtils
    {
        /// <summary>
        /// 基于 TMP_Text 模板计算指定文本在给定宽度约束下的首选高度（返回尺寸：x=宽，y=高）。
        /// 注意：会使用模板的字体、字号、行距、自动换行等参数。
        /// </summary>
        public static Vector2 GetTMPPreferredSize(TMP_Text template, string text, float widthConstraint)
        {
            if (template == null) return new Vector2(widthConstraint, 0f);
            // TMP 的首选值计算：传入期望宽度与无穷高，返回合适的宽高
            var pref = template.GetPreferredValues(text ?? string.Empty, widthConstraint, Mathf.Infinity);
            // x 近似为 widthConstraint（可能会略小），这里用传入值作为最终宽度
            return new Vector2(widthConstraint, Mathf.Max(0f, pref.y));
        }

        /// <summary>
        /// 基于 UnityEngine.UI.Text 模板计算指定文本在给定宽度约束下的首选高度（返回尺寸：x=宽，y=高）。
        /// </summary>
        public static Vector2 GetTextPreferredSize(Text template, string text, float widthConstraint)
        {
            if (template == null) return new Vector2(widthConstraint, 0f);
            var gen = new TextGenerator();
            var settings = template.GetGenerationSettings(new Vector2(widthConstraint, Mathf.Infinity));
            gen.Populate(text ?? string.Empty, settings);
            var height = gen.rectExtents.size.y;
            return new Vector2(widthConstraint, Mathf.Max(0f, height));
        }
    }
}
