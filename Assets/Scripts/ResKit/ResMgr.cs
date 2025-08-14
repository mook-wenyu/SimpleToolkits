using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Debug = UnityEngine.Debug;

/// <summary>
/// 资源加载器类型
/// </summary>
public enum LoaderType
{
    /// <summary>
    /// Unity Resources 加载器
    /// </summary>
    Resources,
    /// <summary>
    /// YooAsset 加载器
    /// </summary>
    YooAsset
}

/// <summary>
/// 资源管理器
/// </summary>
public static class ResMgr
{
    /// <summary>
    /// 资源加载器
    /// </summary>
    public static IResLoader ResLoader { get; private set; }

    /// <summary>
    /// 配置管理器设置
    /// </summary>
    public static SimpleToolkitSettings Settings { get; private set; }

    /// <summary>
    /// 初始化资源管理器
    /// </summary>
    public static async UniTask Init()
    {
        Settings = Resources.Load<SimpleToolkitSettings>(Constants.SimpleToolkitSettingsName);
        SetResLoader(Settings.LoaderType);
        await InitPackageAsync(Settings.YooPackageInfos);
    }

    /// <summary>
    /// 设置资源加载器
    /// </summary>
    /// <param name="loaderType">资源加载器类型</param>
    private static void SetResLoader(LoaderType loaderType)
    {
        ResLoader = loaderType switch
        {
            LoaderType.Resources => new ResourcesLoader(),
            LoaderType.YooAsset => new YooAssetLoader(Settings.GamePlayMode),
            _ => throw new ArgumentOutOfRangeException(nameof(loaderType), loaderType, null)
        };
    }

    /// <summary>
    /// 并行初始化多个资源包
    /// </summary>
    /// <param name="packageInfos">资源包信息列表</param>
    public static async UniTask InitPackageAsync(List<YooPackageInfo> packageInfos)
    {
        if (ResLoader is not YooAssetLoader loader)
        {
            Debug.LogWarning("当前资源加载器不是 YooAssetLoader，跳过包初始化");
            return;
        }

        if (packageInfos == null || packageInfos.Count == 0)
        {
            Debug.LogWarning("资源包信息列表为空，跳过包初始化");
            return;
        }

        try
        {
            // 创建并行任务列表
            var initTasks = packageInfos.Select(packageInfo => loader.InitPackageAsync(
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
    /// 异步加载资源对象
    /// </summary>
    /// <param name="location">资源的定位地址</param>
    /// <param name="onCompleted">加载完成回调</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public static async UniTask<TObject> LoadAssetAsync<TObject>(string location, Action<TObject> onCompleted = null) where TObject : UnityEngine.Object
    {
        return await ResLoader.LoadAssetAsync<TObject>(location, onCompleted);
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="location">场景的定位地址</param>
    /// <param name="sceneMode">场景加载模式</param>
    /// <param name="suspendLoad">场景加载到90%自动挂起</param>
    /// <param name="onCompleted">加载完成回调</param>
    public static SceneHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false, Action<SceneHandle> onCompleted = null)
    {
        if (ResLoader is YooAssetLoader loader)
        {
            return loader.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, onCompleted);
        }
        Debug.LogWarning("当前资源加载器不是 YooAssetLoader，跳过场景加载");
        return null;
    }

    /// <summary>
    /// 异步加载子资源对象
    /// </summary>
    /// <param name="subName">子资源名称</param>
    /// <param name="location">资源的定位地址</param>
    /// <param name="onCompleted">加载完成回调</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public static async UniTask<TObject> LoadSubAssetsAsync<TObject>(string subName, string location, Action<TObject> onCompleted = null) where TObject : UnityEngine.Object
    {
        using var handle = YooAssets.LoadSubAssetsAsync<TObject>(location);
        await handle.ToUniTask();
        var subAsset = handle.GetSubAssetObject<TObject>(subName);

        onCompleted?.Invoke(subAsset);
        return subAsset;
    }

    /// <summary>
    /// 异步加载图集对象
    /// </summary>
    /// <param name="location">资源的定位地址</param>
    /// <param name="spriteName">包含的精灵名称</param>
    /// <param name="onCompleted">加载完成回调</param>
    public static async UniTask<Sprite> LoadSpriteAtlasAsync(string location, string spriteName, Action<Sprite> onCompleted = null)
    {
        return await ResLoader.LoadSpriteAsync(location, spriteName, onCompleted);
    }

    /// <summary>
    /// 异步批量加载多个资源
    /// </summary>
    /// <param name="location">资源的定位地址</param>
    /// <param name="onCompleted">加载完成回调</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    public static async UniTask<List<TObject>> LoadAllAssetAsync<TObject>(string location, Action<List<TObject>> onCompleted = null) where TObject : UnityEngine.Object
    {
        return await ResLoader.LoadAllAssetAsync<TObject>(location, onCompleted);
    }

}
