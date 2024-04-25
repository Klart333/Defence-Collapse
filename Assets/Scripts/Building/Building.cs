using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Building : PooledMonoBehaviour, IBuildable
{
    public event Action<Building> OnDeath;
    public event Action<Vector3> OnEnterDefense;

    [Title("State Data")]
    [SerializeField]
    private ArcherData archerData;

    [Title("Visual")]
    [SerializeField]
    private Material transparentGreen;

    private List<Material> transparentMaterials = new List<Material>();

    private BuildingHandler buildingHandler;
    private BuildingAnimator buildingAnimator;
    private BuildingState buildingState;
    private InputActions inputActions;
    private InputAction fire;
    private InputAction shift;

    private bool selected = false;
    private bool hovered = false;

    public int BuildingGroupIndex { get; set; } = -1;
    public int BuildingLevel { get; set; }

    public Mesh Mesh { get; set; }
    public List<Material> Materials { get; set; }

    public BuildingHandler BuildingHandler
    {
        get
        {
            if (buildingHandler == null)
            {
                buildingHandler = FindAnyObjectByType<BuildingHandler>();
            }

            return buildingHandler;
        }
    }
    public BuildingAnimator BuildingAnimator
    {
        get
        {
            if (buildingAnimator == null)
            {
                buildingAnimator = FindAnyObjectByType<BuildingAnimator>();
            }

            return buildingAnimator;
        }
    }

    public BuildingState BuildingState => buildingState;

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

        if (BuildingHandler != null) BuildingHandler.RemoveBuilding(this);
        BuildingGroupIndex = -1;

        transform.localScale = Vector3.one;
        Events.OnWaveStarted -= OnWaveStarted;

        fire.Disable();
        shift.Disable();
    }

    private void OnMouseDown()
    {
        if (!shift.IsPressed())
        {
            return;
        }

        BuildingHandler.HighlightGroup(BuildingGroupIndex);
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
        if (buildingState == null) return;

        buildingState.Update();

        if (selected && !hovered && fire.WasPerformedThisFrame())
        {
            buildingState.OnDeselected();
            selected = false;
        }
    }

    public void Setup(PrototypeData prototypeData, List<Material> materials)
    {
        Mesh = prototypeData.MeshRot.Mesh;

        GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        GetComponentInChildren<MeshRenderer>().SetMaterials(materials);

        Materials = materials;
        transparentMaterials = new List<Material>();
        for (int i = 0; i < materials.Count; i++)
        {
            transparentMaterials.Add(transparentGreen);
        }
    }

    public void SetState<T>() where T : BuildingState
    {
        if (typeof(ArcherState).IsAssignableFrom(typeof(T)))
        {
            buildingState = new ArcherState(archerData);
        }

        buildingState.OnStateEntered(this);
    }

    private void OnWaveStarted()
    {

    }

    private void LevelUp()
    {
        BuildingLevel += 1;
    }

    public void Die()
    {
        OnDeath?.Invoke(this);

        buildingState.Die();
    }

    public void ToggleIsBuildableVisual(bool isQueried)
    {
        MeshRenderer rend = GetComponentInChildren<MeshRenderer>();

        if (isQueried)
        {
            rend.SetMaterials(transparentMaterials);
        }
        else
        {
            rend.SetMaterials(Materials);
            Place();
        }
    }

    private void Place()
    {
        BuildingHandler.AddBuilding(this);
    }

    public void Highlight()
    {
        Debug.Log("Highlight!");
        selected = true;
        BuildingAnimator.BounceInOut(transform);

        if (buildingState != null)
        {
           buildingState.OnSelected();
        }
    }
}
