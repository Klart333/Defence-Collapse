using Cysharp.Threading.Tasks;
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
            Instance = this as T;
            
            if (!shouldDestroyOnLoad)
            {
               DontDestroyOnLoad(Instance.gameObject);
            }
        }
    }
    
    
    public static async UniTask<T> Get()
    {
        await UniTask.WaitUntil(() => Instance != null);
        return Instance;
    }
}