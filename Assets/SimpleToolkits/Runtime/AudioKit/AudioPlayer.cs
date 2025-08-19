using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 音频播放器实现类
    /// </summary>
    public class AudioPlayer : IAudioPlayer
    {
        private AudioSource _audioSource;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        /// <summary>
        /// 音频源组件
        /// </summary>
        public AudioSource AudioSource => _audioSource;

        /// <summary>
        /// 当前播放的音频剪辑
        /// </summary>
        public AudioClip CurrentClip => _audioSource?.clip;

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

        /// <summary>
        /// 是否已暂停
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 音量
        /// </summary>
        public float Volume
        {
            get => _audioSource?.volume ?? 0f;
            set
            {
                if (_audioSource != null)
                    _audioSource.volume = value;
            }
        }

        /// <summary>
        /// 音调
        /// </summary>
        public float Pitch
        {
            get => _audioSource?.pitch ?? 1f;
            set
            {
                if (_audioSource != null)
                    _audioSource.pitch = value;
            }
        }

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool Loop
        {
            get => _audioSource?.loop ?? false;
            set
            {
                if (_audioSource != null)
                    _audioSource.loop = value;
            }
        }

        /// <summary>
        /// 开始播放事件
        /// </summary>
        public event Action<IAudioPlayer> OnPlayStarted;

        /// <summary>
        /// 播放完成事件
        /// </summary>
        public event Action<IAudioPlayer> OnPlayCompleted;

        /// <summary>
        /// 初始化音频播放器
        /// </summary>
        /// <param name="audioSource">音频源组件</param>
        public void Initialize(AudioSource audioSource)
        {
            _audioSource = audioSource;
            Reset();
        }

        /// <summary>
        /// 播放音频剪辑
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        public void Play(AudioClip clip)
        {
            if (_disposed || _audioSource == null || clip == null)
                return;

            Stop();
            
            _audioSource.clip = clip;
            _audioSource.Play();
            IsPaused = false;

            // 触发开始播放事件
            OnPlayStarted?.Invoke(this);

            // 如果不是循环播放，启动播放完成检测
            if (!_audioSource.loop)
            {
                CheckPlayCompletedAsync().Forget();
            }
        }

        /// <summary>
        /// 异步播放音频剪辑
        /// </summary>
        /// <param name="clipPath">音频剪辑路径</param>
        public async UniTask PlayAsync(string clipPath)
        {
            if (_disposed || string.IsNullOrEmpty(clipPath))
                return;

            var clip = await GKMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<AudioClip>(clipPath);
            Play(clip);
        }

        /// <summary>
        /// 播放一次性音效（可重叠）
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        public void PlayOneShot(AudioClip clip)
        {
            if (_disposed || _audioSource == null || clip == null)
                return;

            _audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 异步播放一次性音效（可重叠）
        /// </summary>
        /// <param name="clipPath">音频剪辑路径</param>
        public async UniTask PlayOneShotAsync(string clipPath)
        {
            if (_disposed || string.IsNullOrEmpty(clipPath))
                return;

            var clip = await GKMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<AudioClip>(clipPath);
            PlayOneShot(clip);
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (_disposed || _audioSource == null || !_audioSource.isPlaying)
                return;

            _audioSource.Pause();
            IsPaused = true;
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            if (_disposed || _audioSource == null || !IsPaused)
                return;

            _audioSource.UnPause();
            IsPaused = false;
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            if (_disposed || _audioSource == null)
                return;

            _audioSource.Stop();
            IsPaused = false;

            // 取消播放完成检测任务
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 重置播放器状态
        /// </summary>
        public void Reset()
        {
            if (_disposed)
                return;

            Stop();
            
            if (_audioSource != null)
            {
                _audioSource.clip = null;
                _audioSource.volume = 1f;
                _audioSource.pitch = 1f;
                _audioSource.loop = false;
            }

            IsPaused = false;
            
            // 重置取消令牌
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 检测播放完成的异步任务
        /// </summary>
        private async UniTaskVoid CheckPlayCompletedAsync()
        {
            try
            {
                // 等待播放完成
                while (_audioSource != null && _audioSource.isPlaying && !_disposed)
                {
                    await UniTask.Yield(_cancellationTokenSource.Token);
                }

                // 如果不是因为取消而结束，则触发播放完成事件
                if (!_cancellationTokenSource.Token.IsCancellationRequested && !_disposed)
                {
                    OnPlayCompleted?.Invoke(this);
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，正常情况
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            OnPlayStarted = null;
            OnPlayCompleted = null;
            _audioSource = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _disposed = true;
        }
    }
}
