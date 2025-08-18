using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleToolkits
{
    public class AudioKit : MonoBehaviour
    {
        private AudioSource _musicSource; // 音乐播放器
        private AudioSource _voiceSource; // 人声播放器
        private AudioSource _soundSource; // 音效播放器

        private float _musicVolume = 1f; // 音乐音量
        private float _voiceVolume = 1f; // 人声音量
        private float _soundVolume = 1f; // 音效音量

        /// <summary>
        /// 当前播放的音乐
        /// </summary>
        public AudioClip currentMusic;
        /// <summary>
        /// 当前播放的人声
        /// </summary>
        public AudioClip currentVoice;
        /// <summary>
        /// 当前播放的音效
        /// </summary>
        public AudioClip currentSound;

        public AudioClip CurrentMusic => currentMusic;
        public AudioClip CurrentVoice => currentVoice;
        public AudioClip CurrentSound => currentSound;

        private void Awake()
        {
            gameObject.transform.position = Vector3.zero;

            // 初始化音频
            if (!_musicSource)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }
            if (!_voiceSource)
            {
                _voiceSource = gameObject.AddComponent<AudioSource>();
            }
            if (!_soundSource)
            {
                _soundSource = gameObject.AddComponent<AudioSource>();
            }

            // 设置音频源属性

            _musicSource.loop = true;
            _voiceSource.loop = false;
            _soundSource.loop = false;

            _musicSource.pitch = 1f;
            _voiceSource.pitch = 1f;
            _soundSource.pitch = 1f;

            SetMusicVolume();
            SetVoiceVolume();
            SetSoundVolume();
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="music"></param>
        public void PlayMusic(AudioClip music)
        {
            if (!music) return;
            _musicSource.clip = music;
            _musicSource.Play();
            currentMusic = music;
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="musicPath"></param>
        public async UniTaskVoid PlayMusic(string musicPath)
        {
            currentMusic = await GSMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<AudioClip>(musicPath);
            PlayMusic(currentMusic);
        }

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume"></param>
        public void SetMusicVolume(float volume = 1f)
        {
            _musicVolume = volume;
            if (!_musicSource) return;
            _musicSource.volume = volume;
        }

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        public void PauseMusic()
        {
            if (!_musicSource) return;
            _musicSource.Pause();
        }

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public void ResumeMusic()
        {
            if (!_musicSource) return;
            _musicSource.UnPause();
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopMusic()
        {
            if (!_musicSource) return;
            _musicSource.Stop();
            _musicSource.clip = null;
            currentMusic = null;
        }

        /// <summary>
        /// 播放人声
        /// </summary>
        /// <param name="voice"></param>
        public void PlayVoice(AudioClip voice)
        {
            if (!voice) return;
            _voiceSource.clip = voice;
            _voiceSource.Play();
            currentVoice = voice;
        }

        /// <summary>
        /// 播放人声
        /// </summary>
        /// <param name="voicePath"></param>
        public async UniTaskVoid PlayVoice(string voicePath)
        {
            currentVoice = await GSMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<AudioClip>(voicePath);
            PlayVoice(currentVoice);
        }

        /// <summary>
        /// 设置人声音量
        /// </summary>
        /// <param name="volume"></param>
        public void SetVoiceVolume(float volume = 1f)
        {
            _voiceVolume = volume;
            if (!_voiceSource) return;
            _voiceSource.volume = volume;
        }

        /// <summary>
        /// 暂停人声
        /// </summary>
        public void PauseVoice()
        {
            if (!_voiceSource) return;
            _voiceSource.Pause();
        }

        /// <summary>
        /// 恢复人声
        /// </summary>
        public void ResumeVoice()
        {
            if (!_voiceSource) return;
            _voiceSource.UnPause();
        }

        /// <summary>
        /// 停止人声
        /// </summary>
        public void StopVoice()
        {
            if (!_voiceSource) return;
            _voiceSource.Stop();
            _voiceSource.clip = null;
            currentVoice = null;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="sound"></param>
        public void PlaySound(AudioClip sound)
        {
            if (!sound) return;
            _soundSource.clip = sound;
            _soundSource.Play();
            currentSound = sound;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundPath"></param>
        public async UniTaskVoid PlaySound(string soundPath)
        {
            currentSound = await GSMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<AudioClip>(soundPath);
            PlaySound(currentSound);
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume"></param>
        public void SetSoundVolume(float volume = 1f)
        {
            _soundVolume = volume;
            if (!_soundSource) return;
            _soundSource.volume = volume;
        }

        /// <summary>
        /// 暂停音效
        /// </summary>
        public void PauseSound()
        {
            if (!_soundSource) return;
            _soundSource.Pause();
        }

        /// <summary>
        /// 恢复音效
        /// </summary>
        public void ResumeSound()
        {
            if (!_soundSource) return;
            _soundSource.UnPause();
        }

        /// <summary>
        /// 停止音效
        /// </summary>
        public void StopSound()
        {
            if (!_soundSource) return;
            _soundSource.Stop();
            _soundSource.clip = null;
        }

        private void OnDestroy()
        {
            StopMusic();
            StopVoice();
            StopSound();
            _musicSource = null;
            _voiceSource = null;
            _soundSource = null;
            currentMusic = null;
            currentVoice = null;
            currentSound = null;
        }
    }
}
