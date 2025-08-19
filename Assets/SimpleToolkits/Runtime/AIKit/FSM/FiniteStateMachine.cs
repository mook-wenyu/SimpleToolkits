using System.Collections.Generic;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 泛型有限状态机（高性能、无协程、可维护、可扩展）。
    /// - 使用 int Id 标识状态，避免字符串比较与 GC。
    /// - 每帧在 Tick 中进行迁移判定，按优先级选择最优迁移。
    /// - 不抛异常，所有方法均做空判与边界检查，确保运行期稳定。
    /// </summary>
    /// <typeparam name="TOwner">状态机持有者类型</typeparam>
    public sealed class FiniteStateMachine<TOwner> : IFSMUpdatable
    {
        // 持有者（强类型）
        private readonly TOwner _owner;

        // 状态表（Id -> 状态实例）
        private readonly Dictionary<int, IFSMState<TOwner>> _states = new(16);

        // 迁移表（FromId -> 多条迁移）
        private readonly Dictionary<int, List<IFSMTransition<TOwner>>> _transitions = new(16);

        // 当前状态
        private int _currentId = -1;
        private IFSMState<TOwner> _currentState;

        public FiniteStateMachine(TOwner owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 是否已经设置初始状态且处于运行中。
        /// </summary>
        public bool IsRunning => _currentState != null;

        /// <summary>
        /// 当前状态 Id（未运行返回 -1）。
        /// </summary>
        public int CurrentId => _currentId;

        /// <summary>
        /// 添加状态（重复 Id 将覆盖旧引用）。
        /// </summary>
        public void AddState(IFSMState<TOwner> state)
        {
            if (state == null) return;
            _states[state.Id] = state;
        }

        /// <summary>
        /// 批量添加状态。
        /// </summary>
        public void AddStates(IEnumerable<IFSMState<TOwner>> states)
        {
            if (states == null) return;
            foreach (var s in states)
            {
                if (s == null) continue;
                _states[s.Id] = s;
            }
        }

        /// <summary>
        /// 添加迁移规则。
        /// </summary>
        public void AddTransition(IFSMTransition<TOwner> transition)
        {
            if (transition == null) return;
            if (!_transitions.TryGetValue(transition.FromId, out var list))
            {
                list = new List<IFSMTransition<TOwner>>(4);
                _transitions.Add(transition.FromId, list);
            }

            // 简单去重：不重复添加完全相同的 From/To
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                var t = list[i];
                if (t.FromId == transition.FromId && t.ToId == transition.ToId)
                {
                    list[i] = transition; // 用新的覆盖（可能优先级或条件实现不同）
                    return;
                }
            }

            list.Add(transition);
        }

        /// <summary>
        /// 批量添加迁移规则。
        /// </summary>
        public void AddTransitions(IEnumerable<IFSMTransition<TOwner>> transitions)
        {
            if (transitions == null) return;
            foreach (var t in transitions)
            {
                AddTransition(t);
            }
        }

        /// <summary>
        /// 设置初始状态（不会调用 OnExit，仅调用目标状态 OnEnter）。
        /// </summary>
        public void SetInitial(int stateId)
        {
            if (!_states.TryGetValue(stateId, out var state)) return;
            _currentId = stateId;
            _currentState = state;
            _currentState.OnEnter(_owner);
        }

        /// <summary>
        /// 强制切换状态（立即触发当前 OnExit 与目标 OnEnter）。
        /// </summary>
        public void ChangeState(int stateId)
        {
            if (_currentId == stateId) return;
            if (!_states.TryGetValue(stateId, out var toState)) return;

            _currentState?.OnExit(_owner);

            _currentId = stateId;
            _currentState = toState;
            _currentState.OnEnter(_owner);
        }

        /// <summary>
        /// 每帧更新与迁移判定。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_currentState == null) return;

            // 迁移判定：选择优先级最高且条件满足的迁移
            if (_transitions.TryGetValue(_currentId, out var list) && list != null)
            {
                var bestIndex = -1;
                var bestPriority = int.MinValue;

                var count = list.Count;
                for (var i = 0; i < count; i++)
                {
                    var t = list[i];
                    if (t == null) continue;
                    if (t.ToId == _currentId) continue; // 忽略自环
                    if (t.CanTransition(_owner))
                    {
                        if (t.Priority > bestPriority)
                        {
                            bestPriority = t.Priority;
                            bestIndex = i;
                        }
                    }
                }

                if (bestIndex >= 0)
                {
                    var selected = list[bestIndex];
                    ChangeState(selected.ToId);
                }
            }

            _currentState.OnUpdate(_owner, deltaTime);
        }

        public void LateTick()
        {
            _currentState?.OnLateUpdate(_owner);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            _currentState?.OnFixedUpdate(_owner, fixedDeltaTime);
        }
    }
}
