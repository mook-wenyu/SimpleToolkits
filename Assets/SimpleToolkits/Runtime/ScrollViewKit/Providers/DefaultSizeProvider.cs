namespace SimpleToolkits
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 生产可用的变尺寸提供者。
    /// - 支持常量主轴尺寸或通过委托按索引计算主轴尺寸。
    /// - 自动根据布局与视口推导跨轴尺寸（当布局控制跨轴尺寸时），否则使用可配置常量。
    /// - 仅主轴尺寸参与虚拟化累计；跨轴尺寸仅用于最终渲染 sizeDelta（控制器在需要时会覆盖跨轴）。
    /// </summary>
    public sealed class DefaultSizeProvider : ISizeProvider
    {
        // 主轴尺寸策略：常量或委托
        private readonly bool _useConstant;
        private readonly float _constantMain;
        private readonly Func<int, float> _mainGetter;
        private readonly float _minMain;
        private readonly float _maxMain;

        // 跨轴尺寸（当布局未控制跨轴时使用；为负数表示使用默认推导）
        private readonly float _crossOverride;

        /// <summary>
        /// 使用常量主轴尺寸的构造。
        /// 适合大多数仅主轴变更不频繁的场景，跨轴尺寸可选常量。
        /// </summary>
        /// <param name="constantMain">主轴固定尺寸（垂直=高度；水平=宽度）</param>
        /// <param name="crossOverride">跨轴固定尺寸；传负值表示自动推导</param>
        public DefaultSizeProvider(float constantMain, float crossOverride = -1f)
        {
            _useConstant = true;
            _constantMain = Mathf.Max(0f, constantMain);
            _mainGetter = null;
            _minMain = _constantMain;
            _maxMain = _constantMain;
            _crossOverride = crossOverride;
        }

        /// <summary>
        /// 使用委托动态计算主轴尺寸的构造，并限定最小/最大值。
        /// </summary>
        /// <param name="mainGetter">返回主轴尺寸的委托（高频调用，应无分配且快速）</param>
        /// <param name="minMain">主轴最小尺寸（用于裁剪）</param>
        /// <param name="maxMain">主轴最大尺寸（用于裁剪）</param>
        /// <param name="crossOverride">跨轴固定尺寸；传负值表示自动推导</param>
        public DefaultSizeProvider(Func<int, float> mainGetter, float minMain, float maxMain, float crossOverride = -1f)
        {
            if (mainGetter == null) throw new ArgumentNullException(nameof(mainGetter));
            _useConstant = false;
            _constantMain = 0f;
            _mainGetter = mainGetter;
            _minMain = Mathf.Max(0f, Math.Min(minMain, maxMain));
            _maxMain = Mathf.Max(0f, Math.Max(minMain, maxMain));
            _crossOverride = crossOverride;
        }

        /// <summary>
        /// 根据布局与视口返回该索引下的 sizeDelta。
        /// 注意：
        /// - 主轴尺寸用于虚拟化累计，应保持稳定且可快速获取。
        /// - 跨轴尺寸在布局控制跨轴大小时会被框架覆盖为内容有效尺寸。
        /// </summary>
        public Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout)
        {
            // 计算主轴尺寸
            float main;
            if (_useConstant)
            {
                main = _constantMain;
            }
            else
            {
                // 业务方提供快速委托，避免 GC 与重计算
                main = Mathf.Clamp(_mainGetter(index), _minMain, _maxMain);
            }

            // 自动推导跨轴尺寸（当布局控制跨轴时，控制器会覆盖；此处仅提供合理默认）
            float cross;
            if (_crossOverride >= 0f)
            {
                cross = _crossOverride;
            }
            else
            {
                if (layout.IsVertical)
                {
                    // 纵向列表：跨轴为宽度
                    if (layout.ControlChildWidth)
                    {
                        cross = Mathf.Max(0f, viewportSize.x - layout.Padding.left - layout.Padding.right);
                    }
                    else
                    {
                        // 给一个保守的默认宽度（可被预制体/外部 LayoutElement 覆盖）
                        cross = Mathf.Max(0f, viewportSize.x * 0.5f);
                    }
                }
                else
                {
                    // 横向列表：跨轴为高度
                    if (layout.ControlChildHeight)
                    {
                        cross = Mathf.Max(0f, viewportSize.y - layout.Padding.top - layout.Padding.bottom);
                    }
                    else
                    {
                        cross = Mathf.Max(0f, viewportSize.y * 0.5f);
                    }
                }
            }

            // 组合为 sizeDelta
            if (layout.IsVertical)
            {
                return new Vector2(cross, main);
            }
            else
            {
                return new Vector2(main, cross);
            }
        }
    }
}
