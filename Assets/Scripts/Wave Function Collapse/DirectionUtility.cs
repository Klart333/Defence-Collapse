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
    
    [Flags]
    public enum MultiDirection
    {
        Right = 1 << 0,
        Left = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
        Forward = 1 << 4,
        Backward = 1 << 5,
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
        
        public static MultiDirection Int2ToMultiDirection(int2 dir)
        {
            MultiDirection multiDirection = dir.x switch
            {
                1 => MultiDirection.Right,
                -1 => MultiDirection.Left,
                _ => 0,
            };
            multiDirection |= dir.y switch
            {
                1 => MultiDirection.Forward,
                -1 => MultiDirection.Backward,
                _ => 0,
            };
            
            return multiDirection;
        }

    }
}