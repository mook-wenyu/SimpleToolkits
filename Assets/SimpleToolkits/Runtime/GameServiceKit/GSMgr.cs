using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 统一的游戏服务管理器
    /// </summary>
    public class GSMgr : MonoSingleton<GSMgr>
    {
        /// <summary>
        /// 游戏服务管理器
        /// </summary>
        public GameService Service { get; private set; } = new();

        /// <summary>
        /// 配置管理器设置
        /// </summary>
        public SimpleToolkitsSettings Settings { get; private set; }

        /// <summary>
        /// 初始化全局管理器
        /// </summary>
        public async UniTask Init()
        {
            // 加载配置
            Settings = Resources.Load<SimpleToolkitsSettings>(Constants.SimpleToolkitsSettingsName);

            // 注册所有服务到GameService管理器
            RegisterAllServices();

            // 初始化所有服务
            await Service.InitializeAllServicesAsync();
        }

        /// <summary>
        /// 注册所有服务到GameService管理器
        /// </summary>
        private void RegisterAllServices()
        {
            // 注册对象池服务
            Service.RegisterService(new PoolManagerService());

            // 注册资源加载器服务
            Service.RegisterService(new YooAssetLoaderService(Settings));

            // 注册配置数据服务
            Service.RegisterService(new ConfigManagerService());

            // 注册本地化服务
            Service.RegisterService(new LocaleManagerService());

            // 注册数据管理服务
            Service.RegisterService(new DataManagerService(Settings));

            // 注册场景管理服务
            Service.RegisterService(new SceneKitService(gameObject));

            // 注册UI管理服务
            Service.RegisterService(new UIKitService(gameObject));

            // 注册控制台服务
            Service.RegisterService(new ConsoleKitService(gameObject));

            // 注册音频服务
            Service.RegisterService(new AudioKitService(gameObject));
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="service">服务实例</param>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns></returns>
        public bool RegisterService<T>(T service) where T : class
        {
            // 如果是Component类型，需要添加到当前GameObject上
            if (service is Component component)
            {
                var existingComponent = gameObject.GetComponent(component.GetType());
                if (existingComponent == null)
                {
                    // 创建新的组件实例并添加到GameObject上
                    var newComponent = gameObject.AddComponent(component.GetType());
                    return Service.RegisterService(newComponent as T);
                }
                else
                {
                    return Service.RegisterService(existingComponent as T);
                }
            }

            return Service.RegisterService(service);
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns></returns>
        public bool UnRegisterService<T>() where T : class
        {
            return Service.UnregisterService<T>();
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns></returns>
        public T GetService<T>() where T : class
        {
            return Service.GetService<T>();
        }

        /// <summary>
        /// 获取服务内部封装的具体对象实例
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TObject">要获取的对象类型</typeparam>
        /// <returns>服务内部的具体对象实例，如果未找到返回null</returns>
        public TObject GetServiceObject<TService, TObject>()
        where TService : class, IGameService
        where TObject : class
        {
            return Service.GetServiceObject<TService, TObject>();
        }

        /// <summary>
        /// 直接获取服务内部封装的具体对象实例，如果缓存中没有，则遍历所有服务查找
        /// </summary>
        /// <typeparam name="T">要获取的对象类型</typeparam>
        /// <returns>服务内部的具体对象实例，如果未找到返回null</returns>
        public T GetObject<T>() where T : class
        {
            return Service.GetObject<T>();
        }


        protected override void OnDestroy()
        {
            // 销毁所有通过GameService管理的服务
            Service.Dispose();

            Settings = null;

            TypeReflectionUtility.Clear();

            base.OnDestroy();
        }

    }
}
