using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 泛型状态接口（高性能：使用 int 作为状态 Id，避免字符串比较与装箱）。
    /// </summary>
    /// <typeparam name="TOwner">状态机持有者类型</typeparam>
    public interface IFSMState<in TOwner>
    {
        /// <summary>
        /// 状态唯一 Id（建议静态缓存常量，避免运行时哈希/字符串比较开销）。
        /// </summary>
        int Id { get; }

        /// <summary>
        /// 进入状态回调。
        /// </summary>
        /// <param name="owner">持有者</param>
        void OnEnter(TOwner owner);

        /// <summary>
        /// 退出状态回调。
        /// </summary>
        /// <param name="owner">持有者</param>
        void OnExit(TOwner owner);

        /// <summary>
        /// Update 帧更新调用。
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <param name="deltaTime">Time.deltaTime</param>
        void OnUpdate(TOwner owner, float deltaTime);

        /// <summary>
        /// LateUpdate 帧更新调用。
        /// </summary>
        /// <param name="owner">持有者</param>
        void OnLateUpdate(TOwner owner);

        /// <summary>
        /// FixedUpdate 物理帧更新调用。
        /// </summary>
        /// <param name="owner">持有者</param>
        /// <param name="fixedDeltaTime">Time.fixedDeltaTime</param>
        void OnFixedUpdate(TOwner owner, float fixedDeltaTime);
    }
}
