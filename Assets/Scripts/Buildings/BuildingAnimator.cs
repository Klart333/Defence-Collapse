using System;
using System.Collections.Generic;
using Buildings;
using Buildings.District;
using DG.Tweening;
using Gameplay.Event;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuildingAnimator : MonoBehaviour
{
    [Title("Animation")]
    [SerializeField]
    private PooledMonoBehaviour particle;

    [Title("Values")]
    [SerializeField]
    private float scaleMultiplier = 1.4f;

    [SerializeField]
    private float duration = 0.4f;

    [SerializeField]
    private Ease ease = Ease.Linear;

    private void OnEnable()
    {
        Events.OnBuildingBuilt += AnimateBuiltBuilding;
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= AnimateBuiltBuilding;
    }

    public void AnimateBuiltBuilding(IEnumerable<IBuildable> buildings)
    {
        foreach (IBuildable buildable in buildings)
        {
            Animate(buildable);
        }
    }

    public void Animate(IBuildable building)
    {
        if (building is not Building or Barricade)
        {
            return;
        }
        
        SpawnParticle(building);
        BounceInOut(building.MeshTransform);
    }

    private void SpawnParticle(IBuildable building)
    {
        var part = particle.GetAtPosAndRot<PooledMonoBehaviour>(building.gameObject.transform.position + Vector3.up * 0.5f, particle.transform.rotation);
        ParticleSystem psys = part.GetComponentInChildren<ParticleSystem>();
        ParticleSystem.ShapeModule shape = psys.shape;
        ParticleSystem.EmissionModule emission = psys.emission;
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 15);

        emission.SetBurst(0, burst);

        shape.meshRenderer = building.MeshRenderer;
    }

    public void BounceInOut(Transform transform)
    {
        transform.DORewind();
        transform.DOPunchScale(transform.lossyScale * scaleMultiplier, duration).SetEase(ease);
    }

}