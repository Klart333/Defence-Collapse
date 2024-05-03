using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New House Data", menuName = "Building/State Data/Normal House")]
public class NormalHouseData : ScriptableObject
{
    [Title("Economy")]
    public int IncomePerHouse = 1;

    [Title("Health")]
    public int MaxHealth;
}

