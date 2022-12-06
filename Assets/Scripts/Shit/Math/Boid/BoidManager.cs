using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    [SerializeField]
    private Boid prefab;

    [SerializeField]
    private int amount;

    [SerializeField]
    private Vector3 bounds;

    [SerializeField]
    private float boundStrength;

    [SerializeField]
    private float validDistance = 5;

    [SerializeField]
    private float minDistance = 1;

    [Header("Weights")]
    [SerializeField]
    private float cohesion = 1;

    [SerializeField]
    private float repelation = 1;

    [SerializeField]
    private float alignment = 1;

    private Boid[] boids;

    public Vector3 center
    {
        get
        {
            return new Vector3(bounds.x / 2.0f, bounds.y / 2.0f, bounds.z / 2.0f);
        }
    }

    private void Start()
    {
        SpawnBoids();
    }

    #region Spawning
    private void SpawnBoids()
    {
        boids = new Boid[amount];

        for (int i = 0; i < amount; i++)
        {
            boids[i] = Instantiate(prefab, GetRandomPos(), Quaternion.Euler(GetRandomPos().normalized * 360));
            boids[i].ExtraSpeed = UnityEngine.Random.Range(0.9f, 1.1f);
        }
    }

    private Vector3 GetRandomPos()
    {
        float x = UnityEngine.Random.Range(0, bounds.x);
        float y = UnityEngine.Random.Range(0, bounds.y);
        float z = UnityEngine.Random.Range(0, bounds.z);
        return new Vector3(x, y, z);
    }
    #endregion

    private void Update()
    {
        for (int i = 0; i < boids.Length; i++)
        {
            AvoidBounds(boids[i]);

            for (int g = 0; g < boids.Length; g++)
            {
                Cohesion(boids[i], boids[g]);
                Repelation(boids[i], boids[g]);
                Alignment(boids[i], boids[g]);
            }
            
        }
    }

    private void Alignment(Boid boid, Boid other)
    {
        float dist = Vector3.Distance(boid.transform.position, other.transform.position);
        if (dist == 0)
        {
            return;
        }

        if (dist < validDistance)
        {
            boid.Direction += other.Direction * alignment;
        }
    }

    private void AvoidBounds(Boid boid)
    {
        Vector3 dirToCenter = (center - boid.transform.position).normalized;
        Vector3 dir = Vector3.zero;

        // Max
        if (boid.transform.position.x > bounds.x)
        {
            dir += dirToCenter * (boid.transform.position.x - bounds.x);
        }

        if (boid.transform.position.y > bounds.y)
        {
            dir += dirToCenter * (boid.transform.position.y - bounds.y);
        }

        if (boid.transform.position.z > bounds.z)
        {
            dir += dirToCenter * (boid.transform.position.z - bounds.z);
        }

        // Min
        if (boid.transform.position.x < 0)
        {
            dir += dirToCenter * Mathf.Abs(boid.transform.position.x);
        }

        if (boid.transform.position.y < 0)
        {
            dir += dirToCenter * Mathf.Abs(boid.transform.position.y);
        }

        if (boid.transform.position.z < 0)
        {
            dir += dirToCenter * Mathf.Abs(boid.transform.position.z);
        }

        boid.Direction += dir * boundStrength;
    }

    private void Repelation(Boid boid, Boid other)
    {
        float dist = Vector3.Distance(boid.transform.position, other.transform.position);
        if (dist == 0)
        {
            return;
        }

        if (dist < validDistance)
        {
            Vector3 awayDir = (boid.transform.position - other.transform.position).normalized;
            Vector3 repelDir = (1.0f / (dist - minDistance)) * awayDir;
            boid.Direction += repelDir * repelation;
        }
    }

    private void Cohesion(Boid boid, Boid other)
    {
        float dist = Vector3.Distance(boid.transform.position, other.transform.position);
        if (dist == 0)
        {
            return;
        }

        if (dist < validDistance)
        {
            Vector3 cohesionDir = (other.transform.position - boid.transform.position).normalized;
            boid.Direction += cohesionDir * cohesion;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;

        Gizmos.DrawWireCube(center, bounds);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(center, 0.1f);
    }

}
