using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMgr : MonoSingleton<AudioMgr>
{
    private AudioSource _musicSource; // 音乐播放器
    private AudioSource _soundSource; // 音效播放器

    // private AudioMixer _audioMixer;

    private float _musicVolume = 1f; // 音乐音量
    private float _soundVolume = 1f; // 音效音量
    
    /// <summary>
    /// 当前播放的音乐
    /// </summary>
    public AudioClip currentMusic;
    /// <summary>
    /// 当前播放的音效
    /// </summary>
    public AudioClip currentSound;

    public override void OnSingletonInit()
    {
        gameObject.transform.position = Vector3.zero;

        // 初始化音频
        if (!_musicSource)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
        }
        if (!_soundSource)
        {
            _soundSource = gameObject.AddComponent<AudioSource>();
        }

        // 设置音频源属性
        _musicSource.loop = true;
        _soundSource.loop = false;

        SetMusicVolume();
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
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="musicPath"></param>
    public async UniTaskVoid PlayMusic(string musicPath)
    {
        currentMusic = await ResMgr.LoadAssetAsync<AudioClip>(musicPath);
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
    /// 播放音效
    /// </summary>
    /// <param name="sound"></param>
    public void PlaySound(AudioClip sound)
    {
        if (!sound) return;
        _soundSource.clip = sound;
        _soundSource.Play();
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="soundPath"></param>
    public async UniTaskVoid PlaySound(string soundPath)
    {
        currentSound = await ResMgr.LoadAssetAsync<AudioClip>(soundPath);
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
    /// 停止音效
    /// </summary>
    public void StopSound()
    {
        if (!_soundSource) return;
        _soundSource.Stop();
        _soundSource.clip = null;
    }

    protected override void OnDestroy()
    {
        StopMusic();
        StopSound();
        _musicSource = null;
        _soundSource = null;
        currentMusic = null;
        currentSound = null;
        base.OnDestroy();
    }
}
