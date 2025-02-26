using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine;
using WaveFunctionCollapse;

public class Building : PooledMonoBehaviour, IBuildable
{
    [Title("Visual")]
    [SerializeField]
    private MaterialData materialData;
    
    [SerializeField]
    private Material transparentGreen;

    [SerializeField]
    private LayerMask highlightedLayer;

    [SerializeField]
    private LayerMask selectedLayer;

    [Title("Collider")]
    [SerializeField]
    private Collider[] cornerColliders;
    
    [SerializeField]
    private BuildableCornerData buildableCornerData;

    [Title("Events")]
    [SerializeField]
    private UnityEvent OnPlacedEvent;

    [SerializeField]
    private UnityEvent OnResetEvent;

    private readonly Vector2Int[] corners =
    {
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1),
    };
    
    private List<Material> transparentMaterials = new List<Material>();

    private BuildingHandler buildingHandler;
    private BuildingAnimator buildingAnimator;
    private MeshRenderer meshRenderer;
    private BuildingUI buildingUI;

    private int originalLayer;
    
    private bool purchasing = true;
    private bool highlighted;
    private bool selected;
    private bool hovered;

    public int BuildingGroupIndex { get; set; } = -1;
    public PrototypeData Prototype { get; private set; }
    public Vector3Int Index { get; private set; }
    public int Importance => 1;

    private BuildingAnimator BuildingAnimator => buildingAnimator ??= FindAnyObjectByType<BuildingAnimator>();
    public BuildingHandler BuildingHandler => buildingHandler ??= FindAnyObjectByType<BuildingHandler>();
    public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
    public BuildingUI BuildingUI => buildingUI ??= GetComponentInChildren<BuildingUI>();
    public MeshWithRotation MeshRot => Prototype.MeshRot;
    public Mesh Mesh => Prototype.MeshRot.Mesh;

    private void Awake()
    {
        originalLayer = gameObject.layer;
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

    public async UniTask Highlight(BuildingCellInformation cellInfo)
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
        //buildingHandler[this].State.OnSelected(transform.position);
        selected = true;
    }

    public void OnDeselected()
    {
        if (!selected) return;

        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        BuildingUI.OnDeselected();
        //buildingHandler.BuildingData[Index].State.OnDeselected();
        selected = false;
    }

    #endregion

    private void Update()
    {
        BuildingHandler[this]?.Update(this);

        if (!InputManager.Instance.Fire.WasReleasedThisFrame())
        {
            return;
        }

        if (hovered && InputManager.Instance.GetShift)
        {
            BuildingHandler.HighlightGroup(this);
        }

        if (highlighted && !hovered && !buildingUI.InMenu)
        {
            BuildingHandler.LowlightGroup(this);
        }
    }

    public void Setup(PrototypeData prototypeData, Vector3 scale)
    {
        Vector3Int? nullableIndex = BuildingManager.Instance.GetIndex(transform.position);
        if (!nullableIndex.HasValue) return;
        
        Index = nullableIndex.Value;
        Prototype = GetPrototype(prototypeData);

        GetComponentInChildren<MeshFilter>().mesh = Mesh;
        MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));

        transparentMaterials = new List<Material>();
        for (int i = 0; i < prototypeData.MaterialIndexes.Length; i++)
        {
            transparentMaterials.Add(transparentGreen);
        }

        transform.localScale = scale;
    }

    private PrototypeData GetPrototype(PrototypeData newProt)
    {
        if (BuildingHandler[this] == null)
        {
            return newProt;
        }

        PrototypeData oldProt = BuildingHandler[this].Prototype;
        if (oldProt == newProt)
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
        for (int i = 0; i < cornerColliders.Length; i++)
        {
            if (MeshRot.Mesh != null && buildableCornerData.BuildableDictionary.TryGetValue(MeshRot.Mesh, out BuildableCorners cornerData))
            {
                bool value = cornerData.CornerDictionary[BuildableCornerData.VectorToCorner(corners[i].x, corners[i].y)];
                cornerColliders[i].gameObject.SetActive(value);
            }
            else
            {
                cornerColliders[i].gameObject.SetActive(false);
            }
        }
        
        BuildingHandler.AddBuilding(this).Forget(Debug.LogError);
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
        return;
        if (!data.Health.Alive)
        {
            DisplayDeath();
        }
    }
}
