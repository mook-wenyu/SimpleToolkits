using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

public class ConfigData : IDisposable
{
    private readonly Dictionary<string, Dictionary<string, BaseConfig>> _jsonDataDict = new();

    private readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public async UniTask LoadAllAsync(string jsonPath, Action onCompleted = null)
    {
        var jsonConfigs = await Mgr.Instance.Loader.LoadAllAssetAsync<TextAsset>(jsonPath);
        foreach (var jsonConfig in jsonConfigs)
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
            string[] key = jsonConfig.name.Split('_');
            _jsonDataDict[key[0]] = config;
            // Mgr.Instance.Loader.Release(jsonConfig);
        }
        // 加载完成回调
        onCompleted?.Invoke();
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
        string configName = typeof(T).Name;

        if (_jsonDataDict.TryGetValue(configName, out var dict) && dict.TryGetValue(id, out var config))
        {
            return config as T;
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
        string configName = typeof(T).Name;

        if (_jsonDataDict.TryGetValue(configName, out var dict))
        {
            return dict.Values.Cast<T>().ToList();
        }

        Debug.LogWarning($"未找到类型 {configName} 的配置数据");
        return Array.Empty<T>();
    }

    /// <summary>
    /// 检查指定类型和ID的配置数据是否存在
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <typeparam name="T">配置类型</typeparam>
    /// <returns></returns>
    public bool Has<T>(string id) where T : BaseConfig
    {
        string configName = typeof(T).Name;
        return _jsonDataDict.TryGetValue(configName, out var dict) && dict.ContainsKey(id);
    }

    /// <summary>
    /// 移除指定类型的所有配置数据
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    public void Remove<T>() where T : BaseConfig
    {
        string configName = typeof(T).Name;
        _jsonDataDict.Remove(configName);
    }

    /// <summary>
    /// 移除指定类型和ID的配置数据
    /// </summary>
    /// <param name="id">配置ID</param>
    /// <typeparam name="T">配置类型</typeparam>
    public void Remove<T>(string id) where T : BaseConfig
    {
        string configName = typeof(T).Name;
        if (_jsonDataDict.TryGetValue(configName, out var dict))
        {
            dict.Remove(id);
        }
    }

    public void Dispose()
    {
        _jsonDataDict.Clear();
    }
}
