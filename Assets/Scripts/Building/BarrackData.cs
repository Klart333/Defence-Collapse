using UnityEngine;

[CreateAssetMenu(fileName = "New Barrack Data", menuName = "Building/Data/Barrack")]
public class BarrackData : ScriptableObject
{
    public float MaxGuys = 10;
    public float SpawnAmount = 2;
    public float Health = 10;
    public float AlarmRange = 5;
}
