using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio Events/Simple")]
public class SimpleAudioEvent : ScriptableObject
{
    [SerializeField]
    public AudioClip[] Clips = new AudioClip[0];

    [SerializeField]
    private RangedFloat volume = new RangedFloat(1, 1);

    [SerializeField]
    [MinMaxRange(0f, 2f)]
    private RangedFloat pitch = new RangedFloat(1, 1);

    [SerializeField]
    private AudioMixerGroup mixer;

    public int Play(AudioSource source)
    {
        source.outputAudioMixerGroup = mixer; // Can be null

        int clipIndex = UnityEngine.Random.Range(0, Clips.Length);
        source.clip = Clips[clipIndex];

        source.pitch = UnityEngine.Random.Range(pitch.minValue, pitch.maxValue);
        source.volume = UnityEngine.Random.Range(volume.minValue, volume.maxValue);

        source.Play();

        return clipIndex;
    }
}
