using Sirenix.OdinInspector;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance;
    [Title("Singleton")]
    [SerializeField]
    private bool shouldDestroyOnLoad = true;

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = GetComponent<T>();

            if (!shouldDestroyOnLoad)
            {
               DontDestroyOnLoad(Instance.gameObject);
            }
        }
    }
}
