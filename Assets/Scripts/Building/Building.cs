using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Building : PooledMonoBehaviour, IBuildable
{
    public event Action<Building> OnDeath;
    public event Action<Vector3> OnEnterDefense;

    [Title("Visual")]
    [SerializeField]
    private Material transparentGreen;

    private List<Material> transparentMaterials = new List<Material>();

    private BuildingState buildingState;
    private InputActions inputActions;
    private InputAction fire;
    private InputAction shift;

    private bool selected = false;
    private bool hovered = false;

    public Fighter[] Fighters { get; set; }
    public Building[] Towers { get; set; }

    public int BuildingLevel { get; set; }

    public Vector3 PlacedPosition { get; set; }
    public List<Material> Materials { get; set; }

    public List<Fighter> SpawnedFighters { get; set; } = new List<Fighter>();
    public List<EnemyMovement> AttackingEnemies { get; set; } = new List<EnemyMovement>();

    private void OnEnable()
    {
        Events.OnWaveStarted += OnWaveStarted;

        inputActions = new InputActions();
        fire = inputActions.Player.Fire;
        fire.Enable();

        shift = inputActions.Player.Shift;
        shift.Enable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        transform.localScale = Vector3.one;
        Events.OnWaveStarted -= OnWaveStarted;

        fire.Disable();
        shift.Disable();
    }

    public void Setup(PrototypeData prototypeData, List<Material> materials)
    {
        GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        GetComponentInChildren<MeshRenderer>().SetMaterials(materials);

        Materials = materials;
        transparentMaterials = new List<Material>();
        for (int i = 0; i < materials.Count; i++)
        {
            transparentMaterials.Add(transparentGreen);
        }
    }

    private void OnWaveStarted()
    {
        buildingState.WaveStarted();

        LevelUp();
    }

    private void LevelUp()
    {
        BuildingLevel += 1;
    }

    public void SetState<T>() where T : BuildingState
    {
        if (typeof(ArcherState).IsAssignableFrom(typeof(T)))
        {

        }
        else if (typeof(BarracksState).IsAssignableFrom(typeof(T)))
        {

        }

        buildingState.OnStateEntered(this);
        buildingState.OnPlaced();
    }

    public void Die()
    {
        OnDeath?.Invoke(this);

        buildingState.Die();
    }

    private void OnMouseDown()
    {
        if (buildingState == null || selected || !shift.IsPressed())
        {
            return;
        }

        selected = true;
        buildingState.OnSelected();
        BuildingAnimator.BounceInOut(this);
    }

    private void OnMouseEnter()
    {
        hovered = true;
    }

    private void OnMouseExit()
    {
        hovered = false;
    }

    private void Update()
    {
        if (buildingState == null)
        {
            return;
        }

        buildingState.Update();

        if (selected && !hovered && fire.WasPerformedThisFrame())
        {
            buildingState.OnDeSelected();
            selected = false;
        }
    }

    public void RegisterAttackingEnemy(EnemyMovement enemyMovement)
    {
        AttackingEnemies.Add(enemyMovement);
        enemyMovement.GetComponent<EnemyHealth>().Health.OnDeath += OnEnemyDeath;
    }

    private void OnEnemyDeath(GameObject obj)
    {
        obj.GetComponent<EnemyHealth>().Health.OnDeath -= OnEnemyDeath;
        AttackingEnemies.Remove(obj.GetComponent<EnemyMovement>());
    }

    public void DeregisterAttackingEnemy(EnemyMovement enemyMovement)
    {
        AttackingEnemies.Remove(enemyMovement);
        enemyMovement.GetComponent<EnemyHealth>().Health.OnDeath -= OnEnemyDeath;
    }

    public void EnterDefense(Vector3 pos)
    {
        OnEnterDefense?.Invoke(pos);
    }

    public void ToggleIsBuildableVisual(bool value)
    {
        MeshRenderer rend = GetComponentInChildren<MeshRenderer>();

        if (value)
        {
            rend.SetMaterials(transparentMaterials);
        }
        else
        {
            rend.SetMaterials(Materials);
        }
    }
}
