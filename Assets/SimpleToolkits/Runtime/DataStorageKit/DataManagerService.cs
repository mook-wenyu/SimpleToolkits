using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 数据管理器服务适配器
    /// </summary>
    public class DataManagerService : IGameService
    {
        public string ServiceName => nameof(DataManager);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// 数据管理器实例
        /// </summary>
        public DataManager Target { get; private set; }

        private readonly SimpleToolkitsSettings _settings;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SimpleToolkits设置</param>
        public DataManagerService(SimpleToolkitsSettings settings = null)
        {
            _settings = settings;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            Target = new DataManager(_settings);
            await Target.InitializeAsync();

            IsInitialized = true;
        }

        public void Dispose()
        {
            Target?.Dispose();
            Target = null;
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }
}
