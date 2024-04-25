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
        FindObjectOfType<WaveFunction>().OnMapGenerated += NavigationBaker_OnMapGenerated;
    }

    private void OnDisable()
    {
        WaveFunction waveFunction = FindObjectOfType<WaveFunction>();
        if (waveFunction)
        {
            waveFunction.OnMapGenerated -= NavigationBaker_OnMapGenerated;
        }
    }

    private void NavigationBaker_OnMapGenerated()
    {
        Bake();
    }

    [ContextMenu("Bake")]
    public void Bake()
    {
        surface.BuildNavMesh();
    }
}