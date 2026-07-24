using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;

namespace Wake.Narrative
{
    public class DialogueController : MonoBehaviour
    {
        public static DialogueController Instance { get; private set; }

        private static readonly string[] ChoiceObjectNames =
        {
            "Choice", "Choice (1)", "Choice (2)", "Choice (3)"
        };

        private GameObject linePanel;
        private TMP_Text lineText;
        private TMP_Text speakerText;
        private Button nextButton;
        private GameObject choicesContainer;
        private Button[] choiceButtons;
        private TMP_Text[] choiceLabels;

        private DialogueSet currentSet;
        private DialogueNode currentNode;

        public bool IsBusy { get; private set; }

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

            Transform selectBtn = linePanelTransform.Find("Select Btn");
            choicesContainer = selectBtn.gameObject;

            choiceButtons = new Button[ChoiceObjectNames.Length];
            choiceLabels = new TMP_Text[ChoiceObjectNames.Length];
            for (int i = 0; i < ChoiceObjectNames.Length; i++)
            {
                Transform choiceTransform = selectBtn.Find(ChoiceObjectNames[i]);
                choiceButtons[i] = choiceTransform.GetComponent<Button>();
                choiceLabels[i] = choiceTransform.GetComponentInChildren<TMP_Text>();
            }

            nextButton.onClick.AddListener(OnNextClicked);
            linePanel.SetActive(false);
        }

        public void StartDialogue(DialogueSet dialogueSet)
        {
            if (dialogueSet == null)
            {
                return;
            }

            currentSet = dialogueSet;
            IsBusy = true;
            linePanel.SetActive(true);
            GoToNode(dialogueSet.StartNodeId);
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
            }
            else
            {
                speakerText.text = string.Empty;
                lineText.text = $"[MISSING LINE: {currentNode.LineId}]";
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
                choiceButtons[i].onClick.AddListener(() => AdvanceToOption(option));
            }
        }

        private void OnNextClicked()
        {
            if (currentNode == null || currentNode.Options == null || currentNode.Options.Count == 0)
            {
                EndDialogue();
                return;
            }

            AdvanceToOption(currentNode.Options[0]);
        }

        private void AdvanceToOption(DialogueOption option)
        {
            GameStateManager.Instance?.ApplyChoiceEffects(
                option.TargetCharacterId, option.TrustDelta, option.AnxietyDelta, option.IntegrityDelta);
            GoToNode(option.NextNodeId);
        }

        private void EndDialogue()
        {
            IsBusy = false;
            currentSet = null;
            currentNode = null;
            if (linePanel != null)
            {
                linePanel.SetActive(false);
            }
        }
    }
}
