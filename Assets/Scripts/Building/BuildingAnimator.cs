using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
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
        foreach (var item in buildings)
        {
            Animate(item);
        }
    }

    public void Animate(IBuildable building)
    {
        SpawnParticle(building);
        BounceInOut(building.gameObject.transform);
    }

    private void SpawnParticle(IBuildable building)
    {
        var part = particle.GetAtPosAndRot<PooledMonoBehaviour>(building.gameObject.transform.position + Vector3.up * 0.5f, particle.transform.rotation);
        ParticleSystem psys = part.GetComponentInChildren<ParticleSystem>();
        var shape = psys.shape;
        var emission = psys.emission;

        int count = building.Importance switch
        {
            0 => 5,
            1 => 15,
            _ => throw new System.NotImplementedException(),
        };
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, count);
        emission.SetBurst(0, burst);

        shape.meshRenderer = building.gameObject.GetComponentInChildren<MeshRenderer>();
    }

    public void BounceInOut(Transform transform)
    {
        transform.DORewind();
        transform.DOPunchScale(transform.lossyScale * scaleMultiplier, duration).SetEase(ease);
    }

}