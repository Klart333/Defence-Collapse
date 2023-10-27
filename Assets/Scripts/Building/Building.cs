using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class Building : MonoBehaviour
{
    public event Action<Building> OnDeath;
    public event Action<Vector3> OnEnterDefense;

    [Header("Placing")]
    [SerializeField]
    private float bounceSpeed = 1;

    [SerializeField]
    private float scaleMult = 0.75f;

    [Header("Stats")]
    [SerializeField]
    private float cost;

    [SerializeField]
    private ArcherData archerData;

    [SerializeField]
    private BarrackData barrackData;

    [Header("Grounding")]
    [SerializeField]
    private Transform[] groundTransforms;

    [SerializeField]
    private float groundDistance = 0.1f;

    [SerializeField]
    private LayerMask layerMask;

    private BuildingState buildingState;
    private InputActions inputActions;
    private InputAction fire;
    private InputAction shift;

    private int collisions = 0;

    public Fighter[] Fighters { get; set; }
    public Building[] Towers { get; set; }

    private bool selected = false;
    private bool hovered = false;

    public int Collsions => collisions;
    public float Cost => cost;
    public int BuildingSize { get; set; }
    public int BuildingLevel { get; set; }

    // Placing
    public Vector3 StartScale { get; private set; }
    public Vector3 PlacingScale => StartScale * scaleMult;
    public float ScaleMult => scaleMult;
    public float BounceSpeed => bounceSpeed;

    public List<Fighter> SpawnedFighters { get; set; } = new List<Fighter>();
    public List<EnemyMovement> AttackingEnemies { get; set; } = new List<EnemyMovement>();

    private void OnEnable()
    {
        StartScale = transform.localScale;

        Events.OnWaveStarted += OnWaveStarted;

        inputActions = new InputActions();
        fire = inputActions.Player.Fire;
        fire.Enable();

        shift = inputActions.Player.Shift;
        shift.Enable();
    }

    private void OnDisable()
    {
        Events.OnWaveStarted -= OnWaveStarted;

        fire.Disable();
        shift.Disable();
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
            buildingState = new ArcherState(archerData);
        }
        else if (typeof(BarracksState).IsAssignableFrom(typeof(T)))
        {
            buildingState = new BarracksState(barrackData);
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
        BuildingPlacer.BounceInOut(this);
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

    #region Placing Checks
    public bool IsGrounded()
    {
        for (int i = 0; i < groundTransforms.Length; i++)
        {
            if (Physics.Raycast(groundTransforms[i].position, Vector3.down, groundDistance, layerMask) == false)
            {
                return false;
            }
        }

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        collisions++;
    }

    private void OnTriggerExit(Collider other)
    {
        collisions--;
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisions++;
    }

    private void OnCollisionExit(Collision collision)
    {
        collisions--;
    }

    #endregion
}
