using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    public class AudioKit : MonoBehaviour
    {
        private AudioPlayer _musicPlayer;
        private AudioPlayer _effectPlayer;
        private AudioPlayer _voicePlayer;

        /// <summary>
        /// 音乐播放器
        /// </summary>
        public IAudioPlayer MusicPlayer => _musicPlayer;
        
        /// <summary>
        /// 音效播放器
        /// </summary>
        public IAudioPlayer EffectPlayer => _effectPlayer;
        
        /// <summary>
        /// 人声播放器
        /// </summary>
        public IAudioPlayer VoicePlayer => _voicePlayer;

        /// <summary>
        /// 当前播放的音乐
        /// </summary>
        public AudioClip CurrentMusic => _musicPlayer?.CurrentClip;
        
        /// <summary>
        /// 当前播放的音效
        /// </summary>
        public AudioClip CurrentEffect => _effectPlayer?.CurrentClip;
        
        /// <summary>
        /// 当前播放的人声
        /// </summary>
        public AudioClip CurrentVoice => _voicePlayer?.CurrentClip;

        private void Awake()
        {
            gameObject.transform.position = Vector3.zero;
            InitializeAudioSystem();
        }

        /// <summary>
        /// 初始化音频系统
        /// </summary>
        private void InitializeAudioSystem()
        {
            // 创建3个固定的音频播放器
            _musicPlayer = CreateAudioPlayer();
            _musicPlayer.Loop = true; // 音乐默认循环播放
            
            _effectPlayer = CreateAudioPlayer();
            _effectPlayer.Loop = false;
            
            _voicePlayer = CreateAudioPlayer();
            _voicePlayer.Loop = false;
        }

        /// <summary>
        /// 创建音频播放器
        /// </summary>
        private AudioPlayer CreateAudioPlayer()
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            var player = new AudioPlayer();
            player.Initialize(audioSource);
            return player;
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="music">音乐剪辑</param>
        public void PlayMusic(AudioClip music)
        {
            if (music == null) return;
            _musicPlayer.Play(music);
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="musicPath">音乐路径</param>
        public async UniTask PlayMusic(string musicPath)
        {
            if (string.IsNullOrEmpty(musicPath)) return;
            await _musicPlayer.PlayAsync(musicPath);
        }

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">音量值</param>
        public void SetMusicVolume(float volume = 1f)
        {
            _musicPlayer.Volume = volume;
        }

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        public void PauseMusic()
        {
            _musicPlayer?.Pause();
        }

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public void ResumeMusic()
        {
            _musicPlayer?.Resume();
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopMusic()
        {
            _musicPlayer?.Stop();
        }

        /// <summary>
        /// 播放音效（可重叠）
        /// </summary>
        /// <param name="effect">音效剪辑</param>
        public void PlayEffect(AudioClip effect)
        {
            if (effect == null) return;
            _effectPlayer.PlayOneShot(effect);
        }

        /// <summary>
        /// 播放音效（可重叠）
        /// </summary>
        /// <param name="effectPath">音效路径</param>
        public async UniTask PlayEffect(string effectPath)
        {
            if (string.IsNullOrEmpty(effectPath)) return;
            await _effectPlayer.PlayOneShotAsync(effectPath);
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">音量值</param>
        public void SetEffectVolume(float volume = 1f)
        {
            _effectPlayer.Volume = volume;
        }

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAllEffects()
        {
            _effectPlayer?.Stop();
        }

        /// <summary>
        /// 播放人声
        /// </summary>
        /// <param name="voice">人声剪辑</param>
        public void PlayVoice(AudioClip voice)
        {
            if (voice == null) return;
            _voicePlayer.Play(voice);
        }

        /// <summary>
        /// 播放人声
        /// </summary>
        /// <param name="voicePath">人声路径</param>
        public async UniTask PlayVoice(string voicePath)
        {
            if (string.IsNullOrEmpty(voicePath)) return;
            await _voicePlayer.PlayAsync(voicePath);
        }

        /// <summary>
        /// 设置人声音量
        /// </summary>
        /// <param name="volume">音量值</param>
        public void SetVoiceVolume(float volume = 1f)
        {
            _voicePlayer.Volume = volume;
        }

        /// <summary>
        /// 暂停人声
        /// </summary>
        public void PauseVoice()
        {
            _voicePlayer?.Pause();
        }

        /// <summary>
        /// 恢复人声
        /// </summary>
        public void ResumeVoice()
        {
            _voicePlayer?.Resume();
        }

        /// <summary>
        /// 停止人声
        /// </summary>
        public void StopVoice()
        {
            _voicePlayer?.Stop();
        }

        /// <summary>
        /// 停止所有音频播放
        /// </summary>
        public void StopAll()
        {
            StopMusic();
            StopAllEffects();
            StopVoice();
        }

        /// <summary>
        /// 暂停所有音频播放
        /// </summary>
        public void PauseAll()
        {
            PauseMusic();
            PauseVoice();
        }

        /// <summary>
        /// 恢复所有音频播放
        /// </summary>
        public void ResumeAll()
        {
            ResumeMusic();
            ResumeVoice();
        }

        private void OnDestroy()
        {
            StopAll();
            
            // 释放播放器资源
            _musicPlayer?.Dispose();
            _effectPlayer?.Dispose();
            _voicePlayer?.Dispose();
            
            // 清理引用
            _musicPlayer = null;
            _effectPlayer = null;
            _voicePlayer = null;
        }
    }
}
