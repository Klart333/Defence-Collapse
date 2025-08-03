using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS.TextProcessing
{

    [BurstCompile]
    partial class FontCleanupSystem : SystemBase
    {
        EntitiesGraphicsSystem hybridRenderer;
        protected override void OnCreate()
        {
            hybridRenderer = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        }
        protected override void OnUpdate()
        {
            BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(CheckedStateRef.WorldUnmanaged);

            foreach ((RefRW<NativeFontPointer> nativeFontPointer, Entity entity) in SystemAPI.Query<RefRW<NativeFontPointer>>()
                .WithNone<UsedGlyphs>()
                .WithNone<MissingGlyphs>()          
                .WithEntityAccess())
            {                
                //Debug.Log($"Destroy Harfbuzz font pointer");
                nativeFontPointer.ValueRW.blob.Dispose();
                nativeFontPointer.ValueRW.face.Dispose();
                nativeFontPointer.ValueRW.font.Dispose();
                ecb.RemoveComponent<NativeFontPointer>(entity);
            }

            foreach ((RefRW<DynamicFontAsset> dynamicFontAsset, Entity entity) in SystemAPI.Query<RefRW<DynamicFontAsset>>()
                .WithNone<UsedGlyphs>()
                .WithNone<MissingGlyphs>()         
                .WithEntityAccess())
            {
                //Debug.Log($"Destroy font material");
                if (dynamicFontAsset.ValueRO.blob.IsCreated) dynamicFontAsset.ValueRW.blob.Dispose();
                Material fontMaterial = hybridRenderer.GetMaterial(dynamicFontAsset.ValueRO.fontMaterialID);
                hybridRenderer.UnregisterMaterial(dynamicFontAsset.ValueRO.fontMaterialID);
                if (fontMaterial != null) Object.Destroy(fontMaterial); // Could get disposed by Unity, according to AI
                Object.Destroy(dynamicFontAsset.ValueRO.texture.Value);
                ecb.RemoveComponent<DynamicFontAsset>(entity);
            }
        }
        protected override void OnDestroy()
        {
            foreach (RefRW<NativeFontPointer> nativeFontPointer in SystemAPI.Query<RefRW<NativeFontPointer>>())
            {
                //Debug.Log($"Destroy Harfbuzz font pointer");
                nativeFontPointer.ValueRW.blob.Dispose();
                nativeFontPointer.ValueRW.face.Dispose();
                nativeFontPointer.ValueRW.font.Dispose();
            }
            
            foreach (RefRW<DynamicFontAsset> dynamicFontAsset in SystemAPI.Query<RefRW<DynamicFontAsset>>())
            {
                if (dynamicFontAsset.ValueRW.blob.IsCreated)
                {
                    dynamicFontAsset.ValueRW.blob.Dispose();
                }
            }
            
            //foreach (var (dynamicFontAsset, entity) in SystemAPI.Query<DynamicFontAsset>()
            //    .WithAll<DynamicFontAsset>()
            //    .WithEntityAccess())
            //{
            //    Debug.Log($"Destroy font material");
            //    if (dynamicFontAsset.blob.IsCreated) dynamicFontAsset.blob.Dispose();
            //    var fontMaterial = hybridRenderer.GetMaterial(dynamicFontAsset.fontMaterialID); //throws-->probably batch rendergroup already destroyed, so no need to destroy resources?
            //    hybridRenderer.UnregisterMaterial(dynamicFontAsset.fontMaterialID);
            //    UnityEngine.Object.Destroy(fontMaterial);
            //    UnityEngine.Object.Destroy(dynamicFontAsset.texture);
            //}
        }
    }
}
