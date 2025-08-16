using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 简单工具包全局管理器
    /// </summary>
    public class Mgr : MonoSingleton<Mgr>
    {
        /// <summary>
        /// 配置管理器设置
        /// </summary>
        public SimpleToolkitsSettings Settings { get; private set; }

        /// <summary>
        /// 资源加载器
        /// </summary>
        public YooAssetLoader Loader { get; private set; }
        /// <summary>
        /// 本地化管理器
        /// </summary>
        public Locale Locale { get; private set; }
        /// <summary>
        /// 控制台管理器
        /// </summary>
        public ConsoleBehaviour Console { get; private set; }
        /// <summary>
        /// 数据配置管理器
        /// </summary>
        public ConfigData Data { get; private set; }
        /// <summary>
        /// 场景管理器
        /// </summary>
        public SceneBehaviour Scene { get; private set; }
        /// <summary>
        /// 对象池管理器
        /// </summary>
        public PoolMgr Pool { get; private set; }
        /// <summary>
        /// UI管理器
        /// </summary>
        public UIBehaviour UI { get; private set; }

        /// <summary>
        /// 初始化全局管理器
        /// </summary>
        public async UniTask Init()
        {
            // 加载配置
            Settings = Resources.Load<SimpleToolkitsSettings>(Constants.SimpleToolkitsSettingsName);
            // 初始化资源加载器
            Loader = new YooAssetLoader(Settings.GamePlayMode);
            await Loader.InitPackagesAsync(Settings.YooPackageInfos);
            // 初始化数据配置管理器
            Data = new ConfigData();
            await Data.LoadAllAsync(Constants.JsonConfigsAssetTagName);
            // 初始化本地化管理器
            Locale = new Locale();
            Locale.InitLanguage();
            // 初始化控制台管理器
            if (!Console)
            {
                Console = gameObject.AddComponent<ConsoleBehaviour>();
            }
            // 初始化场景管理器
            if (!Scene)
            {
                Scene = gameObject.AddComponent<SceneBehaviour>();
            }
            // 初始化对象池管理器
            Pool = new PoolMgr();
            // 初始化UI管理器
            if (!UI)
            {
                UI = gameObject.AddComponent<UIBehaviour>();
            }
        }

        protected override void OnDestroy()
        {
            Destroy(UI);
            UI = null;
            Destroy(Scene);
            Scene = null;
            Destroy(Console);
            Console = null;

            Pool?.Clear();
            Pool = null;
            Locale?.Dispose();
            Locale = null;
            Data?.Dispose();
            Data = null;
            Loader?.Dispose();
            Loader = null;

            Settings = null;

            base.OnDestroy();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


    }
}
