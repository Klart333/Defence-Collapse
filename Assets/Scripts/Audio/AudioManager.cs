using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [Title("Settings")]
    [SerializeField]
    private bool useSoundEffects = true;

    [SerializeField]
    private bool useMusic = true;

    private readonly Queue<AudioSource> audioSources = new Queue<AudioSource>();

    private AudioSource musicSource;

    private void Start()
    {
        musicSource = GetComponent<AudioSource>();

        useSoundEffects = PlayerPrefs.GetInt("SoundEffects") == 0;
        useMusic = PlayerPrefs.GetInt("Music") == 0;

        if (!useMusic)
        {
            musicSource.volume = 0;
        }
    }

    public void PlaySoundEffect(SimpleAudioEvent audio)
    {
        if (!useSoundEffects)
        {
            return;
        }

        if (audioSources.Count == 0)
        {
            audioSources.Enqueue(gameObject.AddComponent<AudioSource>());
        }
        AudioSource source = audioSources.Dequeue();
        AudioClip clip = audio.Play(source);

        StartCoroutine(ReturnToQueue(source, clip.length));
    }

    private IEnumerator ReturnToQueue(AudioSource source, float length)
    {
        yield return new WaitForSeconds(length);

        audioSources.Enqueue(source);
    }

    public void ToggleSoundEffects(bool isOn)
    {
        useSoundEffects = isOn;
        PlayerPrefs.SetInt("SoundEffects", useSoundEffects ? 0 : -1);
    }

    public void ToggleMusic(bool isOn)
    {
        useMusic = isOn;
        PlayerPrefs.SetInt("Music", useMusic ? 0 : -1);
    }
}
