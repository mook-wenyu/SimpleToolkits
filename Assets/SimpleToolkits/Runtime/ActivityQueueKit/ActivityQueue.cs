using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 活动队列，用于按顺序执行活动
    /// </summary>
    public class ActivityQueue : IDisposable
    {
        private readonly Queue<IActivity> _activityQueue = new();
        private IActivity _currentActivity;
        private bool _isRunning;
        private bool _isPaused;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new();

        /// <summary>
        /// 当活动开始执行时触发
        /// </summary>
        public event Action<IActivity> OnActivityStart;

        /// <summary>
        /// 当活动完成时触发
        /// </summary>
        public event Action<IActivity> OnActivityComplete;

        /// <summary>
        /// 当队列完成所有活动时触发
        /// </summary>
        public event Action OnQueueComplete;

        /// <summary>
        /// 当前是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 当前是否已暂停
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// 队列中的活动数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _activityQueue.Count + (_currentActivity != null ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// 添加一个活动到队列
        /// </summary>
        public void Enqueue(IActivity activity)
        {
            if (activity == null) return;

            lock (_lock)
            {
                _activityQueue.Enqueue(activity);
            }
        }

        /// <summary>
        /// 开始执行队列中的活动
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _isPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();

            ProcessQueue().Forget();
        }

        /// <summary>
        /// 暂停队列执行
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            _currentActivity?.Interrupt();
        }

        /// <summary>
        /// 恢复队列执行
        /// </summary>
        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;

            _isPaused = false;
            ProcessQueue().Forget();
        }

        /// <summary>
        /// 清空队列并停止当前活动
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _activityQueue.Clear();
                _currentActivity?.Interrupt();
                _currentActivity = null;
            }
        }

        /// <summary>
        /// 停止队列执行并清空队列
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            Clear();
        }

        private async UniTaskVoid ProcessQueue()
        {
            try
            {
                while (_isRunning && !_cancellationTokenSource.IsCancellationRequested)
                {
                    if (_isPaused)
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    lock (_lock)
                    {
                        if (_activityQueue.Count == 0)
                        {
                            _isRunning = false;
                            OnQueueComplete?.Invoke();
                            return;
                        }

                        _currentActivity = _activityQueue.Dequeue();
                    }

                    try
                    {
                        OnActivityStart?.Invoke(_currentActivity);
                        await _currentActivity.Execute(_cancellationTokenSource.Token);
                        OnActivityComplete?.Invoke(_currentActivity);
                    }
                    catch (OperationCanceledException)
                    {
                        // 活动被取消是正常情况，不记录错误
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Activity {_currentActivity.GetType().Name} failed: {ex}");
                    }
                    finally
                    {
                        _currentActivity = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Activity queue error: {ex}");
                _isRunning = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
