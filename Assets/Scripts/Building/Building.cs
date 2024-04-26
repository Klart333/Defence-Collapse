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

    [SerializeField]
    private LayerMask selectedLayer;

    private List<Material> transparentMaterials = new List<Material>();

    private BuildingHandler buildingHandler;
    private BuildingAnimator buildingAnimator;
    private BuildingState buildingState;
    private InputActions inputActions; // FIX THIS HORRIBLE INPUT SHIT MAN WTF WAS I THINKING
    private MeshRenderer meshRenderer;
    private InputAction fire;
    private InputAction shift;

    private int originalLayer;
    
    private bool selected = false;
    private bool hovered = false;
    private bool purchasing = true;

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
    public MeshRenderer MeshRenderer
    {
        get
        {
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

            return meshRenderer;
        }
    }

    public BuildingState BuildingState => buildingState;

    private void Awake()
    {
        originalLayer = gameObject.layer;
    }

    private void OnEnable()
    {
        Events.OnWaveStarted += OnWaveStarted;
        Events.OnBuildingClicked += OnBuildingClicked;
        Events.OnBuildingCanceled += () => purchasing = false;

        inputActions = new InputActions();
        fire = inputActions.Player.Fire;
        fire.Enable();

        shift = inputActions.Player.Shift;
        shift.Enable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        Reset();

        Events.OnWaveStarted -= OnWaveStarted;
        Events.OnBuildingClicked -= OnBuildingClicked;
        Events.OnBuildingCanceled -= () => purchasing = false;

        fire.Disable();
        shift.Disable();
    }

    private void Reset()
    {
        transform.localScale = Vector3.one;

        BuildingHandler?.RemoveBuilding(this);
        BuildingGroupIndex = -1;

        purchasing = true;
        hovered = false;
        selected = false;
        MeshRenderer.gameObject.layer = originalLayer;

        buildingState = null;
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
        if (selected && !hovered && fire.WasPerformedThisFrame())
        {
            BuildingHandler.LowlightGroup(this);
        }

        if (buildingState == null) return;

        buildingState.Update();
    }

    public void Setup(PrototypeData prototypeData, List<Material> materials)
    {
        Mesh = prototypeData.MeshRot.Mesh;

        GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        MeshRenderer.SetMaterials(materials);

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

    private void OnBuildingClicked(BuildingType arg0)
    {
        purchasing = true;
        BuildingHandler.LowlightGroup(this);
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
        if (isQueried)
        {
            MeshRenderer.SetMaterials(transparentMaterials);
        }
        else
        {
            MeshRenderer.SetMaterials(Materials);
            Place();
        }
    }

    private void Place()
    {
        BuildingHandler.AddBuilding(this);
    }

    public void Highlight()
    {
        if (purchasing) return;

        selected = true;
        BuildingAnimator.BounceInOut(transform);
        MeshRenderer.gameObject.layer = (int)Mathf.Log(selectedLayer.value, 2); // sure ?
        buildingState?.OnSelected();
    }

    public void Lowlight()
    {
        buildingState?.OnDeselected();
        MeshRenderer.gameObject.layer = originalLayer;
        selected = false;
    }
}
