using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    public class Test : MonoBehaviour
    {
        private Image _img;
        // 活动队列示例
        private ActivityQueue _activityQueue;

        // ========================= FSM 示例相关 =========================
        // 使用 FSMManager 统一驱动 FiniteStateMachine<Test>
        private FSMManager _fsmMgr;
        private FiniteStateMachine<Test> _fsm;

        // 状态 Id（使用 int，避免字符串比较开销）
        private const int StateIdle = 0;
        private const int StateMove = 1;

        // FSM 内部共享计时器（由状态更新累加，迁移条件使用）
        private float _fsmTimer;

        private void Awake()
        {
            Init().Forget();
        }

        private async UniTaskVoid Init()
        {
            await GKMgr.Instance.Init();

            var configs = GKMgr.Instance.GetObject<ConfigManager>().GetAll<ExampleConfig>();
            StringBuilder sb = new(configs.Count);
            foreach (var c in configs)
            {
                sb.AppendJoin(",", c.id, c.name, c.hp, c.die, c.pos, c.target);
                sb.AppendLine();
                if (c.duiyou != null)
                    sb.AppendJoin(",", c.duiyou);
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());

            _img = GameObject.Find("Image").GetComponent<Image>();
            _img.sprite = await GKMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<Sprite>("test");

            await GKMgr.Instance.GetObject<UIKit>().RegisterPanel<UIConfirmPanel>(UILayerType.Popup, true, true);

            //await UIKit.Instance.OpenPanel<UIConfirmPanel>();
            //await UIKit.Instance.OpenPanel<UIConfirmPanel>();
            //await UIKit.Instance.OpenPanel<UIConfirmPanel>();


            // 初始化 FSM（与业务初始化独立）
            // SetupFSM();

            GKMgr.Instance.GetObject<ConsoleKit>().RegisterCommand("show_tip", "显示提示", (args) =>
            {
                GKMgr.Instance.GetObject<FlyTipManager>().Show(args[0]);
            });
            GKMgr.Instance.GetObject<ConsoleKit>().RegisterQuickButton("显示提示", "show_tip", new[] {"测试"});

            // 启动活动队列示例
            SetupActivityQueue();

            /*var scene = GKMgr.Instance.GetObject<SceneKit>();
            scene.OnLoadSceneProgress += (s, f) =>
            {
                if (s == "TestScene" && f >= 0.9f)
                {
                    Debug.Log($"Scene: {s} progress: {f}");
                    scene.GetSceneOperation(s).SceneHandle.UnSuspend();
                }
            };
            await scene.LoadSceneAsync("TestScene");*/

        }

        // ========================= Mono 行为帧驱动（驱动 FSMManager） =========================
        private void Update()
        {
            // 便捷重载：内部使用 Time.deltaTime
            _fsmMgr?.Tick();
        }

        private void LateUpdate()
        {
            _fsmMgr?.LateTick();
        }

        private void FixedUpdate()
        {
            // 便捷重载：内部使用 Time.fixedDeltaTime
            _fsmMgr?.FixedTick();
        }

        private void OnDestroy()
        {
            // 释放活动队列
            if (_activityQueue != null)
            {
                _activityQueue.Dispose();
                _activityQueue = null;
            }

            if (_fsmMgr == null || _fsm == null) return;
            _fsmMgr.Unregister(_fsm);
            _fsmMgr.Dispose();
            _fsmMgr = null;
            _fsm = null;
        }

        // ========================= FSM 构建 =========================
        private void SetupFSM()
        {
            _fsmMgr = GKMgr.Instance.GetObject<FSMManager>();
            _fsm = new FiniteStateMachine<Test>(this);

            // 添加状态
            _fsm.AddStates(new IFSMState<Test>[]
            {
                new IdleState(),
                new MoveState(),
            });

            // 添加迁移（2 秒自动切换）
            _fsm.AddTransitions(new IFSMTransition<Test>[]
            {
                new IdleToMove(),
                new MoveToIdle(),
            });

            // 设定初始状态
            _fsm.SetInitial(StateIdle);

            // 注册到 FSMManager，统一调度
            _fsmMgr.Register(_fsm);
        }

        // ========================= ActivityQueue 示例 =========================
        private void SetupActivityQueue()
        {
            // 创建队列并订阅事件（用于观察执行过程）
            _activityQueue = new ActivityQueue();
            _activityQueue.OnActivityStart += a => Debug.Log($"[AQ] Start: {a.GetType().Name}");
            _activityQueue.OnActivityComplete += a => Debug.Log($"[AQ] Complete: {a.GetType().Name}");
            _activityQueue.OnQueueComplete += () => Debug.Log("[AQ] Queue Complete");

            // 1) 立即回调：显示开始提示
            _activityQueue.Enqueue(new CallbackActivity(() =>
            {
                GKMgr.Instance.GetObject<FlyTipManager>().Show("队列开始", 1f);
            }));

            // 2) 延迟 1 秒
            _activityQueue.Enqueue(new DelayActivity(1f));

            // 3) 异步回调：0.5 秒后再提示
            _activityQueue.Enqueue(new AsyncCallbackActivity(async ct =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
                GKMgr.Instance.GetObject<FlyTipManager>().Show("0.5 秒后", 1f);
            }));

            // 4) 条件等待：直到 _img 可用（最多等待 2 秒）
            _activityQueue.Enqueue(new WaitUntilActivity(() => _img != null, timeout: 2f));

            // 5) 收尾提示
            _activityQueue.Enqueue(new CallbackActivity(() =>
            {
                GKMgr.Instance.GetObject<FlyTipManager>().Show("队列结束", 1f);
            }));

            // 启动执行
            _activityQueue.Start();
        }

        // ========================= 状态实现（作为嵌套类，直接访问 Test 的字段） =========================
        private sealed class IdleState : IFSMState<Test>
        {
            public int Id => StateIdle;

            public void OnEnter(Test owner)
            {
                // 进入 Idle 重置计时
                owner._fsmTimer = 0f;
                #if UNITY_EDITOR
                Debug.Log("[FSM] 进入 Idle");
                #endif
            }

            public void OnExit(Test owner)
            {
            }

            public void OnUpdate(Test owner, float deltaTime)
            {
                // Idle 状态仅计时
                owner._fsmTimer += deltaTime;
            }

            public void OnLateUpdate(Test owner)
            {
            }

            public void OnFixedUpdate(Test owner, float fixedDeltaTime)
            {
            }
        }

        private sealed class MoveState : IFSMState<Test>
        {
            public int Id => StateMove;

            public void OnEnter(Test owner)
            {
                owner._fsmTimer = 0f;
                #if UNITY_EDITOR
                Debug.Log("[FSM] 进入 Move");
                #endif
            }

            public void OnExit(Test owner)
            {
            }

            public void OnUpdate(Test owner, float deltaTime)
            {
                owner._fsmTimer += deltaTime;

                // 可选：在 Move 状态下让图片做一个轻微的上下浮动
                if (owner._img)
                {
                    var rt = owner._img.rectTransform;
                    var pos = rt.anchoredPosition;
                    pos.y = Mathf.Sin(Time.time * 3f) * 10f; // 小幅度上下浮动
                    rt.anchoredPosition = pos;
                }
            }

            public void OnLateUpdate(Test owner)
            {
            }

            public void OnFixedUpdate(Test owner, float fixedDeltaTime)
            {
            }
        }

        // ========================= 迁移实现（根据计时器 2 秒切换） =========================
        private sealed class IdleToMove : IFSMTransition<Test>
        {
            public int FromId => StateIdle;
            public int ToId => StateMove;
            public int Priority => 0;

            public bool CanTransition(Test owner)
            {
                return owner._fsmTimer >= 2f; // Idle 停留 2 秒后进入 Move
            }
        }

        private sealed class MoveToIdle : IFSMTransition<Test>
        {
            public int FromId => StateMove;
            public int ToId => StateIdle;
            public int Priority => 0;

            public bool CanTransition(Test owner)
            {
                return owner._fsmTimer >= 2f; // Move 停留 2 秒后回到 Idle
            }
        }
    }
}
