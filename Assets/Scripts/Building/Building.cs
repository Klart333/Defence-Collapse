﻿using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Building : PooledMonoBehaviour, IBuildable
{
    [SerializeField]
    private MaterialData materialData;
    
    [SerializeField]
    private Material transparentGreen;

    [SerializeField]
    private LayerMask highlightedLayer;

    [SerializeField]
    private LayerMask selectedLayer;

    [Title("Events")]
    [SerializeField]
    private UnityEvent OnPlacedEvent;

    [SerializeField]
    private UnityEvent OnResetEvent;

    private List<Material> transparentMaterials = new List<Material>();

    private BuildingHandler buildingHandler;
    private BuildingAnimator buildingAnimator;
    private MeshRenderer meshRenderer;
    private BuildingUI buildingUI;
    private BoxCollider boxCollider;

    private int originalLayer;
    
    private bool highlighted = false;
    private bool selected = false;
    private bool hovered = false;
    private bool purchasing = true;

    public int BuildingGroupIndex { get; set; } = -1;
    public PrototypeData Prototype { get; set; }
    public Vector3Int Index { get; set; }
    public int Importance => 1;

    public BuildingUI BuildingUI
    {
        get
        {
            if (buildingUI == null)
            {
                buildingUI = GetComponentInChildren<BuildingUI>();
            }

            return buildingUI;
        }
    }
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
    public Mesh Mesh => Prototype.MeshRot.Mesh;

    private void Awake()
    {
        originalLayer = gameObject.layer;
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        Events.OnBuildingClicked += OnBuildingClicked;
        Events.OnBuildingCanceled += () => purchasing = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        Reset();

        Events.OnBuildingClicked -= OnBuildingClicked;
        Events.OnBuildingCanceled -= () => purchasing = false;
    }

    private void Reset()
    {
        transform.localScale = Vector3.one;
        MeshRenderer.transform.localScale = Vector3.one;

        BuildingHandler?.RemoveBuilding(this);
        BuildingGroupIndex = -1;

        purchasing = true;
        hovered = false;
        selected = false;
        MeshRenderer.gameObject.layer = originalLayer;

        OnResetEvent?.Invoke();
    }

    #region Highlight

    private void OnMouseDown()
    {
        
    }

    private void OnMouseEnter()
    {
        hovered = true;
    }

    private void OnMouseExit()
    {
        hovered = false;
    }

    public async void Highlight(BuildingCellInformation cellInfo)
    {
        if (purchasing || highlighted) return;

        BuildingAnimator.BounceInOut(transform);
        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        BuildingUI.Highlight(cellInfo);

        await UniTask.NextFrame();
        highlighted = true;
    }

    public void Lowlight()
    {
        BuildingUI.Lowlight();

        MeshRenderer.gameObject.layer = originalLayer;
        highlighted = false;
    }

    public void OnSelected(BuildingCellInformation cellInfo)
    {
        if (purchasing || selected) return;

        if (highlighted)
        {
            BuildingAnimator.BounceInOut(transform);
        }

        MeshRenderer.gameObject.layer = (int)Mathf.Log(selectedLayer.value, 2);

        BuildingUI.OnSelected(cellInfo);
        buildingHandler[this].State.OnSelected(transform.position);
        selected = true;
    }

    public void OnDeselected()
    {
        if (!selected) return;

        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        BuildingUI.OnDeselected();
        buildingHandler.BuildingData[Index].State.OnDeselected();
        selected = false;
    }

    #endregion

    private void Update()
    {
        buildingHandler[this]?.Update(this);

        if (!InputManager.Instance.Fire.WasReleasedThisFrame())
        {
            return;
        }

        if (hovered && InputManager.Instance.GetShift)
        {
            buildingHandler.HighlightGroup(this);
        }

        if (highlighted && !hovered && !buildingUI.InMenu)
        {
            BuildingHandler.LowlightGroup(this);
        }
    }

    public void Setup(PrototypeData prototypeData, Vector3 scale)
    {
        Index = BuildingManager.Instance.GetIndex(transform.position).Value;
        Prototype = GetPrototype(prototypeData);

        GetComponentInChildren<MeshFilter>().mesh = Mesh;
        MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));

        transparentMaterials = new List<Material>();
        for (int i = 0; i < prototypeData.MaterialIndexes.Length; i++)
        {
            transparentMaterials.Add(transparentGreen);
        }

        boxCollider.size = new Vector3(scale.x * 2, 0.04f, scale.z * 2);
        MeshRenderer.transform.localScale = scale;
    }

    private PrototypeData GetPrototype(PrototypeData newProt)
    {
        if (BuildingHandler[this] == null)
        {
            return newProt;
        }

        PrototypeData oldProt = BuildingHandler[this].Prototype;
        if (oldProt.PosX == newProt.PosX && 
            oldProt.NegX == newProt.NegX && 
            oldProt.PosZ == newProt.PosZ &&
            oldProt.NegZ == newProt.NegZ)
        {
            return oldProt;
        }

        return newProt;
    }

    private void OnBuildingClicked(BuildingType arg0)
    {
        purchasing = true;
        BuildingHandler.LowlightGroup(this);
    }

    public void ToggleIsBuildableVisual(bool isQueried)
    {
        if (isQueried)
        {
            MeshRenderer.SetMaterials(transparentMaterials);
        }
        else
        {
            MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));
            Place();
        }
    }

    private void Place()
    {
        BuildingHandler.AddBuilding(this);
        OnPlacedEvent?.Invoke();
    }

    public void DisplayLevelUp()
    {
        
    }

    public void DisplayDeath()
    {
        ToggleIsBuildableVisual(true);
    }

    public void SetData(BuildingData data)
    {
        if (!data.Health.Alive)
        {
            DisplayDeath();
        }
    }
}
