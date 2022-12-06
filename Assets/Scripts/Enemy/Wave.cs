using UnityEngine;

[CreateAssetMenu(fileName = "New Wave", menuName = "Wave/Wave")]
public class Wave : ScriptableObject
{
    public Burst[] Bursts;
    public float[] Delays;
}


