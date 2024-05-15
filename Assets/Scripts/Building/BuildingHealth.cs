using System;
using UnityEngine;

public class BuildingHealth : MonoBehaviour, IHealth
{
    public event Action<BuildingHealth> OnDeath;

    private Building building;

    public IAttacker Attacker => building.BuildingHandler[building].State;

    private void Awake()
    {
        building = GetComponent<Building>();
    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        building.BuildingHandler[building].Health.TakeDamage(damage, out damageDone);
    }
}
