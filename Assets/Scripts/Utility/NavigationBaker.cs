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
        surface.RemoveData();
        surface.BuildNavMesh();
    }
}

public static class Extensions
{
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector3 ToXyZ(this Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }
}