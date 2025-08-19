using UnityEditor;
using UnityEngine;
using System.IO;

namespace Editor
{
    public class ImageScaler : EditorWindow
    {
        private string folderAssetPath;
        private string destinationAssetPath;

        [MenuItem("Tools/Image Scaler")]
        static void Init()
        {
            GetWindow(typeof(ImageScaler));
        }

        void OnGUI()
        {
            GUILayout.Label("Source Folder", EditorStyles.boldLabel);
            folderAssetPath = EditorGUILayout.TextField(folderAssetPath);
            
            GUILayout.Label("Destination Folder", EditorStyles.boldLabel);
            destinationAssetPath = EditorGUILayout.TextField(destinationAssetPath);

            if (string.IsNullOrEmpty(folderAssetPath) || string.IsNullOrEmpty(destinationAssetPath) || !GUILayout.Button("Scale And Save")) return;

            string[] files = Directory.GetFiles(folderAssetPath);
            foreach (string filePath in files)
            {
                Object file = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
                if (file != null && file is Texture2D texture)
                {
                    ScaleAndSave(texture);
                }
            }
        }

        private void ScaleAndSave(Texture2D src)
        {
            const int size = 512;

            // Compute scaled dimensions while preserving aspect ratio
            float aspectSrc = (float)src.width / src.height;
            int targetWidth, targetHeight;
            if (aspectSrc > 1f)
            {
                targetWidth = size;
                targetHeight = Mathf.RoundToInt(size / aspectSrc);
            }
            else
            {
                targetHeight = size;
                targetWidth = Mathf.RoundToInt(size * aspectSrc);
            }

            Texture2D scaled = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] scaledPixels = new Color32[size * size];
            
            // Copy the scaled texture to the center of the result texture
            int offsetX = (size - targetWidth) / 2;
            int offsetY = (size - targetHeight) / 2;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsOutsideImage(y, targetHeight, x, targetWidth, offsetX, offsetY))
                    {
                        scaledPixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    
                    // Find the corresponding source pixel
                    int srcX = (x - offsetX) * src.width / targetWidth;
                    int srcY = (y - offsetY) * src.height / targetHeight;
                    scaledPixels[y * size + x] = src.GetPixelBilinear((float)srcX / src.width, (float)srcY / src.height);
                }
            }

            scaled.SetPixels32(scaledPixels);
            scaled.Apply();

            // Encode to PNG
            byte[] pngData = scaled.EncodeToPNG();

            // Make sure save directory exists
            
            if (!Directory.Exists(destinationAssetPath))
                Directory.CreateDirectory(destinationAssetPath);

            // Save
            string filePath = Path.Combine(destinationAssetPath, src.name + ".png");
            File.WriteAllBytes(filePath, pngData);
            AssetDatabase.Refresh();

            Debug.Log($"Saved scaled image at: {filePath}");
        }

        private static bool IsOutsideImage(int y, int targetHeight, int x, int targetWidth, int offsetX, int offsetY)
        {
            return y < offsetY || y >= targetHeight + offsetY 
                || x < offsetX || x >= targetWidth + offsetX;
        }

        public static Texture2D ScaleBilinear(Texture2D src, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, src.format, false);

            for (int y = 0; y < targetHeight; y++)
            {
                float v = (float)y / (targetHeight - 1);

                for (int x = 0; x < targetWidth; x++)
                {
                    float u = (float)x / (targetWidth - 1);

                    // Sample the source texture using bilinear filtering
                    Color color = src.GetPixelBilinear(u, v);
                    result.SetPixel(x, y, color);
                }
            }

            result.Apply();
            return result;
        }
    }
}