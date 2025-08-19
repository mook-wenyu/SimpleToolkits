namespace SimpleToolkits
{
    /// <summary>
    /// 统一帧更新接口，便于 FSMManager 统一调度。
    /// </summary>
    public interface IFSMUpdatable
    {
        void Tick(float deltaTime);
        void LateTick();
        void FixedTick(float fixedDeltaTime);
    }
}
