using UnityEngine;

namespace Audio
{
    public class PlayAudioMultiple : MonoBehaviour
    {
        [SerializeField]
        private SimpleAudioEvent[] audioEvents;

        public void Play(int index)
        {
            AudioManager.Instance.PlaySoundEffect(audioEvents[index]);
        }
    }
}