using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 统一的游戏功能套件（极简版）
    /// - 注册即初始化，按调用顺序由外部保证依赖
    /// </summary>
    public class GameKit : IDisposable
    {
        // 对象缓存字典，key为对象类型，value为具体的对象实例
        private readonly Dictionary<Type, object> _objectCache = new();

        public GameKit() { }

        /// <summary>
        /// 注册对象并缓存，调用方（GKMgr）负责在注册前完成必要的初始化
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">对象实例</param>
        public async UniTask RegisterKit<T>(T obj) where T : class
        {
            if (obj == null)
            {
                Debug.LogError("RegisterKit 失败：obj 为空");
                return;
            }

            // 直接缓存对象
            UpdateObjectCache(obj);

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 注册 MonoBehaviour 组件类，将其挂到传入的 GameObject 并加入对象缓存。
        /// 若目标 GameObject 已存在该组件，则复用该组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="go">要挂载到的 GameObject</param>
        public async UniTask RegisterMonoKit<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                Debug.LogError("RegisterMonoKit 失败：GameObject 为空");
                return;
            }

            // 获取或添加组件
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }

            // 写入缓存
            UpdateObjectCache(comp);

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 直接通过对象类型获取实例（从缓存）
        /// </summary>
        /// <typeparam name="T">要获取的对象类型</typeparam>
        /// <returns>对象实例或 null</returns>
        public T GetObject<T>() where T : class
        {
            var objectType = typeof(T);
            if (_objectCache.TryGetValue(objectType, out var cachedObj) && cachedObj is T cached)
            {
                return cached;
            }

            Debug.LogError($"对象 {objectType.Name} 未找到（尚未注册或初始化）");
            return null;
        }

        /// <summary>
        /// 更新对象缓存
        /// </summary>
        /// <param name="obj">实例对象</param>
        private void UpdateObjectCache(object obj)
        {
            var objectType = obj.GetType();
            _objectCache[objectType] = obj;
        }

        /// <summary>
        /// 清理对象缓存
        /// </summary>
        /// <param name="obj">实例对象</param>
        private void ClearObjectCache(object obj)
        {
            var objectType = obj.GetType();
            _objectCache.Remove(objectType);
        }

        /// <summary>
        /// 销毁所有已注册对象
        /// </summary>
        public void Clear()
        {
            Debug.Log("开始销毁所有对象...");
            // 由于不再维护注册顺序，按当前缓存的逆序进行尽力释放
            var values = _objectCache.Values.ToList();
            for (var i = values.Count - 1; i >= 0; i--)
            {
                var obj = values[i];
                try
                {
                    if (obj != null)
                    {
                        if (obj is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        ClearObjectCache(obj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"释放对象失败: {obj?.GetType().Name} - {ex.Message}");
                }
            }

            _objectCache.Clear();

            Debug.Log("所有对象销毁完成");
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
