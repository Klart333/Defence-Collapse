using Sirenix.OdinInspector;
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

    [Title("Debug")]
    [SerializeField]
    private bool placingCastle = true;

    private enum BuildingType
    {
        Castle,
        Building,
        Path
    }

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
        Events.OnPathPurchased += PathPurchased;

        buildingManager = GetComponent<BuildingManager>();
    }

    private void BuildingPurchased(Building building)
    {
        //Building spawnedBuilding = Instantiate(building, GetRayPoint(), Quaternion.identity);
        //spawnedBuilding.BuildingSize = 0;
        //spawnedBuilding.BuildingLevel = 0;

        if (placingCastle)
        {
            PlacingTower(BuildingType.Building);
        }
        else
        {
            PlacingTower(BuildingType.Building);
        }
    }

    private void PathPurchased()
    {
        PlacingTower(BuildingType.Path);
    }

    private async void PlacingTower(BuildingType type)
    {
        while (/*(fire.ReadValue<float>() == 0 || !CanPlace(spawnedBuilding)) &&*/ !canceled)
        {
            await Task.Yield();

            Vector3 mousePos = GetRayPoint();
            if (mousePos == Vector3.zero || !fire.WasPerformedThisFrame()) continue;

            switch (type)
            {
                case BuildingType.Building:
                    buildingManager.Query(mousePos);

                    break;
                case BuildingType.Castle:
                    Vector3 minPos = new Vector3(mousePos.x - 2f, mousePos.y, mousePos.z - 2f);
                    Vector3 maxPos = new Vector3(mousePos.x + 2, mousePos.y, mousePos.z + 2);

                    buildingManager.PlaceCastle(minPos, maxPos);
                    placingCastle = false;
                    type = BuildingType.Building;
                    break;
                case BuildingType.Path:
                    buildingManager.BuildPath(mousePos);

                    break;
                default:
                    break;
            }
            
        }

        if (canceled)
        {
            return;
        }
        
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
