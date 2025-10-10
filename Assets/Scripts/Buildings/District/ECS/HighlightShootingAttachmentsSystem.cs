using Buildings.District.DistrictAttachment;
using Effects.ECS;
using Effects.ECS.ECB;
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
    [BurstCompile, UpdateAfter(typeof(DistrictTargetingFinalECBSystem))]
    public partial struct HighlightShootingAttachmentsSystem : ISystem
    {
        private RenderFilterSettings defaultRenderFilterSettings;
        private RenderFilterSettings highlightRenderFilterSettings;
        
        private ComponentLookup<AttackSpeedComponent> attackSpeedLookup;
        private ComponentLookup<ShakeSystem.ShakeComponent> shakeLookup;
        private ComponentLookup<LocalTransform> transformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            const int buildingLayer = 1;
            const int highlightLayer = 3;
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
            
            attackSpeedLookup = SystemAPI.GetComponentLookup<AttackSpeedComponent>(true);
            shakeLookup = SystemAPI.GetComponentLookup<ShakeSystem.ShakeComponent>(true);
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            
            state.RequireForUpdate<UpdateTargetingTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            attackSpeedLookup.Update(ref state);
            transformLookup.Update(ref state);
            shakeLookup.Update(ref state);
            
            new HighlightShootingAttachmentsJob
            {
                HighlightRenderFilterSettings = highlightRenderFilterSettings,
                DefaultRenderFilterSettings = defaultRenderFilterSettings,
                AttackSpeedLookup = attackSpeedLookup,
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
                ShakeLookup = shakeLookup,
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
            public ComponentLookup<AttackSpeedComponent> AttackSpeedLookup;
            
            [ReadOnly]
            public ComponentLookup<ShakeSystem.ShakeComponent> ShakeLookup;
            
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> TransformLookup;
            
            public RenderFilterSettings DefaultRenderFilterSettings;
            public RenderFilterSettings HighlightRenderFilterSettings;
            
            public EntityCommandBuffer.ParallelWriter ECB;
            
            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in AttachementMeshComponent attachmentMeshComponent)
            {
                Entity meshEntity = attachmentMeshComponent.AttachmentMeshEntity;
                float timer = AttackSpeedLookup[attachmentMeshComponent.Target].AttackTimer;
                ECB.SetSharedComponent(sortKey, meshEntity, timer <= 1.0f 
                    ? HighlightRenderFilterSettings 
                    : DefaultRenderFilterSettings);

                bool isShaking = ShakeLookup.TryGetComponent(entity, out ShakeSystem.ShakeComponent shake);
                if (timer <= 1.0f && !isShaking)
                {
                    ECB.AddComponent(sortKey, entity, new ShakeSystem.ShakeComponent
                    {
                        OriginalPosition = TransformLookup[entity].Position,
                        Amplitude = 0.008f,
                        Frequency = 7.5f,
                    });
                }
                else if (timer > 1 && isShaking)
                {
                    TransformLookup.GetRefRW(entity).ValueRW.Position = shake.OriginalPosition;
                    ECB.RemoveComponent<ShakeSystem.ShakeComponent>(sortKey, entity);
                }
            }
        }
    }
}