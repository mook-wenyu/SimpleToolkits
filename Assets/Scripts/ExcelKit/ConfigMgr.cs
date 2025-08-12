using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

public static class ConfigMgr
{
    private static readonly Dictionary<string, IReadOnlyDictionary<string, BaseConfig>> _jsonData = new();

    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    /// <summary>
    /// 初始化数据配置管理器
    /// </summary>
    public static void Init()
    {
        var jsonPath = "JsonConfigs";
        var settings = ResMgr.Instance.Settings;
        if (settings != null)
        {
            bool isResources = settings.jsonRelativePath.StartsWith("Assets/Resources/");
            jsonPath = isResources ? settings.jsonRelativePath["Assets/Resources/".Length..] :
                settings.jsonRelativePath;
            
            LoadAllHandled(jsonPath);
        }
        {
            LoadAllFromResources(jsonPath);
        }
    }

    public static void LoadAllFromResources(string jsonPath)
    {
        var jsonConfigs = Resources.LoadAll<TextAsset>(jsonPath);
        foreach (var jsonConfig in jsonConfigs)
        {
            var config = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
            _jsonData[jsonConfig.name] = config;
        }
    }

    public static void LoadAllHandled(string jsonPath, Action onCompleted = null)
    {
        ResMgr.Instance.LoadAllAssetAsync<TextAsset>(jsonPath, jsonConfigs =>
        {
            foreach (var jsonConfig in jsonConfigs)
            {
                var config = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
                _jsonData[jsonConfig.name] = config;
            }

            // 加载完成回调
            onCompleted?.Invoke();
        }).Forget();
    }

    /// <summary>
    /// 获取指定类型和ID的配置数据
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="id">配置ID</param>
    /// <returns>配置数据</returns>
    [Preserve]
    public static T Get<T>(string id) where T : BaseConfig
    {
        string configName = typeof(T).Name;

        if (_jsonData.TryGetValue(configName, out var dict) && dict.TryGetValue(id, out var config))
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
    public static IReadOnlyList<T> GetAll<T>() where T : BaseConfig
    {
        string configName = typeof(T).Name;

        if (_jsonData.TryGetValue(configName, out var dict))
        {
            return dict.Values.Cast<T>().ToList();
        }

        Debug.LogWarning($"未找到类型 {configName} 的配置数据");
        return Array.Empty<T>();
    }

}
