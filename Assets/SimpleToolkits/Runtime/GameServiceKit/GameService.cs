using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 服务信息包装类
    /// </summary>
    internal class ServiceInfo
    {
        public object Instance { get; set; }
        public Type ServiceType { get; set; }
        public bool IsInitialized { get; set; }
        public Type[] Dependencies { get; set; }

        public ServiceInfo(object instance, Type serviceType, Type[] dependencies = null)
        {
            Instance = instance;
            ServiceType = serviceType;
            Dependencies = dependencies ?? Array.Empty<Type>();
            IsInitialized = false;
        }
    }

    /// <summary>
    /// 统一的游戏服务管理器
    /// </summary>
    public class GameService : IDisposable
    {
        // 存储所有服务的字典，key为服务类型，value为服务信息
        private readonly Dictionary<Type, ServiceInfo> _services = new();
        // 对象缓存字典，key为对象类型，value为具体的对象实例
        private readonly Dictionary<Type, object> _objectCache = new();

        // 已初始化的服务集合
        private readonly HashSet<Type> _initializedServices = new();

        public GameService() { }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">服务实例</param>
        /// <param name="dependencies">服务依赖的其他服务类型</param>
        /// <returns>是否注册成功</returns>
        public bool RegisterService<T>(T service, params Type[] dependencies) where T : class
        {
            var serviceType = typeof(T);

            if (_services.ContainsKey(serviceType))
            {
                Debug.LogError($"Service {serviceType.Name} already registered");
                return false;
            }

            // 如果服务实现了IGameService接口，使用接口定义的依赖
            if (service is IGameService gameService)
            {
                dependencies = gameService.Dependencies;
            }

            var serviceInfo = new ServiceInfo(service, serviceType, dependencies);
            _services[serviceType] = serviceInfo;

            Debug.Log($"Service {serviceType.Name} registered successfully");
            return true;
        }

        /// <summary>
        /// 注册服务（通过类型自动创建实例）
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="dependencies">服务依赖的其他服务类型</param>
        /// <returns>是否注册成功</returns>
        public bool RegisterService<T>(params Type[] dependencies) where T : class, new()
        {
            var service = new T();
            return RegisterService(service, dependencies);
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否注销成功</returns>
        public bool UnregisterService<T>() where T : class
        {
            var serviceType = typeof(T);

            if (!_services.TryGetValue(serviceType, out var serviceInfo))
            {
                Debug.LogError($"Service {serviceType.Name} not found");
                return false;
            }

            // 如果服务实现了IDisposable或IGameService，调用销毁方法
            if (serviceInfo.Instance is IGameService gameService)
            {
                // 清理对象缓存
                ClearObjectCache(gameService);
                gameService.Dispose();
            }
            else if (serviceInfo.Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            // 如果是MonoBehaviour组件，销毁GameObject组件
            if (serviceInfo.Instance is Component component)
            {
                UnityEngine.Object.Destroy(component);
            }

            _services.Remove(serviceType);
            _initializedServices.Remove(serviceType);

            Debug.Log($"Service {serviceType.Name} unregistered successfully");
            return true;
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例，如果未找到返回null</returns>
        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);

            if (_services.TryGetValue(serviceType, out var serviceInfo))
            {
                return serviceInfo.Instance as T;
            }

            Debug.LogError($"Service {serviceType.Name} not found");
            return null;
        }

        /// <summary>
        /// 直接获取指定服务内部封装的具体对象实例
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TObject">要获取的对象类型</typeparam>
        /// <returns>服务内部的具体对象实例，如果未找到返回null</returns>
        public TObject GetServiceObject<TService, TObject>()
        where TService : class, IGameService
        where TObject : class
        {
            // 优先从缓存中查找
            var objectType = typeof(TObject);
            if (_objectCache.TryGetValue(objectType, out var cachedObj) && cachedObj is TObject cachedResult)
            {
                return cachedResult;
            }

            var service = GetService<TService>();
            if (service == null)
            {
                Debug.LogError($"Service {typeof(TService).Name} not found");
                return null;
            }

            // 直接调用接口方法，无需反射
            var obj = service.GetObject();
            if (obj is TObject result)
            {
                // 更新缓存
                _objectCache[objectType] = result;
                return result;
            }

            Debug.LogError(obj == null ? $"GetObject() method in service {typeof(TService).Name} returned null" :
                $"Object returned by service {typeof(TService).Name} is of type {obj.GetType().Name}, expected {typeof(TObject).Name}");

            return null;
        }

        /// <summary>
        /// 直接获取服务内部封装的具体对象实例（自动查找服务）
        /// </summary>
        /// <typeparam name="T">要获取的对象类型</typeparam>
        /// <returns>服务内部的具体对象实例，如果未找到返回null</returns>
        public T GetObject<T>() where T : class
        {
            // 优先从缓存中查找
            var objectType = typeof(T);
            if (_objectCache.TryGetValue(objectType, out var cachedObj) && cachedObj is T cachedResult)
            {
                return cachedResult;
            }

            // 如果缓存中没有，则遍历所有服务查找
            foreach (var serviceInfo in _services.Values)
            {
                if (serviceInfo.Instance is IGameService gameService)
                {
                    // 直接调用接口方法，无需反射
                    var obj = gameService.GetObject();
                    if (obj is T result)
                    {
                        // 更新缓存
                        _objectCache[objectType] = result;
                        return result;
                    }
                }
            }

            Debug.LogError($"Object of type {typeof(T).Name} not found in any registered service");
            return null;
        }

        /// <summary>
        /// 初始化所有已注册的服务（按依赖关系排序）
        /// </summary>
        public async UniTask InitializeAllServicesAsync()
        {
            Debug.Log("开始初始化所有服务...");

            // 构建依赖关系图并进行拓扑排序
            var sortedServices = TopologicalSort();

            // 按依赖顺序初始化服务
            foreach (var serviceType in sortedServices)
            {
                await InitializeServiceAsync(serviceType);
            }

            Debug.Log("所有服务初始化完成");
        }

        /// <summary>
        /// 初始化指定服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        public async UniTask InitializeServiceAsync<T>() where T : class
        {
            await InitializeServiceAsync(typeof(T));
        }

        /// <summary>
        /// 初始化指定类型的服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        private async UniTask InitializeServiceAsync(Type serviceType)
        {
            if (_initializedServices.Contains(serviceType))
            {
                return; // 已经初始化过了
            }

            if (!_services.TryGetValue(serviceType, out var serviceInfo))
            {
                Debug.LogError($"Service {serviceType.Name} not registered");
                return;
            }

            // 先初始化依赖的服务
            foreach (var dependencyType in serviceInfo.Dependencies)
            {
                await InitializeServiceAsync(dependencyType);
            }

            // 初始化当前服务
            try
            {
                if (serviceInfo.Instance is IGameService gameService)
                {
                    await gameService.InitializeAsync();

                    // 服务初始化完成后，更新对象缓存
                    UpdateObjectCache(gameService);
                }

                serviceInfo.IsInitialized = true;
                _initializedServices.Add(serviceType);

                Debug.Log($"Service {serviceType.Name} initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize service {serviceType.Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 拓扑排序，确定服务初始化顺序
        /// </summary>
        /// <returns>按依赖关系排序的服务类型列表</returns>
        private List<Type> TopologicalSort()
        {
            var result = new List<Type>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();

            foreach (var serviceType in _services.Keys)
            {
                if (!visited.Contains(serviceType))
                {
                    TopologicalSortVisit(serviceType, visited, visiting, result);
                }
            }

            return result;
        }

        /// <summary>
        /// 拓扑排序的递归访问方法
        /// </summary>
        private void TopologicalSortVisit(Type serviceType, HashSet<Type> visited, HashSet<Type> visiting, List<Type> result)
        {
            if (visiting.Contains(serviceType))
            {
                throw new InvalidOperationException($"Circular dependency detected involving service {serviceType.Name}");
            }

            if (visited.Contains(serviceType))
            {
                return;
            }

            visiting.Add(serviceType);

            if (_services.TryGetValue(serviceType, out var serviceInfo))
            {
                foreach (var dependency in serviceInfo.Dependencies)
                {
                    if (_services.ContainsKey(dependency))
                    {
                        TopologicalSortVisit(dependency, visited, visiting, result);
                    }
                    else
                    {
                        Debug.LogWarning($"Service {serviceType.Name} depends on {dependency.Name}, but it's not registered");
                    }
                }
            }

            visiting.Remove(serviceType);
            visited.Add(serviceType);
            result.Add(serviceType);
        }

        /// <summary>
        /// 更新服务的对象缓存
        /// </summary>
        /// <param name="service">服务实例</param>
        private void UpdateObjectCache(IGameService service)
        {
            // 直接调用接口方法，无需反射
            var obj = service.GetObject();
            if (obj == null) return;
            var objectType = obj.GetType();
            _objectCache[objectType] = obj;
        }

        /// <summary>
        /// 清理服务的对象缓存
        /// </summary>
        /// <param name="service">服务实例</param>
        private void ClearObjectCache(IGameService service)
        {
            // 直接调用接口方法，无需反射
            var obj = service.GetObject();
            if (obj == null) return;
            var objectType = obj.GetType();
            _objectCache.Remove(objectType);
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已注册</returns>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 检查服务是否已初始化
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已初始化</returns>
        public bool IsServiceInitialized<T>() where T : class
        {
            var serviceType = typeof(T);
            return _services.TryGetValue(serviceType, out var serviceInfo) && serviceInfo.IsInitialized;
        }

        /// <summary>
        /// 获取所有已注册的服务类型
        /// </summary>
        /// <returns>服务类型列表</returns>
        public IEnumerable<Type> GetRegisteredServiceTypes()
        {
            return _services.Keys;
        }

        /// <summary>
        /// 获取服务数量
        /// </summary>
        /// <returns>已注册的服务数量</returns>
        public int GetServiceCount()
        {
            return _services.Count;
        }

        /// <summary>
        /// 刷新指定服务的对象缓存
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        public void RefreshServiceObjectCache<T>() where T : class, IGameService
        {
            var service = GetService<T>();
            if (service == null) return;
            // 先清理旧的缓存
            ClearObjectCache(service);
            // 再更新新的缓存
            UpdateObjectCache(service);
        }

        /// <summary>
        /// 刷新所有服务的对象缓存
        /// </summary>
        public void RefreshAllObjectCache()
        {
            _objectCache.Clear();

            foreach (var serviceInfo in _services.Values)
            {
                if (serviceInfo.Instance is IGameService gameService && serviceInfo.IsInitialized)
                {
                    UpdateObjectCache(gameService);
                }
            }

            Debug.Log("所有服务对象缓存已刷新");
        }

        /// <summary>
        /// 销毁所有服务
        /// </summary>
        public void Clear()
        {
            Debug.Log("开始销毁所有服务...");

            // 按初始化的逆序销毁服务
            var servicesToDispose = _initializedServices.ToList();
            servicesToDispose.Reverse();

            foreach (var serviceType in servicesToDispose)
            {
                if (_services.TryGetValue(serviceType, out var serviceInfo))
                {
                    try
                    {
                        if (serviceInfo.Instance is IGameService gameService)
                        {
                            gameService.Dispose();
                        }
                        else if (serviceInfo.Instance is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        if (serviceInfo.Instance is Component component)
                        {
                            UnityEngine.Object.Destroy(component);
                        }

                        Debug.Log($"Service {serviceType.Name} disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to dispose service {serviceType.Name}: {ex.Message}");
                    }
                }
            }

            _services.Clear();
            _initializedServices.Clear();
            _objectCache.Clear();

            Debug.Log("所有服务销毁完成");
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
