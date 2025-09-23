using Unity.Burst;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace TextMeshDOTS.TextProcessing
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(SceneSystemGroup))]
    public partial struct OnSubSceneUnloadSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (RefRO<SceneSectionData> s in SystemAPI
                .Query<RefRO<SceneSectionData>>()
                .WithAll<SceneEntityReference>()
                //.WithAll<SceneEntityReference, SceneSectionStreamingSystem.StreamingState>()
                .WithNone<RequestSceneLoaded, DisableSceneResolveAndLoad>())
            {
                SceneSection sceneSection = new SceneSection
                {
                    SceneGUID = s.ValueRO.SceneGUID,
                    Section = s.ValueRO.SubSectionIndex,
                };
                Debug.Log($"SubScene Unload");
                foreach (RefRO<NativeFontPointer> nativeFontPointer in SystemAPI.Query<RefRO<NativeFontPointer>>().WithAll<SceneSection>().WithSharedComponentFilter(sceneSection))
                {
                    Debug.Log($"Destroy Harfbuzz font pointer upon Scene Unload");
                    nativeFontPointer.ValueRO.blob.Dispose();
                    nativeFontPointer.ValueRO.face.Dispose();
                    nativeFontPointer.ValueRO.font.Dispose();
                }
            }
        }
    }
}
