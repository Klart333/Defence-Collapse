using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingPlacer : MonoBehaviour
{
    [SerializeField]
    private LayerMask layerMask;

    private BuildingManager buildingManager;
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

        buildingManager = GetComponent<BuildingManager>();
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

        while (/*(fire.ReadValue<float>() == 0 || !CanPlace(spawnedBuilding)) &&*/ !canceled)
        {
            Vector3 mousePos = GetRayPoint();
            if (mousePos != Vector3.zero && fire.WasPerformedThisFrame())
            {
                Vector3 pos = new Vector3(Math.Round(mousePos.x + 1, 2) - 1f, Math.Round(mousePos.y, 2), Math.Round(mousePos.z + 1, 2) - 1f);
                buildingManager.Query(pos);
                Debug.Log("Query at: " +  pos);
            }

            await Task.Yield();
        }

        if (canceled)
        {
            Destroy(spawnedBuilding.gameObject);
            Events.OnBuildingCanceled(spawnedBuilding);
            return;
        }

        spawnedBuilding.transform.position = spawnedBuilding.PlacedPosition;

        Events.OnBuildingBuilt(spawnedBuilding);
        
        //GameEvents.OnEnemyPathUpdated(spawnedBuilding.transform.position);
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
