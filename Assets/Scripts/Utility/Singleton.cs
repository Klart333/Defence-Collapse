using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

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
}