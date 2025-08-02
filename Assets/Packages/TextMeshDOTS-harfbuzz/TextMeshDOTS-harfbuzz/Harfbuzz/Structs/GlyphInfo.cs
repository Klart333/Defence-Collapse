using System.Runtime.InteropServices;
using Unity.Entities;

namespace TextMeshDOTS.HarfBuzz
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphInfo : IBufferElementData
    {
        public uint codepoint;
        private uint mask;
        public uint cluster;
        private uint var1;
        private uint var2;
    }
}
