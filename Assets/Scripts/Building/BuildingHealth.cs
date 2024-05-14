using System;
using UnityEngine;

public class BuildingHealth : MonoBehaviour, IHealth
{
    public event Action<BuildingHealth> OnDeath;

    private Building building;

    public Vector3 Position => transform.position;

    private void Awake()
    {
        building = GetComponent<Building>();
    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        building.BuildingHandler[building].Health.TakeDamage(damage, out damageDone);
    }
}
