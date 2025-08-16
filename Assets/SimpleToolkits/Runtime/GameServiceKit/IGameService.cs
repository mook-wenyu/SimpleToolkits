using System;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits
{
    /// <summary>
    /// 游戏服务接口，定义服务的基本生命周期
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// 服务是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 服务依赖的其他服务类型列表
        /// </summary>
        Type[] Dependencies { get; }

        /// <summary>
        /// 初始化服务
        /// </summary>
        UniTask InitializeAsync();

        /// <summary>
        /// 销毁服务
        /// </summary>
        void Dispose();

        /// <summary>
        /// 获取服务内部封装的具体对象实例
        /// </summary>
        /// <returns>服务内部的具体对象实例</returns>
        object GetObject();
    }
}
