using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

namespace Utility
{
    [CreateAssetMenu(fileName = "Altas Analyzer", menuName = "Utility/Atlas Analyzer", order = 0)]
    public class AtlasAnalyzer : SerializedScriptableObject
    {
        [Title("Atlas")]
        [SerializeField]
        private Texture2D atlas;

        [SerializeField]
        private int colorSize = 64;
        
        [Title("Ground Types")]
        [SerializeField]
        private Dictionary<GroundType, Color> groundTypesDictionary = new Dictionary<GroundType, Color>();
        
        public Texture2D Atlas => atlas;

        private List<Color> cachedColors;
        private List<GroundType> cachedGroundTypes;

        public List<Color> GetAtlasColors()
        {
            if (cachedColors is { Count: > 0 }) return cachedColors;
            
            HashSet<Color> colors = new HashSet<Color>();
            
            int width = atlas.width;
            int height = atlas.height;
            for (int x = 0; x < width; x+=colorSize)
            for (int y = 0; y < height; y+=colorSize)
            {
                Color color = atlas.GetPixel(x, y);
                colors.Add(color);
            }
            
            cachedColors = colors.ToList();
            return cachedColors;
        }

        public List<Color> GetAtlasColorsBuildable(out List<GroundType> groundTypes)
        {
            if (cachedColors is { Count: > 0 } && cachedGroundTypes is { Count: > 0 })
            {
                groundTypes = cachedGroundTypes;
                return cachedColors;
            }
            
            HashSet<Color> colorsUnique = new HashSet<Color>();
            
            int width = atlas.width;
            int height = atlas.height;
            for (int x = 0; x < width; x+=colorSize)
            for (int y = 0; y < height; y+=colorSize)
            {
                Color color = atlas.GetPixel(x, y);
                colorsUnique.Add(color);
            }

            List<Color> colors = colorsUnique.ToList();
            var types = new GroundType[colors.Count];
            foreach (KeyValuePair<GroundType, Color> kvp in groundTypesDictionary)
            {
                float minDist = float.MaxValue;
                Color closestColor = default(Color);
                foreach (Color color in colors)
                {
                    float dist = math.sqrt(math.pow(color.r - kvp.Value.r, 2) + math.pow(color.g - kvp.Value.g, 2) + math.pow(color.b - kvp.Value.b, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestColor = color;
                    }
                }

                if (closestColor != default(Color))
                {
                    types[colors.IndexOf(closestColor)] = kvp.Key;
                }
            }
            
            cachedGroundTypes = types.ToList();
            cachedColors = colors.ToList();
            
            groundTypes = cachedGroundTypes.ToList();
            return cachedColors;
        }

        [Button]
        private void ResetCachedColors()
        {
            cachedColors = null;
            cachedGroundTypes = null;
        }
    }
}