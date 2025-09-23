using System.Collections.Generic;
using Gameplay.Chunk.ECS;
using UnityEngine;

namespace Gameplay.Chunk
{
    [CreateAssetMenu(fileName = "Ground Objects Utility", menuName = "Ground Objects/Utility", order = 0)]
    public class GroundObjectsUtility : ScriptableObject
    {
        public List<GroundObject> GroundObjects = new List<GroundObject>();

        public int GetIndex(GroundObject groundObject)
        {
            return GroundObjects.IndexOf(groundObject);
        }
        
        public GroundObject GetGroundObject(int index)
        {
            return GroundObjects[index];
        }
    }
}