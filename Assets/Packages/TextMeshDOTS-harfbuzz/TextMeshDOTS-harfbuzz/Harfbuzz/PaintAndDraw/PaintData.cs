﻿using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TextMeshDOTS.HarfBuzz
{
    /// <summary>
    /// textureData and clipRect (informing about texture width and height) are the ultimate output of the paint API
    /// use this output to blit it into target texture (atlas) at desired location
    /// </summary>     
    public struct PaintData
    {
        public DrawDelegates drawDelegates;
        public DrawData clipGlyph;
        public float2x3 inverseGlyphTransform;
        public uint glyphID;
        internal FixedStack512Bytes<float2x3> transformStack; //could also use Unity AffineTransform (but this would require use of float3 vs float2)
        public uint color;  
        public BBox clipRect;

        //according to https://github.com/harfbuzz/harfbuzz/issues/3931, there should only be two intermediate surfaces requiered
        //to build COMPOSITE glyphs (foreground, background)...but emperically we find sometimes need for three (e.g. in 😱). Not clear why
        public NativeArray<ColorARGB> paintSurface; // this is the target of all rasterizations and blending.
        public NativeArray<ColorARGB> tempSurface1; // temp storage to cache a backup of current paint surface upon "push group"
        public NativeArray<ColorARGB> tempSurface2; // temp storage to cache a backup of current paint surface upon "push group"
        public NativeArray<ColorARGB> tempSurface3; // temp storage to cache a backup of current paint surface upon "push group"
        public int group;                           // current paint group
        public NativeArray<byte> imageData;
        public PaintImageFormat imageFormat;
        public int imageWidth;
        public int imageHeight;

        public PaintData(DrawDelegates drawDelegates, int edgeCapacity, int contourCapacity, float maxDeviation, Allocator allocator)
        {
            this.drawDelegates = drawDelegates;
            clipGlyph = new DrawData(edgeCapacity, contourCapacity, maxDeviation, allocator);
            glyphID = default;
            inverseGlyphTransform = default;
            transformStack = new();
            transformStack.Add(PaintUtils.AffineTransformIdentity);
            color = default;
            clipRect = BBox.Empty;
            
            paintSurface = default; 
            tempSurface1 = default; 
            tempSurface2 = default; 
            tempSurface3 = default;
            group = 0;

            imageData = default;
            imageFormat = default;
            imageWidth = -1;
            imageHeight = -1; 
        }
        public void Clear()
        {
            clipGlyph.Clear();
            glyphID = default;
            transformStack.Clear();
            transformStack.Add(PaintUtils.AffineTransformIdentity);
            color = default;
            clipRect = BBox.Empty;

            if (paintSurface.IsCreated) Blending.Clear(paintSurface);
            if (tempSurface1.IsCreated) Blending.Clear(tempSurface1);
            if (tempSurface2.IsCreated) Blending.Clear(tempSurface2);
            if (tempSurface3.IsCreated) Blending.Clear(tempSurface3);

            imageData = default;
            imageFormat = default;
            imageWidth = -1;
            imageHeight = -1;            
        }
    }    
}