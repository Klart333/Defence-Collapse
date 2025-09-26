using Random = Unity.Mathematics.Random;
using Unity.Entities.Graphics;
using TextMeshDOTS.Rendering;
using UnityEngine.Rendering;
using TextMeshDOTS.HarfBuzz;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using InputCamera.ECS;
using Unity.Entities;
using UnityEngine;
using Effects.ECS;
using Enemy.ECS;
using Unity.Burst;
using Juice.Ecs;
using Health;

namespace TextMeshDOTS.Authoring
{
    [BurstCompile, UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct DamageNumberSystem : ISystem
    {
        private BlobAssetReference<FontBlob> fontReference;
        private EntityArchetype textRenderArchetype;
        private TextBaseConfiguration textBaseConfiguration;
        private RenderFilterSettings renderFilterSettings;
        private TextRenderControl textRenderControl;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            textRenderArchetype = GetDamageTextArchetype(ref state);

            FontRequest fontRequest = GetFontRequest();
            fontReference = FontBlobber.GetRuntimeFontBlob(fontRequest);
            
            textBaseConfiguration = new TextBaseConfiguration
            {
                fontSize = 5f,
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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new SpawnNumbersJob
            {
                ECB = ecb.AsParallelWriter(),
                FontReference = fontReference, 
                TextBaseConfiguration = textBaseConfiguration,
                RenderFilterSettings = renderFilterSettings,
                TextRenderControl = textRenderControl,
                TextArchetype = textRenderArchetype,
                SpawnOffset = new float3(0, 0.5f, 0),
                BaseSeed = UnityEngine.Random.Range(0, 100000),
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (fontReference.IsCreated) fontReference.Dispose();
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
        
        private EntityArchetype GetDamageTextArchetype(ref SystemState state)
        {
            NativeArray<ComponentType> componentTypeStaging = new NativeArray<ComponentType>(20, Allocator.Temp);
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
            componentTypeStaging[16] = ComponentType.ReadWrite<LifetimeComponent>();            
            componentTypeStaging[17] = ComponentType.ReadWrite<RotateTowardCameraTag>();            
            componentTypeStaging[18] = ComponentType.ReadWrite<FloatAwayComponent>();            
            componentTypeStaging[19] = ComponentType.ReadWrite<RandomComponent>();            

            EntityArchetype archetype = state.EntityManager.CreateArchetype(componentTypeStaging);
            componentTypeStaging.Dispose();
            return archetype;
        }
    }
    
    [BurstCompile]
    public partial struct SpawnNumbersJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public EntityArchetype TextArchetype;
        public RenderFilterSettings RenderFilterSettings;
        public TextBaseConfiguration TextBaseConfiguration;
        public TextRenderControl TextRenderControl;
        
        public BlobAssetReference<FontBlob> FontReference;
        public float3 SpawnOffset;
        public int BaseSeed;

        [BurstCompile]
        public void Execute([EntityIndexInQuery] int entityIndex, Entity entity, in LocalTransform transform, in DamageTakenComponent damageTaken)
        {
            TextBaseConfiguration.color = damageTaken.DamageTakenType switch
            {
                HealthType.Health => Color.green,
                HealthType.Armor => Color.yellow,
                HealthType.Shield => Color.blue,
                _ => Color.black,
            };

            ECB.RemoveComponent<DamageTakenComponent>(entityIndex, entity);

            Entity textEntity = ECB.CreateEntity(entityIndex, TextArchetype);
            ECB.SetSharedComponent(entityIndex, textEntity, RenderFilterSettings);
            
            DynamicBuffer<CalliByte> calliByteBuffer = ECB.AddBuffer<CalliByte>(entityIndex, textEntity);
            CalliString calliString = new CalliString(calliByteBuffer);
            float damageText = math.round(damageTaken.DamageTaken);
            calliString.Append(damageText);
            if (damageTaken.IsCrit)
            {
                calliString.Append('!');
            }

            ECB.SetComponent(entityIndex, textEntity, TextBaseConfiguration);
            ECB.SetComponent(entityIndex, textEntity, new FontBlobReference { value = FontReference });
            ECB.SetComponent(entityIndex, textEntity, LocalTransform.FromPosition(transform.Position + SpawnOffset));
            ECB.SetComponent(entityIndex, textEntity, TextRenderControl);
            
            RandomComponent random = new RandomComponent { Random = Random.CreateFromIndex((uint)(BaseSeed + entityIndex)) };
            ECB.SetComponent(entityIndex, textEntity, random);
            float lifeTime = 0.7f + random.Random.NextFloat(0.2f);
            ECB.SetComponent(entityIndex, textEntity, new LifetimeComponent { Lifetime = lifeTime });
            ECB.SetComponent(entityIndex, textEntity, new FloatAwayComponent { Duration = lifeTime, Speed = 0.5f, Direction = random.Random.NextFloat3Direction() });

            ECB.SetComponentEnabled(entityIndex, textEntity, ComponentType.ReadWrite<MaterialMeshInfo>(), false);
        }
    }
}