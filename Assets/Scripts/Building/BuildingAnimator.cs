using DG.Tweening;
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

    public void AnimateBuiltBuilding(Building building)
    {
        Animate(building.gameObject);
    }

    public void Animate(GameObject building)
    {
        SpawnParticle(building);
        BounceInOut(building.transform);
    }

    private void SpawnParticle(GameObject building)
    {
        var part = particle.GetAtPosAndRot<PooledMonoBehaviour>(building.transform.position + Vector3.up * 0.5f, particle.transform.rotation);
        ParticleSystem psys = part.GetComponentInChildren<ParticleSystem>();
        var shape = psys.shape;
        var emission = psys.emission;

        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 15);
        emission.SetBurst(0, burst);

        shape.mesh = building.GetComponentInChildren<MeshFilter>().sharedMesh;
    }

    public void BounceInOut(Transform transform)
    {
        transform.DORewind();
        transform.DOPunchScale(transform.lossyScale * scaleMultiplier, duration).SetEase(ease);
    }

}