using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableAfterDelay : MonoBehaviour
{
    [SerializeField]
    private float time = 1;

    [SerializeField]
    private bool shouldDestroy = false;

    private void OnEnable()
    {
        if (shouldDestroy)
        {
            Destroy(gameObject, time);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(Delay());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Delay()
    {
        float t = 0;

        while (t < time)
        {
            t += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
