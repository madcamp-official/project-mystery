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
        // "Next"/"Next (1)" (prev/next record) are deliberately excluded -
        // EvidencePanelController hides them permanently now that records
        // are browsed by scrolling the carousel, and this list's blanket
        // SetActive(true) on every tab switch would otherwise undo that.
        private static readonly string[] EvidenceContentNames =
        {
            "Evidences", "Image", "Text (TMP)", "Description",
            "Description Viewport", "Acquisition Place",
            "Related People", "Reliability",
            "Turn", "Turn (1)", "Turn (2)"
        };

        private GameObject tabs;
        private GameObject characters;
        private GameObject characterList;
        private GameObject characterDetail;
        private RectTransform characterCardsContent;
        private RawImage detailPortrait;
        private TMP_Text detailName;
        private TMP_Text detailRole;
        private TMP_Text detailTrust;
        private TMP_Text detailSummary;
        private TMP_Text detailNote;
        private TMP_Text detailUnknownMark;
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
                Transform viewport = panel.Find("Viewport");
                characterList = viewport?.gameObject;
                content = viewport?.Find("Content") as RectTransform;
                if (content != null && content.childCount > 0)
                {
                    characterCardsContent = content;
                    int existingVisibleCount = 0;
                    foreach (DialoguePortraitDefinition person in
                             DialoguePortraitCatalog.All)
                    {
                        if (!person.UsesExpressionSprites ||
                            !CharacterRelationshipProfileCatalog.TryGet(
                                person.CharacterId,
                                out CharacterRelationshipProfile profile))
                        {
                            continue;
                        }

                        Transform existingCard =
                            content.Find(person.CharacterId);
                        if (existingCard != null)
                        {
                            WirePersonCard(
                                existingCard.gameObject,
                                person);
                            ApplyPortraitDiscoveryState(
                                existingCard.gameObject,
                                profile);
                        }
                        else
                        {
                            CreatePersonCard(
                                content,
                                person,
                                existingVisibleCount,
                                profile);
                        }
                        existingVisibleCount++;
                    }
                    int existingRows =
                        Mathf.CeilToInt(existingVisibleCount / 3f);
                    content.sizeDelta = new Vector2(
                        0f,
                        Mathf.Max(
                            300f,
                            25f + existingRows * 325f));
                    BuildCharacterDetail(panel);
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
                characterList = viewportObject;
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

            characterCardsContent = content;
            IReadOnlyList<DialoguePortraitDefinition> people =
                DialoguePortraitCatalog.All;
            int visibleIndex = 0;
            foreach (DialoguePortraitDefinition person in people)
            {
                if (!person.UsesExpressionSprites)
                {
                    continue;
                }
                if (!CharacterRelationshipProfileCatalog.TryGet(
                        person.CharacterId,
                        out CharacterRelationshipProfile profile))
                {
                    continue;
                }
                CreatePersonCard(content, person, visibleIndex++, profile);
            }
            int rows = Mathf.CeilToInt(visibleIndex / 3f);
            content.sizeDelta = new Vector2(
                0f,
                Mathf.Max(300f, 25f + rows * 325f));
            BuildCharacterDetail(panel);
        }

        private void CreatePersonCard(
            RectTransform content,
            DialoguePortraitDefinition person,
            int index,
            CharacterRelationshipProfile profile)
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
                image.raycastTarget = false;

                ApplyPortraitDiscoveryState(card, profile);
            }
            int trust = GameStateManager.Instance?.GetTrust(person.CharacterId) ??
                        GameStateManager.DefaultTrust;
            SaveSlotSelectionController.MakeText(
                rect,
                $"{person.DisplayName}\n" +
                InterrogationRelationshipPresentation.ResolveTrust(trust),
                21f, new Vector2(0f, -115f), new Vector2(270f, 65f))
                .raycastTarget = false;
            WirePersonCard(card, person);
        }

        private void WirePersonCard(
            GameObject card,
            DialoguePortraitDefinition person)
        {
            Image image = card.GetComponent<Image>() ??
                          card.AddComponent<Image>();
            Button button = card.GetComponent<Button>() ??
                            card.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(1f, .88f, .62f, 1f);
            colors.pressedColor =
                new Color(.82f, .70f, .48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.RemoveAllListeners();
            string characterId = person.CharacterId;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                ShowCharacterDetail(characterId);
            });
            foreach (Graphic graphic in
                     card.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.gameObject != card)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        // Runs every time the tab opens (cards themselves are only ever
        // built once, guarded by `built`), so a character discovered
        // mid-session stops showing as a silhouette without needing a
        // scene reload.
        private void RefreshCharacterDiscoveryStates()
        {
            if (characterCardsContent == null)
            {
                return;
            }
            foreach (DialoguePortraitDefinition person in
                     DialoguePortraitCatalog.All)
            {
                if (!person.UsesExpressionSprites ||
                    !CharacterRelationshipProfileCatalog.TryGet(
                        person.CharacterId,
                        out CharacterRelationshipProfile profile))
                {
                    continue;
                }
                Transform card =
                    characterCardsContent.Find(person.CharacterId);
                if (card != null)
                {
                    ApplyPortraitDiscoveryState(card.gameObject, profile);
                }
            }
        }

        private static void ApplyPortraitDiscoveryState(
            GameObject card,
            CharacterRelationshipProfile profile)
        {
            Transform portraitTransform = card.transform.Find("Portrait");
            RawImage portraitImage =
                portraitTransform?.GetComponent<RawImage>();
            if (portraitTransform == null || portraitImage == null)
            {
                return;
            }

            bool discovered = profile.IsDiscovered(GameStateManager.Instance);
            // Cards can reach here from either CreatePersonCard (which
            // already builds this) or the "already exists in scene" path
            // in BuildCharacters (which never did) - creating it lazily
            // here means either origin ends up correct.
            Transform mark = portraitTransform.Find("Unknown Mark");
            if (mark == null)
            {
                TMP_Text created = SaveSlotSelectionController.MakeText(
                    portraitTransform as RectTransform,
                    "?",
                    96f,
                    Vector2.zero,
                    Vector2.zero);
                created.gameObject.name = "Unknown Mark";
                SaveSlotSelectionController.Stretch(created.rectTransform);
                created.alignment = TextAlignmentOptions.Center;
                created.color = Color.white;
                created.raycastTarget = false;
                mark = created.transform;
            }
            if (discovered)
            {
                DialoguePortraitAsset portrait = DialoguePortraitCatalog
                    .Resolve(profile.CharacterId, PortraitEmotion.Neutral);
                portraitImage.texture = portrait.Texture;
                portraitImage.uvRect = portrait.UvRect;
                portraitImage.color = Color.white;
            }
            else
            {
                portraitImage.texture = Texture2D.whiteTexture;
                portraitImage.uvRect = new Rect(0f, 0f, 1f, 1f);
                portraitImage.color = Color.black;
            }
            mark.gameObject.SetActive(!discovered);
        }

        private void BuildCharacterDetail(RectTransform panel)
        {
            Transform existing = panel.Find("Character Detail");
            if (existing != null)
            {
                characterDetail = existing.gameObject;
                detailPortrait = existing
                    .Find("Portrait Frame/Portrait")
                    ?.GetComponent<RawImage>();
                detailUnknownMark = existing
                    .Find("Portrait Frame/Unknown Mark")
                    ?.GetComponent<TMP_Text>();
                detailName = existing.Find("Name")?.GetComponent<TMP_Text>();
                detailRole = existing.Find("Role")?.GetComponent<TMP_Text>();
                detailTrust = existing.Find("Trust")?.GetComponent<TMP_Text>();
                detailSummary = existing.Find("Summary")?.GetComponent<TMP_Text>();
                detailNote = existing.Find("Investigation Note")?.GetComponent<TMP_Text>();
                characterDetail.SetActive(false);
                return;
            }

            characterDetail = SaveSlotSelectionController.Panel(
                panel,
                "Character Detail",
                new Color32(9, 24, 41, 252));
            RectTransform detailRect =
                characterDetail.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(detailRect);
            detailRect.offsetMin = new Vector2(25f, 25f);
            detailRect.offsetMax = new Vector2(-25f, -25f);

            GameObject portraitFrameObject = new(
                "Portrait Frame",
                typeof(RectTransform));
            portraitFrameObject.transform.SetParent(detailRect, false);
            RectTransform portraitFrame =
                portraitFrameObject.GetComponent<RectTransform>();
            SetAnchors(portraitFrame, .06f, .14f, .35f, .88f);

            GameObject portraitObject = new(
                "Portrait",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            portraitObject.transform.SetParent(portraitFrame, false);
            RectTransform portraitRect =
                portraitObject.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(portraitRect);
            detailPortrait = portraitObject.GetComponent<RawImage>();
            detailPortrait.raycastTarget = false;
            AspectRatioFitter fitter =
                portraitObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            detailUnknownMark = SaveSlotSelectionController.MakeText(
                portraitFrame, "?", 120f, Vector2.zero, Vector2.zero);
            detailUnknownMark.gameObject.name = "Unknown Mark";
            SaveSlotSelectionController.Stretch(
                detailUnknownMark.rectTransform);
            detailUnknownMark.alignment = TextAlignmentOptions.Center;
            detailUnknownMark.color = Color.white;
            detailUnknownMark.raycastTarget = false;

            detailName = MakeAnchoredText(
                detailRect,
                "Name",
                34f,
                .40f,
                .78f,
                .95f,
                .90f,
                TextAlignmentOptions.BottomLeft);
            detailRole = MakeAnchoredText(
                detailRect,
                "Role",
                21f,
                .40f,
                .68f,
                .95f,
                .77f,
                TextAlignmentOptions.TopLeft);
            detailTrust = MakeAnchoredText(
                detailRect,
                "Trust",
                21f,
                .40f,
                .57f,
                .95f,
                .67f,
                TextAlignmentOptions.TopLeft);
            detailSummary = MakeAnchoredText(
                detailRect,
                "Summary",
                21f,
                .40f,
                .33f,
                .95f,
                .55f,
                TextAlignmentOptions.TopLeft);
            detailNote = MakeAnchoredText(
                detailRect,
                "Investigation Note",
                19f,
                .40f,
                .13f,
                .95f,
                .31f,
                TextAlignmentOptions.TopLeft);
            TypographyService.Apply(
                detailName,
                TypographyRole.Heading);
            TypographyService.Apply(
                detailRole,
                TypographyRole.TechnicalStrong);
            TypographyService.Apply(
                detailTrust,
                TypographyRole.Technical);
            TypographyService.Apply(
                detailSummary,
                TypographyRole.Body);
            TypographyService.Apply(
                detailNote,
                TypographyRole.BodyRegular);

            Button close = SaveSlotSelectionController.MakeButton(
                detailRect,
                "Back To Character List",
                Vector2.zero,
                Vector2.zero);
            RectTransform closeRect =
                close.GetComponent<RectTransform>();
            SetAnchors(closeRect, .70f, .025f, .95f, .115f);
            TMP_Text closeLabel = SaveSlotSelectionController.MakeText(
                closeRect,
                "목록으로",
                21f,
                Vector2.zero,
                Vector2.zero);
            SaveSlotSelectionController.Stretch(closeLabel.rectTransform);
            closeLabel.raycastTarget = false;
            close.onClick.AddListener(ShowCharacterList);
            characterDetail.SetActive(false);
        }

        private void ShowCharacterDetail(string characterId)
        {
            if (!CharacterRelationshipProfileCatalog.TryGet(
                    characterId,
                    out CharacterRelationshipProfile profile) ||
                !DialoguePortraitCatalog.TryGet(
                    characterId,
                    out DialoguePortraitDefinition definition))
            {
                return;
            }

            GameStateManager state = GameStateManager.Instance;
            bool discovered = profile.IsDiscovered(state);
            DialoguePortraitAsset portrait =
                DialoguePortraitCatalog.Resolve(
                    characterId,
                    PortraitEmotion.Neutral);
            AspectRatioFitter fitter =
                detailPortrait.GetComponent<AspectRatioFitter>();
            if (discovered)
            {
                detailPortrait.texture = portrait.Texture;
                detailPortrait.uvRect = portrait.UvRect;
                detailPortrait.color = Color.white;
                if (fitter != null)
                {
                    fitter.aspectRatio = Mathf.Max(.1f, portrait.AspectRatio);
                }
            }
            else
            {
                detailPortrait.texture = Texture2D.whiteTexture;
                detailPortrait.uvRect = new Rect(0f, 0f, 1f, 1f);
                detailPortrait.color = Color.black;
                if (fitter != null)
                {
                    fitter.aspectRatio = 1f;
                }
            }
            if (detailUnknownMark == null)
            {
                Transform portraitFrame = detailPortrait.transform.parent;
                detailUnknownMark = SaveSlotSelectionController.MakeText(
                    portraitFrame as RectTransform,
                    "?",
                    120f,
                    Vector2.zero,
                    Vector2.zero);
                detailUnknownMark.gameObject.name = "Unknown Mark";
                SaveSlotSelectionController.Stretch(
                    detailUnknownMark.rectTransform);
                detailUnknownMark.alignment = TextAlignmentOptions.Center;
                detailUnknownMark.color = Color.white;
                detailUnknownMark.raycastTarget = false;
            }
            detailUnknownMark.gameObject.SetActive(!discovered);

            int trust = state?.GetTrust(characterId) ??
                        GameStateManager.DefaultTrust;
            detailName.text = definition.DisplayName;
            detailRole.text =
                $"{profile.Role}  ·  {profile.Affiliation}";
            detailTrust.text = string.Equals(
                characterId,
                "ADRIAN",
                System.StringComparison.OrdinalIgnoreCase)
                ? "관계 상태  ·  현재 수사를 진행 중"
                : $"신뢰도 {trust}/{GameStateManager.MaxTrust}  ·  " +
                  InterrogationRelationshipPresentation.ResolveTrust(trust);
            detailSummary.text = $"인물 정보\n{profile.Summary}";
            detailNote.text = profile.IsDiscovered(state)
                ? $"조사 메모\n{profile.KnownNote}"
                : "조사 메모\n아직 직접 만나 확인한 기록이 없습니다.";

            characterList?.SetActive(false);
            characterDetail?.SetActive(true);
            EnsureBackButtonAccessible();
        }

        private void ShowCharacterList()
        {
            characterDetail?.SetActive(false);
            characterList?.SetActive(true);
            EnsureBackButtonAccessible();
        }

        private void ShowEvidence()
        {
            SetEvidenceContent(true);
            ShowCharacterList();
            characters?.SetActive(false);
            EnsureBackButtonAccessible();
            // Re-apply the collected-only state after legacy objects are shown.
            Wake.Evidence.EvidencePanelController.Instance?.Refresh();
        }

        private void ShowCharacters()
        {
            SetEvidenceContent(false);
            characters?.SetActive(true);
            ShowCharacterList();
            RefreshCharacterDiscoveryStates();
            EnsureBackButtonAccessible();
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

        private void EnsureBackButtonAccessible()
        {
            Transform back = transform.Find("Back Btn");
            back?.SetAsLastSibling();
        }

        private static TMP_Text MakeAnchoredText(
            RectTransform parent,
            string name,
            float size,
            float minX,
            float minY,
            float maxX,
            float maxY,
            TextAlignmentOptions alignment)
        {
            TMP_Text text = SaveSlotSelectionController.MakeText(
                parent,
                string.Empty,
                size,
                Vector2.zero,
                Vector2.zero);
            text.gameObject.name = name;
            SetAnchors(
                text.rectTransform,
                minX,
                minY,
                maxX,
                maxY);
            text.alignment = alignment;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void SetAnchors(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
