namespace Saving
{
    public interface ISaveSystem
    {
        public bool SaveData<T>(T data, string fileName);
        public T LoadData<T>(string fileName, T defaultValue = default);
        public bool SaveExists(string fileName);
        public bool DeleteSave(string fileName);
    }
}