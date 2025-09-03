using System;
using Unity.Mathematics;

namespace WaveFunctionCollapse
{
    public enum Direction
    {
        Right = 0,
        Left = 1,
        Up = 2,
        Down = 3,
        Forward = 4,
        Backward = 5,
    }

    public static class DirectionUtility
    {
        public static readonly int2[] BuildableCorners =
        {
            new int2(-1, 1),
            new int2(1, 1),
            new int2(1, -1),
            new int2(-1, -1),
        };
        
        public static Direction Int2ToDirection(int2 dir)
        {
            return dir switch
            {
                {x: 1, y: 0} => Direction.Right,
                {x: -1, y: 0} => Direction.Left,
                {x: 0, y: 1} => Direction.Forward,
                {x: 0, y: -1} => Direction.Backward,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
            };
        }
        
        public static int2 DirectionToInt2(Direction dir)
        {
            return dir switch
            {
                Direction.Right => new int2(1, 0),
                Direction.Left => new int2(-1, 0),
                Direction.Forward => new int2(0, 1),
                Direction.Backward => new int2(0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
            };
        }
    }
}