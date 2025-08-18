using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits
{
    /// <summary>
    /// JSON 文件存储实现
    /// </summary>
    public class JsonFileStorage : IDataStorage
    {
        private SimpleToolkitsSettings _settings;
        private readonly Dictionary<string, object> _data = new();
        private string _dataDirectory;

        public StorageType StorageType => StorageType.JsonFile;

        public int Count => _data.Count;

        public bool Initialize(SimpleToolkitsSettings settings)
        {
            _settings = settings;
            _dataDirectory = Path.Combine(Application.persistentDataPath, "Save");

            // 确保数据目录存在
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            return true;
        }

        public async UniTask<bool> LoadAsync(string fileName)
        {
            var filePath = GetFilePath(fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[JsonFileStorage] 文件不存在: {filePath}");
                _data.Clear();
                return true; // 文件不存在不算错误，返回空数据
            }

            string jsonContent;
            await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                jsonContent = await reader.ReadToEndAsync();
            }

            // 如果启用了加密，先解密
            if (_settings.EnableEncryption)
            {
                jsonContent = DecryptData(jsonContent);
            }

            if (string.IsNullOrEmpty(jsonContent))
            {
                _data.Clear();
                return true;
            }

            // 解析 JSON
            var wrapper = JsonConvert.DeserializeObject<DataWrapper>(jsonContent);
            _data.Clear();

            if (wrapper?.data != null)
            {
                foreach (var item in wrapper.data)
                {
                    _data[item.key] = DeserializeValue(item.value, item.type);
                }
            }

            return true;
        }

        public async UniTask<bool> SaveAsync(string fileName)
        {
            var filePath = GetFilePath(fileName);

            // 创建数据包装器
            var wrapper = new DataWrapper
            {
                data = new List<DataItem>()
            };

            foreach (var kvp in _data)
            {
                var item = new DataItem
                {
                    key = kvp.Key,
                    value = SerializeValue(kvp.Value),
                    type = GetValueType(kvp.Value)
                };
                wrapper.data.Add(item);
            }

            var jsonContent = JsonConvert.SerializeObject(wrapper, Formatting.Indented);

            // 如果启用了加密，再加密
            if (_settings.EnableEncryption)
            {
                jsonContent = EncryptData(jsonContent);
            }

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
            {
                directory = _dataDirectory;
            }
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 流式异步写入文件
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            await writer.WriteAsync(jsonContent);
            await writer.FlushAsync();

            return true;
        }

        public bool HasKey(string key)
        {
            return _data.ContainsKey(key);
        }

        public bool RemoveKey(string key)
        {
            return _data.Remove(key);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public async UniTask<bool> DeleteAsync(string fileName)
        {
            var filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // 清空当前数据缓存
            _data.Clear();
            await UniTask.CompletedTask;
            return true;
        }

        public string[] GetAllKeys()
        {
            var keys = new string[_data.Count];
            _data.Keys.CopyTo(keys, 0);
            return keys;
        }

        public void GetAllKeys(List<string> results)
        {
            results.Clear();
            results.AddRange(_data.Keys);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is bool boolValue)
                    return boolValue;
                if (bool.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            _data[key] = value;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is int intValue)
                    return intValue;
                if (int.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _data[key] = value;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is float floatValue)
                    return floatValue;
                if (float.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            _data[key] = value;
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (_data.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        public void SetString(string key, string value)
        {
            _data[key] = value;
        }

        public T GetObject<T>(string key, T defaultValue = null) where T : class
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is T directValue)
                    return directValue;

                var jsonValue = value.ToString();
                return JsonConvert.DeserializeObject<T>(jsonValue);
            }
            return defaultValue;
        }

        public object GetObject(Type type, string key, object defaultValue = null)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (type.IsInstanceOfType(value))
                    return value;

                var jsonValue = value.ToString();
                return JsonConvert.DeserializeObject(jsonValue, type);
            }
            return defaultValue;
        }

        public void SetObject<T>(string key, T value) where T : class
        {
            _data[key] = value;
        }

        public void SetObject(string key, object value)
        {
            _data[key] = value;
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(_dataDirectory, fileName + ".json");
        }

        private string SerializeValue(object value)
        {
            if (value == null) return null;

            return value switch
            {
                bool or int or float or string => value.ToString(),
                _ => JsonConvert.SerializeObject(value)
            };
        }

        private object DeserializeValue(string valueStr, string type)
        {
            if (string.IsNullOrEmpty(valueStr)) return null;

            return type switch
            {
                "bool" => bool.Parse(valueStr),
                "int" => int.Parse(valueStr),
                "float" => float.Parse(valueStr),
                "string" => valueStr,
                _ => valueStr // 对象类型在获取时再反序列化
            };
        }

        private string GetValueType(object value)
        {
            return value switch
            {
                bool => "bool",
                int => "int",
                float => "float",
                string => "string",
                _ => "object"
            };
        }

        private string EncryptData(string data)
        {
            // AES 加密实现
            if (string.IsNullOrEmpty(data)) return data;

            var key = _settings.EncryptionKey;
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 确保密钥长度为32字节

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();

            // 先写入IV
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(data);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        private string DecryptData(string encryptedData)
        {
            // AES 解密实现
            if (string.IsNullOrEmpty(encryptedData)) return encryptedData;

            var key = _settings.EncryptionKey;
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 确保密钥长度为32字节
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = keyBytes;

            // 从加密数据中提取IV
            var iv = new byte[aes.IV.Length];
            Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }


        [Serializable]
        private class DataWrapper
        {
            public List<DataItem> data;
        }

        [Serializable]
        private class DataItem
        {
            public string key;
            public string value;
            public string type;
        }
    }
}
