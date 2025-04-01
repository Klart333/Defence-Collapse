using MeshCollider = Unity.Physics.MeshCollider;
using Material = Unity.Physics.Material;
using Collider = Unity.Physics.Collider;
using Random = UnityEngine.Random;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using System;

namespace WaveFunctionCollapse
{
    public class GroundGenerator : MonoBehaviour
    {
        public event Action OnMapGenerated;
        public event Action<Cell> OnCellCollapsed;

        [Title("Wave Function")]
        [SerializeField]
        private WaveFunction waveFunction;

        [Title("Settings")]
        [SerializeField]
        private int chillTimeMs;

        [SerializeField]
        private float maxMillisecondsPerFrame = 4;

        [SerializeField]
        private bool shouldCombine = true;

        [Title("Debug")]
        [SerializeField]
        private bool shouldRun = true;
        
        private BlobAssetReference<Collider> blobCollider;

        public WaveFunction WaveFunction => waveFunction;

        private void Start()
        {
            if (shouldRun)
                _ = Run();
        }

        private void OnDisable()
        {
            blobCollider.Dispose();
        }

        public async UniTask Run()
        {
            waveFunction.ParentTransform = transform;
            if (!waveFunction.Load())
            {
                return;
            }

            //await PredeterminePath();
            await BottomBuildUp();

            Stopwatch watch = Stopwatch.StartNew();
            while (!waveFunction.AllCollapsed)
            {
                Cell collapsedCell = waveFunction.Iterate();
                OnCellCollapsed?.Invoke(collapsedCell);

                if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;

                await UniTask.NextFrame();
                if (chillTimeMs > 0)
                {
                    await UniTask.Delay(chillTimeMs);
                }

                watch.Restart();
            }

            if (shouldCombine)
            {
                CombineMeshes();
            }

            OnMapGenerated?.Invoke();
        }

        private async UniTask BottomBuildUp()
        {
            for (int x = 0; x < waveFunction.GridSize.x; x++)
            for (int z = 0; z < waveFunction.GridSize.z; z++)
            {
                if ((x == 0 || x == waveFunction.GridSize.x - 1) && (z == 0 || z == waveFunction.GridSize.z - 1))
                {
                    PlaceGround(x, z);
                    await UniTask.Yield();
                    continue;
                }

                if ((x != 0 && x != waveFunction.GridSize.x - 1) && (z != 0 && z != waveFunction.GridSize.z - 1))
                {
                    continue;
                }

                if (Random.value < 0.8f)
                {
                    continue;
                }

                PlaceGround(x, z);
                await UniTask.Yield();
            }
        

            return;

            void PlaceGround(int x, int z)
            {
                int index = waveFunction.GetIndex(x, 0, z);
                if (waveFunction.Cells[index].PossiblePrototypes.Contains(waveFunction.Prototypes[0]))
                {
                    waveFunction.SetCell(index, waveFunction.Prototypes[0]);

                    waveFunction.Propagate();
                }
            }
        }


        private void CombineMeshes()
        {
            Mesh mesh = GetComponent<MeshCombiner>().CombineMeshes();
            blobCollider = MeshCollider.Create(mesh, new CollisionFilter
            {
                BelongsTo = 6,
                CollidesWith = 6,
                GroupIndex = 0,
            }, Material.Default);

            ComponentType[] componentTypes = new ComponentType[4]
            {
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(PhysicsCollider),
                typeof(PhysicsWorldIndex),
            };

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity entity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = gameObject.transform.localToWorldMatrix,
            });
            entityManager.SetComponentData(entity, new LocalTransform()
            {
                Position = transform.localPosition,
                Rotation = transform.localRotation,
                Scale = transform.localScale.x,
            });
            entityManager.SetComponentData(entity, new PhysicsCollider
            {
                Value = blobCollider,
            });
        }
    }


}