using System;
using Unity.Mathematics;

namespace WaveFunctionCollapse
{
    public enum Direction
    {
        Right,
        Left,
        Up,
        Down,
        Forward,
        Backward
    }

    public static class DirectionUtility
    {
        public static Direction Int2ToDirection(int2 dir)
        {
            return dir switch
            {
                {x: 1, y: 0} => Direction.Right,
                {x: -1, y: 0} => Direction.Left,
                {x: 0, y: 1} => Direction.Forward,
                {x: 0, y: -1} => Direction.Backward,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
            };
        }
    }
}