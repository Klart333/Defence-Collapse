namespace Saving
{
    using System;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Provides methods for saving and loading data using JSON serialization
    /// </summary>
    public class JsonSaveSystem : ISaveSystem
    {
        // File extension for saved files
        private const string FILE_EXTENSION = ".json";
        
        /// <summary>
        /// Saves data to a JSON file
        /// </summary>
        /// <typeparam name="T">Type of the data to save</typeparam>
        /// <param name="data">The data to save</param>
        /// <param name="fileName">Name of the file (without extension)</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public bool SaveData<T>(T data, string fileName)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, fileName + FILE_EXTENSION);
            
            try
            {
                string jsonData = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(fullPath, jsonData);
                
                Debug.Log($"Data successfully saved to {fullPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save data to {fullPath}. Error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads data from a JSON file
        /// </summary>
        /// <typeparam name="T">Type of the data to load</typeparam>
        /// <param name="fileName">Name of the file (without extension)</param>
        /// <param name="defaultValue">Default value to return if loading fails</param>
        /// <returns>The loaded data or defaultValue if loading fails</returns>
        public T LoadData<T>(string fileName, T defaultValue = default)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, fileName + FILE_EXTENSION);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"File not found at {fullPath}");
                return defaultValue;
            }

            try
            {
                string jsonData = File.ReadAllText(fullPath);
                T loadedData = JsonUtility.FromJson<T>(jsonData);
                Debug.Log($"Data successfully loaded from {fullPath}");
                return loadedData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load data from {fullPath}. Error: {e.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <param name="fileName">Name of the file (without extension)</param>
        /// <returns>True if file exists, false otherwise</returns>
        public bool SaveExists(string fileName)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, fileName + FILE_EXTENSION);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Deletes the specified save file
        /// </summary>
        /// <param name="fileName">Name of the file (without extension)</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public bool DeleteSave(string fileName)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, fileName + FILE_EXTENSION);
            
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Debug.Log($"File deleted at {fullPath}");
                    return true;
                }
                
                Debug.LogWarning($"No file to delete at {fullPath}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete file at {fullPath}. Error: {e.Message}");
                return false;
            }
        }
    }
}