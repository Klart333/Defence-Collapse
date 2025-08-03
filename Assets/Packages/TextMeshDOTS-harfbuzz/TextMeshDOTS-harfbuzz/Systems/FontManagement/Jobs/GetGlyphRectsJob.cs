using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TextCore;
using TextMeshDOTS.HarfBuzz.Bitmap;
using UnityEngine;
using Font = TextMeshDOTS.HarfBuzz.Font;


namespace TextMeshDOTS.TextProcessing
{
    [BurstCompile]
    struct GetGlyphRectsJob : IJob
    {
        public NativeList<GlyphBlob> placedGlyphs;

        public Entity fontEntity;

        [ReadOnly] public ComponentLookup<AtlasData> atlasDataLookup;
        [ReadOnly] public ComponentLookup<NativeFontPointer> nativeFontPointerLookup;
        [ReadOnly] public ComponentLookup<FontAssetMetadata> fontAssetMetadataLookup;

        public BufferLookup<MissingGlyphs> missingGlyphsBuffer;
        public BufferLookup<UsedGlyphs> usedGlyphsBuffer;
        public BufferLookup<UsedGlyphRects> usedGlyphRectsBuffer;
        public BufferLookup<FreeGlyphRects> freeGlyphRectsBuffer;

        public void Execute()
        {
            AtlasData atlasData = atlasDataLookup[fontEntity];
            NativeFontPointer nativeFontPointer = nativeFontPointerLookup[fontEntity];
            DynamicBuffer<uint> missingGlyphs = missingGlyphsBuffer[fontEntity].Reinterpret<uint>();
            DynamicBuffer<uint> usedGlyphs = usedGlyphsBuffer[fontEntity].Reinterpret<uint>();
            DynamicBuffer<GlyphRect> usedGlyphRects = usedGlyphRectsBuffer[fontEntity].Reinterpret<GlyphRect>();
            DynamicBuffer<GlyphRect> freeGlyphRects = freeGlyphRectsBuffer[fontEntity].Reinterpret<GlyphRect>();

            Font font = nativeFontPointer.font;
            NativeList<GlyphBlob> glyphsToPlace = new NativeList<GlyphBlob>(256, Allocator.Temp);

            for (int i = 0, ii = missingGlyphs.Length; i < ii; i++)
            {
                uint glyphID = missingGlyphs[i];
                //GetGlyphExtends is a very costly function. For a COLR glyph, rect is determined by parsing all vertices of maybe 20 sub-glyphs
                //calling it in parallel makes things worse (mutex lock?)
                font.GetGlyphExtents(glyphID, out GlyphExtents extends);                
                GlyphBlob hbGlyph = new GlyphBlob { glyphID = glyphID, glyphExtents = extends};
                glyphsToPlace.Add(hbGlyph);
            };
            bool success = NativeAtlas.AddGlyphs(atlasData.padding, glyphsToPlace, placedGlyphs, usedGlyphs, usedGlyphRects, freeGlyphRects);
            if (!success)
            {
                FontAssetMetadata fontAssetMetaData = fontAssetMetadataLookup[fontEntity];
                Debug.Log($"{glyphsToPlace.Length} glyphs could not be placed for font {fontAssetMetaData.family} {fontAssetMetaData.subfamily} ");
                //for (int i = 0, ii = usedGlyphs.Length; i < ii; i++)
                //{
                //    var glyphExtents= glyphsToPlace[i].glyphExtents;
                //    Debug.Log($"Glyph Rect: {glyphExtents.width} {glyphExtents.height} padding: {atlasData.padding}");
                //}
                //for (int i = 0, ii = freeGlyphRects.Length; i < ii; i++)
                //{
                //    var freeGlyphRect = freeGlyphRects[i];
                //    Debug.Log($"Free Rect: {freeGlyphRect.width} {freeGlyphRect.height}");
                //}
                //missingGlyphs.Clear();
                //for (int i = 0, ii = usedGlyphs.Length; i < ii; i++)
                //    missingGlyphs.Add(glyphsToPlace[i].glyphID);
                //return;
            }
            missingGlyphs.Clear();
        }
    }    
}
