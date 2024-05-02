using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavigationBaker : MonoBehaviour
{
    [SerializeField]
    private NavMeshSurface surface;

    private void OnEnable()
    {
        Events.OnWaveStarted += NavigationBaker_OnMapGenerated;
    }

    private void OnDisable()
    {
        Events.OnWaveStarted -= NavigationBaker_OnMapGenerated;
    }

    private void NavigationBaker_OnMapGenerated()
    {
        Bake();
    }

    [Button]
    public void Bake()
    {
        surface.BuildNavMesh();
    }
}