using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace SimpleToolkits
{
    /// <summary>
    /// 场景状态
    /// </summary>
    public enum SceneStatus
    {
        /// <summary>
        /// 未加载
        /// </summary>
        None,
        /// <summary>
        /// 加载中
        /// </summary>
        Loading,
        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,
        /// <summary>
        /// 卸载中
        /// </summary>
        Unloading
    }

    /// <summary>
    /// 场景操作
    /// </summary>
    public class SceneOperation
    {
        public SceneStatus Status { get; set; } = SceneStatus.None;
        public SceneHandle SceneHandle { get; set; }
    }

    /// <summary>
    /// 场景管理器
    /// </summary>
    public class SceneKit : MonoBehaviour
    {
        private YooAssetLoader _resKit;
        private readonly Dictionary<string, SceneOperation> _sceneOperations = new();

        /// <summary>
        /// 场景加载进度事件
        /// </summary>
        public event Action<string, float> OnLoadSceneProgress;

        /// <summary>
        /// 场景加载成功事件
        /// </summary>
        public event Action<string> OnLoadSceneSuccess;

        /// <summary>
        /// 场景加载失败事件
        /// </summary>
        public event Action<string, string> OnLoadSceneFailure;

        /// <summary>
        /// 场景卸载成功事件
        /// </summary>
        public event Action<string> OnUnloadSceneSuccess;

        private void Awake()
        {
            _resKit = GKMgr.Instance.GetObject<YooAssetLoader>();
        }

        private void OnDestroy()
        {
            foreach (var sceneName in _sceneOperations.Keys)
            {
                if (_sceneOperations[sceneName].Status == SceneStatus.Loaded)
                {
                    UnloadSceneAsync(sceneName).Forget();
                }
            }
            _sceneOperations.Clear();
        }

        /// <summary>
        /// 获取场景操作
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称</param>
        /// <returns>场景操作</returns>
        public SceneOperation GetSceneOperation(string sceneAssetName)
        {
            return _sceneOperations.GetValueOrDefault(sceneAssetName, null);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称</param>
        /// <param name="sceneMode">加载场景的方式</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        public async UniTask LoadSceneAsync(string sceneAssetName, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = true)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new ArgumentNullException(nameof(sceneAssetName));
            }

            if (_sceneOperations.TryGetValue(sceneAssetName, out var existingOperation) && existingOperation.Status != SceneStatus.None)
            {
                Debug.LogWarning($"Scene '{sceneAssetName}' is already in state '{existingOperation.Status}'.");
                return;
            }

            var operation = new SceneOperation {Status = SceneStatus.Loading};
            _sceneOperations[sceneAssetName] = operation;

            try
            {
                var handle = _resKit.LoadSceneAsync(sceneAssetName, sceneMode, LocalPhysicsMode.None, suspendLoad);
                operation.SceneHandle = handle;

                MonitorLoadingProgress(sceneAssetName, handle).Forget();

                await handle.ToUniTask();

                if (handle.Status == EOperationStatus.Succeed)
                {
                    operation.Status = SceneStatus.Loaded;
                    OnLoadSceneSuccess?.Invoke(sceneAssetName);
                }
                else
                {
                    throw new Exception(handle.LastError);
                }
            }
            catch (Exception e)
            {
                _sceneOperations.Remove(sceneAssetName);
                var errorMessage = $"Failed to load scene '{sceneAssetName}': {e.Message}";
                OnLoadSceneFailure?.Invoke(sceneAssetName, errorMessage);
                Debug.LogException(new Exception(errorMessage, e));
            }
        }

        /// <summary>
        /// 监听场景加载进度
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="handle">场景加载句柄</param>
        private async UniTaskVoid MonitorLoadingProgress(string sceneName, SceneHandle handle)
        {
            if (handle == null) return;

            while (!handle.IsDone)
            {
                OnLoadSceneProgress?.Invoke(sceneName, handle.Progress);
                await UniTask.Yield();
            }
            OnLoadSceneProgress?.Invoke(sceneName, 1f);
        }

        /// <summary>
        /// 异步卸载场景
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称</param>
        public async UniTask UnloadSceneAsync(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new ArgumentNullException(nameof(sceneAssetName));
            }

            if (!_sceneOperations.TryGetValue(sceneAssetName, out var operation) || operation.Status != SceneStatus.Loaded)
            {
                Debug.LogWarning($"Scene '{sceneAssetName}' is not loaded or in a transient state.");
                return;
            }

            operation.Status = SceneStatus.Unloading;

            try
            {
                await operation.SceneHandle.UnloadAsync();
                _sceneOperations.Remove(sceneAssetName);
                OnUnloadSceneSuccess?.Invoke(sceneAssetName);
            }
            catch (Exception e)
            {
                operation.Status = SceneStatus.Loaded;
                Debug.LogException(new Exception($"Failed to unload scene '{sceneAssetName}': {e.Message}", e));
            }
        }
    }
}
