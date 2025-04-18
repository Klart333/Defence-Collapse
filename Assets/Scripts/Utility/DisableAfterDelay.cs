using System;
using System.Collections;
using Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

public class DisableAfterDelay : MonoBehaviour
{
    [FormerlySerializedAs("Lifetime")]
    [SerializeField]
    private float lifeTime = 1f;

    [SerializeField]
    private bool shouldDestroy;

    [SerializeField]
    private bool resetScale = true;
    
    private Vector3 startScale;
    
    private IGameSpeed gameSpeed;
    
    public float LifeLeft { get; private set; }

    public float Lifetime
    {
        get => lifeTime;
        set
        {
            lifeTime = value;
            LifeLeft = value;
        }
    }

    private void Start()
    {
        gameSpeed = GameSpeedManager.Instance;
    }

    private void OnEnable()
    {
        if (shouldDestroy)
        {
            Destroy(gameObject, Lifetime);
        }
        else
        {
            startScale = transform.localScale;
            StopAllCoroutines();
            StartCoroutine(Delay());
        }
    }

    private void OnDisable()
    {
        if (resetScale)
        {
            transform.localScale = startScale;
        }

        LifeLeft = Lifetime;
        StopAllCoroutines();
    }

    private IEnumerator Delay()
    {
        float t = 0;

        while (t < Lifetime)
        {
            yield return null;
            
            LifeLeft = Lifetime - t;
            t += Time.deltaTime * gameSpeed?.Value ?? 1;
            
            //Debug.Log("Life Left: " + LifeLeft);
        }

        LifeLeft = 0;
        gameObject.SetActive(false);
    }
}
