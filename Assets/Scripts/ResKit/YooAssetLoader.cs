using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using YooAsset;
using Object = UnityEngine.Object;

/// <summary>
/// YooAsset资源加载器
/// 采用"即时卸载"策略：使用using语句自动管理AssetHandle生命周期，使用字典缓存管理
/// </summary>
public class YooAssetLoader : IDisposable
{
    private readonly Dictionary<string, Object> _assetCache;

    /// <summary>
    /// 获取运行模式。
    /// </summary>
    public readonly EPlayMode playMode;

    public YooAssetLoader(EPlayMode gamePlayMode)
    {
        _assetCache = new Dictionary<string, Object>();

        playMode = gamePlayMode;

        #if UNITY_EDITOR
        playMode = EPlayMode.EditorSimulateMode;
        #elif UNITY_WEBGL
            PlayMode = EPlayMode.WebPlayMode;
        #endif

        // 初始化资源系统
        YooAssets.Initialize();
        YooAssets.SetOperationSystemMaxTimeSlice(30);
        // YooAssets.SetCacheSystemCachedFileVerifyLevel(EVerifyLevel.High);
        // YooAssets.SetDownloadSystemBreakpointResumeFileSize(4096 * 8);

        Debug.Log($"资源系统运行模式：{playMode}\nYooAsset 资源加载器，初始化完成！");
    }

    /// <summary>
    /// 并行初始化多个资源包
    /// </summary>
    /// <param name="packageInfos">资源包信息列表</param>
    public async UniTask InitPackagesAsync(List<YooPackageInfo> packageInfos)
    {
        if (packageInfos == null || packageInfos.Count == 0)
        {
            Debug.LogWarning("资源包信息列表为空，跳过包初始化");
            return;
        }

        try
        {
            // 创建并行任务列表
            var initTasks = packageInfos.Select(packageInfo => InitPackageAsync(
                packageInfo.packageName,
                packageInfo.hostServerURL,
                packageInfo.fallbackHostServerURL,
                packageInfo.isDefaultPackage
            )).ToArray();

            // 等待所有任务完成
            bool[] results = await UniTask.WhenAll(initTasks);
            for (var i = 0; i < results.Length; i++)
            {
                if (!results[i])
                {
                    Debug.LogError($"资源包 {packageInfos[i].packageName} 初始化失败!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }


    /// <summary>
    /// 初始化资源包
    /// </summary>
    /// <param name="packageName">资源包名称</param>
    /// <param name="hostServerURL">主服务器地址</param>
    /// <param name="fallbackHostServerURL">备用服务器地址</param>
    /// <param name="isDefaultPackage">是否为默认资源包，约定 “DefaultPackage” 默认为默认的资源包</param>
    public async UniTask<bool> InitPackageAsync(string packageName, string hostServerURL = "", string fallbackHostServerURL = "", bool isDefaultPackage = false)
    {
        var resourcePackage = YooAssets.TryGetPackage(packageName);
        if (resourcePackage == null)
        {
            resourcePackage = YooAssets.CreatePackage(packageName);
            if (packageName == Constants.DefaultPackageName || isDefaultPackage)
            {
                // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
                YooAssets.SetDefaultPackage(resourcePackage);
            }
        }

        var initializationOperationHandler = CreateInitializationOperationHandler(resourcePackage, hostServerURL, fallbackHostServerURL);
        await initializationOperationHandler;

        if (initializationOperationHandler.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"资源包初始化失败：{initializationOperationHandler.Error}");
            return false;
        }

        bool result = await RequestPackageVersionAndUpdatePackageManifest(resourcePackage);

        Debug.Log($"资源包：{packageName}，初始化完成：{result}");
        return result;
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
    /// 根据运行模式创建初始化操作数据
    /// </summary>
    /// <returns></returns>
    private InitializationOperation CreateInitializationOperationHandler(ResourcePackage resourcePackage, string hostServerURL, string fallbackHostServerURL)
    {
        switch (playMode)
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

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="location">资源路径</param>
    /// <param name="onCompleted">加载完成回调</param>
    /// <returns>加载的资源对象</returns>
    public async UniTask<T> LoadAssetAsync<T>(string location, Action<T> onCompleted = null) where T : Object
    {
        // 检查缓存
        if (_assetCache.TryGetValue(location, out var cachedAsset) && cachedAsset is T cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        using var handle = YooAssets.LoadAssetAsync<T>(location);
        await handle.ToUniTask();

        if (handle.AssetObject is T asset)
        {
            _assetCache[location] = asset;

            onCompleted?.Invoke(asset);
            return asset;
        }

        onCompleted?.Invoke(null);
        return null;
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="location">场景的定位地址</param>
    /// <param name="sceneMode">场景加载模式</param>
    /// <param name="physicsMode">场景物理模式</param>
    /// <param name="suspendLoad">场景加载到90%自动挂起</param>
    /// <param name="onCompleted">加载完成回调</param>
    public SceneHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, Action<SceneHandle> onCompleted = null)
    {
        var handle = YooAssets.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad);
        onCompleted?.Invoke(handle);
        return handle;
    }

    /// <summary>
    /// 异步加载子资源
    /// </summary>
    /// <param name="location">场景的定位地址</param>
    /// <param name="subName">子资源名称</param>
    /// <param name="onCompleted">加载完成回调</param>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns>加载的子资源对象</returns>
    public async UniTask<T> LoadSubAssetAsync<T>(string location, string subName, Action<T> onCompleted = null) where T : Object
    {
        var cacheKey = $"{location}#{subName}";

        // 检查缓存
        if (_assetCache.TryGetValue(cacheKey, out var cachedAsset) && cachedAsset is T cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        using var handle = YooAssets.LoadSubAssetsAsync<T>(location);
        await handle.ToUniTask();

        var subAsset = handle.GetSubAssetObject<T>(subName);
        if (subAsset != null)
        {
            _assetCache[cacheKey] = subAsset;

            onCompleted?.Invoke(subAsset);
            return subAsset;
        }

        onCompleted?.Invoke(null);
        return null;
    }

    /// <summary>
    /// 异步加载图集中的精灵
    /// </summary>
    /// <param name="location">图集路径</param>
    /// <param name="spriteName">精灵名称</param>
    /// /// <param name="onCompleted">加载完成回调</param>
    /// <returns>加载的精灵对象</returns>
    public async UniTask<Sprite> LoadSpriteAsync(string location, string spriteName, Action<Sprite> onCompleted = null)
    {
        var cacheKey = $"{location}@{spriteName}";

        // 检查缓存
        if (_assetCache.TryGetValue(cacheKey, out var cachedAsset) && cachedAsset is Sprite cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        using var handle = YooAssets.LoadAssetAsync<SpriteAtlas>(location);
        await handle.ToUniTask();

        if (handle.AssetObject is SpriteAtlas atlas)
        {
            var sprite = atlas.GetSprite(spriteName);
            if (sprite != null)
            {
                _assetCache[cacheKey] = sprite;

                onCompleted?.Invoke(sprite);
                return sprite;
            }
        }

        onCompleted?.Invoke(null);
        return null;
    }

    /// <summary>
    /// 异步批量加载多个资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="location">资源路径或标签</param>
    /// /// <param name="onCompleted">加载完成回调</param>
    /// <returns>加载的资源对象数组</returns>
    public async UniTask<List<T>> LoadAllAssetAsync<T>(string location, Action<List<T>> onCompleted = null) where T : Object
    {
        var loadedAssets = new List<T>();

        try
        {
            // 尝试作为资源标签获取资源信息列表
            var assetInfos = YooAssets.GetAssetInfos(location);

            if (assetInfos is {Length: > 0})
            {
                // 并行加载所有资源
                var loadTasks = new List<UniTask<T>>();

                foreach (var assetInfo in assetInfos)
                {
                    loadTasks.Add(LoadAssetAsync<T>(assetInfo.Address));
                }

                var results = await UniTask.WhenAll(loadTasks);

                // 只添加成功加载的资源
                foreach (var result in results)
                {
                    if (result != null)
                    {
                        loadedAssets.Add(result);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load asset from tag: {location}");
                    }
                }
            }
            else
            {
                // 如果不是标签，尝试作为单个资源路径加载
                var asset = await LoadAssetAsync<T>(location);
                if (asset != null)
                {
                    loadedAssets.Add(asset);
                }
                else
                {
                    Debug.LogWarning($"No assets found for location: {location}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading assets from location '{location}': {ex.Message}");
        }

        onCompleted?.Invoke(loadedAssets);
        return loadedAssets;
    }

    /// <summary>
    /// 检查资源是否已缓存
    /// </summary>
    /// <param name="location">资源路径</param>
    /// <returns>是否已缓存</returns>
    public bool HasAsset(string location)
    {
        return _assetCache.ContainsKey(location);
    }

    /// <summary>
    /// 获取缓存中的资源数量
    /// </summary>
    /// <returns>缓存资源数量</returns>
    public int GetCacheCount()
    {
        return _assetCache.Count;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="asset">要释放的资源</param>
    public void Release(Object asset)
    {
        if (asset == null) return;

        // 从缓存中移除该资源的所有条目
        var keysToRemove = new List<string>();
        foreach (var kvp in _assetCache)
        {
            if (kvp.Value == asset)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            _assetCache.Remove(key);
        }
    }

    /// <summary>
    /// 释放所有资源并清空缓存
    /// </summary>
    public void UnloadAllAssetsAsync()
    {
        _assetCache.Clear();
    }

    public void Dispose()
    {
        UnloadAllAssetsAsync();
    }
}
