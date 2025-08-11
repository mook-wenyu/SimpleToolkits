using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;

/// <summary>
/// 资源管理器
/// 默认为单机模式
/// （暂未实现联机模式）
/// </summary>
public class ResMgr : MonoSingleton<ResMgr>
{
    /// <summary>
    /// 获取运行模式。
    /// </summary>
    public EPlayMode PlayMode { get; private set; } = EPlayMode.OfflinePlayMode;

    /// <summary>
    /// 默认资源包
    /// </summary>
    private ResourcePackage _defaultPackage;

    /// <summary>
    /// 先设置运行模式 SetPlayMode ，然后
    /// 初始化资源管理器，最后
    /// 请调用 InitPackageAsync
    /// </summary>
    public override void OnSingletonInit()
    {
        #if UNITY_EDITOR
            PlayMode = EPlayMode.EditorSimulateMode;
        #elif UNITY_WEBGL
            PlayMode = EPlayMode.WebPlayMode;
        #endif
        
        Debug.Log($"资源系统运行模式：{PlayMode}");
        // 初始化资源系统
        YooAssets.Initialize();
        YooAssets.SetOperationSystemMaxTimeSlice(30);
        // YooAssets.SetCacheSystemCachedFileVerifyLevel(EVerifyLevel.High);
        // YooAssets.SetDownloadSystemBreakpointResumeFileSize(4096 * 8);
    }

    /// <summary>
    /// 初始化资源包
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="hostServerURL"></param>
    /// <param name="fallbackHostServerURL"></param>
    /// <param name="isDefaultPackage">为默认的资源包，约定 “DefaultPackage” 默认为默认的资源包</param>
    /// <returns></returns>
    public async UniTask<bool> InitPackageAsync(string packageName, string hostServerURL, string fallbackHostServerURL, bool isDefaultPackage = false)
    {
        var resourcePackage = YooAssets.TryGetPackage(packageName);
        if (resourcePackage == null)
        {
            resourcePackage = YooAssets.CreatePackage(packageName);
            if (packageName == Constants.DefaultPackageName || isDefaultPackage)
            {
                // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
                YooAssets.SetDefaultPackage(resourcePackage);
                _defaultPackage = resourcePackage;
            }
        }

        var initializationOperationHandler = CreateInitializationOperationHandler(resourcePackage, hostServerURL, fallbackHostServerURL);
        await initializationOperationHandler;

        if (initializationOperationHandler.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"资源包初始化失败：{initializationOperationHandler.Error}");
            return false;
        }

        return await RequestPackageVersionAndUpdatePackageManifest(_defaultPackage);
    }

    /// <summary>
    /// 初始化编辑器模拟模式 (EditorSimulateMode)
    /// </summary>
    /// <param name="resourcePackage">资源包</param>
    /// <returns></returns>
    private InitializationOperation InitializeYooAssetEditorSimulateMode(ResourcePackage resourcePackage)
    {
        var buildResult = EditorSimulateModeHelper.SimulateBuild(resourcePackage.PackageName);
        string packageRoot = buildResult.PackageRootDirectory;
        var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
        var initParameters = new EditorSimulateModeParameters
        {
            EditorFileSystemParameters = editorFileSystemParams
        };
        return resourcePackage.InitializeAsync(initParameters);
    }

    /// <summary>
    /// 初始化单机运行模式 (OfflinePlayMode)
    /// </summary>
    /// <param name="resourcePackage">资源包</param>
    /// <returns></returns>
    private InitializationOperation InitializeYooAssetOfflinePlayMode(ResourcePackage resourcePackage)
    {
        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        var initParameters = new OfflinePlayModeParameters
        {
            BuildinFileSystemParameters = buildinFileSystemParams
        };
        return resourcePackage.InitializeAsync(initParameters);
    }

    /// <summary>
    /// 初始化联机运行模式 (HostPlayMode)
    /// </summary>
    /// <param name="resourcePackage">资源包</param>
    /// <param name="defaultHostServer"></param>
    /// <param name="fallbackHostServer"></param>
    /// <returns></returns>
    private InitializationOperation InitializeYooAssetHostPlayMode(ResourcePackage resourcePackage, string defaultHostServer, string fallbackHostServer)
    {
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

        var initParameters = new HostPlayModeParameters
        {
            BuildinFileSystemParameters = buildinFileSystemParams,
            CacheFileSystemParameters = cacheFileSystemParams
        };
        return resourcePackage.InitializeAsync(initParameters);
    }

    /// <summary>
    /// 初始化 Web运行模式 (WebPlayMode)
    /// </summary>
    /// <param name="resourcePackage">资源包</param>
    /// <param name="defaultHostServer"></param>
    /// <param name="fallbackHostServer"></param>
    /// <returns></returns>
    private InitializationOperation InitializeYooAssetWebPlayMode(ResourcePackage resourcePackage, string defaultHostServer, string fallbackHostServer)
    {
        //说明：RemoteServices类定义请参考联机运行模式！
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
        var webRemoteFileSystemParams = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices); //支持跨域下载

        var initParameters = new WebPlayModeParameters
        {
            WebServerFileSystemParameters = webServerFileSystemParams,
            WebRemoteFileSystemParameters = webRemoteFileSystemParams
        };
        return resourcePackage.InitializeAsync(initParameters);
    }

    /// <summary>
    /// 请求包版本并更新包清单
    /// </summary>
    private async UniTask<bool> RequestPackageVersionAndUpdatePackageManifest(ResourcePackage package)
    {
        var versionOperation = package.RequestPackageVersionAsync();
        await versionOperation;

        if (versionOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"获取包版本失败：{versionOperation.Error}");
            return false;
        }

        var manifestOperation = package.UpdatePackageManifestAsync(versionOperation.PackageVersion);
        await manifestOperation;

        if (manifestOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"更新包清单失败：{manifestOperation.Error}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 异步加载资源对象
    /// </summary>
    /// <param name="location">资源的定位地址</param>
    /// <param name="priority">加载的优先级</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public async UniTask<TObject> LoadAssetAsync<TObject>(string location, uint priority = 0U) where TObject : UnityEngine.Object
    {
        using var handle = YooAssets.LoadAssetAsync<TObject>(location, priority);
        await handle.ToUniTask();
        return handle.AssetObject as TObject;
    }

    /// <summary>
    /// 委托加载资源对象
    /// </summary>
    /// <param name="location">资源的定位地址</param>
    /// <param name="completed">异步完成后执行委托</param>
    /// <param name="priority">加载的优先级</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public void LoadAssetHandle<TObject>(string location, Action<TObject> completed = null, uint priority = 0U) where TObject : UnityEngine.Object
    {
        using var handle = YooAssets.LoadAssetAsync<TObject>(location, priority);
        handle.Completed += assetHandle =>
        {
            completed?.Invoke(assetHandle.AssetObject as TObject);
        };
    }

    /// <summary>
    /// 异步加载子资源对象
    /// </summary>
    /// <param name="subName">子资源名称</param>
    /// <param name="location">资源的定位地址</param>
    /// <param name="priority">加载的优先级</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public async UniTask<TObject> LoadSubAssetsAsync<TObject>(string subName, string location, uint priority = 0U) where TObject : UnityEngine.Object
    {
        using var handle = YooAssets.LoadSubAssetsAsync<TObject>(location, priority);
        await handle.ToUniTask();
        return handle.GetSubAssetObject<TObject>(subName);
    }

    /// <summary>
    /// 委托加载子资源对象
    /// </summary>
    /// <param name="subName">子资源名称</param>
    /// <param name="location">资源的定位地址</param>
    /// <param name="completed">异步完成后执行委托</param>
    /// <param name="priority">加载的优先级</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public void LoadSubAssetsHandle<TObject>(string subName, string location, Action<TObject> completed = null, uint priority = 0U) where TObject : UnityEngine.Object
    {
        using var handle = YooAssets.LoadSubAssetsAsync<TObject>(location, priority);
        handle.Completed += assetsHandle =>
        {
            completed?.Invoke(assetsHandle.GetSubAssetObject<TObject>(subName));
        };
    }

    /// <summary>
    /// 异步加载图集对象
    /// </summary>
    /// <param name="spriteName">包含的精灵名称</param>
    /// <param name="location">资源的定位地址</param>
    /// <param name="priority">加载的优先级</param>
    public async UniTask<Sprite> LoadSpriteAtlasAsync(string spriteName, string location, uint priority = 0U)
    {
        using var handle = YooAssets.LoadAssetAsync(location, priority);
        await handle.ToUniTask();
        var spriteAtlas = handle.AssetObject as SpriteAtlas;
        return spriteAtlas != null ? spriteAtlas.GetSprite(spriteName) : null;
    }

    /// <summary>
    /// 委托加载图集对象
    /// </summary>
    /// <param name="spriteName">包含的精灵名称</param>
    /// <param name="location">资源的定位地址</param>
    /// <param name="completed">异步完成后执行委托</param>
    /// <param name="priority">加载的优先级</param>
    public void LoadSpriteAtlasHandle(string spriteName, string location, Action<Sprite> completed = null, uint priority = 0U)
    {
        using var handle = YooAssets.LoadAssetAsync(location, priority);
        handle.Completed += assetHandle =>
        {
            var spriteAtlas = assetHandle.AssetObject as SpriteAtlas;
            var spriteObject = spriteAtlas != null ? spriteAtlas.GetSprite(spriteName) : null;
            completed?.Invoke(spriteObject);
        };
    }

    /// <summary>
    /// 获取资源信息列表
    /// </summary>
    /// <param name="mTag">资源标签</param>
    public AssetInfo[] GetAssetInfos(string mTag)
    {
        return YooAssets.GetAssetInfos(mTag);
    }

    /// <summary>
    /// 设置运行模式
    /// </summary>
    /// <param name="playMode">运行模式</param>
    public void SetPlayMode(EPlayMode playMode)
    {
        PlayMode = playMode;
    }

    /// <summary>
    /// 根据运行模式创建初始化操作数据
    /// </summary>
    /// <returns></returns>
    private InitializationOperation CreateInitializationOperationHandler(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
    {
        switch (PlayMode)
        {
            case EPlayMode.EditorSimulateMode:
            {
                // 编辑器下的模拟模式
                return InitializeYooAssetEditorSimulateMode(resourcePackage);
            }
            case EPlayMode.OfflinePlayMode:
            {
                // 单机运行模式
                return InitializeYooAssetOfflinePlayMode(resourcePackage);
            }
            case EPlayMode.HostPlayMode:
            {
                // 联机运行模式
                return InitializeYooAssetHostPlayMode(resourcePackage, hostServerURL, fallbackHostServerURL);
            }
            case EPlayMode.WebPlayMode:
            {
                // WebGL运行模式
                return InitializeYooAssetWebPlayMode(resourcePackage, hostServerURL, fallbackHostServerURL);
            }
            default:
            {
                return null;
            }
        }
    }
}
