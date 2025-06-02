namespace Saving
{
    public static class SaveLoad
    {
        
        
    }

    public struct ExpSaveData
    {
        public int Exp;
        public GemStoneSaveData[] GemStones;
    }

    public struct GemStoneSaveData
    {
        public int Seed;
        public int Exp;
    }
}