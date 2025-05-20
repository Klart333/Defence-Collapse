using UnityEngine;

namespace Audio
{
    public class PlayAudio : MonoBehaviour
    {
        [SerializeField]
        private SimpleAudioEvent audioEvent;

        public void Play()
        {
            AudioManager.Instance.PlaySoundEffect(audioEvent);
        }
    }
}