using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits
{
    /// <summary>
    /// PlayerPrefs 存储实现
    /// </summary>
    public class PlayerPrefsStorage : IDataStorage
    {
        private readonly Dictionary<string, object> _cache = new();
        private string _keyPrefix = "";

        public StorageType StorageType => StorageType.PlayerPrefs;

        public int Count => _cache.Count;

        public bool Initialize(SimpleToolkitsSettings settings)
        {
            _keyPrefix = Application.productName + "_";
            return true;
        }

        public async UniTask<bool> LoadAsync(string fileName)
        {
            // PlayerPrefs 操作是同步的，但为了保持接口一致性，我们使用 UniTask.CompletedTask
            await UniTask.Yield();

            _cache.Clear();

            // PlayerPrefs 不支持直接获取所有键，所以我们需要维护一个键列表
            var keysListKey = GetPrefixedKey($"{fileName}_KeysList");
            var keysList = PlayerPrefs.GetString(keysListKey, "");

            if (!string.IsNullOrEmpty(keysList))
            {
                var keys = keysList.Split('|');
                foreach (var key in keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        var prefixedKey = GetPrefixedKey(key);
                        if (PlayerPrefs.HasKey(prefixedKey))
                        {
                            // 尝试确定值的类型并加载
                            var typeKey = prefixedKey + "_Type";
                            var valueType = PlayerPrefs.GetString(typeKey, "string");

                            object value = valueType switch
                            {
                                "bool" => PlayerPrefs.GetInt(prefixedKey, 0) == 1,
                                "int" => PlayerPrefs.GetInt(prefixedKey, 0),
                                "float" => PlayerPrefs.GetFloat(prefixedKey, 0f),
                                "string" => PlayerPrefs.GetString(prefixedKey, ""),
                                _ => PlayerPrefs.GetString(prefixedKey, "")
                            };

                            _cache[key] = value;
                        }
                    }
                }
            }

            return true;
        }

        public async UniTask<bool> SaveAsync(string fileName)
        {
            // PlayerPrefs 操作是同步的，但为了保持接口一致性，我们使用 UniTask.Yield
            await UniTask.Yield();

            // 保存所有缓存的数据
            var keysList = new List<string>();

            foreach (var kvp in _cache)
            {
                var prefixedKey = GetPrefixedKey(kvp.Key);
                var typeKey = prefixedKey + "_Type";

                switch (kvp.Value)
                {
                    case bool boolValue:
                        PlayerPrefs.SetInt(prefixedKey, boolValue ? 1 : 0);
                        PlayerPrefs.SetString(typeKey, "bool");
                        break;
                    case int intValue:
                        PlayerPrefs.SetInt(prefixedKey, intValue);
                        PlayerPrefs.SetString(typeKey, "int");
                        break;
                    case float floatValue:
                        PlayerPrefs.SetFloat(prefixedKey, floatValue);
                        PlayerPrefs.SetString(typeKey, "float");
                        break;
                    case string stringValue:
                        PlayerPrefs.SetString(prefixedKey, stringValue);
                        PlayerPrefs.SetString(typeKey, "string");
                        break;
                    default:
                        // 对象序列化为 JSON
                        var jsonValue = JsonConvert.SerializeObject(kvp.Value);
                        PlayerPrefs.SetString(prefixedKey, jsonValue);
                        PlayerPrefs.SetString(typeKey, "object");
                        break;
                }

                keysList.Add(kvp.Key);
            }

            // 保存键列表
            var keysListKey = GetPrefixedKey($"{fileName}_KeysList");
            PlayerPrefs.SetString(keysListKey, string.Join("|", keysList));

            PlayerPrefs.Save();
            return true;
        }

        public bool HasKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        public bool RemoveKey(string key)
        {
            if (_cache.Remove(key))
            {
                // 从 PlayerPrefs 中删除
                var prefixedKey = GetPrefixedKey(key);
                PlayerPrefs.DeleteKey(prefixedKey);
                PlayerPrefs.DeleteKey(prefixedKey + "_Type");

                return true;
            }
            return false;
        }

        public void Clear()
        {
            // 删除所有相关的 PlayerPrefs 键
            foreach (var key in _cache.Keys)
            {
                var prefixedKey = GetPrefixedKey(key);
                PlayerPrefs.DeleteKey(prefixedKey);
                PlayerPrefs.DeleteKey(prefixedKey + "_Type");
            }

            _cache.Clear();
            PlayerPrefs.Save();
        }

        public async UniTask<bool> DeleteAsync(string fileName)
        {
            // PlayerPrefs 操作是同步的，但为了保持接口一致性，我们使用 UniTask.Yield
            await UniTask.Yield();

            // PlayerPrefs 中删除指定文件的所有数据
            var keysListKey = GetPrefixedKey($"{fileName}_KeysList");
            var keysList = PlayerPrefs.GetString(keysListKey, "");

            if (!string.IsNullOrEmpty(keysList))
            {
                var keys = keysList.Split('|');
                foreach (var key in keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        var prefixedKey = GetPrefixedKey(key);
                        PlayerPrefs.DeleteKey(prefixedKey);
                        PlayerPrefs.DeleteKey(prefixedKey + "_Type");
                    }
                }
            }

            // 删除键列表
            PlayerPrefs.DeleteKey(keysListKey);
            PlayerPrefs.Save();

            // 如果删除的是当前加载的文件，清空缓存
            _cache.Clear();
            return true;
        }

        public string[] GetAllKeys()
        {
            var keys = new string[_cache.Count];
            _cache.Keys.CopyTo(keys, 0);
            return keys;
        }

        public void GetAllKeys(List<string> results)
        {
            results.Clear();
            results.AddRange(_cache.Keys);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_cache.TryGetValue(key, out var value) && value is bool boolValue)
                return boolValue;
            return defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            _cache[key] = value;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_cache.TryGetValue(key, out var value) && value is int intValue)
                return intValue;
            return defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _cache[key] = value;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_cache.TryGetValue(key, out var value) && value is float floatValue)
                return floatValue;
            return defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            _cache[key] = value;
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (_cache.TryGetValue(key, out var value) && value is string stringValue)
                return stringValue;
            return defaultValue;
        }

        public void SetString(string key, string value)
        {
            _cache[key] = value;
        }

        public T GetObject<T>(string key, T defaultValue = null) where T : class
        {
            if (_cache.TryGetValue(key, out var value))
            {
                if (value is T directValue)
                    return directValue;

                if (value is string jsonValue)
                {
                    return JsonConvert.DeserializeObject<T>(jsonValue);
                }
            }
            return defaultValue;
        }

        public object GetObject(Type type, string key, object defaultValue = null)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                if (type.IsInstanceOfType(value))
                    return value;

                if (value is string jsonValue)
                {
                    return JsonConvert.DeserializeObject(jsonValue, type);
                }
            }
            return defaultValue;
        }

        public void SetObject<T>(string key, T value) where T : class
        {
            _cache[key] = value;
        }

        public void SetObject(string key, object value)
        {
            _cache[key] = value;
        }

        private string GetPrefixedKey(string key)
        {
            return _keyPrefix + key;
        }
    }
}
