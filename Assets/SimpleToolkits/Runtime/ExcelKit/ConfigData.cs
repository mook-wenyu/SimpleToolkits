using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace SimpleToolkits
{
    public class ConfigData : IDisposable
    {
        private readonly Dictionary<string, Dictionary<string, BaseConfig>> _jsonDataDict = new();

        private readonly JsonSerializerSettings _jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public async UniTask LoadAllAsync(string jsonPath, Action<bool> onCompleted = null)
        {
            var jsonConfigs = await GSMgr.Instance.GetObject<YooAssetLoader>().LoadAllAssetAsync<TextAsset>(jsonPath);
            foreach (var jsonConfig in jsonConfigs)
            {
                var config = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
                var key = jsonConfig.name.Split('_');
                _jsonDataDict[key[0]] = config;
                // loader.Release(jsonConfig);
            }
            // 加载完成回调
            onCompleted?.Invoke(false);
        }

        public void LoadAllFromResources(string jsonPath)
        {
            var jsonConfigs = Resources.LoadAll<TextAsset>(jsonPath);
            foreach (var jsonConfig in jsonConfigs)
            {
                var config = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
                _jsonDataDict[jsonConfig.name] = config;
            }
        }

        /// <summary>
        /// 获取指定类型和ID的配置数据
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="id">配置ID</param>
        /// <returns>配置数据</returns>
        [Preserve]
        public T Get<T>(string id) where T : BaseConfig
        {
            var configName = typeof(T).Name;

            if (_jsonDataDict.TryGetValue(configName, out var dict) && dict.TryGetValue(id, out var config))
            {
                return config as T;
            }

            Debug.LogWarning($"未找到类型 {configName} ID为 {id} 的配置数据");
            return null;
        }

        /// <summary>
        /// 获取指定类型和ID的配置数据（非泛型版本）
        /// </summary>
        /// <param name="type">配置类型</param>
        /// <param name="id">配置ID</param>
        /// <returns>配置数据</returns>
        [Preserve]
        public BaseConfig Get(Type type, string id)
        {
            if (type == null)
            {
                Debug.LogError("类型参数不能为 null");
                return null;
            }

            if (!typeof(BaseConfig).IsAssignableFrom(type))
            {
                Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
                return null;
            }

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("配置ID不能为空");
                return null;
            }

            var configName = type.Name;

            if (_jsonDataDict.TryGetValue(configName, out var dict) && dict.TryGetValue(id, out var config))
            {
                if (config != null && type.IsInstanceOfType(config))
                {
                    return config;
                }
            }

            Debug.LogWarning($"未找到类型 {configName} ID为 {id} 的配置数据");
            return null;
        }

        /// <summary>
        /// 获取指定类型的所有配置数据
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置数据列表</returns>
        [Preserve]
        public IReadOnlyList<T> GetAll<T>() where T : BaseConfig
        {
            var configName = typeof(T).Name;

            if (_jsonDataDict.TryGetValue(configName, out var dict))
            {
                return dict.Values.Cast<T>().ToList();
            }

            Debug.LogWarning($"未找到类型 {configName} 的配置数据");
            return Array.Empty<T>();
        }

        /// <summary>
        /// 获取指定类型的所有配置数据（非泛型版本）
        /// </summary>
        /// <param name="type">配置类型</param>
        /// <returns>配置数据列表</returns>
        [Preserve]
        public IReadOnlyList<BaseConfig> GetAll(Type type)
        {
            if (type == null)
            {
                Debug.LogError("类型参数不能为 null");
                return Array.Empty<BaseConfig>();
            }

            if (!typeof(BaseConfig).IsAssignableFrom(type))
            {
                Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
                return Array.Empty<BaseConfig>();
            }

            var configName = type.Name;

            if (_jsonDataDict.TryGetValue(configName, out var dict))
            {
                // 使用 LINQ 将 BaseConfig 转换为具体类型，然后再转回 BaseConfig
                var result = new List<BaseConfig>();
                foreach (var config in dict.Values)
                {
                    if (config != null && type.IsInstanceOfType(config))
                    {
                        result.Add(config);
                    }
                }
                return result;
            }

            Debug.LogWarning($"未找到类型 {configName} 的配置数据");
            return Array.Empty<BaseConfig>();
        }

        /// <summary>
        /// 检查指定类型和ID的配置数据是否存在
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns></returns>
        public bool Has<T>(string id) where T : BaseConfig
        {
            var configName = typeof(T).Name;
            return _jsonDataDict.TryGetValue(configName, out var dict) && dict.ContainsKey(id);
        }

        /// <summary>
        /// 检查指定类型和ID的配置数据是否存在（非泛型版本）
        /// </summary>
        /// <param name="type">配置类型</param>
        /// <param name="id">配置ID</param>
        /// <returns>如果配置存在则返回 true，否则返回 false</returns>
        [Preserve]
        public bool Has(Type type, string id)
        {
            if (type == null)
            {
                Debug.LogError("类型参数不能为 null");
                return false;
            }

            if (!typeof(BaseConfig).IsAssignableFrom(type))
            {
                Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
                return false;
            }

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("配置ID不能为空");
                return false;
            }

            var configName = type.Name;
            return _jsonDataDict.TryGetValue(configName, out var dict) &&
                   dict.ContainsKey(id) &&
                   dict[id] != null &&
                   type.IsInstanceOfType(dict[id]);
        }

        /// <summary>
        /// 移除指定类型和ID的配置数据，
        /// ID为空则移除指定类型所有配置数据
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <typeparam name="T">配置类型</typeparam>
        public void Remove<T>(string id = null) where T : BaseConfig
        {
            var configName = typeof(T).Name;
            if (!_jsonDataDict.TryGetValue(configName, out var dict)) return;
            if (string.IsNullOrEmpty(id))
            {
                _jsonDataDict.Remove(configName);
                return;
            }
            dict.Remove(id);
        }

        /// <summary>
        /// 移除指定类型和ID的配置数据（非泛型版本），
        /// ID为空则移除指定类型所有配置数据
        /// </summary>
        /// <param name="type">配置类型</param>
        /// <param name="id">配置ID</param>
        [Preserve]
        public void Remove(Type type, string id = null)
        {
            if (type == null)
            {
                Debug.LogError("类型参数不能为 null");
                return;
            }

            if (!typeof(BaseConfig).IsAssignableFrom(type))
            {
                Debug.LogError($"类型 {type.Name} 不继承自 BaseConfig");
                return;
            }

            var configName = type.Name;
            if (!_jsonDataDict.TryGetValue(configName, out var dict)) return;
            if (string.IsNullOrEmpty(id))
            {
                _jsonDataDict.Remove(configName);
                return;
            }
            dict.Remove(id);
        }

        public void Dispose()
        {
            _jsonDataDict.Clear();
        }
    }
}
