using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wake.Evidence;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Narrative
{
    public class DialogueController : MonoBehaviour, IProductionScenePlayer
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

        private readonly Dictionary<string, PortraitDefinition> portraits =
            new Dictionary<string, PortraitDefinition>(StringComparer.OrdinalIgnoreCase);

        public bool IsBusy { get; private set; }
        public string ActiveProductionSceneId =>
            productionFlow?.ActiveSceneId ?? string.Empty;

        private readonly struct PortraitDefinition
        {
            public readonly string ResourceName;
            public readonly Rect Crop;

            public PortraitDefinition(string resourceName, Rect crop)
            {
                ResourceName = resourceName;
                Crop = crop;
            }
        }

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
            RegisterPortraits();

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

        private void RegisterPortraits()
        {
            // Character sheets use the right-hand close-up as the dialogue portrait.
            Rect standardCloseUp = new Rect(0.46f, 0f, 0.54f, 1f);
            AddPortrait("ADRIAN", "adrian_vale", standardCloseUp);
            AddPortrait("CLAIRE", "claire_hawthorne", standardCloseUp);
            AddPortrait("DANIEL", "daniel_mercer", standardCloseUp);
            AddPortrait("RICHARD", "richard_hawthorne", standardCloseUp);
            AddPortrait("EVELYN", "evelyn_shaw", standardCloseUp);
            AddPortrait("THOMAS", "thomas_reed", standardCloseUp);
            AddPortrait("OWEN", "owen_price", standardCloseUp);

            // Marcus and Helena share one four-pose character sheet.
            AddPortrait("MARCUS", "marcus_bell_and_helena_ward", new Rect(0.25f, 0f, 0.28f, 1f));
            AddPortrait("HELENA", "marcus_bell_and_helena_ward", new Rect(0.70f, 0f, 0.30f, 1f));
        }

        private void AddPortrait(string id, string resourceName, Rect crop)
        {
            PortraitDefinition definition = new PortraitDefinition(resourceName, crop);
            portraits[id] = definition;

            // Current CSV files use display names while the final data contract uses IDs.
            portraits[GetDisplayName(id)] = definition;
        }

        private static string GetDisplayName(string id)
        {
            switch (id)
            {
                case "ADRIAN": return "Adrian Vale";
                case "CLAIRE": return "Claire Hawthorne";
                case "DANIEL": return "Daniel Mercer";
                case "RICHARD": return "Richard Hawthorne";
                case "EVELYN": return "Evelyn Shaw";
                case "THOMAS": return "Thomas Reed";
                case "MARCUS": return "Marcus Bell";
                case "HELENA": return "Helena Ward";
                case "OWEN": return "Owen Price";
                default: return id;
            }
        }

        private void ShowPortrait(string speaker)
        {
            if (speakerPortrait == null)
            {
                return;
            }

            string lookup = string.IsNullOrWhiteSpace(speaker) ? string.Empty : speaker.Trim();
            int modeSeparator = lookup.IndexOf('_');
            if (modeSeparator > 0)
            {
                lookup = lookup.Substring(0, modeSeparator);
            }

            if (!portraits.TryGetValue(lookup, out PortraitDefinition definition))
            {
                // Keep compatibility with the short display names in prototype CSV files.
                foreach (KeyValuePair<string, PortraitDefinition> pair in portraits)
                {
                    if (pair.Key.StartsWith(lookup, StringComparison.OrdinalIgnoreCase) ||
                        lookup.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = pair.Value;
                        lookup = pair.Key;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(lookup) || string.IsNullOrEmpty(definition.ResourceName))
            {
                speakerPortrait.gameObject.SetActive(false);
                return;
            }

            Texture2D texture = Resources.Load<Texture2D>($"Characters/{definition.ResourceName}");
            if (texture == null)
            {
                Debug.LogWarning($"No portrait texture found for speaker '{speaker}'.");
                speakerPortrait.gameObject.SetActive(false);
                return;
            }

            speakerPortrait.texture = texture;
            speakerPortrait.uvRect = definition.Crop;
            speakerPortraitAspect.aspectRatio =
                texture.width * definition.Crop.width / (texture.height * definition.Crop.height);
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

            if (IsBusy)
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
            speakerText.text = record.Speaker;
            lineText.text = record.TextKo;
            ShowPortrait(speaker.PortraitId);
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
                Wake.Core.GameStateManager.Instance?.ClearDialogueCheckpoint();
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
            if (ProductionPuzzleCatalog.TryGetByScene(
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
