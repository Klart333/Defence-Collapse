using System.Collections;
using UnityEngine;

public class DisableAfterDelay : MonoBehaviour
{
    public float Lifeime = 1;

    [SerializeField]
    private bool shouldDestroy = false;

    private Vector3 startScale;

    private void OnEnable()
    {
        if (shouldDestroy)
        {
            Destroy(gameObject, Lifeime);
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
        transform.localScale = startScale;

        StopAllCoroutines();
    }

    private IEnumerator Delay()
    {
        float t = 0;

        while (t < Lifeime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
