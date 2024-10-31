using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;

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