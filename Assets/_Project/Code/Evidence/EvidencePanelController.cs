using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

// Layout Y reference (Evidence panel is center-anchored on the full canvas):
// Status HUD bottom edge sits at local y=+89, carousel row at y=-195 -
// everything between must stay inside that band or it renders under/behind the HUD.

namespace Wake.Evidence
{
    public class EvidencePanelController : MonoBehaviour
    {
        public static EvidencePanelController Instance { get; private set; }

        private const float ItemSpacing = 100f;
        private const float SelectedScale = 1.25f;

        private Transform carouselContainer;
        private GameObject itemTemplate;
        private readonly List<GameObject> spawnedItems = new();

        private Image detailImage;
        private TMP_Text titleText;
        private TMP_Text detailText;

        private Button nextButton;
        private Button prevButton;
        private Button turnLeftButton;
        private Button turnRightButton;
        private Button backButton;

        private int selectedIndex;
        private int currentViewIndex;
        private Transform evidenceRoot;

        private void Awake()
        {
            Instance = this;

            Transform canvas = GameObject.Find("Canvas").transform;
            evidenceRoot = canvas.Find("Evidence");

            carouselContainer = evidenceRoot.Find("Evidences");
            itemTemplate = carouselContainer.Find("Evedence").gameObject;

            for (int i = carouselContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = carouselContainer.GetChild(i);
                if (child.gameObject != itemTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
            itemTemplate.SetActive(false);

            RectMask2D mask = carouselContainer.GetComponent<RectMask2D>();
            if (mask == null)
            {
                carouselContainer.gameObject.AddComponent<RectMask2D>();
            }

            GridLayoutGroup gridLayout = carouselContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                DestroyImmediate(gridLayout);
            }

            GameObject imageRoot = evidenceRoot.Find("Image").gameObject;
            detailImage = imageRoot.GetComponent<Image>();

            titleText = evidenceRoot.Find("Text (TMP)").GetComponent<TMP_Text>();

            Transform descriptionTransform = evidenceRoot.Find("Image/Evidence");
            descriptionTransform.SetParent(evidenceRoot, false);
            descriptionTransform.name = "Description";
            detailText = descriptionTransform.GetComponent<TMP_Text>();
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.enableAutoSizing = true;
            detailText.fontSizeMin = 14f;
            detailText.fontSizeMax = 28f;
            detailText.overflowMode = TextOverflowModes.Truncate;

            TMP_FontAsset koreanFont = StatusHUDController.RuntimeKoreanFont;
            if (koreanFont != null)
            {
                titleText.font = koreanFont;
                detailText.font = koreanFont;
            }

            LayoutRects();

            nextButton = evidenceRoot.Find("Next").GetComponent<Button>();
            prevButton = evidenceRoot.Find("Next (1)").GetComponent<Button>();
            backButton = evidenceRoot.Find("Back Btn").GetComponent<Button>();
            turnLeftButton = evidenceRoot.Find("Turn").GetComponent<Button>();
            turnRightButton = evidenceRoot.Find("Turn (1)").GetComponent<Button>();
            evidenceRoot.Find("Turn (2)").gameObject.SetActive(false);
            evidenceRoot.Find("Turn (3)").gameObject.SetActive(false);

            nextButton.onClick.AddListener(() => Advance(1));
            prevButton.onClick.AddListener(() => Advance(-1));
            backButton.onClick.AddListener(() => UIManager.Instance.ShowIngame());
            turnLeftButton.onClick.AddListener(() => Rotate(-1));
            turnRightButton.onClick.AddListener(() => Rotate(1));
        }

        private void LayoutRects()
        {
            const float CarouselY = -195f;

            SetRect(detailImage.rectTransform, new Vector2(-160f, -30f), new Vector2(240f, 160f));
            SetRect(titleText.rectTransform, new Vector2(180f, 60f), new Vector2(260f, 44f));
            titleText.fontSize = 32f;
            titleText.alignment = TextAlignmentOptions.TopLeft;

            SetRect(detailText.rectTransform, new Vector2(180f, -30f), new Vector2(260f, 160f));

            SetRect(carouselContainer.GetComponent<RectTransform>(), new Vector2(0f, CarouselY), new Vector2(600f, 110f));

            RectTransform turnLeftRect = (RectTransform)evidenceRoot.Find("Turn");
            SetRect(turnLeftRect, new Vector2(-160f, -135f), turnLeftRect.sizeDelta);
            RectTransform turnRightRect = (RectTransform)evidenceRoot.Find("Turn (1)");
            SetRect(turnRightRect, new Vector2(-160f, 75f), turnRightRect.sizeDelta);

            RectTransform prevRect = (RectTransform)evidenceRoot.Find("Next (1)");
            SetRect(prevRect, new Vector2(-360f, CarouselY), prevRect.sizeDelta);
            RectTransform nextRect = (RectTransform)evidenceRoot.Find("Next");
            SetRect(nextRect, new Vector2(360f, CarouselY), nextRect.sizeDelta);
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        public void Refresh()
        {
            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, collected.Count - 1));
            currentViewIndex = 0;
            RebuildCarousel();
            ApplySelection();
        }

        private void RebuildCarousel()
        {
            foreach (GameObject item in spawnedItems)
            {
                Destroy(item);
            }
            spawnedItems.Clear();

            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;
            for (int i = 0; i < collected.Count; i++)
            {
                GameObject instance = Instantiate(itemTemplate, carouselContainer);
                instance.SetActive(true);

                TMP_Text label = instance.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = collected[i].DisplayName;
                    TMP_FontAsset koreanFont = StatusHUDController.RuntimeKoreanFont;
                    if (koreanFont != null)
                    {
                        label.font = koreanFont;
                    }
                }

                int capturedIndex = i;
                Button itemButton = instance.GetComponent<Button>();
                if (itemButton != null)
                {
                    itemButton.onClick.AddListener(() => SelectIndex(capturedIndex));
                }
                spawnedItems.Add(instance);
            }

            PositionCarouselItems();
        }

        private void PositionCarouselItems()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                RectTransform rect = spawnedItems[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2((i - selectedIndex) * ItemSpacing, 0f);
                bool isSelected = i == selectedIndex;
                rect.localScale = Vector3.one * (isSelected ? SelectedScale : 1f);
            }
        }

        private void Advance(int delta)
        {
            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;
            if (collected.Count == 0)
            {
                return;
            }

            SelectIndex(Mathf.Clamp(selectedIndex + delta, 0, collected.Count - 1));
        }

        private void SelectIndex(int index)
        {
            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;
            if (collected.Count == 0)
            {
                return;
            }

            selectedIndex = Mathf.Clamp(index, 0, collected.Count - 1);
            currentViewIndex = 0;
            PositionCarouselItems();
            ApplySelection();
        }

        private void Rotate(int delta)
        {
            EvidenceDefinition evidence = GetSelectedEvidence();
            if (evidence == null || evidence.Views == null || evidence.Views.Length == 0)
            {
                return;
            }

            int count = evidence.Views.Length;
            currentViewIndex = (currentViewIndex + delta + count) % count;
            ApplyView(evidence);
        }

        private void ApplySelection()
        {
            EvidenceDefinition evidence = GetSelectedEvidence();
            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;

            bool hasEvidence = evidence != null;
            detailImage.gameObject.SetActive(hasEvidence);
            titleText.gameObject.SetActive(hasEvidence);
            detailText.gameObject.SetActive(hasEvidence);

            if (hasEvidence)
            {
                titleText.text = evidence.DisplayName;
                ApplyView(evidence);
            }
            else
            {
                titleText.text = string.Empty;
                detailText.text = "확보한 증거가 없습니다.";
            }

            prevButton.interactable = selectedIndex > 0;
            nextButton.interactable = selectedIndex < collected.Count - 1;
            bool hasMultipleViews = evidence != null && evidence.Views != null && evidence.Views.Length > 1;
            turnLeftButton.gameObject.SetActive(hasMultipleViews);
            turnRightButton.gameObject.SetActive(hasMultipleViews);
        }

        private void ApplyView(EvidenceDefinition evidence)
        {
            if (evidence.Views != null && evidence.Views.Length > 0)
            {
                detailImage.sprite = evidence.Views[currentViewIndex];
            }

            detailText.text = evidence.Description;
        }

        private EvidenceDefinition GetSelectedEvidence()
        {
            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;
            if (selectedIndex < 0 || selectedIndex >= collected.Count)
            {
                return null;
            }

            return collected[selectedIndex];
        }
    }
}
