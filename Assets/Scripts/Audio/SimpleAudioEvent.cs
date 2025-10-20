using Random = UnityEngine.Random;

using UnityEngine.Audio;
using UnityEngine;
using System;

namespace Audio
{
    [CreateAssetMenu(menuName = "Audio Events/Simple")]
    public class SimpleAudioEvent : ScriptableObject
    {
        [SerializeField]
        private AudioClip[] clips = Array.Empty<AudioClip>();

        [SerializeField]
        private RangedFloat volume = new RangedFloat(1, 1);

        [SerializeField]
        [MinMaxRange(0f, 2f)]
        private RangedFloat pitch = new RangedFloat(1, 1);

        [SerializeField]
        private AudioMixerGroup mixer;

        public AudioClip Play(AudioSource source)
        {
            source.outputAudioMixerGroup = mixer; // Can be null

            int clipIndex = Random.Range(0, clips.Length);
            source.clip = clips[clipIndex];

            source.pitch = Random.Range(pitch.minValue, pitch.maxValue);
            source.volume = Random.Range(volume.minValue, volume.maxValue);

            source.Play();
            return clips[clipIndex];
        }
    }

}