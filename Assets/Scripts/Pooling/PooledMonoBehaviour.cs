using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class PooledMonoBehaviour : MonoBehaviour
{
    // Note: Multiple functions with the same name 'Get', this is because the thing they "get" is based on the T and the name can't really be more specifik 
    // Note: Some methods are numbered, this is for clarity on how the main steps for how spawning the objects works

    [Title("Pool")]
    [SerializeField]
    private int initialPoolSize = 10;

    public event Action<PooledMonoBehaviour> OnReturnToPool; // Takes a PooledMonoBehaviour as an argument so that we know what object to return

    public int InitialPoolSize => initialPoolSize;


    private DisableAfterDelay delay;

    public DisableAfterDelay Delay
    {
        get
        {
            if (!delay)
            {
                delay = GetComponent<DisableAfterDelay>();
            }

            return delay;
        }
    }

    public T Get<T>() where T : PooledMonoBehaviour // 2. Second step called from GetAtPosAndRot<T>, here we ask for a disabled gameobject from the Pool
    {
        Pool pool = Pool.GetPool(this); // 3. Calls the static method of Pool, GetPool(PooledMonoBehaviour prefab) returns the pool for the prefab, if there isn't one, a pool is created  

        var pooledObject = pool.Get<T>(); // 4. The pooledObject is the one we want to show/enable in the scene. We get it from the Get<T> function of the pool of the prefab, the Get<T> Method returns the first element of the pooled queue 

        pooledObject.gameObject.SetActive(true); // We now have the object we want show/enable, so all we have to do is enable it

        return pooledObject; // returns the gameobject, this is for accessing it and making adjustments after it has spawned
    }
    
    public T GetAtPosAndRot<T>(Vector3 position, Quaternion rotation) where T : PooledMonoBehaviour // 1. This is the first step in spawning a prefab (kinda optional)
    {
        var pooledObject = Get<T>(); // What this method does is takes the pooledObject from Get<T> and assign its position and rotation

        pooledObject.transform.position = position;
        pooledObject.transform.rotation = rotation;

        return pooledObject;
    }

    protected virtual void OnDisable() // When we disable the gameobject we return it to the pool 
    {
        OnReturnToPool?.Invoke(this);
    }
}
