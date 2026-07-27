namespace Wake.UI
{
    public interface IIngameUiHost
    {
        bool IsShowingIngamePanel { get; }
        bool IsSettingsOpen { get; }
        int OpenRuntimeModalCount { get; }
        void ShowIngame();
        void ShowEvidence();
        void ShowEvidence(string evidenceId);
        void CloseSettings();
    }

    public static class IngameUi
    {
        public static IIngameUiHost Current { get; private set; }

        public static void Register(IIngameUiHost host)
        {
            Current = host;
        }
    }
}
