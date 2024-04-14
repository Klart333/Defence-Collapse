using Sirenix.OdinInspector;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class BuildingAnimator : MonoBehaviour
{
    [Title("Animation")]
    [SerializeField]
    private PooledMonoBehaviour particle;

    private void OnEnable()
    {
        Events.OnBuildingBuilt += AnimateBuiltBuilding;
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= AnimateBuiltBuilding;
    }

    private async void AnimateBuiltBuilding(Building building)
    {
        float mult = 1.0f / building.ScaleMult;
        var part = particle.GetAtPosAndRot<PooledMonoBehaviour>(building.transform.position + Vector3.up * 0.5f, particle.transform.rotation);
        ParticleSystem psys = part.GetComponentInChildren<ParticleSystem>();
        var shape = psys.shape;
        var emission = psys.emission;

        shape.mesh = building.GetComponentInChildren<MeshFilter>().sharedMesh;
        shape.radius *= mult;

        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 15 * mult);
        emission.SetBurst(0, burst);
        await BounceBuilding(building, building.ScaleMult);
    }

    public static async Task BounceBuilding(Building building, float scaleMult)
    {
        float t = 0;

        Vector3 targetScale = building.StartScale;
        Vector3 startScale = targetScale * scaleMult;

        while (t <= 1.0f)
        {
            t += Time.deltaTime * building.BounceSpeed;

            try
            {
                building.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, Math.Elastic(t));
            }
            catch (Exception)
            {
                return;
            }

            await Task.Yield();
        }

        building.transform.localScale = targetScale;
    }

    public static async void BounceInOut(Building building)
    {
        float t = 0;

        while (t <= .7f)
        {
            t += Time.deltaTime;

            try
            {
                building.transform.localScale = building.StartScale * (1.0f + Math.Elastic(t) / 4.0f);
            }
            catch (Exception)
            {
                return;
            }

            await Task.Yield();
        }
        t = 1;

        while (t >= 0.0f)
        {
            t -= Time.deltaTime;

            try
            {
                building.transform.localScale = building.StartScale * (1.0f + Math.EasInElastic(t) / 4.0f);
            }
            catch (Exception)
            {
                return;
            }

            await Task.Yield();
        }

        building.transform.localScale = building.StartScale;
    }


}