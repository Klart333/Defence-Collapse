using Buildings.District.DistrictAttachment;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

namespace Buildings.District.ECS
{
    [BurstCompile, UpdateAfter(typeof(DistrictAttachementMeshSystem))]
    public partial struct AnimateBowSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            
            state.RequireForUpdate<UpdateTargetingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            transformLookup.Update(ref state);

            new AnimateBowJob
            {
                TransformLookup = transformLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct AnimateBowJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public ComponentLookup<LocalTransform> TransformLookup; 
        
        public void Execute(in AnimateBowComponent animateBowComponent, in AttachmentAttackValue attachmentAttackValue)
        {
            float length = animateBowComponent.LengthAtFull * attachmentAttackValue.Value;
            float height = animateBowComponent.StringLength;
            float hypotenuse = math.sqrt(length * length + height * height); 
            float angle = math.acos(height / hypotenuse);
            
            float scale = hypotenuse / height;
            
            RefRW<LocalTransform> arrowTransform = TransformLookup.GetRefRW(animateBowComponent.ArrowEntity);
            arrowTransform.ValueRW.Position = animateBowComponent.ArrowStartPosition - arrowTransform.ValueRW.Forward() * animateBowComponent.LengthAtFull * attachmentAttackValue.Value * (1.0f / 0.3f);
            
            RefRW<LocalTransform> lowerStringTransform = TransformLookup.GetRefRW(animateBowComponent.LowerString);
            lowerStringTransform.ValueRW.Scale = scale;
            lowerStringTransform.ValueRW.Rotation = quaternion.AxisAngle(lowerStringTransform.ValueRW.Right(), -angle);
            
            RefRW<LocalTransform> upperStringTransform = TransformLookup.GetRefRW(animateBowComponent.UpperString);
            upperStringTransform.ValueRW.Scale = scale;
            upperStringTransform.ValueRW.Rotation = quaternion.AxisAngle(upperStringTransform.ValueRW.Right(), angle);
        }
    }
}