using System;
using UnityEngine;

public class BuildingHealth : MonoBehaviour, IHealth
{
    public event Action<BuildingHealth> OnDeath;

    private Building building;

    public HealthComponent Health => building.BuildingHandler[building].Health;
    public Vector3 OriginPosition => Health.OriginPosition;

    private void Awake()
    {
        building = GetComponent<Building>();
    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        building.BuildingHandler[building].Health.TakeDamage(damage, out damageDone);
    }
}
