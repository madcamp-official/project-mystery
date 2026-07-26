using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
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

        private GameObject linePanel;
        private TMP_Text lineText;
        private TMP_Text speakerText;
        private RawImage speakerPortrait;
        private AspectRatioFitter speakerPortraitAspect;
        private Button nextButton;
        private GameObject choicesContainer;
        private Button[] choiceButtons;
        private TMP_Text[] choiceLabels;

        private DialogueSet currentSet;
        private DialogueNode currentNode;
        private ProductionDialogueFlow productionFlow;

        public bool IsBusy { get; private set; }
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
            speakerText = linePanelTransform.Find("Image/Text (TMP)").GetComponent<TMP_Text>();
            TMP_FontAsset koreanFont = StatusHUDController.RuntimeKoreanFont;
            lineText.font = koreanFont;
            speakerText.font = koreanFont;
            CreatePortrait(linePanelTransform);

            Transform selectBtn = linePanelTransform.Find("Select Btn");
            choicesContainer = selectBtn.gameObject;

            choiceButtons = new Button[ChoiceObjectNames.Length];
            choiceLabels = new TMP_Text[ChoiceObjectNames.Length];
            for (int i = 0; i < ChoiceObjectNames.Length; i++)
            {
                Transform choiceTransform = selectBtn.Find(ChoiceObjectNames[i]);
                choiceButtons[i] = choiceTransform.GetComponent<Button>();
                choiceLabels[i] = choiceTransform.GetComponentInChildren<TMP_Text>();
                choiceLabels[i].font = koreanFont;
            }

            nextButton.onClick.AddListener(OnNextClicked);
            linePanel.SetActive(false);
        }

        private void CreatePortrait(Transform parent)
        {
            GameObject portraitObject = new GameObject(
                "Speaker Portrait",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            portraitObject.transform.SetParent(parent, false);
            portraitObject.transform.SetAsFirstSibling();

            RectTransform rect = portraitObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(-660f, 20f);
            rect.sizeDelta = new Vector2(360f, 430f);

            speakerPortrait = portraitObject.GetComponent<RawImage>();
            speakerPortrait.raycastTarget = false;

            speakerPortraitAspect = portraitObject.GetComponent<AspectRatioFitter>();
            speakerPortraitAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
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

            DialoguePortraitAsset asset =
                DialoguePortraitCatalog.Resolve(speaker, emotion);
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
            IsBusy = false;
            currentSet = null;
            currentNode = null;
            productionFlow = null;
            linePanel?.SetActive(false);
            choicesContainer?.SetActive(false);
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
            if (productionFlow == null || productionFlow.IsComplete)
            {
                EndDialogue();
                return;
            }

            SaveProductionCheckpoint();
            bool hasChoices = productionFlow.IsAwaitingChoice;
            choicesContainer.SetActive(hasChoices);
            nextButton.gameObject.SetActive(!hasChoices);
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
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() =>
                    {
                        productionFlow.SelectChoice(selectedIndex);
                        RenderProduction();
                    });
                }
                return;
            }

            DialogueRecord record = productionFlow.Current;
            DialogueSpeakerIdentity speaker = DialoguePresentationMap.GetSpeaker(record.Speaker);
            speakerText.text =
                DialoguePresentationMap.GetSpeakerLabel(record.Speaker, speaker);
            lineText.text = record.TextKo;
            ShowPortrait(
                speaker.PortraitId,
                DialoguePresentationMap.GetEmotion(record.Emotion));
            FindFirstObjectByType<StatusHUDController>()?.SetContextCharacter(speaker.PortraitId);
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
                lineText.text = line.Text;
                ShowPortrait(line.Speaker);
                StatusHUDController hud = FindFirstObjectByType<StatusHUDController>();
                hud?.SetContextCharacter(line.Speaker);
            }
            else
            {
                speakerText.text = string.Empty;
                lineText.text = $"[MISSING LINE: {currentNode.LineId}]";
                ShowPortrait(string.Empty);
                StatusHUDController hud = FindFirstObjectByType<StatusHUDController>();
                hud?.ClearContextCharacter();
            }

            bool hasBranch = currentNode.Options != null && currentNode.Options.Count > 1;
            choicesContainer.SetActive(hasBranch);
            nextButton.gameObject.SetActive(!hasBranch);

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
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => ResolveOption(option));
            }
        }

        private void OnNextClicked()
        {
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
    }
}
