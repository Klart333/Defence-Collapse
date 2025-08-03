using System;
using System.IO;
using System.Linq;
using TextmeshDOTS;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.HarfBuzz.Bitmap;
using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using static TextMeshDOTS.TextCoreExtensions;
using Font = TextMeshDOTS.HarfBuzz.Font;


namespace TextMeshDOTS.TextProcessing
{
    //[DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct NativeFontLoaderSystem : ISystem
    {
        EntityQuery textRendererQ, changedTextRendererQ, fontEntitiesQ, fontstateQ;
        NativeList<LoadRequest> newLoadRequests;
        EntityArchetype nativeFontDataArchetype, fontStateArchetype;
        DrawDelegates drawFunctions;
        PaintDelegates paintFunctions;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            newLoadRequests = new NativeList<LoadRequest>(16, Allocator.Persistent);
            nativeFontDataArchetype = TextMeshDOTSArchetypes.GetNativeFontDataArchetype(ref state);

            fontStateArchetype = TextMeshDOTSArchetypes.GetFontStateArchetype(ref state);
            state.EntityManager.CreateEntity(fontStateArchetype);

            fontstateQ = SystemAPI.QueryBuilder()
                .WithAll<FontState>()
                .Build();

            textRendererQ = SystemAPI.QueryBuilder()
                .WithAll<FontBlobReference>()
                .WithAll<TextRenderControl>()
                .WithPresent<MaterialMeshInfo>()
                .Build();

            changedTextRendererQ = SystemAPI.QueryBuilder()
                .WithAll<FontBlobReference>()
                .WithAll<TextRenderControl>()
                .Build();
            changedTextRendererQ.SetChangedVersionFilter(ComponentType.ReadWrite<FontBlobReference>());

            fontEntitiesQ = SystemAPI.QueryBuilder()
                .WithAll<FontAssetRef>()
                .WithAll<FontAssetMetadata>()
                .WithAll<AtlasData>()
                .Build();

            drawFunctions = new DrawDelegates(true);
            paintFunctions =  new PaintDelegates(true);
            state.RequireForUpdate(fontstateQ);
            state.RequireForUpdate(changedTextRendererQ);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (changedTextRendererQ.IsEmpty)
                return;

            //Debug.Log($"{changedTextRendererQ.CalculateEntityCount()} TextRender have changed, trigger font loading, or link to existing fonts");
            NativeArray<FontAssetRef> allFontAssetRefs = fontEntitiesQ.ToComponentDataArray<FontAssetRef>(state.WorldUpdateAllocator);
            NativeArray<FontBlobReference> changedFontBlobReferences = changedTextRendererQ.ToComponentDataArray<FontBlobReference>(state.WorldUpdateAllocator);

            for (int i = 0, ii = changedFontBlobReferences.Length; i < ii; i++)
            {
                FontBlobReference changedFontBlobReference = changedFontBlobReferences[i];
                ref FontBlob fontBlob = ref changedFontBlobReference.value.Value;
                if (!allFontAssetRefs.Contains(fontBlob.fontAssetRef))
                {
                    LoadRequest loadRequest = new LoadRequest { fontBlobRef = changedFontBlobReference, fontAssetRef = fontBlob.fontAssetRef };
                    if (!fontBlob.useSystemFont)
                    {                        
                        string fontPath = Path.Combine(Application.streamingAssetsPath, fontBlob.fontAssetPath.ToString());
                        //Debug.Log($"Try to load {fontBlob.fontAssetPath.ToString()}, from path {Application.streamingAssetsPath} full path {fontPath}");
                        if (!File.Exists(fontPath))
                            Debug.Log($"Could not find font in {fontPath}");
                        else
                        {
                            loadRequest.filePath = fontPath;
                            if (!newLoadRequests.Contains(loadRequest))
                                newLoadRequests.Add(loadRequest);
                        }
                    }
                    else
                    {                        
                        //loading rules: https://www.high-logic.com/fontcreator/manual15/fonttype.html
                        bool typeographicFamilyDataMissing = (fontBlob.typographicFamily.IsEmpty || fontBlob.typographicSubfamily.IsEmpty);
                        FixedString128Bytes family = typeographicFamilyDataMissing ? fontBlob.fontFamily : fontBlob.typographicFamily;
                        FixedString128Bytes subFamily = typeographicFamilyDataMissing ? fontBlob.fontSubFamily : fontBlob.typographicSubfamily;
                        if (!TryGetSystemFontReference(family.ToString(), subFamily.ToString(), out UnityFontReference unityFontReference))
                            Debug.Log($"Could not find system font {family} {subFamily}");
                        else
                        {
                            loadRequest.filePath = unityFontReference.filePath;
                            if (!newLoadRequests.Contains(loadRequest))
                                newLoadRequests.Add(loadRequest);
                        }
                        //Debug.Log($"Try to load system font {family} {subFamily}, from path {loadRequest.filePath}");
                    }
                }
            }
            NativeArray<FontBlobReference> allrequieredFonts = textRendererQ.ToComponentDataArray<FontBlobReference>(state.WorldUpdateAllocator);
            NativeArray<Entity> allrequieredFontEntities = textRendererQ.ToEntityArray(state.WorldUpdateAllocator);

            //validate if all existing fonts are still requiered
            NativeHashMap<FontAssetRef, Entity> allRequieredFontsNR = new NativeHashMap<FontAssetRef, Entity>(256, state.WorldUpdateAllocator);
            for (int i = 0, ii = allrequieredFonts.Length; i < ii; i++)
            {
                FontAssetRef fontAssetRef = allrequieredFonts[i].value.Value.fontAssetRef;
                if (!allRequieredFontsNR.ContainsKey(fontAssetRef))
                    allRequieredFontsNR.Add(fontAssetRef, allrequieredFontEntities[i]);
            }
            for (int i = 0, ii = allFontAssetRefs.Length; i < ii; i++)
            {
                FontAssetRef existingFont = allFontAssetRefs[i];
                if (!allRequieredFontsNR.TryGetValue(existingFont, out Entity item))
                {
                    Debug.Log("Destroy not needed font");
                    state.EntityManager.DestroyEntity(item); //can destroy existing font as it is not needed anymore by any of the active TextRenderer
                }
            }

            //even if no new fonts are found, the backer will reset all MaterialMeshInfo (disable it, values are zero
            //-->run EnableAndValidateMaterialMeshInfoJob (same job that run after registering new materials)
            if (newLoadRequests.Length == 0) 
            {                
                //validate MaterialMeshInfo (TextRender connected to correct FontAssets?)
                NativeArray<Entity> allFontEntities = fontEntitiesQ.ToEntityArray(state.WorldUpdateAllocator);
                NativeHashMap<FontAssetRef, Entity> fontEntityLookup = new NativeHashMap<FontAssetRef, Entity>(allFontAssetRefs.Length, state.WorldUpdateAllocator);
                ComponentLookup<DynamicFontAsset> dynamicFontAssetLookup = SystemAPI.GetComponentLookup<DynamicFontAsset>(false);
                ComponentLookup<FontAssetRef> fontAssetRefLookup = SystemAPI.GetComponentLookup<FontAssetRef>(false);
                for (int i = 0, ii = allFontEntities.Length; i < ii; i++)
                {
                    Entity entity = allFontEntities[i];
                    FontAssetRef fontAssetRef = fontAssetRefLookup[entity];
                    fontEntityLookup.Add(fontAssetRef, entity);
                }

                EnableAndValidateMaterialMeshInfoJob validateMaterialMeshInfoJob = new EnableAndValidateMaterialMeshInfoJob
                {
                    fontEntityLookup = fontEntityLookup,
                    dynamicFontAssetLookup = dynamicFontAssetLookup,
                };
                state.Dependency = validateMaterialMeshInfoJob.ScheduleParallel(textRendererQ, state.Dependency);

                return;
            }

            Entity fontStateEntity = fontstateQ.GetSingletonEntity();
            if (!SystemAPI.HasComponent<FontsDirtyTag>(fontStateEntity))
                state.EntityManager.AddComponent<FontsDirtyTag>(fontStateEntity);

            //load new fonts
            for (int i = 0, ii = newLoadRequests.Length; i < ii; i++)
                LoadFont(newLoadRequests[i], ref state);

            newLoadRequests.Clear();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (newLoadRequests.IsCreated) newLoadRequests.Dispose();

            drawFunctions.Dispose();
            paintFunctions.Dispose();
        }
        void LoadFont(LoadRequest loadRequest, ref SystemState state)
        {
            ref FontBlob fontBlobRef = ref loadRequest.fontBlobRef.value.Value;
            Blob blob = new Blob(loadRequest.filePath.ToString());
            Face face = new Face(blob.ptr, 0);
            Font font = new Font(face.ptr);

            SDFOrientation sdfOrientation = face.HasTrueTypeOutlines() ? SDFOrientation.TRUETYPE : SDFOrientation.POSTSCRIPT;            

            NativeFontPointer nativeFontPointer = new NativeFontPointer { 
                orientation = sdfOrientation, 
                blob = blob, 
                face = face, 
                font = font, 
                drawFunctions = drawFunctions,
                paintFunctions = paintFunctions,
            };

            FontAssetMetadata fontAssetMetadata = new FontAssetMetadata { family = fontBlobRef.fontFamily, subfamily = fontBlobRef.fontSubFamily };

            //initialize texture. To save space, review how to initialize it with size 0
            //(as done by TextCore), and only increase once needed
            DynamicFontAsset dynamicFontAsset;
            AtlasData atlasData;
            if (face.HasCOLR() || face.HasColorBitmap())
            {
                atlasData = new AtlasData
                {
                    atlasHeight = 2048,
                    atlasWidth = 2048,
                    padding = 8,                //10% of atlas height or width
                    samplingPointSize = fontBlobRef.samplingPointSizeBitmap,    //size of font (in pixel) in atlas
                };
                Texture2D texture2D = new Texture2D(atlasData.atlasWidth, atlasData.atlasHeight, TextureFormat.ARGB32,false);
                NativeArray<ColorARGB> textureData = texture2D.GetRawTextureData<ColorARGB>();
                Blending.SetTransparent(textureData);
                dynamicFontAsset = new DynamicFontAsset { texture = texture2D, textureType = TextureType.ARGB };
            }
            else
            {
                atlasData = new AtlasData
                {
                    atlasHeight = 2048,
                    atlasWidth = 2048,
                    padding = fontBlobRef.samplingPointSizeSDF / 6,  //samplingPointSizeSDF is clamped to 64..96, so padding will be clamped to 10..16
                    samplingPointSize = fontBlobRef.samplingPointSizeSDF,  //size of font (in pixel) in atlas
                };
                Texture2D texture2D = new Texture2D(atlasData.atlasWidth, atlasData.atlasHeight, TextureFormat.Alpha8, false);
                NativeArray<byte> rawTextureData = texture2D.GetRawTextureData<byte>();

                //initialize to black
                for (int i = 0; i < rawTextureData.Length; i++)
                    rawTextureData[i] = 0;
                texture2D.Apply();
                dynamicFontAsset = new DynamicFontAsset { texture = texture2D, textureType = TextureType.SDF};
            }
            font.SetScale(atlasData.samplingPointSize, atlasData.samplingPointSize);

            Entity fontEntity = state.EntityManager.CreateEntity(nativeFontDataArchetype);
            state.EntityManager.SetComponentData(fontEntity, fontBlobRef.fontAssetRef);
            state.EntityManager.SetComponentData(fontEntity, fontAssetMetadata);            
            state.EntityManager.SetComponentData(fontEntity, atlasData);
            state.EntityManager.AddComponentData(fontEntity, dynamicFontAsset);
            state.EntityManager.AddComponentData(fontEntity, nativeFontPointer);

            DynamicBuffer<FreeGlyphRects> freeGlyphRects = state.EntityManager.GetBuffer<FreeGlyphRects>(fontEntity);
            NativeAtlas.InitializeFreeGlyphRects(ref freeGlyphRects, atlasData.atlasWidth, atlasData.atlasHeight);        
        }

        public struct LoadRequest : IEquatable<LoadRequest>
        {
            public FontBlobReference fontBlobRef;
            public FixedString512Bytes filePath;
            public FontAssetRef fontAssetRef;
            public override bool Equals(object obj) => obj is FontAssetRef other && Equals(other);

            public bool Equals(LoadRequest other)
            {
                return GetHashCode() == other.GetHashCode();
            }
            public static bool operator ==(LoadRequest e1, LoadRequest e2)
            {
                return e1.GetHashCode() == e2.GetHashCode();
            }
            public static bool operator !=(LoadRequest e1, LoadRequest e2)
            {
                return e1.GetHashCode() != e2.GetHashCode();
            }
            public override int GetHashCode()
            {                
                return fontAssetRef.GetHashCode(); 
            }
        }
    }
}