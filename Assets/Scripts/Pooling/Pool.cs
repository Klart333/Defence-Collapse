using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Pool : MonoBehaviour
{
    // Note: Some methods are numbered, this is for clarity on how the main steps for how spawning the objects works

    private static readonly Dictionary<PooledMonoBehaviour, Pool> dictionaryPools = new Dictionary<PooledMonoBehaviour, Pool>(); // The List of Main Pools

    // Dictionaries are basically Lists but with keys (and i hate them)
    // The advantage of a dictionary is in going through the list and checking for a specific element, lookup time, where a list has to compare every item individually, the dictionary uses a hash lookup algorithm, which is fancy words for magic    
    // Basically a list scales O(n) for lookup times, which means that the operation time, O, scales based of the number of elements, N. Which is logical, because the list needs to do one operation for every element
    // But the dictionary, using black magic, has a lookup time of O(1), which means the operation time is always the same, no matter how many elements you need to check

    private readonly Queue<PooledMonoBehaviour> queueObjects = new Queue<PooledMonoBehaviour>();

    // A Queue is basically a stripped down list, and it's not as grandiose as dictionaries
    // The reason to use a Queue is partly for some performance improvements as a queue is fit for purpose, and partly for simplicity
    // But the main reason is for communication and exposing the intent more explicitly

    private PooledMonoBehaviour poolPrefab; // The prefab this Pool handles, Assigned in GetPool
    private int index = 0;

    public static void Clear()
    {
        dictionaryPools.Clear();
    }
    
    public static Pool GetPool(PooledMonoBehaviour prefab) // 3. Called from PooledMonoBehaviour.Get<T>, here we get the Pool that handles the prefab in question
    {
        if (dictionaryPools.TryGetValue(prefab, value: out Pool pool1)) // We search the dictionary for the Pool, if it's already there we return, otherwise see below
        {
            return pool1;
        }

        // If it isn't found that means we don't have a pool for that prefab so we want to create one

        Pool pool = new GameObject("Pool - " + prefab.name).AddComponent<Pool>(); // Adds the parent gameobject for all the pooled objects and slaps on a Pool script

        pool.poolPrefab = prefab; // Assigns the prefab the pool handles

        dictionaryPools.Add(prefab, pool); // Adds the Main pool to the dictionary with the key of the prefab

        return pool; // We return the pool 
    }

    public T Get<T>(Transform parent = null) where T : PooledMonoBehaviour // 4. Gets the object we want from the pool, and if the pool is empty we just create one
    {
        if (queueObjects.Count == 0) // If the queue is empty we need to Grow it, becuase the need to return the an element for the queue. (Except for in the beginning, it probably won't be called, because there would need to be over 50 sharks in the scene at once for it to be necessary)
        {
            GrowPool(!parent ? transform : parent);    
        }

        var pooledObject = queueObjects.Dequeue(); // returns the first element in the queue

        return pooledObject as T; // We return the element/object, passed as the type we specified
    }

    private void GrowPool(Transform parent) // (4.5, only called in the beginning and on rare occasions) Grows the pool in the beginning of the game, or anytime it need to be expanded
    {
        for (int i = 0; i < poolPrefab.InitialPoolSize; i++) // We specify how big the pool of the prefab this script handles, and create that many disabled gameobjects, ready to be enabled whenever we want
        {
            PooledMonoBehaviour pooledObject = Instantiate(poolPrefab, parent, false); 
            pooledObject.gameObject.name += $" {index + i}"; // Helps keeping track
            pooledObject.gameObject.SetActive(false);
            pooledObject.OnReturnToPool += AddObjectToAvailableQueue;
            
            queueObjects.Enqueue(pooledObject);
        }

        index += poolPrefab.InitialPoolSize;
    }
    
    /// <summary>
    /// Called From the event OnReturnToPool
    /// </summary>
    private void AddObjectToAvailableQueue(PooledMonoBehaviour pooledObject) // When we disable a gameobject we pass it here and readd it to the queue, we previoulsy 'Dequeued' it. 
    {
        queueObjects.Enqueue(pooledObject);
    }

}
