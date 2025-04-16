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
    private ProtoypeMeshes protoypeMeshes;
    
    [SerializeField]
    private Material transparentGreen;

    [SerializeField]
    private LayerMask highlightedLayer;

    [SerializeField]
    private LayerMask selectedLayer;
    
    [SerializeField]
    private Transform meshTransform;

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

    private bool purchasing = true;
    private int originalLayer;
    private bool highlighted;
    private bool selected;

    public PrototypeData Prototype { get; private set; }
    public int BuildingGroupIndex { get; set; } = -1;
    public ChunkIndex ChunkIndex { get; private set; }
    public int Importance => 1;

    private BuildingAnimator BuildingAnimator => buildingAnimator ??= FindAnyObjectByType<BuildingAnimator>();
    public BuildingHandler BuildingHandler => buildingHandler ??= FindAnyObjectByType<BuildingHandler>();
    public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
    private bool Hovered => cornerColliders.Any(x => x.IsHovered);
    public MeshWithRotation MeshRot => Prototype.MeshRot;
    public Transform MeshTransform => meshTransform;

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
        
        OnResetEvent?.Invoke();
    }

    #region Highlight

    public async UniTaskVoid Highlight()
    {
        if (purchasing || highlighted) return;

        BuildingAnimator.BounceInOut(meshTransform);
        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        await UniTask.NextFrame();
        highlighted = true;
    }

    public void Lowlight()
    {
        MeshRenderer.gameObject.layer = originalLayer;
        highlighted = false;
    }

    public void OnSelected()
    {
        if (purchasing || selected) return;

        if (highlighted)
        {
            BuildingAnimator.BounceInOut(meshTransform);
        }

        MeshRenderer.gameObject.layer = (int)Mathf.Log(selectedLayer.value, 2);

        //buildingHandler[this].State.OnSelected(transform.position);
        selected = true;
    }

    public void OnDeselected()
    {
        if (!selected) return;

        MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

        //buildingHandler.BuildingData[Index].State.OnDeselected();
        selected = false;
    }

    #endregion

    private void Update()
    {
        if (!InputManager.Instance.Fire.WasReleasedThisFrame())
        {
            return;
        }

        bool hovered = Hovered;
        if (hovered && InputManager.Instance.GetShift)
        {
            BuildingHandler.HighlightGroup(this);
        }

        if (highlighted && !hovered)
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
        
        ChunkIndex = nullableIndex.Value;
        Prototype = prototypeData;

        GetComponentInChildren<MeshFilter>().mesh = Prototype.MeshRot.MeshIndex != -1
         ? protoypeMeshes.Meshes[Prototype.MeshRot.MeshIndex] 
         : null;
        MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));

        transparentMaterials = new List<Material>();
        for (int i = 0; i < Prototype.MaterialIndexes.Length; i++)
        {
            transparentMaterials.Add(transparentGreen);
        }
        
        transform.localScale = scale;
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
            if (MeshRot.MeshIndex != -1 && buildableCornerData.BuildableDictionary.TryGetValue(protoypeMeshes[MeshRot.MeshIndex], out BuildableCorners cornerData))
            {
                bool value = cornerData.CornerDictionary[BuildableCornerData.VectorToCorner(corners[i].x, corners[i].y)].Buildable;
                cornerColliders[i].gameObject.SetActive(value);
            }
            else
            {
                cornerColliders[i].gameObject.SetActive(false);
            }
        }
        
        indexer.OnRebuilt += IndexerOnOnRebuilt;
        indexer.NeedsRebuilding = true;
        indexer.DelayFrames = 1;
        
        BuildingHandler.AddBuilding(this).Forget();
        OnPlacedEvent?.Invoke();
    }

    private void IndexerOnOnRebuilt()
    {
        indexer.OnRebuilt -= IndexerOnOnRebuilt;
        
        ChunkIndex chunkIndex = ChunkIndex;
        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            PathIndex index = indexer.Indexes[i];
            AttackingSystem.DamageEvent.TryAdd(index, x => BuildingHandler.BuildingTakeDamage(chunkIndex, x, index));
        }
    }

    public void OnDestroyed()
    {
        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            StopAttackingSystem.KilledIndexes.Enqueue(indexer.Indexes[i]);
        }

        for (int i = 0; i < indexer.Indexes.Count; i++)
        {
            PathIndex index = indexer.Indexes[i];
            AttackingSystem.DamageEvent.Remove(index);
        }
        
        gameObject.SetActive(false);
    }
}
