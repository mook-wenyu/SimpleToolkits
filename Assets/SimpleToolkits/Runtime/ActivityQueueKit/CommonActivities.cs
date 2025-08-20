using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 延迟活动
    /// </summary>
    public class DelayActivity : ActivityBase
    {
        private readonly float _delaySeconds;

        public DelayActivity(float delaySeconds, string name = null) : base(name)
        {
            _delaySeconds = Mathf.Max(0, delaySeconds);
        }

        public override async UniTask Execute(CancellationToken cancellationToken = default)
        {
            if (_delaySeconds <= 0) return;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_delaySeconds), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 延迟被取消是正常情况
            }
        }
    }

    /// <summary>
    /// 回调活动
    /// </summary>
    public class CallbackActivity : ActivityBase
    {
        private readonly Action _callback;

        public CallbackActivity(Action callback, string name = null) : base(name)
        {
            _callback = callback;
        }

        public override UniTask Execute(CancellationToken cancellationToken = default)
        {
            _callback?.Invoke();
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 异步回调活动
    /// </summary>
    public class AsyncCallbackActivity : ActivityBase
    {
        private readonly Func<CancellationToken, UniTask> _asyncCallback;

        public AsyncCallbackActivity(Func<CancellationToken, UniTask> asyncCallback, string name = null) : base(name)
        {
            _asyncCallback = asyncCallback;
        }

        public override async UniTask Execute(CancellationToken cancellationToken = default)
        {
            if (_asyncCallback != null)
            {
                await _asyncCallback(cancellationToken);
            }
        }
    }

    /// <summary>
    /// 协程活动
    /// </summary>
    public class CoroutineActivity : ActivityBase
    {
        private readonly Func<IEnumerator> _coroutineFactory;
        private Coroutine _coroutine;
        private readonly MonoBehaviour _runner;

        public CoroutineActivity(MonoBehaviour runner, Func<IEnumerator> coroutineFactory, string name = null) : base(name)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _coroutineFactory = coroutineFactory ?? throw new ArgumentNullException(nameof(coroutineFactory));
        }

        public override async UniTask Execute(CancellationToken cancellationToken = default)
        {
            if (_coroutineFactory == null) return;

            var completionSource = new UniTaskCompletionSource();

            _coroutine = _runner.StartCoroutine(RunCoroutine(completionSource));

            await using (cancellationToken.Register(Interrupt))
            {
                await completionSource.Task;
            }
        }

        private IEnumerator RunCoroutine(UniTaskCompletionSource completionSource)
        {
            yield return _coroutineFactory();
            completionSource.TrySetResult();
        }

        public override void Interrupt()
        {
            base.Interrupt();

            if (_coroutine != null && _runner != null)
            {
                _runner.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }

    /// <summary>
    /// 条件等待活动
    /// </summary>
    public class WaitUntilActivity : ActivityBase
    {
        private readonly Func<bool> _condition;
        private readonly float _timeout;
        private readonly bool _withRealTime;

        public WaitUntilActivity(Func<bool> condition, float timeout = -1f, bool withRealTime = false, string name = null) : base(name)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _timeout = timeout;
            _withRealTime = withRealTime;
        }

        public override async UniTask Execute(CancellationToken cancellationToken = default)
        {
            if (_condition == null) return;

            var startTime = _withRealTime ? Time.realtimeSinceStartup : Time.time;

            while (!_condition() && !mIsInterrupted && !cancellationToken.IsCancellationRequested)
            {
                if (_timeout > 0)
                {
                    var currentTime = _withRealTime ? Time.realtimeSinceStartup : Time.time;
                    if (currentTime - startTime >= _timeout)
                    {
                        break;
                    }
                }

                await UniTask.Yield(cancellationToken);
            }
        }
    }
}
