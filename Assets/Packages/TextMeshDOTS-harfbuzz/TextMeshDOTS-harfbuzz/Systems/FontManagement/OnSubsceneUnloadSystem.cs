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
            foreach (SceneSectionData s in SystemAPI
                .Query<SceneSectionData>()
                .WithAll<SceneEntityReference>()
                //.WithAll<SceneEntityReference, SceneSectionStreamingSystem.StreamingState>()
                .WithNone<RequestSceneLoaded, DisableSceneResolveAndLoad>())
            {
                SceneSection sceneSection = new SceneSection
                {
                    SceneGUID = s.SceneGUID,
                    Section = s.SubSectionIndex,
                };
                Debug.Log($"SubScene Unload");
                foreach (NativeFontPointer nativeFontPointer in SystemAPI.Query<NativeFontPointer>().WithAll<SceneSection>().WithSharedComponentFilter(sceneSection))
                {
                    Debug.Log($"Destroy Harfbuzz font pointer upon Scene Unload");
                    nativeFontPointer.blob.Dispose();
                    nativeFontPointer.face.Dispose();
                    nativeFontPointer.font.Dispose();
                }
            }
        }
    }
}
