using System;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 生产可用的基础统一尺寸适配器基类（非示例代码）。
    /// - 通过继承并实现抽象方法，快速对接 ScrollView
    /// - 支持运行期覆盖数量、可选的回收/创建生命周期回调
    /// - 仅依赖 IScrollAdapter/IRecyclableScrollAdapter 接口
    /// </summary>
    public abstract class BaseScrollAdapter : IScrollAdapter, IRecyclableScrollAdapter
    {
        // 预制体（必须提供）
        private readonly RectTransform _prefab;
        // 数量获取委托（若未提供则使用 GetCount()）
        private readonly Func<int> _countGetter;
        // 运行期覆盖数量（<0 表示未覆盖）
        private int _override = -1;

        protected BaseScrollAdapter(RectTransform prefab, Func<int> countGetter = null)
        {
            _prefab = prefab;
            _countGetter = countGetter;
        }

        /// <summary>必须实现：绑定索引到 Cell。</summary>
        protected abstract void OnBind(int index, RectTransform cell);
        /// <summary>可选：Cell 首次实例化时。</summary>
        protected virtual void OnCreated(RectTransform cell) { }
        /// <summary>可选：Cell 回收时（可清理监听/动画）。</summary>
        protected virtual void OnRecycled(int index, RectTransform cell) { }
        /// <summary>可重写：当未提供 countGetter 时，从派生类返回数量。</summary>
        protected virtual int GetCount() => 0;

        //================ IScrollAdapter ================
        public int Count => _override >= 0 ? _override : (_countGetter != null ? _countGetter() : Mathf.Max(0, GetCount()));
        public void OverrideCount(int count) { _override = count; }
        public RectTransform GetCellPrefab() => _prefab;
        public void BindCell(int index, RectTransform cell) => OnBind(index, cell);

        //================ IRecyclableScrollAdapter ================
        public void OnCellCreated(RectTransform cell) => OnCreated(cell);
        public void OnCellRecycled(int index, RectTransform cell) => OnRecycled(index, cell);
    }

    /// <summary>
    /// 生产可用的基础变尺寸适配器基类（非示例代码）。
    /// - 仅在单列/单行布局下使用（ConstraintCount==1）
    /// - 实现 GetItemSize 以返回每项尺寸（sizeDelta 语义）
    /// </summary>
    public abstract class BaseVariableSizeAdapter : BaseScrollAdapter, IVariableSizeAdapter
    {
        protected BaseVariableSizeAdapter(RectTransform prefab, Func<int> countGetter = null)
            : base(prefab, countGetter)
        {
        }

        /// <summary>
        /// 必须实现：返回指定项在当前布局/视口下的尺寸。
        /// 注意：主轴尺寸用于虚拟化累计，跨轴尺寸通常由布局控制。
        /// </summary>
        public abstract Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
    }
}
