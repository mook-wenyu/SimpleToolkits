using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// YooAssetLoader服务适配器
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
    /// ConfigData服务适配器
    /// </summary>
    public class ConfigDataService : IGameService
    {
        public string ServiceName => nameof(ConfigData);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(YooAssetLoaderService)};

        /// <summary>
        /// ConfigData 实例
        /// </summary>
        public ConfigData Target { get; private set; }

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

            Target = new ConfigData();
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
    /// Locale服务适配器
    /// </summary>
    public class LocaleService : IGameService
    {
        public string ServiceName => nameof(Locale);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(ConfigDataService)};

        /// <summary>
        /// Locale 实例
        /// </summary>
        public Locale Target { get; private set; }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            Target = new Locale();
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
    /// PoolMgr服务适配器
    /// </summary>
    public class PoolMgrService : IGameService
    {
        public string ServiceName => nameof(PoolMgr);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// PoolMgr 实例
        /// </summary>
        public PoolMgr Target { get; private set; }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            Target = new PoolMgr();

            IsInitialized = true;
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            Target?.Clear();
            Target = null;
            IsInitialized = false;
        }

        public object GetObject() => Target;
    }

    /// <summary>
    /// SceneBehaviour服务适配器
    /// </summary>
    public class SceneBehaviourService : IGameService
    {
        public string ServiceName => nameof(SceneComponent);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(YooAssetLoaderService)};

        /// <summary>
        /// SceneComponent 实例
        /// </summary>
        public SceneComponent Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public SceneBehaviourService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<SceneComponent>();
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
    /// UIBehaviour服务适配器
    /// </summary>
    public class UIBehaviourService : IGameService
    {
        public string ServiceName => nameof(UIComponent);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(PoolMgrService)};

        /// <summary>
        /// UIComponent 实例
        /// </summary>
        public UIComponent Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public UIBehaviourService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<UIComponent>();
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
    /// ConsoleBehaviour服务适配器
    /// </summary>
    public class ConsoleBehaviourService : IGameService
    {
        public string ServiceName => nameof(ConsoleComponent);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => Array.Empty<Type>();

        /// <summary>
        /// ConsoleComponent 实例
        /// </summary>
        public ConsoleComponent Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public ConsoleBehaviourService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<ConsoleComponent>();
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
    /// AudioBehaviour服务适配器
    /// </summary>
    public class AudioBehaviourService : IGameService
    {
        public string ServiceName => nameof(AudioComponent);
        public bool IsInitialized { get; private set; }
        public Type[] Dependencies => new[] {typeof(YooAssetLoaderService)};

        /// <summary>
        /// AudioComponent 实例
        /// </summary>
        public AudioComponent Target { get; private set; }
        private readonly GameObject _parentGameObject;

        public AudioBehaviourService(GameObject parentGameObject)
        {
            _parentGameObject = parentGameObject;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (!Target)
            {
                Target = _parentGameObject.AddComponent<AudioComponent>();
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
