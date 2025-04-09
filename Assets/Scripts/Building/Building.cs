using System.Collections.Generic;
using DataStructures.Queue.ECS;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine.Events;
using System.Linq;
using Pathfinding;
using UnityEngine;

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
    private Hovered[] cornerColliders;

    [SerializeField]
    private Indexer indexer;
    
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

    private BuildingAnimator buildingAnimator;
    private BuildingHandler buildingHandler;
    private MeshRenderer meshRenderer;
    private BuildingUI buildingUI;

    private bool purchasing = true;
    private int originalLayer;
    private bool highlighted;
    private bool selected;

    public PrototypeData Prototype { get; private set; }
    public int BuildingGroupIndex { get; set; } = -1;
    public ChunkIndex Index { get; private set; }
    public int Importance => 1;

    private bool Hovered => cornerColliders.Any(x => x.IsHovered);
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
        selected = false;
        MeshRenderer.gameObject.layer = originalLayer;

        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            int index = indexer.Indexes[i];
            AttackingSystem.DamageEvent.Remove(index);
        }
        
        OnResetEvent?.Invoke();
    }

    #region Highlight

    public async UniTask Highlight(BuildingCellInformation cellInfo)
    {
        if (purchasing || highlighted) return;

        BuildingAnimator.BounceInOut(transform);
        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        BuildingUI?.Highlight(cellInfo);

        await UniTask.NextFrame();
        highlighted = true;
    }

    public void Lowlight()
    {
        BuildingUI?.Lowlight();

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

        BuildingUI?.OnSelected(cellInfo);
        //buildingHandler[this].State.OnSelected(transform.position);
        selected = true;
    }

    public void OnDeselected()
    {
        if (!selected) return;

        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        BuildingUI?.OnDeselected();
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

        bool hovered = Hovered;
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
        ChunkIndex? nullableIndex = BuildingManager.Instance.GetIndex(transform.position + scale / 2.0f);
        if (!nullableIndex.HasValue)
        {
            Debug.LogError("Could not find chunk index");
            return;
        }
        
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
            if (Prototype.MaterialIndexes != null)
            {
                MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));
            }
            
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
        
        indexer.OnRebuilt += IndexerOnOnRebuilt;
        
        BuildingHandler.AddBuilding(this).Forget(Debug.LogError);
        OnPlacedEvent?.Invoke();
    }

    private void IndexerOnOnRebuilt()
    {
        indexer.OnRebuilt -= IndexerOnOnRebuilt;
        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            int index = indexer.Indexes[i];
            AttackingSystem.DamageEvent.TryAdd(index, TakeDamage);
        }
    }

    private void TakeDamage(float damage)
    {
        Debug.Log($"Taking {damage} damage");
        BuildingHandler[this].TakeDamage(damage);
    }

    public void OnDestroyed()
    {
        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            StopAttackingSystem.KilledIndexes.Enqueue(indexer.Indexes[i]);
        }

        GetComponent<PathTarget>().enabled = false;
        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            int index = indexer.Indexes[i];
            AttackingSystem.DamageEvent.Remove(index);
        }
        
        ToggleIsBuildableVisual(true);
    }

    public void DisplayLevelUp()
    {
        
    }

    public void SetData(BuildingData data)
    {
        
    }
}
