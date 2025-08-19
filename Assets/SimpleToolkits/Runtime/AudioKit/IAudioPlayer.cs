using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// 音频播放器接口
    /// </summary>
    public interface IAudioPlayer : IDisposable
    {
        /// <summary>
        /// 音频源组件
        /// </summary>
        AudioSource AudioSource { get; }

        /// <summary>
        /// 当前播放的音频剪辑
        /// </summary>
        AudioClip CurrentClip { get; }

        /// <summary>
        /// 是否正在播放
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 是否已暂停
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 音量
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// 音调
        /// </summary>
        float Pitch { get; set; }

        /// <summary>
        /// 是否循环播放
        /// </summary>
        bool Loop { get; set; }

        /// <summary>
        /// 开始播放事件
        /// </summary>
        event Action<IAudioPlayer> OnPlayStarted;

        /// <summary>
        /// 播放完成事件
        /// </summary>
        event Action<IAudioPlayer> OnPlayCompleted;

        /// <summary>
        /// 播放音频剪辑
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        void Play(AudioClip clip);

        /// <summary>
        /// 异步播放音频剪辑
        /// </summary>
        /// <param name="clipPath">音频剪辑路径</param>
        UniTask PlayAsync(string clipPath);

        /// <summary>
        /// 播放一次性音效（可重叠）
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        void PlayOneShot(AudioClip clip);

        /// <summary>
        /// 异步播放一次性音效（可重叠）
        /// </summary>
        /// <param name="clipPath">音频剪辑路径</param>
        UniTask PlayOneShotAsync(string clipPath);

        /// <summary>
        /// 暂停播放
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复播放
        /// </summary>
        void Resume();

        /// <summary>
        /// 停止播放
        /// </summary>
        void Stop();

        /// <summary>
        /// 重置播放器状态
        /// </summary>
        void Reset();
    }
}
