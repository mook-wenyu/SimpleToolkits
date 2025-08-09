using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMgr : MonoSingleton<AudioMgr>
{
    private AudioSource _musicSource; // 音乐播放器
    private AudioSource _soundSource; // 音效播放器

    private float _musicVolume = 1f; // 音乐音量
    private float _soundVolume = 1f; // 音效音量

    // AudioClip 缓存字典
    private readonly Dictionary<string, AudioClip> _audioClipDict = new();
    
    public string currentMusicName;
    public string currentSoundName;

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
        if (music == null) return;

        currentMusicName = music.name;
        _audioClipDict.TryAdd(music.name, music);

        _musicSource.clip = music;
        _musicSource.Play();
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="musicName"></param>
    public void PlayMusic(string musicName)
    {
        AudioClip music = null;
        if (!_audioClipDict.ContainsKey(musicName))
        {
            music = Resources.Load<AudioClip>("Audios/" + musicName);
        }
        PlayMusic(music);
    }

    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    /// <param name="volume"></param>
    public void SetMusicVolume(float volume = 1f)
    {
        _musicVolume = volume;
        if (_musicSource == null) return;
        _musicSource.volume = volume;
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseMusic()
    {
        if (_musicSource == null) return;
        _musicSource.Pause();
    }

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeMusic()
    {
        if (_musicSource == null) return;
        _musicSource.UnPause();
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopMusic()
    {
        if (_musicSource == null) return;
        _musicSource.Stop();
        _musicSource.clip = null;
        currentMusicName = null;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sound"></param>
    /// <param name="volume"></param>
    public void PlaySound(AudioClip sound, float volume)
    {
        if (!sound) return;

        currentSoundName = sound.name;
        _audioClipDict.TryAdd(sound.name, sound);

        _soundSource.PlayOneShot(sound, volume);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sound"></param>
    public void PlaySound(AudioClip sound)
    {
        _soundSource.PlayOneShot(sound, _soundVolume);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="soundName"></param>
    /// <param name="volume"></param>
    public void PlaySound(string soundName, float volume)
    {
        AudioClip sound = null;
        if (!_audioClipDict.ContainsKey(soundName))
        {
            sound = Resources.Load<AudioClip>("Audios/" + soundName);
        }
        PlaySound(sound, volume);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="soundName"></param>
    public void PlaySound(string soundName)
    {
        PlaySound(soundName, _soundVolume);
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    /// <param name="volume"></param>
    public void SetSoundVolume(float volume = 1f)
    {
        _soundVolume = volume;
        if (_soundSource == null) return;
        _soundSource.volume = volume;
    }
    
    /// <summary>
    /// 停止音效
    /// </summary>
    public void StopSound()
    {
        if (_soundSource == null) return;
        _soundSource.Stop();
        _soundSource.clip = null;
        currentSoundName = null;
    }

    protected override void OnDestroy()
    {
        StopMusic();
        StopSound();
        _musicSource = null;
        _soundSource = null;
        base.OnDestroy();
    }
}
