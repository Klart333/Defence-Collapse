using Unity.Entities.Graphics;
using TextMeshDOTS.Rendering;
using TextMeshDOTS.Authoring;
using TextMeshDOTS.HarfBuzz;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using InputCamera.ECS;
using Unity.Rendering;
using Unity.Entities;
using TextMeshDOTS;
using UnityEngine;
using Effects.ECS;
using Unity.Burst;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem)), UpdateBefore(typeof(EnemyModifierSystem))]
    public partial struct BossNameSystem : ISystem
    {
        private BlobAssetReference<FontBlob> fontReference;
        private EntityArchetype textRenderArchetype;
        private TextBaseConfiguration textBaseConfiguration;
        private RenderFilterSettings renderFilterSettings;
        private TextRenderControl textRenderControl;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            textRenderArchetype = GetSingleFontTextArchetype(ref state);

            FontRequest fontRequest = GetFontRequest();
            fontReference = FontBlobber.GetRuntimeFontBlob(fontRequest);
            
            textBaseConfiguration = new TextBaseConfiguration
            {
                fontSize = 4f,
                color= Color.white,
                fontStyles = FontStyles.Normal,
                maxLineWidth = 300,
                wordSpacing = 0,
                lineSpacing = 0,
                paragraphSpacing = 0,
                lineJustification = HorizontalAlignmentOptions.Left,
                verticalAlignment = VerticalAlignmentOptions.TopBase,
                isOrthographic = false,
            };
            
            const int layer = 1;
            renderFilterSettings = new RenderFilterSettings
            {
                Layer = layer,
                RenderingLayerMask = 1 << layer,
                ShadowCastingMode = ShadowCastingMode.Off,
                ReceiveShadows = false,
                MotionMode = MotionVectorGenerationMode.ForceNoMotion,
                StaticShadowCaster = false,
            };
            
            textRenderControl = new TextRenderControl { flags = TextRenderControl.Flags.Dirty };            
            
            EntityQuery query = SystemAPI.QueryBuilder().WithAll<EntityNameComponent, EnemySpawnedTag>().Build();
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new BossNameJob
            {
                ECB = ecb.AsParallelWriter(),
                FontReference = fontReference, 
                TextBaseConfiguration = textBaseConfiguration,
                RenderFilterSettings = renderFilterSettings,
                TextRenderControl = textRenderControl,
                TextArchetype = textRenderArchetype,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            fontReference.Dispose();
        }
        
        private FontRequest GetFontRequest()
        {
            //use FontUtility Scriptable Object to extract the following needed information
            //see ReadMe for more details how
            return new FontRequest
            {
                fontAssetPath = "GermaniaOne-Regular.ttf",
                fontFamily = "GermaniaOne",
                fontSubFamily = "Regular",
                typographicFamily = "",
                typographicSubfamily = "",
                fontWeight = FontWeight.Normal,
                fontWidth = 100,
                isItalic = false,
                slant = 0,
                useSystemFont = false,
                samplingPointSizeSDF = 64,
                samplingPointSizeBitmap = 64
            };
        }
        
        private EntityArchetype GetSingleFontTextArchetype(ref SystemState state)
        {
            NativeArray<ComponentType> componentTypeStaging = new NativeArray<ComponentType>(18, Allocator.Temp);
            componentTypeStaging[0]  = ComponentType.ReadWrite<FontBlobReference>();            
            componentTypeStaging[1]  = ComponentType.ReadWrite<TextBaseConfiguration>();
            componentTypeStaging[2]  = ComponentType.ReadWrite<TextRenderControl>();
            componentTypeStaging[3]  = ComponentType.ReadWrite<GlyphOTF>();
            componentTypeStaging[4]  = ComponentType.ReadWrite<CalliByte>();
            componentTypeStaging[5]  = ComponentType.ReadWrite<XMLTag>();
            componentTypeStaging[6]  = ComponentType.ReadWrite<RenderGlyph>();
            componentTypeStaging[7]  = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[8]  = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[9]  = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[10] = ComponentType.ReadWrite<WorldToLocal_Tag>();
            componentTypeStaging[11] = ComponentType.ReadWrite<WorldRenderBounds>();
            componentTypeStaging[12] = ComponentType.ReadWrite<RenderBounds>();
            componentTypeStaging[13] = ComponentType.ReadWrite<PerInstanceCullingTag>();
            componentTypeStaging[14] = ComponentType.ReadWrite<MaterialMeshInfo>();
            componentTypeStaging[15] = ComponentType.ReadWrite<RenderFilterSettings>();            
            componentTypeStaging[16] = ComponentType.ReadOnly<RotateTowardCameraLTWTag>();       
            componentTypeStaging[17] = ComponentType.ReadWrite<Parent>();       

            return state.EntityManager.CreateArchetype(componentTypeStaging); 
        }
    }

    [BurstCompile, WithAll(typeof(EnemySpawnedTag))]
    public partial struct BossNameJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public EntityArchetype TextArchetype;
        public RenderFilterSettings RenderFilterSettings;
        public TextBaseConfiguration TextBaseConfiguration;
        public TextRenderControl TextRenderControl;
        
        public BlobAssetReference<FontBlob> FontReference;

        [BurstCompile]
        public void Execute([EntityIndexInQuery] int entityIndex, Entity entity, in LocalTransform transform, in EntityNameComponent bossComponent)
        {
            Entity textEntity = ECB.CreateEntity(entityIndex, TextArchetype);
            ECB.SetSharedComponent(entityIndex, textEntity, RenderFilterSettings);
            
            DynamicBuffer<CalliByte> calliByteBuffer = ECB.AddBuffer<CalliByte>(entityIndex, textEntity);
            CalliString calliString = new CalliString(calliByteBuffer);
            calliString.Append(bossComponent.Name);

            ECB.SetComponent(entityIndex, textEntity, TextBaseConfiguration);
            ECB.SetComponent(entityIndex, textEntity, new FontBlobReference { value = FontReference });
            ECB.SetComponent(entityIndex, textEntity, LocalTransform.FromPosition(new float3(0, bossComponent.Offset, 0)));
            ECB.SetComponent(entityIndex, textEntity, TextRenderControl);
            ECB.SetComponent(entityIndex, textEntity, new Parent { Value = entity });
            
            ECB.AppendToBuffer(entityIndex, entity, new LinkedEntityGroup { Value = textEntity });
            
            ECB.SetComponentEnabled(entityIndex, textEntity, ComponentType.ReadWrite<MaterialMeshInfo>(), false);
        }
    }
}