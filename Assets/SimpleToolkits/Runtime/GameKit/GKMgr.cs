using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 统一的游戏管理器（使用 GameKit 极简版）
    /// </summary>
    public class GKMgr : MonoSingleton<GKMgr>
    {
        /// <summary>
        /// 游戏功能套件
        /// </summary>
        public GameKit Kit { get; private set; } = new();

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

            // 注册所有 Kits（按依赖顺序手动保证）
            await RegisterAllKitsAsync();
        }

        /// <summary>
        /// 按顺序注册所有 Kits（直接注册具体对象）
        /// </summary>
        private async UniTask RegisterAllKitsAsync()
        {
            // 先注册基础依赖（纯 C# 对象）
            var poolManager = new PoolManager();
            await Kit.RegisterKit(poolManager);

            var yooLoader = new YooAssetLoader(Settings.GamePlayMode);
            await yooLoader.InitPackagesAsync(Settings.YooPackageInfos);
            await Kit.RegisterKit(yooLoader);

            // 依赖 YooAssetLoader 的放在其后
            var configMgr = new ConfigManager();
            await configMgr.LoadAllAsync(Constants.JsonConfigsAssetTagName);
            await Kit.RegisterKit(configMgr);

            var localeMgr = new LocaleManager();
            localeMgr.InitLanguage();
            await Kit.RegisterKit(localeMgr);

            var dataMgr = new DataStorageManager(Settings);
            await dataMgr.InitializeAsync();
            await Kit.RegisterKit(dataMgr);

            // MonoBehaviour 组件类（挂到当前 GameObject）
            await Kit.RegisterMonoKit<SceneKit>(gameObject);
            await Kit.RegisterMonoKit<UIKit>(gameObject);
            await Kit.RegisterMonoKit<FlyTipManager>(gameObject);
            await Kit.GetObject<FlyTipManager>().Init();
            await Kit.RegisterMonoKit<ConsoleKit>(gameObject);
            await Kit.RegisterMonoKit<AudioKit>(gameObject);

            // 其它（纯 C# 对象）
            var webManager = new WebManager();
            await Kit.RegisterKit(webManager);

            var pathfindingMgr = new PathfindingManager();
            await Kit.RegisterKit(pathfindingMgr);

            var fsmMgr = new FSMManager();
            await Kit.RegisterKit(fsmMgr);
        }

        /// <summary>
        /// 注册对象（纯 C# 对象）
        /// </summary>
        /// <param name="obj">对象实例</param>
        /// <typeparam name="T">对象类型</typeparam>
        public async UniTask RegisterKit<T>(T obj) where T : class
        {
            await Kit.RegisterKit(obj);
        }

        /// <summary>
        /// 注册 MonoBehaviour 组件类
        /// </summary>
        /// <param name="go">要挂载到的 GameObject</param>
        /// <typeparam name="T">组件类型</typeparam>
        public async UniTask RegisterMonoKit<T>(GameObject go) where T : Component
        {
            await Kit.RegisterMonoKit<T>(go);
        }

        /// <summary>
        /// 直接获取对象（从 GameKit 的对象缓存）
        /// </summary>
        /// <typeparam name="T">要获取的对象类型</typeparam>
        /// <returns>对象实例</returns>
        public T GetObject<T>() where T : class
        {
            return Kit.GetObject<T>();
        }


        protected override void OnDestroy()
        {
            // 销毁所有通过 GameKit 注册的 Kits
            Kit.Dispose();

            Settings = null;

            TypeReflectionUtility.Clear();

            base.OnDestroy();
        }

    }
}
