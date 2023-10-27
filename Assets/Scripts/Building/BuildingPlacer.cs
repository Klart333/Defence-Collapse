using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingPlacer : MonoBehaviour
{
    [SerializeField]
    private LayerMask layerMask;

    [Header("Animation")]
    [SerializeField]
    private PooledMonoBehaviour particle;

    private Camera cam;

    private InputActions InputActions;
    private InputAction fire;
    private InputAction mouse;
    private InputAction cancel;

    private bool canceled
    {
        get
        {
            return cancel.ReadValue<float>() > 0;
        }
    }

    private void OnEnable()
    {
        InputActions = new InputActions();

        fire = InputActions.Player.Fire;
        fire.Enable();
        
        cancel = InputActions.Player.Cancel;
        cancel.Enable();

        mouse = InputActions.Player.Mouse;
        mouse.Enable();
    }

    private void OnDisable()
    {
        fire.Disable();
        cancel.Disable();
        mouse.Disable();
    }

    private void Start()
    {
        cam = Camera.main;
        Events.OnBuildingPurchased += BuildingPurchased;
        Events.OnBuildingBuilt += AnimateBuiltBuilding;
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

    private void BuildingPurchased(Building building)
    {
        Building spawnedBuilding = Instantiate(building, GetRayPoint(), Quaternion.identity);
        spawnedBuilding.BuildingSize = 0;
        spawnedBuilding.BuildingLevel = 0;

        PlacingTower(spawnedBuilding);
    }

    private async void PlacingTower(Building spawnedBuilding)
    {
        await Task.Yield();

        while ((fire.ReadValue<float>() == 0 || !CanPlace(spawnedBuilding)) && !canceled)
        {
            Vector3 mousePos = GetRayPoint();
            if (mousePos != Vector3.zero)
            {
                Vector3 pos = new Vector3(Math.Round(mousePos.x, 0.666f), Math.Round(mousePos.y, 2), Math.Round(mousePos.z, 0.666f));
                spawnedBuilding.transform.position = Vector3.Lerp(spawnedBuilding.transform.position, pos, 0.15f);
                spawnedBuilding.transform.localScale = spawnedBuilding.PlacingScale;
            }

            await Task.Yield();
        }

        if (canceled)
        {
            Destroy(spawnedBuilding.gameObject);
            Events.OnBuildingCanceled(spawnedBuilding);
            return;
        }

        Events.OnBuildingBuilt(spawnedBuilding);
        GameEvents.OnEnemyPathUpdated(spawnedBuilding.transform.position);
    }

    private bool CanPlace(Building spawnedBuilding)
    {
        return spawnedBuilding.Collsions == 0 && spawnedBuilding.IsGrounded();
    }

    private Vector3 GetRayPoint()
    {
        Ray ray = cam.ScreenPointToRay(mouse.ReadValue<Vector2>());
        if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
        {
            return hit.point;
        }

        return Vector3.zero;
    }
}
