using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 资源加载器接口
/// 包含缓存管理功能
/// </summary>
public interface IResLoader
{
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="location">资源路径</param>
    /// <param name="onCompleted">加载完成回调</param>
    /// <returns>加载的资源对象</returns>
    UniTask<T> LoadAssetAsync<T>(string location, Action<T> onCompleted = null) where T : Object;
    
    /// <summary>
    /// 异步加载图集中的精灵
    /// </summary>
    /// <param name="location">图集路径</param>
    /// <param name="spriteName">精灵名称</param>
    /// /// <param name="onCompleted">加载完成回调</param>
    /// <returns>加载的精灵对象</returns>
    UniTask<Sprite> LoadSpriteAsync(string location, string spriteName, Action<Sprite> onCompleted = null);

    /// <summary>
    /// 异步批量加载多个资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="location">资源路径或标签</param>
    /// /// <param name="onCompleted">加载完成回调</param>
    /// <returns>加载的资源对象数组</returns>
    UniTask<List<T>> LoadAllAssetAsync<T>(string location, Action<List<T>> onCompleted = null) where T : Object;

    /// <summary>
    /// 检查资源是否已缓存
    /// </summary>
    /// <param name="location">资源路径</param>
    /// <returns>是否已缓存</returns>
    bool HasAsset(string location);

    /// <summary>
    /// 获取缓存中的资源数量
    /// </summary>
    /// <returns>缓存资源数量</returns>
    int GetCacheCount();

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="asset">要释放的资源</param>
    void ReleaseAsset(Object asset);

    /// <summary>
    /// 释放所有资源并清空缓存
    /// </summary>
    void ReleaseAllAssets();
}
