using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

/// <summary>
/// Unity Resources资源加载器
/// 采用"即时卸载"策略：加载资源后立即从Resources系统卸载，使用字典缓存管理
/// </summary>
public class ResourcesLoader : IResLoader
{
    private readonly Dictionary<string, Object> _assetCache = new();

    public ResourcesLoader()
    {
        Debug.Log("Unity Resources 资源加载器，初始化完成！");
    }

    public async UniTask<T> LoadAssetAsync<T>(string location, Action<T> onCompleted = null) where T : Object
    {
        // 检查缓存
        if (_assetCache.TryGetValue(location, out var cachedAsset) && cachedAsset is T cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        var request = Resources.LoadAsync<T>(location);
        await request.ToUniTask();

        if (request.asset is T asset)
        {
            _assetCache[location] = asset;
            Resources.UnloadAsset(request.asset);
            
            onCompleted?.Invoke(asset);
            return asset;
        }
        
        onCompleted?.Invoke(null);
        return null;
    }

    public async UniTask<Sprite> LoadSpriteAsync(string location, string spriteName, Action<Sprite> onCompleted = null)
    {
        var cacheKey = $"{location}@{spriteName}";

        // 检查缓存
        if (_assetCache.TryGetValue(cacheKey, out var cachedAsset) && cachedAsset is Sprite cached)
        {
            onCompleted?.Invoke(cached);
            return cached;
        }

        var request = Resources.LoadAsync<SpriteAtlas>(location);
        await request.ToUniTask();
        
        if (request.asset is SpriteAtlas atlas)
        {
            var sprite = atlas.GetSprite(spriteName);
            if (sprite != null)
            {
                _assetCache[cacheKey] = sprite;
                Resources.UnloadAsset(request.asset);
                
                onCompleted?.Invoke(sprite);
                return sprite;
            }
        }
        
        onCompleted?.Invoke(null);
        return null;
    }

    public async UniTask<List<T>> LoadAllAssetAsync<T>(string location, Action<List<T>> onCompleted = null) where T : Object
    {
        var loadedAssets = new List<T>();

        try
        {
            // 使用 Resources.LoadAll 批量加载文件夹中的资源
            var request = Resources.LoadAll<T>(location);

            if (request is {Length: > 0})
            {
                foreach (var asset in request)
                {
                    if (asset != null)
                    {
                        // 为每个资源生成缓存键
                        var cacheKey = $"{location}/{asset.name}";
                        _assetCache[cacheKey] = asset;
                        
                        loadedAssets.Add(asset);
                    }
                }

                // 立即卸载 Resources 系统中的资源，保持与现有策略一致
                foreach (var asset in request)
                {
                    Resources.UnloadAsset(asset);
                }
            }
            else
            {
                Debug.LogWarning($"No assets found in folder: {location}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading assets from folder '{location}': {ex.Message}");
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
    }

    public void ReleaseAllAssets()
    {
        _assetCache.Clear();
        // 可选择性调用UnloadUnusedAssets清理
        Resources.UnloadUnusedAssets();
    }
}
