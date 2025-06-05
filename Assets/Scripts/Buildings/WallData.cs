using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wall Data", menuName = "Building/State Data/Wall")]
public class WallData : ScriptableObject
{
    [Title("Stats")]
    public Stats Stats;
}

