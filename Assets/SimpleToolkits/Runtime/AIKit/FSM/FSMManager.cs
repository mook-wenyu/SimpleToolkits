using UnityEngine;
using System.Collections.Generic;

namespace SimpleToolkits
{
    /// <summary>
    /// 全局 FSM 管理器（不依赖 MonoBehaviour）
    /// - 统一调度所有实现了 IFSMUpdatable 的对象。
    /// - 提供外部可调用的 Tick/LateTick/FixedTick 接口，可由任意时间源驱动；
    /// - 提供使用 UnityEngine.Time 的便捷无参重载；
    /// - 非线程安全，需在主线程调用。
    /// </summary>
    public class FSMManager
    {
        // 主列表：当前被调度的 Updatable 集合
        private readonly List<IFSMUpdatable> _updatables = new(64);
        // 待添加/移除缓冲，避免遍历期间修改集合
        private readonly List<IFSMUpdatable> _pendingAdd = new(16);
        private readonly List<IFSMUpdatable> _pendingRemove = new(16);
        // 标记当前是否在 Tick 中，防止遍历期修改
        private bool _isTicking;

        /// <summary>
        /// 当前被调度对象数量。
        /// </summary>
        public int Count => _updatables.Count;

        /// <summary>
        /// 注册一个需要被 FSMManager 调度的对象。
        /// </summary>
        public void Register(IFSMUpdatable updatable)
        {
            if (updatable == null) return;
            if (_isTicking)
            {
                if (!_pendingAdd.Contains(updatable)) _pendingAdd.Add(updatable);
                return;
            }
            if (!_updatables.Contains(updatable)) _updatables.Add(updatable);
        }

        /// <summary>
        /// 反注册一个对象，停止被调度。
        /// </summary>
        public void Unregister(IFSMUpdatable updatable)
        {
            if (updatable == null) return;
            if (_isTicking)
            {
                if (!_pendingRemove.Contains(updatable)) _pendingRemove.Add(updatable);
                return;
            }
            _updatables.Remove(updatable);
            _pendingAdd.Remove(updatable);
        }

        /// <summary>
        /// 清空所有调度对象（谨慎使用）。
        /// </summary>
        public void Clear()
        {
            if (_isTicking)
            {
                foreach (var u in _updatables)
                {
                    if (u == null) continue;
                    if (!_pendingRemove.Contains(u)) _pendingRemove.Add(u);
                }
                return;
            }
            _updatables.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }

        /// <summary>
        /// 外部驱动的帧更新（使用提供的 deltaTime）。
        /// </summary>
        public void Tick(float deltaTime)
        {
            _isTicking = true;
            var list = _updatables;
            foreach (var u in list)
            {
                u?.Tick(deltaTime);
            }
            _isTicking = false;
            FlushPending();
        }

        /// <summary>
        /// 外部驱动的 LateUpdate。
        /// </summary>
        public void LateTick()
        {
            _isTicking = true;
            var list = _updatables;
            foreach (var u in list)
            {
                u?.LateTick();
            }
            _isTicking = false;
            FlushPending();
        }

        /// <summary>
        /// 外部驱动的物理帧更新（使用提供的 fixedDeltaTime）。
        /// </summary>
        public void FixedTick(float fixedDeltaTime)
        {
            _isTicking = true;
            var list = _updatables;
            foreach (var u in list)
            {
                u?.FixedTick(fixedDeltaTime);
            }
            _isTicking = false;
            FlushPending();
        }

        /// <summary>
        /// 便捷重载：使用 UnityEngine.Time.deltaTime 调用 Tick。
        /// </summary>
        public void Tick()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// 便捷重载：使用 UnityEngine.Time.fixedDeltaTime 调用 FixedTick。
        /// </summary>
        public void FixedTick()
        {
            FixedTick(Time.fixedDeltaTime);
        }

        private void FlushPending()
        {
            // 先处理移除，避免刚加入又被移除的边界问题
            if (_pendingRemove.Count > 0)
            {
                foreach (var t in _pendingRemove)
                {
                    _updatables.Remove(t);
                }
                _pendingRemove.Clear();
            }

            if (_pendingAdd.Count > 0)
            {
                foreach (var u in _pendingAdd)
                {
                    if (u == null) continue;
                    if (!_updatables.Contains(u)) _updatables.Add(u);
                }
                _pendingAdd.Clear();
            }
        }

        /// <summary>
        /// 释放资源：清空调度列表。
        /// </summary>
        public void Dispose()
        {
            // 这里不抛异常，仅做安全清理
            if (_isTicking)
            {
                // 若正在 Tick，标记清空，下一帧 Flush 时会被清掉
                foreach (var u in _updatables)
                {
                    if (u == null) continue;
                    if (!_pendingRemove.Contains(u)) _pendingRemove.Add(u);
                }
                return;
            }
            Clear();
        }
    }
}
