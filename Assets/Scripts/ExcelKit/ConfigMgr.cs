using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        string jsonPath = GetJsonPath();
        LoadAllFromResources(jsonPath);
    }

    /// <summary>
    /// 获取 json 文件的路径
    /// </summary>
    /// <returns></returns>
    public static string GetJsonPath()
    {
        var excelSettings = Resources.Load<ExcelExporterSettings>("ExcelExporterSettings");

        if (excelSettings == null) return "JsonConfigs";

        return excelSettings.jsonRelativePath.StartsWith("Resources/") ?
            excelSettings.jsonRelativePath["Resources/".Length..] : "JsonConfigs";
    }

    /// <summary>
    /// 从 Resources 加载所有配置数据
    /// </summary>
    /// <param name="jsonPath"></param>
    public static void LoadAllFromResources(string jsonPath)
    {
        var jsonConfigs = Resources.LoadAll<TextAsset>(jsonPath);
        foreach (var jsonConfig in jsonConfigs)
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, BaseConfig>>(jsonConfig.text, _jsonSerializerSettings);
            _jsonData[jsonConfig.name] = config;
        }
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
