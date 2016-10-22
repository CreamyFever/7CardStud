using UnityEngine;
using SingletonPattern;

/// <summary>
/// 音を管理するマネージャークラス
/// </summary>
public class SoundManager : SingletonPattern<SoundManager>
{
    public AudioSource bgmSource;
    public AudioSource efxSource;
    public float lowPitchRange = 0.95f;
    public float highPitchRange = 1.05f;

    public AudioListener globalAudioListener;

    public enum SoundState
    {
        Main,
        Lobby,
        Room,
        Win
    }

    public SoundState soundState;

    public static SoundManager _instance;

    public bool isPlaying = false;

    public AudioClip[] bgmClip;
    public AudioClip[] efxClip;

    void Awake()
    {
        _instance = this;

        DontDestroyOnLoad(globalAudioListener);
    }


    void Update()
    {
        if(!isPlaying)
        {
            isPlaying = true;
            switch(soundState)
            {
                case SoundState.Main:
                    ChangeSceneMusic(bgmClip[0]);
                    break;
                case SoundState.Lobby:
                    ChangeSceneMusic(bgmClip[1]);
                    break;
                case SoundState.Room:
                    TurnOffMusic();
                    break;
                case SoundState.Win:
                    ChangeSceneMusic(bgmClip[2]);
                    break;
            }
        }
    }

    /// <summary>
    /// 効果音を再生する。
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySoundEffect(AudioClip clip)
    {
        if (!efxSource.isPlaying)
        {
            efxSource.clip = clip;
            efxSource.Play();
        }
    }


    public void RandomSoundEffect(params AudioClip[] clips)
    {
        int randomIndex = Random.Range(0, clips.Length);
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        efxSource.pitch = randomPitch;

        if (!efxSource.isPlaying)
        {
            efxSource.clip = clips[randomIndex];
            efxSource.Play();
        }
    }

    /// <summary>
    /// シーンで流れるBGMを変える。
    /// </summary>
    /// <param name="clip"></param>
    public void ChangeSceneMusic(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.Play();
    }
    
    public void TurnOnMusic()
    {
        bgmSource.Play();
    }

    public void TurnOffMusic()
    {
        bgmSource.Stop();
    }	
}
