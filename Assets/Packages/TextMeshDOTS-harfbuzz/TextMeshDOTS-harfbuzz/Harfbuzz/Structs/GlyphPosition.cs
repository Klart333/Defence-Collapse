using System.Runtime.InteropServices;
using Unity.Entities;

namespace TextMeshDOTS.HarfBuzz
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphPosition : IBufferElementData
    {
        public int xAdvance;
        public int yAdvance;
        public int xOffset;
        public int yOffset;
        private uint var1;

        public override string ToString()
        {
            return $" {xAdvance} {yAdvance} {xOffset} {yOffset} ";
        }
    }
}
