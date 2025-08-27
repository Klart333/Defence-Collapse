using System;
using System.Collections.Generic;

namespace Utility
{
    public static class ListRotator
    {
        /// <summary>
        /// Rotates a 4 element array according to rotation
        /// </summary>
        /// <param name="values"></param>
        /// <param name="rotation"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public static void RotateInPlace<T>(T[] values, int rotation)
        {
            if (values.Length != 4)
                throw new System.ArgumentException("List must have exactly 4 elements");

            rotation %= 4; // normalize to 0–3
            if (rotation == 0) return;

            // Save a copy of the values before overwriting
            T right = values[0];
            T up = values[1];
            T left = values[2];
            T down = values[3];

            switch (rotation)
            {
                case 1:
                    values[0] = down;
                    values[1] = right;
                    values[2] = up;
                    values[3] = left;
                    break;

                case 2: 
                    values[0] = left;
                    values[1] = down;
                    values[2] = right;
                    values[3] = up;
                    break;

                case 3: 
                    values[0] = up;
                    values[1] = left;
                    values[2] = down;
                    values[3] = right;
                    break;
            }
        }
    }

}