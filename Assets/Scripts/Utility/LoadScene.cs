using Gameplay;
using Unity.Entities;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Utility
{
    public class LoadScene : MonoBehaviour
    {
        public void LoadSceneIndex(int index)
        {
            GameManager.Instance.ResetWorld();
            SceneManager.LoadScene(index);
        }
    }
}