using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Narrative
{
    public class DialogueController :
        MonoBehaviour,
        IProductionScenePlayer,
        IProductionSceneLaunchAvailability
    {
        public static DialogueController Instance { get; private set; }

        private static readonly string[] ChoiceObjectNames =
        {
            "Choice", "Choice (1)", "Choice (2)", "Choice (3)"
        };

        private const string TypewriterSfxResourcePath = "SoundEffect/Type_Writer";
        private const int MaximumAutoRoutedRecords = 64;

        [SerializeField] private float typewriterCharacterInterval = 0.02f;

        private GameObject linePanel;
        private TMP_Text lineText;
        private TMP_Text speakerText;
        private RawImage speakerPortrait;
        private AspectRatioFitter speakerPortraitAspect;
        private Button nextButton;
        private DialogueAdvanceControl advanceControl;
        private GameObject choicesContainer;
        private Button[] choiceButtons;
        private TMP_Text[] choiceLabels;
        private DialogueChoicePresentation choicePresentation;
        private ResponsiveDialogueLayout responsiveLayout;
        private DialoguePresentationView presentationView;
        private InvestigationDialogueUIController investigationUi;

        private DialogueSet currentSet;
        private DialogueNode currentNode;
        private ProductionDialogueFlow productionFlow;
        private bool ambientLineActive;
        private string pendingInvestigationTitle = string.Empty;

        private AudioSource typewriterAudioSource;
        private AudioClip typewriterClip;
        private Coroutine typewriterRoutine;
        private bool isTypewriterActive;
        private int lastAdvanceInputFrame = -1;
        private IReadOnlyList<string> linePages = Array.Empty<string>();
        private int linePageIndex;
        private bool lineNarrationOrSystem;
        private bool choicesPendingAfterPages;

        public bool IsBusy { get; private set; }
        public DialoguePresentationSpec ActivePresentation { get; private set; } =
            DialoguePresentationPolicy.Hidden;
        public event Action<DialoguePresentationSpec> PresentationChanged;
        public string ActiveProductionSceneId =>
            productionFlow?.ActiveSceneId ?? string.Empty;

        private void Awake()
        {
            Instance = this;
            BindUi();
        }

        private void BindUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                Debug.LogError("DialogueController could not find Canvas in scene.");
                return;
            }

            Transform linePanelTransform = canvas.Find("Ingame/Line Panel");
            linePanel = linePanelTransform.gameObject;
            lineText = linePanelTransform.Find("Panel/line").GetComponent<TMP_Text>();
            nextButton = linePanelTransform.Find("Panel/Next").GetComponent<Button>();
            advanceControl =
                nextButton.GetComponent<DialogueAdvanceControl>() ??
                nextButton.gameObject.AddComponent<DialogueAdvanceControl>();
            advanceControl.Initialize(nextButton);
            speakerText = linePanelTransform.Find("Image/Text (TMP)").GetComponent<TMP_Text>();
            CreatePortrait(
                linePanelTransform.parent,
                linePanelTransform.GetSiblingIndex() + 1);

            Transform selectBtn = linePanelTransform.Find("Select Btn");
            choicesContainer = selectBtn.gameObject;

            DialogueChoiceButtonSet choiceSet =
                DialogueChoiceButtonPool.EnsureCapacity(
                    selectBtn,
                    ChoiceObjectNames,
                    ProductionDialogueFlow.ChoiceCapacity);
            choiceButtons = choiceSet.Buttons;
            choiceLabels = choiceSet.Labels;
            choicePresentation =
                selectBtn.GetComponent<DialogueChoicePresentation>() ??
                selectBtn.gameObject.AddComponent<
                    DialogueChoicePresentation>();
            choicePresentation.Initialize(
                selectBtn as RectTransform,
                choiceButtons);
            DialogueTypography.ApplySurface(
                lineText,
                speakerText,
                choiceLabels);

            nextButton.onClick.AddListener(OnNextClicked);
            typewriterClip = Resources.Load<AudioClip>(TypewriterSfxResourcePath);
            typewriterAudioSource = GetComponent<AudioSource>();
            if (typewriterAudioSource == null)
            {
                typewriterAudioSource = gameObject.AddComponent<AudioSource>();
            }
            typewriterAudioSource.playOnAwake = false;
            typewriterAudioSource.loop = true;
            typewriterAudioSource.clip = typewriterClip;
            typewriterAudioSource.volume =
                AudioManager.Instance?.SfxVolume ?? 1f;
            responsiveLayout = canvas.gameObject
                .GetComponent<ResponsiveDialogueLayout>();
            if (responsiveLayout == null)
            {
                responsiveLayout = canvas.gameObject
                    .AddComponent<ResponsiveDialogueLayout>();
            }
            responsiveLayout.Initialize(
                canvas.GetComponent<Canvas>(),
                linePanelTransform.GetComponent<RectTransform>(),
                speakerPortrait.GetComponent<RectTransform>(),
                lineText,
                speakerText,
                nextButton.GetComponent<RectTransform>(),
                selectBtn.GetComponent<RectTransform>(),
                choiceButtons);
            responsiveLayout.SetPresentationDriven(true);
            presentationView =
                canvas.GetComponent<DialoguePresentationView>();
            if (presentationView == null)
            {
                presentationView =
                    canvas.gameObject.AddComponent<
                        DialoguePresentationView>();
            }
            presentationView.Initialize(
                linePanelTransform.parent as RectTransform,
                linePanelTransform as RectTransform,
                speakerPortrait.GetComponent<RectTransform>(),
                lineText,
                speakerText,
                nextButton.GetComponent<RectTransform>(),
                selectBtn.GetComponent<RectTransform>());
            investigationUi =
                canvas.GetComponent<InvestigationDialogueUIController>();
            if (investigationUi == null)
            {
                investigationUi =
                    canvas.gameObject.AddComponent<
                        InvestigationDialogueUIController>();
            }
            investigationUi.Initialize(canvas);
            linePanel.SetActive(false);
            advanceControl.SetState(DialogueAdvanceState.Hidden);
        }

        private void Update()
        {
            if (!IsBusy ||
                linePanel == null ||
                !linePanel.activeInHierarchy ||
                choicesContainer == null ||
                choicesContainer.activeInHierarchy ||
                nextButton == null ||
                !nextButton.gameObject.activeInHierarchy)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool advancePressed =
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame;
            if (advancePressed)
            {
                OnNextClicked();
            }
        }

        private void SetLineText(string text, bool isNarrationOrSystem)
        {
            StopTypewriter();
            Canvas.ForceUpdateCanvases();
            linePages = DialogueTextPaginator.SplitToFit(
                text,
                lineText,
                DialogueTypographyMetrics.LineMinimum);
            linePageIndex = 0;
            lineNarrationOrSystem = isNarrationOrSystem;
            choicesPendingAfterPages = false;
            PresentCurrentLinePage();
        }

        private void PresentCurrentLinePage()
        {
            StopTypewriter();
            string content = linePages.Count > 0
                ? linePages[Mathf.Clamp(
                    linePageIndex,
                    0,
                    linePages.Count - 1)]
                : string.Empty;
            lineText.pageToDisplay = 1;
            lineText.text = content;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
            int totalCharacters = lineText.textInfo.characterCount;
            if (totalCharacters <= 0)
            {
                advanceControl?.SetState(
                    DialogueAdvanceState.AdvanceLine);
                return;
            }

            isTypewriterActive = true;
            advanceControl?.SetState(
                DialogueAdvanceState.RevealLine);
            if (lineNarrationOrSystem && typewriterClip != null)
            {
                typewriterAudioSource.volume =
                    AudioManager.Instance?.SfxVolume ?? 1f;
                typewriterAudioSource.Play();
            }

            typewriterRoutine = StartCoroutine(
                TypewriterRoutine(totalCharacters));
        }

        private bool HasMoreLinePages =>
            linePageIndex + 1 < linePages.Count;

        private bool TryAdvanceLinePage()
        {
            if (!HasMoreLinePages)
                return false;

            linePageIndex++;
            PresentCurrentLinePage();
            return true;
        }

        private IEnumerator TypewriterRoutine(int totalCharacters)
        {
            for (int i = 1; i <= totalCharacters; i++)
            {
                lineText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typewriterCharacterInterval);
            }

            FinishTypewriter();
        }

        private void SkipTypewriter()
        {
            if (typewriterRoutine != null)
            {
                StopCoroutine(typewriterRoutine);
                typewriterRoutine = null;
            }

            FinishTypewriter();
        }

        private void FinishTypewriter()
        {
            lineText.maxVisibleCharacters = int.MaxValue;
            isTypewriterActive = false;
            typewriterRoutine = null;
            if (choicesPendingAfterPages && !HasMoreLinePages)
                RevealPendingChoices();
            else
                advanceControl?.SetState(
                    DialogueAdvanceState.AdvanceLine);
            if (typewriterAudioSource != null && typewriterAudioSource.isPlaying)
            {
                typewriterAudioSource.Stop();
            }
        }

        private void StopTypewriter()
        {
            if (typewriterRoutine != null)
            {
                StopCoroutine(typewriterRoutine);
                typewriterRoutine = null;
            }

            isTypewriterActive = false;
            if (typewriterAudioSource != null && typewriterAudioSource.isPlaying)
            {
                typewriterAudioSource.Stop();
            }
        }

        private void PresentChoicesOrContinuePages()
        {
            responsiveLayout?.RefreshChoiceLayout();
            if (HasMoreLinePages)
            {
                choicesPendingAfterPages = true;
                choicePresentation.Hide();
                advanceControl.SetState(DialogueAdvanceState.AdvanceLine);
                return;
            }

            choicesPendingAfterPages = false;
            choicePresentation.Show();
        }

        private void RevealPendingChoices()
        {
            choicesPendingAfterPages = false;
            responsiveLayout?.RefreshChoiceLayout();
            choicePresentation.Show();
            advanceControl.SetState(DialogueAdvanceState.Hidden);
        }

        private void CreatePortrait(Transform parent, int siblingIndex)
        {
            GameObject portraitObject = new GameObject(
                "Speaker Portrait",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            portraitObject.transform.SetParent(parent, false);
            portraitObject.transform.SetSiblingIndex(siblingIndex);

            RectTransform rect = portraitObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(-660f, 20f);
            rect.sizeDelta = new Vector2(360f, 430f);
            Wake.UI.RuntimeUiLayoutRegistry.CopyLayout(
                rect,
                "dialogue.speaker-portrait");

            speakerPortrait = portraitObject.GetComponent<RawImage>();
            speakerPortrait.raycastTarget = false;

            speakerPortraitAspect = portraitObject.GetComponent<AspectRatioFitter>();
            speakerPortraitAspect.aspectMode =
                AspectRatioFitter.AspectMode.None;
            portraitObject.SetActive(false);
        }

        private void ShowPortrait(
            string speaker,
            PortraitEmotion emotion = PortraitEmotion.Neutral)
        {
            if (speakerPortrait == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(speaker))
            {
                speakerPortrait.gameObject.SetActive(false);
                return;
            }

            int anxiety =
                Wake.Core.GameStateManager.Instance?.PublicAnxiety ?? 15;
            PortraitEmotion resolvedEmotion =
                NpcAnxietyExpressionPolicy.Resolve(
                    speaker,
                    emotion,
                    anxiety);
            DialoguePortraitAsset asset =
                DialoguePortraitCatalog.Resolve(speaker, resolvedEmotion);
            if (!asset.Found)
            {
                Debug.LogWarning($"No portrait texture found for speaker '{speaker}'.");
                speakerPortrait.gameObject.SetActive(false);
                return;
            }

            speakerPortrait.texture = asset.Texture;
            speakerPortrait.uvRect = asset.UvRect;
            speakerPortraitAspect.aspectRatio = asset.AspectRatio;
            speakerPortrait.gameObject.SetActive(true);
            presentationView?.Apply(ActivePresentation);
        }

        public void StartDialogue(DialogueSet dialogueSet)
        {
            if (dialogueSet == null)
            {
                return;
            }

            currentSet = dialogueSet;
            productionFlow = null;
            IsBusy = true;
            linePanel.SetActive(true);
            GoToNode(dialogueSet.StartNodeId);
        }

        public bool StartAmbientLine(
            string speaker,
            string text,
            string emotion = "neutral")
        {
            if (IsBusy || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            currentSet = null;
            currentNode = null;
            productionFlow = null;
            ambientLineActive = true;
            IsBusy = true;
            linePanel.SetActive(true);
            choicePresentation.Hide();
            presentationView?.SetChoicesVisible(false);
            advanceControl.SetState(DialogueAdvanceState.AdvanceLine);
            speakerText.text = DialoguePortraitCatalog.GetDisplayName(speaker);
            ApplyPresentation(
                DialoguePresentationPolicy.ForAmbient(
                    DialoguePresentationMap.GetSpeaker(speaker)));
            SetLineText(text, isNarrationOrSystem: false);
            responsiveLayout?.ResetTextScroll();
            ShowPortrait(
                speaker,
                DialoguePresentationMap.GetEmotion(emotion));
            FindFirstObjectByType<StatusHUDController>()
                ?.SetContextCharacter(speaker);
            return true;
        }

        public bool StartProductionScene(string sceneId)
        {
            if (productionFlow != null &&
                string.Equals(
                    productionFlow.ActiveSceneId,
                    sceneId?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!CanStartProductionScene(sceneId))
            {
                return false;
            }

            productionFlow = CreateProductionFlow();
            if (productionFlow == null || !productionFlow.StartScene(sceneId))
            {
                productionFlow = null;
                return false;
            }

            BeginProductionPresentation();
            return true;
        }

        public bool CanStartProductionScene(string sceneId)
        {
            string normalized = sceneId?.Trim().ToUpperInvariant() ?? string.Empty;
            if (normalized.Length == 0)
            {
                return false;
            }

            if (productionFlow != null &&
                string.Equals(
                    productionFlow.ActiveSceneId,
                    normalized,
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (IsBusy ||
                DialogueDatabase.Instance == null ||
                !DialogueDatabase.Instance.ContainsScene(normalized))
            {
                return false;
            }

            ProductionDialogueFlow candidate = CreateProductionFlow();
            return candidate != null && candidate.CanStartScene(normalized);
        }

        public void CancelActiveDialogue()
        {
            StopTypewriter();
            IsBusy = false;
            currentSet = null;
            currentNode = null;
            productionFlow = null;
            ambientLineActive = false;
            pendingInvestigationTitle = string.Empty;
            ApplyPresentation(DialoguePresentationPolicy.Hidden);
            linePanel?.SetActive(false);
            investigationUi?.Hide();
            choicePresentation?.Hide();
            advanceControl?.SetState(DialogueAdvanceState.Hidden);
            speakerPortrait?.gameObject.SetActive(false);
            FindFirstObjectByType<StatusHUDController>()
                ?.ClearContextCharacter();
            FindFirstObjectByType<NarrativeLocationHUDController>()
                ?.Clear();
        }

        public bool RestoreProductionScene(
            Wake.Core.ProductionDialogueCheckpoint checkpoint)
        {
            if (checkpoint == null || IsBusy)
            {
                return false;
            }

            productionFlow = CreateProductionFlow();
            if (productionFlow == null || !productionFlow.RestoreScene(checkpoint))
            {
                productionFlow = null;
                return false;
            }

            BeginProductionPresentation();
            return true;
        }

        private ProductionDialogueFlow CreateProductionFlow()
        {
            DialogueDatabase database = DialogueDatabase.Instance;
            if (database == null)
            {
                return null;
            }

            return new ProductionDialogueFlow(
                database.Records.Values,
                null,
                Wake.Core.GameStateManager.Instance,
                evidenceId =>
                    EvidenceInventory.Instance != null &&
                    EvidenceInventory.Instance.TryAddById(evidenceId));
        }

        private void BeginProductionPresentation()
        {
            currentSet = null;
            currentNode = null;
            IsBusy = true;
            linePanel.SetActive(true);
            FindFirstObjectByType<NarrativeLocationHUDController>()
                ?.ShowScene(productionFlow.ActiveSceneId);
            RenderProduction();
        }

        private void RenderProduction()
        {
            int routedRecords = 0;
            while (productionFlow != null &&
                   !productionFlow.IsComplete &&
                   !productionFlow.IsAwaitingChoice &&
                   productionFlow.Current != null &&
                   routedRecords < MaximumAutoRoutedRecords)
            {
                DialogueRecord current = productionFlow.Current;
                if (ProductionPresentationRouting.IsSystemEvent(current))
                {
                    productionFlow.Advance();
                    DispatchSystemEvent(current);
                    routedRecords++;
                    continue;
                }

                if (InvestigationPresentationPolicy.IsMarker(current))
                {
                    SaveProductionCheckpoint();
                    PresentInvestigationTarget(current);
                    return;
                }

                if (InvestigationPresentationPolicy.IsInvestigationResult(
                        current))
                {
                    SaveProductionCheckpoint();
                    PresentInvestigationResult(current);
                    return;
                }

                break;
            }

            if (routedRecords >= MaximumAutoRoutedRecords)
            {
                Debug.LogError(
                    $"Scene '{productionFlow?.ActiveSceneId}' exceeded " +
                    "the non-dialogue auto-routing guard.");
                EndDialogue();
                return;
            }

            if (productionFlow == null || productionFlow.IsComplete)
            {
                EndDialogue();
                return;
            }

            investigationUi?.Hide();
            linePanel.SetActive(true);
            SaveProductionCheckpoint();
            bool hasChoices = productionFlow.IsAwaitingChoice;
            presentationView?.SetChoicesVisible(hasChoices);
            if (hasChoices)
                choicesContainer.SetActive(true);
            else
                choicePresentation.Hide();
            advanceControl.SetState(
                hasChoices
                    ? DialogueAdvanceState.Hidden
                    : DialogueAdvanceState.AdvanceLine);
            if (hasChoices)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    bool active = i < productionFlow.Choices.Count;
                    choiceButtons[i].gameObject.SetActive(active);
                    if (!active)
                    {
                        continue;
                    }

                    int selectedIndex = i;
                    choiceLabels[i].text = productionFlow.Choices[i].TextKo;
                    DialogueTypography.ApplyChoice(
                        choiceLabels[i],
                        productionFlow.Choices[i].TextKo);
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() =>
                    {
                        productionFlow.SelectChoice(selectedIndex);
                        RenderProduction();
                    });
                }
                PresentChoicesOrContinuePages();
                return;
            }

            DialogueRecord record = productionFlow.Current;
            DialogueSpeakerIdentity speaker =
                DialoguePresentationMap.GetSpeaker(
                    record.Speaker,
                    record.LineType);
            ApplyPresentation(
                DialoguePresentationPolicy.ForProduction(speaker));
            speakerText.text =
                DialoguePresentationMap.GetSpeakerLabel(record.Speaker, speaker);
            bool isNarrationOrSystem =
                speaker.Kind == DialogueSpeakerKind.Narration;
            SetLineText(record.TextKo, isNarrationOrSystem);
            responsiveLayout?.ResetTextScroll();
            ShowPortrait(
                speaker.PortraitId,
                DialoguePresentationMap.GetEmotion(record.Emotion));
            FindFirstObjectByType<StatusHUDController>()?.SetContextCharacter(speaker.PortraitId);
        }

        private void DispatchSystemEvent(DialogueRecord record)
        {
            ProductionUiEventPresentation presentation =
                ProductionPresentationRouting.ClassifySystemEvent(record);
            if (presentation.Channel ==
                ProductionUiEventChannel.Evidence)
            {
                EvidencePanelController.Instance?.Refresh();
            }

            if (!presentation.ShowToast)
                return;

            if (presentation.Channel is
                ProductionUiEventChannel.Theory or
                ProductionUiEventChannel.Interaction)
            {
                ToastController.Instance?.ShowAlert(
                    presentation.Message);
                return;
            }

            ToastController.Instance?.Show(presentation.Message);
        }

        private void PresentInvestigationTarget(DialogueRecord marker)
        {
            StopTypewriter();
            ApplyPresentation(
                DialoguePresentationPolicy.ForInvestigation());
            advanceControl?.SetState(DialogueAdvanceState.Hidden);
            linePanel.SetActive(false);
            pendingInvestigationTitle =
                InvestigationPresentationPolicy.MarkerTitle(marker);
            if (investigationUi == null)
            {
                productionFlow?.Advance();
                RenderProduction();
                return;
            }
            investigationUi.ShowTarget(
                pendingInvestigationTitle,
                () =>
                {
                    investigationUi.Hide();
                    productionFlow?.Advance();
                    RenderProduction();
                });
        }

        private void PresentInvestigationResult(DialogueRecord record)
        {
            StopTypewriter();
            ApplyPresentation(
                DialoguePresentationPolicy.ForInvestigation());
            advanceControl?.SetState(DialogueAdvanceState.Hidden);
            linePanel.SetActive(false);
            string title = string.IsNullOrWhiteSpace(
                pendingInvestigationTitle)
                ? InvestigationPresentationPolicy.ResultTitle(record)
                : pendingInvestigationTitle;
            string section =
                InvestigationPresentationPolicy.IsObservation(record)
                    ? "현장 관찰"
                    : "현장 조사";
            if (investigationUi == null)
            {
                productionFlow?.Advance();
                RenderProduction();
                return;
            }
            investigationUi.ShowObservation(
                section,
                title,
                record.TextKo,
                () =>
                {
                    investigationUi.Hide();
                    pendingInvestigationTitle = string.Empty;
                    productionFlow?.Advance();
                    RenderProduction();
                });
        }

        private void GoToNode(string nodeId)
        {
            currentNode = currentSet != null ? currentSet.FindNode(nodeId) : null;
            if (currentNode == null)
            {
                EndDialogue();
                return;
            }

            RenderCurrentNode();
        }

        private void RenderCurrentNode()
        {
            DialogueDatabase db = DialogueDatabase.Instance;
            if (db != null && db.TryGetLine(currentNode.LineId, out DialogueLine line))
            {
                speakerText.text = line.Speaker;
                DialogueSpeakerIdentity legacySpeaker =
                    DialoguePresentationMap.GetSpeaker(line.Speaker);
                ApplyPresentation(
                    DialoguePresentationPolicy.ForProduction(
                        legacySpeaker));
                bool isNarrationOrSystem =
                    legacySpeaker.Kind == DialogueSpeakerKind.Narration ||
                    legacySpeaker.Kind == DialogueSpeakerKind.System;
                SetLineText(line.Text, isNarrationOrSystem);
                responsiveLayout?.ResetTextScroll();
                ShowPortrait(line.Speaker);
                StatusHUDController hud = FindFirstObjectByType<StatusHUDController>();
                hud?.SetContextCharacter(line.Speaker);
            }
            else
            {
                speakerText.text = string.Empty;
                SetLineText(
                    $"[MISSING LINE: {currentNode.LineId}]",
                    isNarrationOrSystem: true);
                responsiveLayout?.ResetTextScroll();
                ShowPortrait(string.Empty);
                StatusHUDController hud = FindFirstObjectByType<StatusHUDController>();
                hud?.ClearContextCharacter();
            }

            bool hasBranch = currentNode.Options != null && currentNode.Options.Count > 1;
            presentationView?.SetChoicesVisible(hasBranch);
            if (hasBranch)
                choicesContainer.SetActive(true);
            else
                choicePresentation.Hide();
            advanceControl.SetState(
                hasBranch
                    ? DialogueAdvanceState.Hidden
                    : DialogueAdvanceState.AdvanceLine);

            if (!hasBranch)
            {
                return;
            }

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool active = i < currentNode.Options.Count;
                choiceButtons[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                DialogueOption option = currentNode.Options[i];
                string label = option.OptionLineId;
                if (db != null && db.TryGetLine(option.OptionLineId, out DialogueLine optionLine))
                {
                    label = optionLine.Text;
                }

                choiceLabels[i].text = label;
                DialogueTypography.ApplyChoice(
                    choiceLabels[i],
                    label);
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => ResolveOption(option));
            }
            PresentChoicesOrContinuePages();
        }

        private void OnNextClicked()
        {
            // Enter both drives our own keyboard polling below and Unity's
            // built-in UI Submit action on the currently-selected button
            // (InputSystemUIInputModule binds Submit to Enter by default,
            // and clicking Next with the mouse leaves it selected). One
            // physical keypress can therefore call this twice in the same
            // frame - once from each path - which would skip and advance
            // in a single press. Collapse same-frame calls into one.
            if (Time.frameCount == lastAdvanceInputFrame)
            {
                return;
            }
            lastAdvanceInputFrame = Time.frameCount;

            if (isTypewriterActive)
            {
                SkipTypewriter();
                return;
            }

            if (TryAdvanceLinePage())
                return;

            if (ambientLineActive)
            {
                EndDialogue();
                return;
            }

            if (productionFlow != null)
            {
                productionFlow.Advance();
                RenderProduction();
                return;
            }

            if (currentNode == null || currentNode.Options == null || currentNode.Options.Count == 0)
            {
                EndDialogue();
                return;
            }

            ResolveOption(currentNode.Options[0]);
        }

        private void ResolveOption(DialogueOption option)
        {
            choicePresentation?.Hide();
            if (option == null)
            {
                EndDialogue();
                return;
            }

            option.ApplyStateEffects();
            GoToNode(option.NextNodeId);
        }

        private void EndDialogue()
        {
            StopTypewriter();
            string completedProductionScene = productionFlow?.ActiveSceneId;
            if (productionFlow?.Phase == ProductionScenePhase.Completed)
            {
                Wake.Core.GameStateManager.Instance?.ClearDialogueCheckpoint(
                    completedProductionScene,
                    productionFlow.PendingInteractionId);
            }
            else
            {
                SaveProductionCheckpoint();
            }
            IsBusy = false;
            currentSet = null;
            currentNode = null;
            productionFlow = null;
            ambientLineActive = false;
            pendingInvestigationTitle = string.Empty;
            ApplyPresentation(DialoguePresentationPolicy.Hidden);
            presentationView?.SetChoicesVisible(false);
            choicePresentation?.Hide();
            advanceControl?.SetState(DialogueAdvanceState.Hidden);
            investigationUi?.Hide();
            if (linePanel != null)
            {
                linePanel.SetActive(false);
            }
            StatusHUDController hud = FindFirstObjectByType<StatusHUDController>();
            hud?.ClearContextCharacter();
            if (string.Equals(
                    completedProductionScene,
                    ExitInspectionCatalog.SceneId,
                    StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<ExitInspectionUIController>()?.Open();
            }
            else if (ProductionPuzzleCatalog.TryGetByScene(
                    completedProductionScene,
                    out ProductionPuzzleDefinition puzzle))
            {
                FindFirstObjectByType<ProductionPuzzleUIController>()?.Open(puzzle.Id);
            }
            else if (string.Equals(
                         completedProductionScene,
                         CameraBlindSpotSession.SceneId,
                         StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<CameraBlindSpotUIController>()?.Open();
            }
            else if (string.Equals(
                         completedProductionScene,
                         MarcusInterrogationCatalog.SceneId,
                         StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<MarcusInterrogationUIController>()?.Open();
            }
            else if (string.Equals(
                         completedProductionScene,
                         TimelinePuzzleCatalog.SceneId,
                         StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<TimelinePuzzleUIController>()?.Open();
            }
            else if (string.Equals(
                         completedProductionScene,
                         OrpheusRecordCatalog.SceneId,
                         StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<OrpheusAudioRestorationUIController>()?.Open();
            }
            else if (string.Equals(
                         completedProductionScene,
                         "D8-01",
                         StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<FinalAccusationUIController>()?.Open();
            }
            else if (string.Equals(
                         completedProductionScene,
                         ProductionEndingCatalog.ConfessionSceneId,
                         StringComparison.OrdinalIgnoreCase))
            {
                StartProductionScene(ProductionEndingCatalog.EpilogueSceneId);
            }
            else if (string.Equals(
                         completedProductionScene,
                         ProductionEndingCatalog.EpilogueSceneId,
                         StringComparison.OrdinalIgnoreCase))
            {
                FindFirstObjectByType<ProductionEndingUIController>()
                    ?.ShowEpilogue();
            }
        }

        private void SaveProductionCheckpoint()
        {
            if (productionFlow == null ||
                productionFlow.Phase == ProductionScenePhase.Completed)
            {
                return;
            }

            Wake.Core.GameStateManager.Instance?.SaveDialogueCheckpoint(
                productionFlow.ActiveSceneId,
                productionFlow.CurrentIndex,
                productionFlow.IsAwaitingChoice,
                productionFlow.PendingInteractionId);
        }

        private void ApplyPresentation(DialoguePresentationSpec presentation)
        {
            bool changed = ActivePresentation != presentation;
            ActivePresentation = presentation;
            presentationView?.Apply(presentation);
            if (changed)
                PresentationChanged?.Invoke(presentation);
        }
    }
}
