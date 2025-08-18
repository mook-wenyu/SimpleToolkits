using System;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 数据存储工厂类
    /// </summary>
    public static class DataStorageFactory
    {
        /// <summary>
        /// 创建数据存储实例
        /// </summary>
        /// <param name="storageType">存储类型</param>
        /// <returns>数据存储实例</returns>
        public static IDataStorage CreateStorage(StorageType storageType)
        {
            return storageType switch
            {
                StorageType.PlayerPrefs => new PlayerPrefsStorage(),
                StorageType.JsonFile => new JsonFileStorage(),
                StorageType.Auto => CreateAutoStorage(),
                _ => throw new ArgumentException($"不支持的存储类型: {storageType}")
            };
        }

        /// <summary>
        /// 根据平台自动选择存储方式
        /// </summary>
        /// <returns>数据存储实例</returns>
        private static IDataStorage CreateAutoStorage()
        {
            // 根据平台自动选择最适合的存储方式
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[DataStorageFactory] WebGL 平台，使用 PlayerPrefs 存储");
            return new PlayerPrefsStorage();
#elif UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            Debug.Log("[DataStorageFactory] 原生平台，使用 JSON 文件存储");
            return new JsonFileStorage();
#else
            Debug.Log("[DataStorageFactory] 未知平台，默认使用 PlayerPrefs 存储");
            return new PlayerPrefsStorage();
#endif
        }

        /// <summary>
        /// 获取推荐的存储方式
        /// </summary>
        /// <returns>推荐的存储方式</returns>
        public static StorageType GetRecommendedStorageType()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return StorageType.PlayerPrefs;
#elif UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            return StorageType.JsonFile;
#else
            return StorageType.PlayerPrefs;
#endif
        }

        /// <summary>
        /// 检查指定存储方式是否在当前平台可用
        /// </summary>
        /// <param name="storageType">存储类型</param>
        /// <returns>是否可用</returns>
        public static bool IsStorageTypeAvailable(StorageType storageType)
        {
            return storageType switch
            {
                StorageType.PlayerPrefs => true, // 所有平台都支持
                StorageType.JsonFile => CanUsePersistentDataPath(),
                StorageType.Auto => true,
                _ => false
            };
        }

        /// <summary>
        /// 检查是否可以使用 persistentDataPath
        /// </summary>
        /// <returns>是否可用</returns>
        private static bool CanUsePersistentDataPath()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false; // WebGL 平台不支持文件系统访问
#else
            try
            {
                var path = Application.persistentDataPath;
                return !string.IsNullOrEmpty(path);
            }
            catch
            {
                return false;
            }
#endif
        }
    }
}
