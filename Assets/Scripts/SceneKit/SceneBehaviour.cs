using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

/// <summary>
/// 场景管理器
/// </summary>
public class SceneBehaviour : MonoBehaviour
{
    private sealed class SceneHandleData
    {
        public readonly SceneHandle sceneHandle;
        public readonly object userData;

        public SceneHandleData(SceneHandle sceneHandle, object userData)
        {
            this.sceneHandle = sceneHandle;
            this.userData = userData;
        }
    }

    // 加载完成
    private readonly Dictionary<string, SceneHandle> _loadedSceneAssetNames = new();
    // 加载中
    private readonly Dictionary<string, SceneHandleData> _loadingSceneAssetNames = new();
    // 卸载中
    private readonly Dictionary<string, SceneHandle> _unloadingSceneAssetNames = new();
    /// <summary>
    /// 场景加载成功事件
    /// </summary>
    private event Action<string, object> OnLoadSceneSuccess;
    /// <summary>
    /// 场景加载失败事件
    /// </summary>
    private event Action<string, EOperationStatus, string, object> OnLoadSceneFailure;
    /// <summary>
    /// 场景卸载成功事件
    /// </summary>
    private event Action<string, object> OnUnloadSceneSuccess;
    /// <summary>
    /// 场景卸载失败事件
    /// </summary>
    private event Action<string, object> OnUnloadSceneFailure;

    void OnDestroy()
    {
        string[] loadedSceneAssetNames = _loadedSceneAssetNames.Keys.ToArray();
        foreach (string loadedSceneAssetName in loadedSceneAssetNames)
        {
            if (SceneIsUnloading(loadedSceneAssetName))
            {
                continue;
            }

            UnloadScene(loadedSceneAssetName);
        }

        _loadedSceneAssetNames.Clear();
        _loadingSceneAssetNames.Clear();
        _unloadingSceneAssetNames.Clear();
    }

    /// <summary>
    /// 获取场景是否已加载
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <returns>场景是否已加载。</returns>
    public bool SceneIsLoaded(string sceneAssetName)
    {
        if (!string.IsNullOrEmpty(sceneAssetName))
        {
            return _loadedSceneAssetNames.ContainsKey(sceneAssetName);
        }

        Debug.LogException(new ArgumentNullException(sceneAssetName));
        return false;
    }

    /// <summary>
    /// 获取已加载场景的资源名称
    /// </summary>
    /// <returns>已加载场景的资源名称</returns>
    public string[] GetLoadedSceneAssetNames()
    {
        return _loadedSceneAssetNames.Keys.ToArray();
    }

    /// <summary>
    /// 获取场景是否正在加载
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <returns>场景是否正在加载</returns>
    public bool SceneIsLoading(string sceneAssetName)
    {
        if (!string.IsNullOrEmpty(sceneAssetName))
        {
            return _loadingSceneAssetNames.ContainsKey(sceneAssetName);
        }
        Debug.LogException(new ArgumentNullException(sceneAssetName));
        return false;
    }

    /// <summary>
    /// 获取正在加载场景的资源名称
    /// </summary>
    /// <returns>正在加载场景的资源名称</returns>
    public string[] GetLoadingSceneAssetNames()
    {
        return _loadingSceneAssetNames.Keys.ToArray();
    }

    /// <summary>
    /// 获取场景是否正在卸载
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <returns>场景是否正在卸载</returns>
    public bool SceneIsUnloading(string sceneAssetName)
    {
        if (!string.IsNullOrEmpty(sceneAssetName))
        {
            return _unloadingSceneAssetNames.ContainsKey(sceneAssetName);
        }
        Debug.LogException(new ArgumentNullException(sceneAssetName));
        return false;
    }

    /// <summary>
    /// 获取正在卸载场景的资源名称
    /// </summary>
    /// <returns>正在卸载场景的资源名称</returns>
    public string[] GetUnloadingSceneAssetNames()
    {
        return _unloadingSceneAssetNames.Keys.ToArray();
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <param name="sceneMode">加载场景的方式</param>
    public UniTask<SceneHandle> LoadSceneAsync(string sceneAssetName, LoadSceneMode sceneMode = LoadSceneMode.Single)
    {
        return LoadSceneAsync(sceneAssetName, sceneMode, null);
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <param name="userData">用户自定义数据</param>
    public UniTask<SceneHandle> LoadSceneAsync(string sceneAssetName, object userData)
    {
        return LoadSceneAsync(sceneAssetName, LoadSceneMode.Single, userData);
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <param name="sceneMode">加载场景的方式</param>
    /// <param name="userData">用户自定义数据</param>
    public async UniTask<SceneHandle> LoadSceneAsync(string sceneAssetName, LoadSceneMode sceneMode, object userData)
    {
        if (string.IsNullOrEmpty(sceneAssetName))
        {
            Debug.LogException(new ArgumentNullException(sceneAssetName));
            return null;
        }

        if (SceneIsUnloading(sceneAssetName))
        {
            Debug.LogException(new Exception($"Scene asset '{sceneAssetName}' is being unloaded."));
            return null;
        }

        if (SceneIsLoading(sceneAssetName))
        {
            Debug.LogException(new Exception($"Scene asset '{sceneAssetName}' is being loaded."));
            return null;
        }

        if (SceneIsLoaded(sceneAssetName))
        {
            Debug.LogException(new Exception($"Scene asset '{sceneAssetName}' is already loaded."));
            return null;
        }

        var sceneOperationHandle = Mgr.Instance.Loader.LoadSceneAsync(sceneAssetName, sceneMode, LocalPhysicsMode.None, true);
        _loadingSceneAssetNames.Add(sceneAssetName, new SceneHandleData(sceneOperationHandle, userData));
        sceneOperationHandle.Completed += OnLoadSceneCompleted;
        return sceneOperationHandle;
    }

    private void OnLoadSceneCompleted(SceneHandle sceneOperationHandle)
    {
        _loadedSceneAssetNames.Add(sceneOperationHandle.GetAssetInfo().AssetPath, sceneOperationHandle);
        _loadingSceneAssetNames.Remove(sceneOperationHandle.GetAssetInfo().AssetPath, out var value);

        if (value == null) return;

        if (sceneOperationHandle.IsDone)
        {
            _loadingSceneAssetNames.Remove(sceneOperationHandle.SceneName);
            OnLoadSceneSuccess?.Invoke(sceneOperationHandle.SceneName, value.userData);
        }
        else
        {
            _loadingSceneAssetNames.Remove(sceneOperationHandle.SceneName);
            var appendErrorMessage = $"Load scene failure, scene asset name '{sceneOperationHandle.SceneName}', status '{sceneOperationHandle.Status}', error message '{sceneOperationHandle.LastError}'.";
            OnLoadSceneFailure?.Invoke(sceneOperationHandle.SceneName, sceneOperationHandle.Status, appendErrorMessage, value.userData);
            Debug.LogException(new Exception(appendErrorMessage));
        }
    }

    /// <summary>
    /// 卸载场景
    /// </summary>
    /// <param name="sceneAssetName">场景资源名称</param>
    /// <param name="userData">用户自定义数据</param>
    public void UnloadScene(string sceneAssetName, object userData = null)
    {
        if (string.IsNullOrEmpty(sceneAssetName))
        {
            Debug.LogException(new ArgumentNullException(sceneAssetName));
            return;
        }

        if (SceneIsUnloading(sceneAssetName))
        {
            Debug.LogException(new Exception($"Scene asset '{sceneAssetName}' is being unloaded."));
            return;
        }

        if (SceneIsLoading(sceneAssetName))
        {
            Debug.LogException(new Exception($"Scene asset '{sceneAssetName}' is being loaded."));
            return;
        }

        if (!SceneIsLoaded(sceneAssetName))
        {
            Debug.LogException(new Exception($"Scene asset '{sceneAssetName}' is not loaded yet."));
            return;
        }

        if (!_loadedSceneAssetNames.TryGetValue(sceneAssetName, out var sceneOperationHandle)) return;

        var unloadSceneOperationHandle = sceneOperationHandle.UnloadAsync();
        _loadedSceneAssetNames.Remove(sceneAssetName);
        _unloadingSceneAssetNames.Add(sceneAssetName, sceneOperationHandle);

        unloadSceneOperationHandle.Completed += OnUnloadSceneOperationHandleOnCompleted;
        return;

        void OnUnloadSceneOperationHandleOnCompleted(AsyncOperationBase asyncOperationBase)
        {
            if (asyncOperationBase.IsDone)
            {
                _unloadingSceneAssetNames.Remove(sceneAssetName);
                _loadedSceneAssetNames.Remove(sceneAssetName);
                OnUnloadSceneSuccess?.Invoke(sceneAssetName, userData);
            }
            else
            {
                _unloadingSceneAssetNames.Remove(sceneAssetName);
                OnUnloadSceneFailure?.Invoke(sceneAssetName, userData);
                Debug.LogException(new Exception($"Unload scene failure, scene asset name '{sceneAssetName}'."));
            }
        }
    }
}
