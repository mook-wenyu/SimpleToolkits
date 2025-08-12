using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 原生资源加载器（可扩展支持Addressables等）
/// 使用字典缓存管理
/// </summary>
public class NativeLoader : IResLoader
{
    private readonly Dictionary<string, Object> _assetCache = new();
    
    public NativeLoader()
    {
        Debug.Log("Native 资源加载器，初始化完成！");
    }

    public async UniTask<T> LoadAssetAsync<T>(string location, Action<T> onCompleted = null) where T : Object
    {
        // 检查缓存
        if (_assetCache.TryGetValue(location, out var cachedAsset) && cachedAsset is T cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        // 这里可以扩展支持Addressables或其他原生加载方式
        // 目前作为示例，使用Resources作为后备
        var asset = Resources.Load<T>(location);
        if (asset != null)
        {
            _assetCache[location] = asset;
            
            onCompleted?.Invoke(asset);
            return asset;
        }

        await UniTask.Yield();
        
        onCompleted?.Invoke(null);
        return null;
    }

    public async UniTask<Sprite> LoadSpriteAsync(string location, string spriteName, Action<Sprite> onCompleted = null)
    {
        var cacheKey = $"{location}@{spriteName}";

        // 检查缓存
        if (_assetCache.TryGetValue(location, out var cachedAsset) && cachedAsset is Sprite cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        // 扩展点：可以在这里实现原生的图集加载
        await UniTask.Yield();
        
        onCompleted?.Invoke(null);
        return null;
    }

    public async UniTask<List<T>> LoadAllAssetAsync<T>(string location, Action<List<T>> onCompleted = null) where T : Object
    {
        var loadedAssets = new List<T>();

        try
        {
            // 扩展点：可以在这里实现原生的批量加载（如Addressables）
            // 目前作为示例，使用Resources作为后备
            var assets = Resources.LoadAll<T>(location);
            if (assets != null && assets.Length > 0)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        var cacheKey = $"{location}/{asset.name}";
                        _assetCache[cacheKey] = asset;
                        loadedAssets.Add(asset);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No assets found for location: {location}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading assets from location '{location}': {ex.Message}");
        }

        await UniTask.Yield();
        
        onCompleted?.Invoke(loadedAssets);
        return loadedAssets;
    }

    public bool HasAsset(string location)
    {
        return _assetCache.ContainsKey(location);
    }

    public int GetCacheCount()
    {
        return _assetCache.Count;
    }

    public void ReleaseAsset(Object asset)
    {
        if (asset == null) return;

        // 从缓存中移除该资源的所有条目
        var keysToRemove = new List<string>();
        foreach (var kvp in _assetCache)
        {
            if (kvp.Value == asset)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            _assetCache.Remove(key);
        }
        // 扩展点：可以在这里实现原生的资源释放
    }

    public void ReleaseAllAssets()
    {
        _assetCache.Clear();
        // 扩展点：可以在这里实现原生的批量资源释放
    }
}
