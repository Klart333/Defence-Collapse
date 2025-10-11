#if UNITY_EDITOR
using System.IO;
using Sirenix.OdinInspector;

namespace Utility
{
    using UnityEngine;
    public class SaveShaderTexture : MonoBehaviour
    {
        public string textureName = "SavedTexture";
        public int TextureLength =1024;

        public Texture2D GetTexture()
        {
            RenderTexture buffer = new RenderTexture(
                TextureLength, 
                TextureLength, 
                0,                            // No depth/stencil buffer
                RenderTextureFormat.ARGB32,   // Standard colour format
                RenderTextureReadWrite.sRGB // No sRGB conversions
            );

            var texture = new Texture2D(TextureLength,TextureLength,TextureFormat.ARGB32,true);

            MeshRenderer render = GetComponent<MeshRenderer>();
            //texture = render.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Material material = render.sharedMaterial;

            Graphics.Blit(null, buffer, material);
            RenderTexture.active = buffer;           // If not using a scene camera

            texture.ReadPixels(
                new Rect(0, 0, TextureLength, TextureLength), // Capture the whole texture
                0, 0,                          // Write starting at the top-left texel
                false);                          // No mipmaps
        
            return texture;
        }
        
        [Button]
    public void Save()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null || mr.sharedMaterial == null)
        {
            Debug.LogError("No MeshRenderer or material found.");
            return;
        }

        Texture2D src = GetTexture();

        // Ensure it's readable (in case it's imported as non-readable)
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(src);
        var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            UnityEditor.AssetDatabase.ImportAsset(path);
        }
#endif

        // Copy pixels
        Texture2D output = new Texture2D(TextureLength, TextureLength, TextureFormat.ARGB32, false);

        int half = TextureLength / 2;

        // Get all pixels once for speed
        Color[] pixels = src.GetPixels(0, 0, TextureLength, TextureLength);
        Color[] newPixels = new Color[pixels.Length];

        // Copy each quadrant swapped
        // Helper lambda to compute 1D index
        int idx(int x, int y) => y * TextureLength + x;

        for (int y = 0; y < TextureLength; y++)
        {
            for (int x = 0; x < TextureLength; x++)
            {
                int srcX = x;
                int srcY = y;

                // Swap quadrants
                if (x < half && y < half) // bottom-left → top-right
                {
                    srcX = x + half;
                    srcY = y + half;
                }
                else if (x >= half && y < half) // bottom-right → top-left
                {
                    srcX = x - half;
                    srcY = y + half;
                }
                else if (x < half && y >= half) // top-left → bottom-right
                {
                    srcX = x + half;
                    srcY = y - half;
                }
                else if (x >= half && y >= half) // top-right → bottom-left
                {
                    srcX = x - half;
                    srcY = y - half;
                }

                newPixels[idx(x, y)] = pixels[idx(srcX, srcY)];
            }
        }

        output.SetPixels(newPixels);
        output.Apply();

        string folder = Path.Combine(Application.dataPath, "Art/Textures");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        File.WriteAllBytes(Path.Combine(folder, textureName + ".png"), output.EncodeToPNG());
        Debug.Log($"Saved swapped texture as {textureName}.png");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
        // Update is called once per frame
        void Update()
        {
            // Save();
            if(Input.GetKeyDown(KeyCode.Space))
            {
                Save();
            }
        }
    }

}
#endif