using System;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public abstract class BuildingState
{
    public abstract void WaveStarted();
    public abstract void OnStateEntered(Building building);
}

public class ArcherState : BuildingState
{
    public override void OnStateEntered(Building building)
    {

    }

    public override void WaveStarted()
    {

    }
}

public class BarracksState : BuildingState
{
    private Building building;
    public override void OnStateEntered(Building building)
    {
        this.building = building;
        SpawnFighter();
    }

    private void SpawnFighter()
    {
        Fighter fighter = building.Fighters[UnityEngine.Random.Range(0, building.Fighters.Length)];
        Vector3 pos = building.transform.position + Vector3.right;
        GameObject.Instantiate(fighter, pos, Quaternion.identity);
    }

    public override void WaveStarted()
    {
        SpawnFighter();
    }
}
