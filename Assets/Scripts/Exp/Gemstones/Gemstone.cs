namespace Exp.Gemstones
{
    public class Gemstone
    {
        public int Level { get; set; }
        public GemstoneType GemstoneType { get; set; }
        
        public IGemstoneEffect[] Effects { get; set; }
    }

    public enum GemstoneType
    {
        Ruby, 
        Sapphire,
        Emerald
    } 
}