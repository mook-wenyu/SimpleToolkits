using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 对象池管理器服务适配器
    /// </summary>
    public class PoolManagerService : IGameService
    {
        public string ServiceName => nameof(PoolManager);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// 对象池管理器实例
        /// </summary>
        public PoolManager Target { get; private set; }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            Target = new PoolManager();

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            Target?.Dispose();
            Target = null;
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

    /// <summary>
    /// YooAssetLoader 服务适配器
    /// </summary>
    public class YooAssetLoaderService : IGameService
    {
        public string ServiceName => nameof(YooAssetLoader);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// YooAssetLoader 实例
        /// </summary>
        public YooAssetLoader Target { get; private set; }
        private readonly SimpleToolkitsSettings _settings;

        public YooAssetLoaderService(SimpleToolkitsSettings settings)
        {
            _settings = settings;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            Target = new YooAssetLoader(_settings.GamePlayMode);
            await Target.InitPackagesAsync(_settings.YooPackageInfos);

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

    /// <summary>
    /// ConfigManager 服务适配器
    /// </summary>
    public class ConfigManagerService : IGameService
    {
        public string ServiceName => nameof(ConfigManager);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(YooAssetLoaderService)};

        /// <summary>
        /// ConfigManager 实例
        /// </summary>
        public ConfigManager Target { get; private set; }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            // 获取依赖的YooAssetLoaderService实例
            var loaderService = GSMgr.Instance.Service.GetService<YooAssetLoaderService>();
            if (loaderService == null)
            {
                throw new InvalidOperationException("YooAssetLoaderService is not registered or initialized.");
            }

            var loader = loaderService.Target;
            if (loader == null)
            {
                throw new InvalidOperationException("YooAssetLoader is not initialized in YooAssetLoaderService.");
            }

            Target = new ConfigManager();
            await Target.LoadAllAsync(Constants.JsonConfigsAssetTagName);

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

    /// <summary>
    /// LocaleManager 服务适配器
    /// </summary>
    public class LocaleManagerService : IGameService
    {
        public string ServiceName => nameof(LocaleManager);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(ConfigManagerService)};

        /// <summary>
        /// LocaleManager 实例
        /// </summary>
        public LocaleManager Target { get; private set; }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            Target = new LocaleManager();
            Target.InitLanguage();

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            Target?.Dispose();
            Target = null;
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

    /// <summary>
    /// SceneKit 服务适配器
    /// </summary>
    public class SceneKitService : IGameService
    {
        public string ServiceName => nameof(SceneKit);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(YooAssetLoaderService)};

        /// <summary>
        /// SceneKit 实例
        /// </summary>
        public SceneKit Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public SceneKitService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<SceneKit>();
            }

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (Target)
            {
                UnityEngine.Object.Destroy(Target);
                Target = null;
            }
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

    /// <summary>
    /// UIKit 服务适配器
    /// </summary>
    public class UIKitService : IGameService
    {
        public string ServiceName => nameof(UIKit);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(PoolManagerService)};

        /// <summary>
        /// UIKit 实例
        /// </summary>
        public UIKit Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public UIKitService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<UIKit>();
            }

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (Target)
            {
                UnityEngine.Object.Destroy(Target);
                Target = null;
            }
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

    /// <summary>
    /// ConsoleKit 服务适配器
    /// </summary>
    public class ConsoleKitService : IGameService
    {
        public string ServiceName => nameof(ConsoleKit);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// ConsoleKit 实例
        /// </summary>
        public ConsoleKit Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public ConsoleKitService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<ConsoleKit>();
            }

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (Target)
            {
                UnityEngine.Object.Destroy(Target);
                Target = null;
            }
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

    /// <summary>
    /// AudioKit 服务适配器
    /// </summary>
    public class AudioKitService : IGameService
    {
        public string ServiceName => nameof(AudioKit);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(YooAssetLoaderService)};

        /// <summary>
        /// AudioKit 实例
        /// </summary>
        public AudioKit Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public AudioKitService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<AudioKit>();
            }

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (Target)
            {
                UnityEngine.Object.Destroy(Target);
                Target = null;
            }
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

}
