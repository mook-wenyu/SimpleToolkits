using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits
{
    /// <summary>
    /// 统一的配置和存档管理器
    /// </summary>
    public class DataStorageManager : IDisposable
    {
        private readonly SimpleToolkitsSettings _settings;

        private IDataStorage _configStorage;
        private IDataStorage _saveStorage;
        private CancellationTokenSource _autoSaveCancellationTokenSource;

        /// <summary>
        /// 当前存档名称
        /// </summary>
        private string _currentSaveName = "AutoSave";

        /// <summary>
        /// 配置存储
        /// </summary>
        public IDataStorage ConfigStorage => _configStorage;

        /// <summary>
        /// 存档存储
        /// </summary>
        public IDataStorage SaveStorage => _saveStorage;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 当前存档名称
        /// </summary>
        public string CurrentSaveName
        {
            get => _currentSaveName;
            set
            {
                if (_currentSaveName != value)
                {
                    _currentSaveName = value;
                    // 当存档名称改变时，重新加载存档数据
                    if (IsInitialized)
                    {
                        LoadSaveAsync().Forget();
                    }
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SimpleToolkits设置</param>
        public DataStorageManager(SimpleToolkitsSettings settings = null)
        {
            _settings = settings;
        }

        /// <summary>
        /// 初始化数据管理器
        /// </summary>
        /// <returns>是否初始化成功</returns>
        public async UniTask<bool> InitializeAsync()
        {
            if (IsInitialized) return true;

            // 创建配置存储
            var actualStorageType = GetActualStorageType();
            _configStorage = DataStorageFactory.CreateStorage(actualStorageType);
            _saveStorage = DataStorageFactory.CreateStorage(actualStorageType);

            if (_configStorage == null || _saveStorage == null)
            {
                Debug.LogError("[DataStorageManager] 创建存储实例失败");
                return false;
            }

            // 初始化存储
            if (!_configStorage.Initialize(_settings))
            {
                Debug.LogError("[DataStorageManager] 初始化配置存储失败");
                return false;
            }

            if (!_saveStorage.Initialize(_settings))
            {
                Debug.LogError("[DataStorageManager] 初始化存档存储失败");
                return false;
            }

            // 加载数据
            await LoadAllAsync();

            // 启动自动保存
            StartAutoSave();

            IsInitialized = true;

            return true;
        }

        /// <summary>
        /// 获取实际使用的存储方式
        /// </summary>
        /// <returns>实际存储方式</returns>
        public StorageType GetActualStorageType()
        {
            if (_settings.StorageType != StorageType.Auto)
                return _settings.StorageType;

            // 根据平台自动选择存储方式
#if UNITY_WEBGL && !UNITY_EDITOR
            return StorageType.PlayerPrefs;
#else
            return StorageType.JsonFile;
#endif
        }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        public async UniTask LoadAllAsync()
        {
            await LoadConfigAsync();
            await LoadSaveAsync();
        }

        /// <summary>
        /// 保存所有数据
        /// </summary>
        public async UniTask SaveAllAsync()
        {
            await SaveConfigAsync();
            await SaveSaveAsync();
        }

        /// <summary>
        /// 加载配置数据
        /// </summary>
        public async UniTask<bool> LoadConfigAsync()
        {
            return await _configStorage.LoadAsync(_settings.ConfigFileName);
        }

        /// <summary>
        /// 保存配置数据
        /// </summary>
        public async UniTask<bool> SaveConfigAsync()
        {
            return await _configStorage.SaveAsync(_settings.ConfigFileName);
        }

        /// <summary>
        /// 加载存档数据
        /// </summary>
        public async UniTask<bool> LoadSaveAsync()
        {
            return await _saveStorage.LoadAsync(_currentSaveName);
        }

        /// <summary>
        /// 加载指定存档数据
        /// </summary>
        /// <param name="saveName">存档名称</param>
        public async UniTask<bool> LoadSaveAsync(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogError("[DataStorageManager] 存档名称不能为空");
                return false;
            }

            _currentSaveName = saveName;
            return await _saveStorage.LoadAsync(_currentSaveName);
        }

        /// <summary>
        /// 保存存档数据
        /// </summary>
        public async UniTask<bool> SaveSaveAsync()
        {
            return await _saveStorage.SaveAsync(_currentSaveName);
        }

        /// <summary>
        /// 保存到指定存档
        /// </summary>
        /// <param name="saveName">存档名称</param>
        public async UniTask<bool> SaveSaveAsync(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogError("[DataStorageManager] 存档名称不能为空");
                return false;
            }

            return await _saveStorage.SaveAsync(saveName);
        }

        /// <summary>
        /// 切换到指定存档
        /// </summary>
        /// <param name="saveName">存档名称</param>
        public async UniTask<bool> SwitchSaveAsync(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogError("[DataStorageManager] 存档名称不能为空");
                return false;
            }

            // 先保存当前存档
            await SaveSaveAsync();

            // 切换到新存档并加载
            _currentSaveName = saveName;
            return await LoadSaveAsync();
        }

        /// <summary>
        /// 删除指定存档
        /// </summary>
        /// <param name="saveName">存档名称</param>
        public async UniTask<bool> DeleteSaveAsync(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogError("[DataStorageManager] 存档名称不能为空");
                return false;
            }

            // 检查是否尝试删除受保护的存档
            if (saveName == Constants.DefaultSaveName || saveName == _currentSaveName)
            {
                Debug.LogError("[DataStorageManager] 无法删除受保护的存档");
                return false;
            }

            return await _saveStorage.DeleteAsync(saveName);
        }

        /// <summary>
        /// 启动自动保存
        /// </summary>
        private void StartAutoSave()
        {
            StopAutoSave();

            if (_settings.AutoSaveInterval > 0)
            {
                _autoSaveCancellationTokenSource = new CancellationTokenSource();
                AutoSaveTask(_autoSaveCancellationTokenSource.Token).Forget();
            }
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        private void StopAutoSave()
        {
            if (_autoSaveCancellationTokenSource != null)
            {
                _autoSaveCancellationTokenSource.Cancel();
                _autoSaveCancellationTokenSource.Dispose();
                _autoSaveCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 自动保存任务
        /// </summary>
        private async UniTaskVoid AutoSaveTask(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_settings.AutoSaveInterval), cancellationToken: cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await SaveAllAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不需要处理
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataStorageManager] 自动保存任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void ClearAll()
        {
            _configStorage?.Clear();
            _saveStorage?.Clear();
        }

        /// <summary>
        /// 清空当前存档数据
        /// </summary>
        public async UniTask ClearCurrentSaveAsync()
        {
            if (_saveStorage != null)
            {
                await _saveStorage.DeleteAsync(_currentSaveName);
                // 重新加载以确保数据为空
                await LoadSaveAsync();
            }
        }

        /// <summary>
        /// 销毁资源
        /// </summary>
        public void Dispose()
        {
            StopAutoSave();
            ClearAll();
            _configStorage = null;
            _saveStorage = null;
            IsInitialized = false;
        }
    }
}
