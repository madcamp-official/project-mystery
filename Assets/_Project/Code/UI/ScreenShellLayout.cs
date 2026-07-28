using UnityEngine;

namespace Wake.UI
{
    public enum ScreenShellType
    {
        System,
        Exploration,
        Dialogue,
        Investigation,
        Puzzle,
        Ending,
        ModalOverlay
    }

    public readonly struct ScreenShellPolicy
    {
        public bool BlocksGameplayHud { get; }
        public bool ShowsGlobalNavigation { get; }
        public bool CapturesInput { get; }

        public ScreenShellPolicy(
            bool blocksGameplayHud,
            bool showsGlobalNavigation,
            bool capturesInput)
        {
            BlocksGameplayHud = blocksGameplayHud;
            ShowsGlobalNavigation = showsGlobalNavigation;
            CapturesInput = capturesInput;
        }
    }

    public static class ScreenShellSlotIds
    {
        public const string SafeArea = "shell.safeArea";
        public const string PortraitLeft = "shell.portrait.left";
        public const string PortraitRight = "shell.portrait.right";
        public const string Choices = "shell.choices";
        public const string ModalDim = "shell.modal.dim";
        public const string ModalPanel = "shell.modal.panel";
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScreenRegionSet))]
    public sealed class ScreenShellLayout : MonoBehaviour
    {
        [SerializeField] private ScreenShellType shellType;
        [SerializeField] private ScreenRegionSet regions;
        [SerializeField] private RuntimeUiLayoutSlot safeArea;
        [SerializeField] private RuntimeUiLayoutSlot portraitLeft;
        [SerializeField] private RuntimeUiLayoutSlot portraitRight;
        [SerializeField] private RuntimeUiLayoutSlot choices;
        [SerializeField] private RuntimeUiLayoutSlot modalDim;
        [SerializeField] private RuntimeUiLayoutSlot modalPanel;

        public ScreenShellType ShellType => shellType;
        public ScreenRegionSet Regions => regions;
        public RuntimeUiLayoutSlot SafeArea => safeArea;
        public RuntimeUiLayoutSlot PortraitLeft => portraitLeft;
        public RuntimeUiLayoutSlot PortraitRight => portraitRight;
        public RuntimeUiLayoutSlot Choices => choices;
        public RuntimeUiLayoutSlot ModalDim => modalDim;
        public RuntimeUiLayoutSlot ModalPanel => modalPanel;

        public bool IsComplete =>
            regions != null &&
            regions.IsComplete &&
            safeArea != null &&
            portraitLeft != null &&
            portraitRight != null &&
            choices != null &&
            modalDim != null &&
            modalPanel != null;

        public ScreenShellPolicy Policy => shellType switch
        {
            ScreenShellType.System =>
                new ScreenShellPolicy(true, false, false),
            ScreenShellType.Exploration =>
                new ScreenShellPolicy(false, true, false),
            ScreenShellType.Dialogue =>
                new ScreenShellPolicy(false, true, false),
            ScreenShellType.Investigation =>
                new ScreenShellPolicy(false, true, false),
            ScreenShellType.Puzzle =>
                new ScreenShellPolicy(false, true, false),
            ScreenShellType.Ending =>
                new ScreenShellPolicy(true, false, false),
            ScreenShellType.ModalOverlay =>
                new ScreenShellPolicy(false, false, true),
            _ => new ScreenShellPolicy(false, false, false)
        };

        public void Configure(
            ScreenShellType type,
            ScreenRegionSet regionSet,
            RuntimeUiLayoutSlot safeAreaSlot,
            RuntimeUiLayoutSlot leftPortraitSlot,
            RuntimeUiLayoutSlot rightPortraitSlot,
            RuntimeUiLayoutSlot choicesSlot,
            RuntimeUiLayoutSlot dimSlot,
            RuntimeUiLayoutSlot panelSlot)
        {
            shellType = type;
            regions = regionSet;
            safeArea = safeAreaSlot;
            portraitLeft = leftPortraitSlot;
            portraitRight = rightPortraitSlot;
            choices = choicesSlot;
            modalDim = dimSlot;
            modalPanel = panelSlot;
        }

        private void OnValidate()
        {
            if (regions == null)
            {
                regions = GetComponent<ScreenRegionSet>();
            }
        }
    }
}
