namespace SimpleToolkits
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 高性能对象池 - 专为ScrollView优化
    /// </summary>
    public class ScrollCellPool
    {
        private readonly Queue<RectTransform> _pool = new Queue<RectTransform>();
        private readonly RectTransform _prefab;
        private readonly Transform _parent;
        private readonly IScrollAdapter _adapter;
        private readonly int _maxPoolSize;

        public int PoolCount => _pool.Count;

        public ScrollCellPool(RectTransform prefab, Transform parent, IScrollAdapter adapter, int maxPoolSize = 50)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _maxPoolSize = maxPoolSize;
        }

        /// <summary>获取Cell（从池中取出或创建新的）</summary>
        public RectTransform Get()
        {
            RectTransform cell;

            if (_pool.Count > 0)
            {
                cell = _pool.Dequeue();
            }
            else
            {
                cell = CreateNewCell();
            }

            if (cell != null)
            {
                cell.gameObject.SetActive(true);
            }

            return cell;
        }

        /// <summary>归还Cell到池中</summary>
        public void Return(RectTransform cell, int index)
        {
            if (cell == null) return;

            // 回调适配器
            _adapter.OnCellRecycled(index, cell);

            // 隐藏Cell
            cell.gameObject.SetActive(false);

            // 归还到池中或销毁
            if (_pool.Count < _maxPoolSize)
            {
                _pool.Enqueue(cell);
            }
            else
            {
                DestroyCell(cell);
            }
        }

        /// <summary>预热池（创建初始Cell）</summary>
        public void Prewarm(int count)
        {
            count = Mathf.Min(count, _maxPoolSize);
            
            for (int i = 0; i < count; i++)
            {
                var cell = CreateNewCell();
                if (cell != null)
                {
                    cell.gameObject.SetActive(false);
                    _pool.Enqueue(cell);
                }
            }
        }

        /// <summary>清空池</summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var cell = _pool.Dequeue();
                DestroyCell(cell);
            }
        }

        private RectTransform CreateNewCell()
        {
            if (_prefab == null) return null;

            var cell = GameObject.Instantiate(_prefab, _parent);
            _adapter.OnCellCreated(cell);
            return cell;
        }

        private void DestroyCell(RectTransform cell)
        {
            if (cell != null && cell.gameObject != null)
            {
                GameObject.Destroy(cell.gameObject);
            }
        }
    }

    /// <summary>
    /// 活跃Cell管理器 - 管理当前显示的Cell
    /// </summary>
    public class ActiveCellManager
    {
        private readonly Dictionary<int, RectTransform> _activeCells = new Dictionary<int, RectTransform>();
        private readonly ScrollCellPool _pool;
        private readonly IScrollAdapter _adapter;

        public int ActiveCount => _activeCells.Count;

        public ActiveCellManager(ScrollCellPool pool, IScrollAdapter adapter)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        /// <summary>获取或创建指定索引的Cell</summary>
        public RectTransform GetOrCreateCell(int index)
        {
            // 如果已存在，直接返回
            if (_activeCells.TryGetValue(index, out var existingCell))
            {
                return existingCell;
            }

            // 从池中获取新Cell
            var cell = _pool.Get();
            if (cell == null) return null;

            // 绑定数据
            _adapter.BindCell(index, cell);

            // 记录到活跃列表
            _activeCells[index] = cell;

            return cell;
        }

        /// <summary>回收指定索引的Cell</summary>
        public void RecycleCell(int index)
        {
            if (_activeCells.TryGetValue(index, out var cell))
            {
                _activeCells.Remove(index);
                _pool.Return(cell, index);
            }
        }

        /// <summary>回收范围外的Cell</summary>
        public void RecycleOutsideRange(int firstVisible, int lastVisible)
        {
            var toRemove = new List<int>();

            foreach (var kvp in _activeCells)
            {
                var index = kvp.Key;
                if (index < firstVisible || index > lastVisible)
                {
                    toRemove.Add(index);
                }
            }

            foreach (var index in toRemove)
            {
                RecycleCell(index);
            }
        }

        /// <summary>获取指定索引的Cell（如果存在）</summary>
        public RectTransform GetCell(int index)
        {
            _activeCells.TryGetValue(index, out var cell);
            return cell;
        }

        /// <summary>清空所有活跃Cell</summary>
        public void Clear()
        {
            var indices = new List<int>(_activeCells.Keys);
            foreach (var index in indices)
            {
                RecycleCell(index);
            }
        }

        /// <summary>获取所有活跃的索引</summary>
        public IEnumerable<int> GetActiveIndices()
        {
            return _activeCells.Keys;
        }
    }
}