using Buildings.District.DistrictAttachment;
using Enemy.ECS;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities.Graphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Buildings.District.ECS
{
    [BurstCompile, UpdateAfter(typeof(DistrictAttachementMeshSystem))]
    public partial struct HighlightShootingAttachmentsSystem : ISystem
    {
        private RenderFilterSettings defaultRenderFilterSettings;
        private RenderFilterSettings highlightRenderFilterSettings;
        
        private BufferLookup<LinkedEntityGroup> childLookup;
        private ComponentLookup<AttackSpeedComponent> attackSpeedLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            const int buildingLayer = 2;
            const int highlightLayer = 10;
            defaultRenderFilterSettings = new RenderFilterSettings
            {
                Layer = buildingLayer,
                RenderingLayerMask = 1 << buildingLayer,
                ShadowCastingMode = ShadowCastingMode.On,
                ReceiveShadows = true,
                MotionMode = MotionVectorGenerationMode.ForceNoMotion,
                StaticShadowCaster = false,
            };
            
            highlightRenderFilterSettings= new RenderFilterSettings
            {
                Layer = highlightLayer,
                RenderingLayerMask = 1 << highlightLayer,
                ShadowCastingMode = ShadowCastingMode.On,
                ReceiveShadows = true,
                MotionMode = MotionVectorGenerationMode.ForceNoMotion,
                StaticShadowCaster = false,
            };

            childLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            attackSpeedLookup = SystemAPI.GetComponentLookup<AttackSpeedComponent>(true);
            
            state.RequireForUpdate<UpdateTargetingTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            attackSpeedLookup.Update(ref state);
            childLookup.Update(ref state);
            
            new HighlightShootingAttachmentsJob
            {
                HighlightRenderFilterSettings = highlightRenderFilterSettings,
                DefaultRenderFilterSettings = defaultRenderFilterSettings,
                AttackSpeedLookup = attackSpeedLookup,
                ECB = ecb.AsParallelWriter(),
                ChildLookup = childLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile]
        public partial struct HighlightShootingAttachmentsJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<LinkedEntityGroup> ChildLookup;

            [ReadOnly]
            public ComponentLookup<AttackSpeedComponent> AttackSpeedLookup;
            
            public RenderFilterSettings DefaultRenderFilterSettings;
            public RenderFilterSettings HighlightRenderFilterSettings;
            
            public EntityCommandBuffer.ParallelWriter ECB;
            
            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in AttachementMeshComponent attachmentMeshComponent)
            {
                Entity meshEntity = ChildLookup[entity][1].Value;
                 float timer = AttackSpeedLookup[attachmentMeshComponent.Target].AttackTimer;
                ECB.SetSharedComponent(sortKey, meshEntity, timer <= 1.0f 
                    ? HighlightRenderFilterSettings 
                    : DefaultRenderFilterSettings);
            }
        }
    }
}