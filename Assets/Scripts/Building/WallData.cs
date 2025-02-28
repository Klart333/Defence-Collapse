using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wall Data", menuName = "Building/State Data/Wall")]
public class WallData : ScriptableObject
{
    [Title("Economy")]
    public int IncomePerHouse = 1;

    [Title("Stats")]
    public Stats Stats;
}

