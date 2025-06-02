namespace Gameplay
{
    public static class PersistantGameStats
    {
        public static GameStats CurrentPersistantGameStats { get; private set; }

        public static void CreateNewGameStats(int difficulty)
        {
            CurrentPersistantGameStats = new GameStats
            {
                ChunksExplored = 1,
                TownHallLevel = 1,
                Difficulty = difficulty,
            };
        }

        public static void SaveCurrentGameStats()
        {
            
        }
    }

    public class GameStats
    {
        public int Difficulty;
        public int WaveCount;
        public int TownHallLevel;
        public int ChunksExplored;
    }
}