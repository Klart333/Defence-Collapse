using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledMonoBehaviour : MonoBehaviour
{
    // Note: Multiple functions with the same name 'Get', this is because the thing they "get" is based on the T and the name can't really be more specifik 
    // Note: Some methods are numbered, this is for clarity on how the main steps for how spawning the objects works

    [SerializeField]
    private int initialPoolSize = 10;

    public event Action<PooledMonoBehaviour> OnReturnToPool; // Takes a PooledMonoBehaviour as an argument so that we know what object to return

    public int InitialPoolSize { get { return initialPoolSize; } } 

    public T Get<T>(Transform overrideParent = null) where T : PooledMonoBehaviour // 2. Second step called from GetAtPosAndRot<T>, here we ask for a disabled gameobject from the Pool
    {
        Pool pool;
        if (overrideParent == null)
        {
            pool = Pool.GetPool(this); // 3. Calls the static method of Pool, GetPool(PooledMonoBehaviour prefab) returns the pool for the prefab, if there isn't one, a pool is created  
        }
        else
        {
            pool = Pool.GetPool(this, overrideParent); // Same but uses the given transform instead if it needs to create one
        }

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

    protected virtual void OnDisable() // When we disable the gameobject (and the script) we return it to the pool 
    { // (It's virtual bc the sharks need to reset when disabled, not related to the pooling (...well it's kinda related but, you know))
        if (OnReturnToPool != null)
        {
            OnReturnToPool(this); 
        }
    }

    protected void ReturnToPool() // Is called instead of destroying the gameobject, implemented in Shark
    {
        gameObject.SetActive(false);
    }
}
