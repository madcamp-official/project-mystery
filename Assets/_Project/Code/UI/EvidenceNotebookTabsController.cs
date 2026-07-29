using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class EvidenceNotebookTabsController : MonoBehaviour
    {
        private static readonly string[] EvidenceContentNames =
        {
            "Evidences", "Image", "Text (TMP)", "Description",
            "Description Viewport", "Acquisition Place",
            "Related People", "Reliability",
            "Next", "Next (1)", "Turn", "Turn (1)", "Turn (2)"
        };

        private GameObject tabs;
        private GameObject characters;
        private bool built;

        private void Start()
        {
            Build();
            ShowEvidence();
        }

        private void OnEnable()
        {
            if (built)
            {
                ShowEvidence();
            }
        }

        private void Build()
        {
            if (built)
            {
                return;
            }
            built = true;
            RectTransform root = transform as RectTransform;

            Transform existingTabs = root.Find("Notebook Tabs");
            RectTransform tabsRect;
            if (existingTabs != null)
            {
                tabs = existingTabs.gameObject;
                tabsRect = tabs.GetComponent<RectTransform>();
            }
            else
            {
                tabs = new GameObject("Notebook Tabs", typeof(RectTransform));
                tabs.transform.SetParent(root, false);
                tabsRect = tabs.GetComponent<RectTransform>();
                if (!RuntimeUiLayoutRegistry.CopyWorldLayout(
                        tabsRect,
                        "evidence.tabs"))
                {
                    Debug.LogError(
                        "Evidence notebook is missing the authored tabs slot.");
                }

                Button evidence = SaveSlotSelectionController.MakeButton(
                    tabsRect, "Evidence Tab", new Vector2(-155f, 0f), new Vector2(290f, 54f));
                SaveSlotSelectionController.MakeText(
                    evidence.transform as RectTransform, "조사 기록", 24f,
                    Vector2.zero, new Vector2(250f, 44f));
                Button people = SaveSlotSelectionController.MakeButton(
                    tabsRect, "Characters Tab", new Vector2(155f, 0f), new Vector2(290f, 54f));
                SaveSlotSelectionController.MakeText(
                    people.transform as RectTransform, "인물 · 관계", 24f,
                    Vector2.zero, new Vector2(250f, 44f));
            }

            WireTabButtons(tabsRect);
            BuildCharacters(root);
        }

        private void WireTabButtons(RectTransform tabsRect)
        {
            Button evidence = tabsRect.Find("Evidence Tab")?.GetComponent<Button>();
            if (evidence != null)
            {
                evidence.onClick.RemoveListener(ShowEvidence);
                evidence.onClick.AddListener(ShowEvidence);
            }
            Button people = tabsRect.Find("Characters Tab")?.GetComponent<Button>();
            if (people != null)
            {
                people.onClick.RemoveListener(ShowCharacters);
                people.onClick.AddListener(ShowCharacters);
            }
        }

        private void BuildCharacters(RectTransform root)
        {
            Transform existingPanel = root.Find("Characters And Relationships");
            RectTransform panel;
            RectTransform content;
            if (existingPanel != null)
            {
                characters = existingPanel.gameObject;
                panel = existingPanel.GetComponent<RectTransform>();
                RuntimeUiLayoutRegistry.CopyWorldLayout(panel, "evidence.people-panel");
                content = panel.Find("Viewport/Content") as RectTransform;
                if (content != null && content.childCount > 0)
                {
                    return;
                }
            }
            else
            {
                characters = SaveSlotSelectionController.Panel(
                    root, "Characters And Relationships", new Color32(5, 15, 29, 235));
                panel = characters.GetComponent<RectTransform>();
                if (!RuntimeUiLayoutRegistry.CopyWorldLayout(
                        panel,
                        "evidence.people-panel"))
                {
                    Debug.LogError(
                        "Evidence notebook is missing the authored people slot.");
                }

                GameObject viewportObject = new(
                    "Viewport", typeof(RectTransform), typeof(Image),
                    typeof(RectMask2D), typeof(ScrollRect));
                viewportObject.transform.SetParent(panel, false);
                RectTransform viewport = viewportObject.GetComponent<RectTransform>();
                SaveSlotSelectionController.Stretch(viewport);
                viewport.offsetMin = new Vector2(25f, 25f);
                viewport.offsetMax = new Vector2(-25f, -25f);
                viewportObject.GetComponent<Image>().color = new Color(0, 0, 0, .08f);

                GameObject contentObject = new("Content", typeof(RectTransform));
                contentObject.transform.SetParent(viewport, false);
                content = contentObject.GetComponent<RectTransform>();
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(.5f, 1f);
                content.sizeDelta = new Vector2(0f, 1020f);
                ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.content = content;
                scroll.horizontal = false;
                scroll.vertical = true;
            }

            IReadOnlyList<DialoguePortraitDefinition> people =
                DialoguePortraitCatalog.All;
            int visibleIndex = 0;
            foreach (DialoguePortraitDefinition person in people)
            {
                if (!person.UsesExpressionSprites)
                {
                    continue;
                }
                CreatePersonCard(content, person, visibleIndex++);
            }
        }

        private static void CreatePersonCard(
            RectTransform content,
            DialoguePortraitDefinition person,
            int index)
        {
            int column = index % 3;
            int row = index / 3;
            GameObject card = SaveSlotSelectionController.Panel(
                content, person.CharacterId, new Color32(14, 34, 56, 245));
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(300f, 300f);
            rect.anchoredPosition =
                new Vector2((column - 1) * 330f, -165f - row * 325f);
            card.AddComponent<Outline>().effectColor = new Color32(183, 137, 60, 255);

            DialoguePortraitAsset portrait =
                DialoguePortraitCatalog.Resolve(person.CharacterId, PortraitEmotion.Neutral);
            if (portrait.Found)
            {
                GameObject imageObject = new(
                    "Portrait", typeof(RectTransform), typeof(RawImage));
                imageObject.transform.SetParent(rect, false);
                RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.anchorMin = new Vector2(.15f, .30f);
                imageRect.anchorMax = new Vector2(.85f, .94f);
                imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;
                RawImage image = imageObject.GetComponent<RawImage>();
                image.texture = portrait.Texture;
                image.uvRect = portrait.UvRect;
                image.raycastTarget = false;
            }
            int trust = GameStateManager.Instance?.GetTrust(person.CharacterId) ??
                        GameStateManager.DefaultTrust;
            SaveSlotSelectionController.MakeText(
                rect,
                $"{person.DisplayName}\n" +
                InterrogationRelationshipPresentation.ResolveTrust(trust),
                21f, new Vector2(0f, -115f), new Vector2(270f, 65f));
        }

        private void ShowEvidence()
        {
            SetEvidenceContent(true);
            characters?.SetActive(false);
            // Re-apply the collected-only state after legacy objects are shown.
            Wake.Evidence.EvidencePanelController.Instance?.Refresh();
        }

        private void ShowCharacters()
        {
            SetEvidenceContent(false);
            characters?.SetActive(true);
        }

        private void SetEvidenceContent(bool visible)
        {
            foreach (string childName in EvidenceContentNames)
            {
                Transform child = transform.Find(childName);
                if (child != null)
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }
    }
}
