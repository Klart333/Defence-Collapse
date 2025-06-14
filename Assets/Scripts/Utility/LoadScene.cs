using UnityEngine;
using Juice;

namespace Utility
{
    public class LoadScene : MonoBehaviour
    {
        public void LoadSceneIndex(int index)
        {
            SceneTransitionManager.Instance.LoadScene(index).Forget();
        }
    }
}