using Exp;
using UnityEngine;

namespace Gameplay.GameOver
{
    public interface IGetFactor
    {
        public float GetFactor(AnimationCurve curve, out float level);
        public string GetDisplayText(float level);
    }
    
    public class WaveFactor : IGetFactor
    {
        public float GetFactor(AnimationCurve curve, out float level)
        {
            level = PersistantGameStats.CurrentPersistantGameStats.TurnCount;
            return curve.Evaluate(level);
        }

        public string GetDisplayText(float level) => level.ToString("N0");
    }
    
    public class TownHallFactor : IGetFactor
    {
        public float GetFactor(AnimationCurve curve, out float level)
        {
            level = PersistantGameStats.CurrentPersistantGameStats.TownHallLevel;
            return curve.Evaluate(level);
        }
        
        public string GetDisplayText(float level) => level.ToString("N0");
    }
    
    public class ChunkFactor : IGetFactor
    {
        public float GetFactor(AnimationCurve curve, out float level)
        {
            level = PersistantGameStats.CurrentPersistantGameStats.ChunksExplored;
            return curve.Evaluate(level);
        }
        
        public string GetDisplayText(float level) => level.ToString("N0");
    }
    
    public class DifficultyFactor : IGetFactor
    {
        public float GetFactor(AnimationCurve curve, out float level)
        {
            level = PersistantGameStats.CurrentPersistantGameStats.Difficulty;
            return curve.Evaluate(level);
        }

        public string GetDisplayText(float level)
        {
            int diff = (int)level;
            return diff switch
            {
                0 => "Easy",
                1 => "Normal",
                2 => "Hard",
                3 => "Expert",
                4 => "Master",
                5 => "Very master",
                6 => "Very very master",
                _ => diff.ToString()
            };
        }

    }

    public class ExpMultiplierFactor : IGetFactor
    {
        public float GetFactor(AnimationCurve curve, out float level)
        {
            level = ExpManager.Instance.ExpMultiplier.Value;
            return curve.Evaluate(level);
        }
        
        public string GetDisplayText(float level) => "";
    }
}