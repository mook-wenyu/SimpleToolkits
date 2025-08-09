using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class ResMgr : MonoSingleton<ResMgr>
{
    public const string Default_Package = "DefaultPackage";

    /// <summary>
    /// 默认资源包
    /// </summary>
    public ResourcePackage Package { get; private set; }

    /// <summary>
    /// 初始化资源管理器
    /// </summary>
    public override void OnSingletonInit()
    {
        StartCoroutine(InitPackage(Default_Package));
    }

    /// <summary>
    /// 初始化资源包
    /// </summary>
    /// <param name="packageName"></param>
    /// <returns></returns>
    public IEnumerator InitPackage(string packageName)
    {
        if (Package != null) yield break;

        // 初始化资源系统
        YooAssets.Initialize();

        // 创建默认的资源包
        Package = YooAssets.CreatePackage(packageName);
        var initParameters = GetInitParameters();
        var initOperation = Package.InitializeAsync(initParameters);
        yield return initOperation;

        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(Package);

        if (initOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"资源包初始化失败：{initOperation.Error}");
            yield break;
        }

        yield return RequestPackageVersionAndUpdatePackageManifest(Package);
    }

    private InitializeParameters GetInitParameters()
    {
        #if UNITY_EDITOR
        // 编辑器模拟模式
        var buildResult = EditorSimulateModeHelper.SimulateBuild(Default_Package);
        var packageRoot = buildResult.PackageRootDirectory;
        var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
        var initParameters = new EditorSimulateModeParameters
        {
            EditorFileSystemParameters = editorFileSystemParams
        };
        return initParameters;

        #else
        // 单机运行模式
        var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        var initParameters = new OfflinePlayModeParameters
        {
            BuildinFileSystemParameters = buildinFileSystemParams
        };
        return initParameters;
        #endif
    }

    /// <summary>
    /// 请求包版本并更新包清单 默认资源包
    /// </summary>
    /// <returns></returns>
    private IEnumerator RequestPackageVersionAndUpdatePackageManifest(ResourcePackage package)
    {
        var versionOperation = package.RequestPackageVersionAsync();
        yield return versionOperation;

        if (versionOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"获取包版本失败：{versionOperation.Error}");
            yield break;
        }

        var manifestOperation = package.UpdatePackageManifestAsync(versionOperation.PackageVersion);
        yield return manifestOperation;

        if (manifestOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"更新包清单失败：{manifestOperation.Error}");
        }
    }

    /// <summary>
    /// 异步加载资源对象
    /// </summary>
    /// <param name="location">资源的定位地址</param>
    /// <param name="priority">加载的优先级</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    /// <returns></returns>
    public async UniTask<TObject> LoadAssetAsync<TObject>(string location, uint priority = 0U) where TObject : UnityEngine.Object
    {
        using var handle = Package.LoadAssetAsync<TObject>(location, priority);
        await handle.ToUniTask();
        return handle.AssetObject as TObject;
    }

    /// <summary>
    /// 异步加载子资源对象
    /// </summary>
    /// <param name="subName">子资源名称</param>
    /// <param name="location">资源的定位地址</param>
    /// <param name="priority">加载的优先级</param>
    /// <typeparam name="TObject">资源类型</typeparam>
    /// <returns></returns>
    public async UniTask<TObject> LoadSubAssetsAsync<TObject>(string subName, string location, uint priority = 0U) where TObject : UnityEngine.Object
    {
        using var handle = Package.LoadSubAssetsAsync<TObject>(location, priority);
        await handle.ToUniTask();
        return handle.GetSubAssetObject<TObject>(subName);
    }
}
