using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Evidence
{
    public class EvidencePanelController : MonoBehaviour
    {
        public static EvidencePanelController Instance { get; private set; }

        private const float ItemSpacing = 180f;
        private const float SelectedScale = 1.15f;

        private readonly List<GameObject> spawnedItems = new();
        private Transform carouselContainer;
        private GameObject itemTemplate;
        private Image detailImage;
        private TMP_Text titleText;
        private TMP_Text detailText;
        private TMP_Text acquisitionText;
        private TMP_Text relatedPeopleText;
        private TMP_Text reliabilityText;
        private ScrollRect descriptionScroll;
        private EvidenceRecordDetailView detailView;
        private Button nextButton;
        private Button prevButton;
        private Button turnLeftButton;
        private Button turnRightButton;
        private Button theoryBoardButton;
        private Button backButton;
        private int selectedIndex;
        private int currentViewIndex;
        private Transform evidenceRoot;
        private EvidencePanelViewModel viewModel =
            new(Array.Empty<EvidencePanelItem>(), 0, 0);

        private void Awake()
        {
            Instance = this;
            evidenceRoot = FindAuthoredEvidenceRoot();
            if (evidenceRoot == null)
            {
                Debug.LogError(
                    "EvidencePanelController could not find the authored " +
                    "Canvas/Evidence root.");
                enabled = false;
                return;
            }

            carouselContainer = evidenceRoot.Find("Evidences");
            itemTemplate = carouselContainer?.Find("Evedence")?.gameObject;
            if (carouselContainer == null || itemTemplate == null)
            {
                Debug.LogError(
                    "EvidencePanelController requires the authored carousel.");
                enabled = false;
                return;
            }

            ClearLegacyCarouselItems();
            ConfigureCarousel();
            if (!BindAuthoredDetail())
            {
                enabled = false;
                return;
            }
            BindButtons();
            ApplyTypography();
        }

        private static Transform FindAuthoredEvidenceRoot()
        {
            // 이 컨트롤러는 GameSystems에 있고 UI는 별도 Canvas에 있다.
            // additive 테스트나 화면 전환 중에는 같은 이름의 Canvas가
            // 공존할 수 있으므로 활성 오브젝트 하나만 찾지 말고,
            // authored 상세 영역을 실제로 가진 Evidence를 선택한다.
            Transform[] transforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name != "Evidence" ||
                    candidate.parent == null ||
                    candidate.parent.name != "Canvas")
                {
                    continue;
                }
                if (candidate.Find("Image") != null &&
                    candidate.Find("Text (TMP)") != null &&
                    candidate.Find("Description Viewport/Description") != null)
                {
                    return candidate;
                }
            }
            return null;
        }

        private void ClearLegacyCarouselItems()
        {
            for (int index = carouselContainer.childCount - 1;
                 index >= 0;
                 index--)
            {
                Transform child = carouselContainer.GetChild(index);
                if (child.gameObject != itemTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
            itemTemplate.SetActive(false);
        }

        private void ConfigureCarousel()
        {
            if (carouselContainer.GetComponent<RectMask2D>() == null)
            {
                carouselContainer.gameObject.AddComponent<RectMask2D>();
            }
            GridLayoutGroup grid =
                carouselContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                DestroyImmediate(grid);
            }
        }

        private bool BindAuthoredDetail()
        {
            detailImage = evidenceRoot.Find("Image")?.GetComponent<Image>();
            titleText = evidenceRoot.Find("Text (TMP)")
                ?.GetComponent<TMP_Text>();
            Transform descriptionTransform =
                evidenceRoot.Find(
                    "Description Viewport/Description") ??
                evidenceRoot.Find("Description") ??
                evidenceRoot.Find("Image/Evidence");
            detailText = descriptionTransform?.GetComponent<TMP_Text>();
            acquisitionText = evidenceRoot.Find("Acquisition Place")
                ?.GetComponent<TMP_Text>();
            relatedPeopleText = evidenceRoot.Find("Related People")
                ?.GetComponent<TMP_Text>();
            reliabilityText = evidenceRoot.Find("Reliability")
                ?.GetComponent<TMP_Text>();
            descriptionScroll = descriptionTransform
                ?.GetComponentInParent<ScrollRect>(true);
            if (detailImage == null ||
                titleText == null ||
                detailText == null ||
                acquisitionText == null ||
                relatedPeopleText == null ||
                reliabilityText == null ||
                descriptionScroll == null)
            {
                Debug.LogError(
                    "EvidencePanelController requires the authored record " +
                    "image, title, metadata and description viewport.");
                return false;
            }

            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.enableAutoSizing = false;
            detailText.fontSize = 28f;
            detailText.overflowMode = TextOverflowModes.Overflow;
            detailView =
                evidenceRoot.GetComponent<EvidenceRecordDetailView>() ??
                evidenceRoot.gameObject
                    .AddComponent<EvidenceRecordDetailView>();
            detailView.Configure(
                detailImage,
                titleText,
                acquisitionText,
                relatedPeopleText,
                reliabilityText,
                detailText,
                descriptionScroll);
            return true;
        }

        private void BindButtons()
        {
            nextButton = RequireButton("Next");
            prevButton = RequireButton("Next (1)");
            backButton = RequireButton("Back Btn");
            turnLeftButton = RequireButton("Turn");
            turnRightButton = RequireButton("Turn (1)");
            theoryBoardButton = RequireButton("Turn (2)");
            Transform obsolete = evidenceRoot.Find("Turn (3)");
            if (obsolete != null)
            {
                obsolete.gameObject.SetActive(false);
            }
            if (nextButton == null ||
                prevButton == null ||
                backButton == null ||
                turnLeftButton == null ||
                turnRightButton == null ||
                theoryBoardButton == null)
            {
                enabled = false;
                return;
            }

            SetButtonLabel(theoryBoardButton, "기록 비교");
            nextButton.onClick.AddListener(() => Advance(1));
            prevButton.onClick.AddListener(() => Advance(-1));
            backButton.onClick.AddListener(
                () => UIManager.Instance?.ShowIngame());
            turnLeftButton.onClick.AddListener(() => Rotate(-1));
            turnRightButton.onClick.AddListener(() => Rotate(1));
            theoryBoardButton.onClick.AddListener(OpenTheoryBoard);
        }

        private Button RequireButton(string name)
        {
            Button button = evidenceRoot.Find(name)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError(
                    $"EvidencePanelController is missing button '{name}'.");
            }
            return button;
        }

        private static void SetButtonLabel(Button button, string text)
        {
            TMP_Text label =
                button?.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private void ApplyTypography()
        {
            EvidenceTypography.ApplySurface(
                evidenceRoot,
                titleText,
                detailText,
                theoryBoardButton?.GetComponentInChildren<TMP_Text>(true));
            TypographyService.Apply(
                acquisitionText,
                TypographyRole.BodyRegular);
            TypographyService.Apply(
                relatedPeopleText,
                TypographyRole.BodyRegular);
            TypographyService.Apply(
                reliabilityText,
                TypographyRole.BodyRegular);
        }

        private void OpenTheoryBoard()
        {
            evidenceRoot
                .GetComponent<EvidenceTheoryBoardController>()
                ?.Open();
        }

        public void Refresh() => Refresh(null);

        public void Refresh(string preferredEvidenceId)
        {
            if (!enabled)
            {
                return;
            }
            string selectedId = GetSelectedItem()?.Id;
            viewModel = EvidencePanelPresentation.Create(
                EvidenceInventory.Instance,
                Wake.Core.GameStateManager.Instance?.EvidenceIntegrity ?? 100);
            string targetId = string.IsNullOrEmpty(preferredEvidenceId)
                ? selectedId
                : preferredEvidenceId;
            int restoredIndex = string.IsNullOrEmpty(targetId)
                ? selectedIndex
                : FindIndex(targetId);
            selectedIndex = Mathf.Clamp(
                restoredIndex,
                0,
                Mathf.Max(0, viewModel.Items.Count - 1));
            currentViewIndex = 0;
            RebuildCarousel();
            ApplySelection();
        }

        private int FindIndex(string evidenceId)
        {
            for (int index = 0; index < viewModel.Items.Count; index++)
            {
                if (viewModel.Items[index].Id == evidenceId)
                {
                    return index;
                }
            }
            return 0;
        }

        private void RebuildCarousel()
        {
            foreach (GameObject item in spawnedItems)
            {
                Destroy(item);
            }
            spawnedItems.Clear();
            for (int index = 0; index < viewModel.Items.Count; index++)
            {
                GameObject instance =
                    Instantiate(itemTemplate, carouselContainer);
                instance.SetActive(true);
                TMP_Text label = instance.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = viewModel.Items[index].CarouselLabel;
                    EvidenceTypography.ApplyCarouselLabel(label);
                }
                ApplyItemState(
                    instance.GetComponent<Image>(),
                    viewModel.Items[index].State);
                int capturedIndex = index;
                Button button = instance.GetComponent<Button>();
                button?.onClick.AddListener(
                    () => SelectIndex(capturedIndex));
                spawnedItems.Add(instance);
            }
            PositionCarouselItems();
        }

        private static void ApplyItemState(
            Graphic background,
            EvidencePanelItemState state)
        {
            if (background == null)
            {
                return;
            }
            background.color = state switch
            {
                EvidencePanelItemState.Collected =>
                    UiVisualThemeService.Resolve(UiColorToken.Success),
                EvidencePanelItemState.Unreliable =>
                    UiVisualThemeService.Resolve(UiColorToken.Danger),
                _ => UiVisualThemeService.Resolve(
                    UiColorToken.SurfaceRaised)
            };
        }

        private void PositionCarouselItems()
        {
            for (int index = 0; index < spawnedItems.Count; index++)
            {
                RectTransform rect =
                    spawnedItems[index].GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax =
                    rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition =
                    new Vector2((index - selectedIndex) * ItemSpacing, 0f);
                rect.localScale = Vector3.one *
                    (index == selectedIndex ? SelectedScale : 1f);
            }
        }

        private void Advance(int delta)
        {
            if (viewModel.Items.Count > 0)
            {
                SelectIndex(Mathf.Clamp(
                    selectedIndex + delta,
                    0,
                    viewModel.Items.Count - 1));
            }
        }

        private void SelectIndex(int index)
        {
            if (viewModel.Items.Count == 0)
            {
                return;
            }
            selectedIndex = Mathf.Clamp(
                index,
                0,
                viewModel.Items.Count - 1);
            currentViewIndex = 0;
            PositionCarouselItems();
            ApplySelection();
        }

        private void Rotate(int delta)
        {
            EvidenceDefinition evidence = GetSelectedItem()?.Definition;
            if (evidence?.Views == null || evidence.Views.Length == 0)
            {
                return;
            }
            currentViewIndex =
                (currentViewIndex + delta + evidence.Views.Length) %
                evidence.Views.Length;
            ApplyView(evidence);
        }

        private void ApplySelection()
        {
            EvidencePanelItem? selected = GetSelectedItem();
            detailView.Bind(selected);
            if (selected.HasValue)
            {
                EvidencePanelItem item = selected.Value;
                EvidenceTypography.ApplyDetail(
                    detailText,
                    item.Entry.Category);
                if (item.HasImage)
                {
                    ApplyView(item.Definition);
                }
            }

            prevButton.interactable = selectedIndex > 0;
            nextButton.interactable =
                selectedIndex < viewModel.Items.Count - 1;
            EvidenceDefinition evidence = selected?.Definition;
            bool hasMultipleViews =
                evidence?.Views != null && evidence.Views.Length > 1;
            turnLeftButton.gameObject.SetActive(hasMultipleViews);
            turnRightButton.gameObject.SetActive(hasMultipleViews);
        }

        private void ApplyView(EvidenceDefinition evidence)
        {
            Sprite sprite = evidence.Views != null &&
                            evidence.Views.Length > 0
                ? evidence.Views[currentViewIndex]
                : null;
            detailView.SetImage(sprite);
        }

        private EvidencePanelItem? GetSelectedItem()
        {
            if (selectedIndex < 0 ||
                selectedIndex >= viewModel.Items.Count)
            {
                return null;
            }
            return viewModel.Items[selectedIndex];
        }
    }
}
