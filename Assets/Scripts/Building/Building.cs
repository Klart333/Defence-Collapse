using System;
using System.Collections;
using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField]
    private float cost;

    [Header("Grounding")]
    [SerializeField]
    private Transform[] groundTransforms;

    [SerializeField]
    private float groundDistance = 0.1f;

    [SerializeField]
    private LayerMask layerMask;

    private BuildingState buildingState;

    private int collisions = 0;

    public Fighter[] Fighters { get; set; }
    public Building[] Towers { get; set; }

    public int Collsions => collisions;
    public float Cost => cost;

    private void Start()
    {
        Events.OnWaveStarted += OnWaveStarted;
    }

    private void OnWaveStarted()
    {
        buildingState.WaveStarted();
    }

    public void SetState<T>() where T : BuildingState
    {
        if (typeof(ArcherState).IsAssignableFrom(typeof(T)))
        {
            buildingState = new ArcherState();
        }
        else if (typeof(BarracksState).IsAssignableFrom(typeof(T)))
        {
            buildingState = new BarracksState();
        }

        buildingState.OnStateEntered(this);
    }

    #region Placing Checks
    public bool IsGrounded()
    {
        for (int i = 0; i < groundTransforms.Length; i++)
        {
            if (Physics.Raycast(groundTransforms[i].position, Vector3.down, groundDistance, layerMask) == false)
            {
                return false;
            }
        }

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        collisions++;
    }

    private void OnTriggerExit(Collider other)
    {
        collisions--;
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisions++;
    }

    private void OnCollisionExit(Collision collision)
    {
        collisions--;
    }
    #endregion

}
