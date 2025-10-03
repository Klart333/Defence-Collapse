namespace Saving
{
    public class PersistantSaveManager : Singleton<PersistantSaveManager>
    {
        private MessagePackSaveSystem msgPackSaveSystem;

        public TurnAmountSaveLoad TurnAmountSaveLoad { get; private set; }
        public ExpSaveLoad ExpSaveLoad { get; private set; }

        private void OnEnable()
        {
            if (Instance != this)
            {
                return;
            }
            
            msgPackSaveSystem = new MessagePackSaveSystem();
            
            TurnAmountSaveLoad = new TurnAmountSaveLoad(msgPackSaveSystem);
            ExpSaveLoad = new ExpSaveLoad(msgPackSaveSystem);
        }
    }
}