using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.VFX;
using Unity.Entities;
using UnityEngine;

namespace VFX
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXExplosionRequest
    {
        public Vector3 Position;
        public float Scale;
    }
    
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXChainLightningRequest
    {
        public Vector3 Position;
        public Vector3 Size;
        public Vector3 Angle;
        public Vector3 Color;
    }

    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXSpawnToDataRequest
    {
        public int IndexInData;
    }

    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXTrailData : IKillableVFX
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 Color;
        public float Size;
        public float Length;

        public void Kill()
        {
            Size = -1f;
        }
    }
    
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXFireData : IKillableVFX
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;

        public void Kill()
        {
            Size = -1f;
        }
    }
    
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXPoisonParticleData : IKillableVFX
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;

        public void Kill()
        {
            Size = -1f;
        }
    }
    
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct VFXLightningTrailData : IKillableVFX
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;

        public void Kill()
        {
            Size = -1f;
        }
    }

    public static class VFXReferences
    {
        public static VisualEffect   HitSparksGraph;
        public static GraphicsBuffer HitSparksRequestsBuffer;

        public static VisualEffect   ExplosionsGraph;
        public static GraphicsBuffer ExplosionsRequestsBuffer;
        
        public static VisualEffect   ChainLightningGraph;
        public static GraphicsBuffer ChainLightningRequestsBuffer;

        public static VisualEffect   TrailGraph;
        public static GraphicsBuffer TrailRequestsBuffer;
        public static GraphicsBuffer TrailDatasBuffer;
        
        public static VisualEffect   FireParticleGraph;
        public static GraphicsBuffer FireParticleRequestsBuffer;
        public static GraphicsBuffer FireParticlesDatasBuffer;
        
        public static VisualEffect   PoisonParticleGraph;
        public static GraphicsBuffer PoisonParticleRequestsBuffer;
        public static GraphicsBuffer PoisonParticlesDatasBuffer;

        public static VisualEffect LightningTrailGraph;
        public static GraphicsBuffer LightningTrailRequestsBuffer;
        public static GraphicsBuffer LightningTrailDatasBuffer;
    }

    public interface IKillableVFX
    {
        public void Kill();
    }

    public struct VFXManager<T> where T : unmanaged
    {
        public NativeReference<int> RequestsCount;
        public NativeArray<T> Requests;

        public bool GraphIsInitialized { get; private set; }

        public VFXManager(int maxRequests, ref GraphicsBuffer graphicsBuffer)
        {
            RequestsCount = new NativeReference<int>(0, Allocator.Persistent);
            Requests = new NativeArray<T>(maxRequests, Allocator.Persistent);

            graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxRequests,
                Marshal.SizeOf(typeof(T)));

            GraphIsInitialized = false;
        }

        public void Dispose(ref GraphicsBuffer graphicsBuffer)
        {
            graphicsBuffer?.Dispose();
            if (RequestsCount.IsCreated)
            {
                RequestsCount.Dispose();
            }

            if (Requests.IsCreated)
            {
                Requests.Dispose();
            }
        }

        public void Update(
            VisualEffect vfxGraph,
            ref GraphicsBuffer graphicsBuffer,
            float deltaTimeMultiplier,
            int spawnBatchId,
            int requestsCountId,
            int requestsBufferId)
        {
            if (vfxGraph == null || graphicsBuffer == null) return;
            
            vfxGraph.playRate = deltaTimeMultiplier;

            if (!GraphIsInitialized)
            {
                vfxGraph.SetGraphicsBuffer(requestsBufferId, graphicsBuffer);
                GraphIsInitialized = true;
            }

            if (graphicsBuffer.IsValid())
            {
                graphicsBuffer.SetData(Requests, 0, 0, RequestsCount.Value);
                vfxGraph.SetInt(requestsCountId, math.min(RequestsCount.Value, Requests.Length));
                vfxGraph.SendEvent(spawnBatchId);
                RequestsCount.Value = 0;
            }
        }

        public void AddRequest(T request)
        {
            if (RequestsCount.Value < Requests.Length)
            {
                Requests[RequestsCount.Value] = request;
                RequestsCount.Value++;
            }
        }
    }

    public struct VFXManagerParented<T> where T : unmanaged, IKillableVFX
    {
        public NativeReference<int> RequestsCount;
        public NativeArray<VFXSpawnToDataRequest> Requests;
        public NativeArray<T> Datas;
        private NativeQueue<int> FreeIndexes;

        public bool GraphIsInitialized { get; private set; }

        public VFXManagerParented(int maxCount, ref GraphicsBuffer requestsGraphicsBuffer, ref GraphicsBuffer datasGraphicsBuffer)
        {
            RequestsCount = new NativeReference<int>(0, Allocator.Persistent);
            Requests = new NativeArray<VFXSpawnToDataRequest>(maxCount, Allocator.Persistent);
            Datas = new NativeArray<T>(maxCount, Allocator.Persistent);
            FreeIndexes = new NativeQueue<int>(Allocator.Persistent);

            requestsGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxCount,
                Marshal.SizeOf(typeof(VFXSpawnToDataRequest)));
            datasGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxCount,
                Marshal.SizeOf(typeof(T)));

            for (int i = 0; i < maxCount; i++)
            {
                FreeIndexes.Enqueue(i);
            }

            GraphIsInitialized = false;
        }

        public void Dispose(ref GraphicsBuffer requestsGraphicsBuffer, ref GraphicsBuffer datasGraphicsBuffer)
        {
            requestsGraphicsBuffer?.Dispose();
            datasGraphicsBuffer?.Dispose();
            if (RequestsCount.IsCreated)
            {
                RequestsCount.Dispose();
            }

            if (Requests.IsCreated)
            {
                Requests.Dispose();
            }

            if (Datas.IsCreated)
            {
                Datas.Dispose();
            }

            if (FreeIndexes.IsCreated)
            {
                FreeIndexes.Dispose();
            }
        }

        public void Update(
            VisualEffect vfxGraph,
            ref GraphicsBuffer requestsGraphicsBuffer,
            ref GraphicsBuffer datasGraphicsBuffer,
            float deltaTimeMultiplier,
            int spawnBatchId,
            int requestsCountId,
            int requestsBufferId,
            int datasBufferId)
        {
            if (vfxGraph == null || requestsGraphicsBuffer == null || datasGraphicsBuffer == null) return;
            vfxGraph.playRate = deltaTimeMultiplier;

            if (!GraphIsInitialized)
            {
                vfxGraph.SetGraphicsBuffer(requestsBufferId, requestsGraphicsBuffer);
                vfxGraph.SetGraphicsBuffer(datasBufferId, datasGraphicsBuffer);
                GraphIsInitialized = true;
            }

            if (!requestsGraphicsBuffer.IsValid() || !datasGraphicsBuffer.IsValid()) return;
            
            requestsGraphicsBuffer.SetData(Requests, 0, 0, RequestsCount.Value);
            datasGraphicsBuffer.SetData(Datas);

            vfxGraph.SetInt(requestsCountId, math.min(RequestsCount.Value, Requests.Length));
            vfxGraph.SendEvent(spawnBatchId);

            RequestsCount.Value = 0;
        }

        public int Create()
        {
            if (!FreeIndexes.TryDequeue(out int index)) return -1;
            
            // Request to spawn
            if (RequestsCount.Value >= Requests.Length) return index;
            
            Requests[RequestsCount.Value] = new VFXSpawnToDataRequest
            {
                IndexInData = index,
            };
            RequestsCount.Value++;

            return index;

        }

        public void Kill(int index)
        {
            if (index < 0 || index >= Datas.Length) return;
            
            T data = Datas[index];
            data.Kill();
            Datas[index] = data;

            FreeIndexes.Enqueue(index);
        }
    }

    public struct VFXHitSparksSingleton : IComponentData
    {
        public VFXManager<VFXHitSparksRequest> Manager;
    }

    public struct VFXExplosionsSingleton : IComponentData
    {
        public VFXManager<VFXExplosionRequest> Manager;
    }
    
    public struct VFXChainLightningSingleton : IComponentData
    {
        public VFXManager<VFXChainLightningRequest> Manager;
    }
    
    public struct VFXTrailSingleton : IComponentData
    {
        public VFXManagerParented<VFXTrailData> Manager;
    }
    
    public struct VFXFireParticlesSingleton : IComponentData
    {
        public VFXManagerParented<VFXFireData> Manager;
    }
    
    public struct VFXPoisonParticlesSingleton : IComponentData
    {
        public VFXManagerParented<VFXPoisonParticleData> Manager;
    }

    public struct VFXLightningTrailSingleton : IComponentData
    {
        public VFXManagerParented<VFXLightningTrailData> Manager;
    }
    
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct VFXSystem : ISystem
    {
        private int _spawnBatchId;
        private int _requestsCountId;
        private int _requestsBufferId;
        private int _datasBufferId;

        private VFXManager<VFXChainLightningRequest> chainLightningManager;
        private VFXManager<VFXExplosionRequest> explosionsManager;
        private VFXManager<VFXHitSparksRequest> hitSparksManager;
        
        private VFXManagerParented<VFXPoisonParticleData> poisonParticleManager;
        private VFXManagerParented<VFXLightningTrailData> lightningTrailManager;
        private VFXManagerParented<VFXFireData> fireParticleManager;
        private VFXManagerParented<VFXTrailData> trailManager;

        private const int PoisonParticleCapacity = 1000;
        private const int ChainLightningCapacity = 1000;
        private const int LightningTrailCapacity = 1000;
        private const int FireParticleCapacity = 1000;
        private const int ExplosionsCapacity = 1000;
        private const int HitSparksCapacity = 1000;
        private const int TrailCapacity = 1000;

        public void OnCreate(ref SystemState state)
        {
            // Names to Ids
            _spawnBatchId = Shader.PropertyToID("SpawnBatch");
            _requestsCountId = Shader.PropertyToID("SpawnRequestsCount");
            _requestsBufferId = Shader.PropertyToID("SpawnRequestsBuffer");
            _datasBufferId = Shader.PropertyToID("DatasBuffer");

            // VFX managers
            chainLightningManager = new VFXManager<VFXChainLightningRequest>(ChainLightningCapacity, ref VFXReferences.ChainLightningRequestsBuffer);
            hitSparksManager  = new VFXManager<VFXHitSparksRequest>(HitSparksCapacity , ref VFXReferences.HitSparksRequestsBuffer );
            explosionsManager = new VFXManager<VFXExplosionRequest>(ExplosionsCapacity, ref VFXReferences.ExplosionsRequestsBuffer);

            poisonParticleManager = new VFXManagerParented<VFXPoisonParticleData>(PoisonParticleCapacity, ref VFXReferences.PoisonParticleRequestsBuffer, ref VFXReferences.PoisonParticlesDatasBuffer);
            lightningTrailManager = new VFXManagerParented<VFXLightningTrailData>(LightningTrailCapacity, ref VFXReferences.LightningTrailRequestsBuffer, ref VFXReferences.LightningTrailDatasBuffer);
            fireParticleManager = new VFXManagerParented<VFXFireData>(FireParticleCapacity, ref VFXReferences.FireParticleRequestsBuffer, ref VFXReferences.FireParticlesDatasBuffer);
            trailManager = new VFXManagerParented<VFXTrailData>(TrailCapacity, ref VFXReferences.TrailRequestsBuffer, ref VFXReferences.TrailDatasBuffer);

            // Singletons
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXHitSparksSingleton
            {
                Manager = hitSparksManager,
            });
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXExplosionsSingleton
            {
                Manager = explosionsManager,
            });
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXChainLightningSingleton
            {
                Manager = chainLightningManager,
            });
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXTrailSingleton
            {
                Manager = trailManager,
            });
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXFireParticlesSingleton
            {
                Manager = fireParticleManager,
            });
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXPoisonParticlesSingleton
            {
                Manager = poisonParticleManager,
            });
            state.EntityManager.AddComponentData(state.EntityManager.CreateEntity(), new VFXLightningTrailSingleton
            {
                Manager = lightningTrailManager,
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            poisonParticleManager.Dispose(ref VFXReferences.PoisonParticleRequestsBuffer, ref VFXReferences.PoisonParticlesDatasBuffer);
            lightningTrailManager.Dispose(ref VFXReferences.LightningTrailRequestsBuffer, ref VFXReferences.LightningTrailDatasBuffer);
            fireParticleManager.Dispose(ref VFXReferences.FireParticleRequestsBuffer, ref VFXReferences.FireParticlesDatasBuffer);
            trailManager.Dispose(ref VFXReferences.TrailRequestsBuffer, ref VFXReferences.TrailDatasBuffer);
            
            chainLightningManager.Dispose(ref VFXReferences.ChainLightningRequestsBuffer);
            explosionsManager.Dispose(ref VFXReferences.ExplosionsRequestsBuffer);
            hitSparksManager.Dispose(ref VFXReferences.HitSparksRequestsBuffer);
        }

        public void OnUpdate(ref SystemState state)
        {
            // This is required because we must use data in native collections on the main thread, to send it to VFXGraphs
            SystemAPI.QueryBuilder().WithAll<VFXPoisonParticlesSingleton>().Build().CompleteDependency();
            SystemAPI.QueryBuilder().WithAll<VFXChainLightningSingleton>().Build().CompleteDependency();
            SystemAPI.QueryBuilder().WithAll<VFXLightningTrailSingleton>().Build().CompleteDependency();
            SystemAPI.QueryBuilder().WithAll<VFXFireParticlesSingleton>().Build().CompleteDependency();
            SystemAPI.QueryBuilder().WithAll<VFXExplosionsSingleton>().Build().CompleteDependency();
            SystemAPI.QueryBuilder().WithAll<VFXHitSparksSingleton>().Build().CompleteDependency();
            SystemAPI.QueryBuilder().WithAll<VFXTrailSingleton>().Build().CompleteDependency();

            // Update managers
            float rateRatio = SystemAPI.Time.DeltaTime / Time.deltaTime;

            hitSparksManager.Update(
                VFXReferences.HitSparksGraph,
                ref VFXReferences.HitSparksRequestsBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId);

            explosionsManager.Update(
                VFXReferences.ExplosionsGraph,
                ref VFXReferences.ExplosionsRequestsBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId);
            
            chainLightningManager.Update(
                VFXReferences.ChainLightningGraph,
                ref VFXReferences.ChainLightningRequestsBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId);

            trailManager.Update(
                VFXReferences.TrailGraph,
                ref VFXReferences.TrailRequestsBuffer,
                ref VFXReferences.TrailDatasBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId,
                _datasBufferId);
            
            fireParticleManager.Update(
                VFXReferences.FireParticleGraph,
                ref VFXReferences.FireParticleRequestsBuffer,
                ref VFXReferences.FireParticlesDatasBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId,
                _datasBufferId);
            
            poisonParticleManager.Update(
                VFXReferences.PoisonParticleGraph,
                ref VFXReferences.PoisonParticleRequestsBuffer,
                ref VFXReferences.PoisonParticlesDatasBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId,
                _datasBufferId);
            
            lightningTrailManager.Update(
                VFXReferences.LightningTrailGraph,
                ref VFXReferences.LightningTrailRequestsBuffer,
                ref VFXReferences.LightningTrailDatasBuffer,
                rateRatio,
                _spawnBatchId,
                _requestsCountId,
                _requestsBufferId,
                _datasBufferId);
        }
    }
}