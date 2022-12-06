using UnityEngine;

[CreateAssetMenu(fileName = "New Burst", menuName = "Wave/Burst")]
public class Burst : ScriptableObject
{
    public EnemyMovement Enemy;
    public int Amount;
    public float SpawnRate;
}


