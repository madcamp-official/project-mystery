using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Seat0A.UI;

namespace Seat0A.Evidence
{
    public class EvidencePanelController : MonoBehaviour
    {
        public static EvidencePanelController Instance { get; private set; }

        private const int PageSize = 6;

        private Transform evidencesContainer;
        private GameObject itemTemplate;
        private readonly List<GameObject> spawnedItems = new();

        private Image detailImage;
        private TMP_Text detailText;
        private GameObject detailRoot;
        private GameObject listRoot;

        private Button turnLeftButton;
        private Button turnRightButton;
        private Button nextPageButton;
        private Button prevPageButton;
        private Button backButton;

        private int currentPage;
        private EvidenceDefinition selectedEvidence;
        private int currentViewIndex;
        private bool detailOpen;

        private void Awake()
        {
            Instance = this;

            Transform canvas = GameObject.Find("Canvas").transform;
            Transform evidenceRoot = canvas.Find("Evidence");

            evidencesContainer = evidenceRoot.Find("Evidences");
            itemTemplate = evidencesContainer.Find("Evedence").gameObject;

            for (int i = evidencesContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = evidencesContainer.GetChild(i);
                if (child.gameObject != itemTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
            itemTemplate.SetActive(false);

            listRoot = evidencesContainer.gameObject;
            detailRoot = evidenceRoot.Find("Image").gameObject;
            detailImage = detailRoot.GetComponent<Image>();
            detailText = evidenceRoot.Find("Image/Evidence").GetComponent<TMP_Text>();

            nextPageButton = evidenceRoot.Find("Next").GetComponent<Button>();
            prevPageButton = evidenceRoot.Find("Next (1)").GetComponent<Button>();
            backButton = evidenceRoot.Find("Back Btn").GetComponent<Button>();

            turnLeftButton = evidenceRoot.Find("Turn").GetComponent<Button>();
            turnRightButton = evidenceRoot.Find("Turn (1)").GetComponent<Button>();
            evidenceRoot.Find("Turn (2)").gameObject.SetActive(false);
            evidenceRoot.Find("Turn (3)").gameObject.SetActive(false);

            nextPageButton.onClick.AddListener(() => ChangePage(1));
            prevPageButton.onClick.AddListener(() => ChangePage(-1));
            backButton.onClick.AddListener(OnBackClicked);
            turnLeftButton.onClick.AddListener(() => Rotate(-1));
            turnRightButton.onClick.AddListener(() => Rotate(1));

            CloseDetail();
        }

        public void Refresh()
        {
            currentPage = 0;
            CloseDetail();
            RenderPage();
        }

        private void RenderPage()
        {
            foreach (GameObject item in spawnedItems)
            {
                Destroy(item);
            }
            spawnedItems.Clear();

            IReadOnlyList<EvidenceDefinition> collected = EvidenceInventory.Instance.Collected;
            int start = currentPage * PageSize;
            int end = Mathf.Min(start + PageSize, collected.Count);

            for (int i = start; i < end; i++)
            {
                EvidenceDefinition evidence = collected[i];
                GameObject instance = Instantiate(itemTemplate, evidencesContainer);
                instance.SetActive(true);

                TMP_Text label = instance.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = evidence.DisplayName;
                }

                instance.GetComponent<Button>().onClick.AddListener(() => OpenDetail(evidence));
                spawnedItems.Add(instance);
            }

            prevPageButton.interactable = currentPage > 0;
            nextPageButton.interactable = end < collected.Count;
        }

        private void ChangePage(int delta)
        {
            currentPage = Mathf.Max(0, currentPage + delta);
            RenderPage();
        }

        private void OpenDetail(EvidenceDefinition evidence)
        {
            selectedEvidence = evidence;
            currentViewIndex = 0;
            detailOpen = true;
            listRoot.SetActive(false);
            detailRoot.SetActive(true);
            nextPageButton.gameObject.SetActive(false);
            prevPageButton.gameObject.SetActive(false);
            turnLeftButton.gameObject.SetActive(true);
            turnRightButton.gameObject.SetActive(true);
            ApplyView();
        }

        private void Rotate(int delta)
        {
            if (selectedEvidence == null || selectedEvidence.Views == null || selectedEvidence.Views.Length == 0)
            {
                return;
            }

            int count = selectedEvidence.Views.Length;
            currentViewIndex = (currentViewIndex + delta + count) % count;
            ApplyView();
        }

        private void ApplyView()
        {
            if (selectedEvidence == null)
            {
                return;
            }

            if (selectedEvidence.Views != null && selectedEvidence.Views.Length > 0)
            {
                detailImage.sprite = selectedEvidence.Views[currentViewIndex];
            }

            detailText.text = selectedEvidence.Description;
        }

        private void CloseDetail()
        {
            detailOpen = false;
            selectedEvidence = null;
            if (detailRoot != null)
            {
                detailRoot.SetActive(false);
            }
            if (listRoot != null)
            {
                listRoot.SetActive(true);
            }
            if (nextPageButton != null)
            {
                nextPageButton.gameObject.SetActive(true);
                prevPageButton.gameObject.SetActive(true);
            }
            if (turnLeftButton != null)
            {
                turnLeftButton.gameObject.SetActive(false);
                turnRightButton.gameObject.SetActive(false);
            }
        }

        private void OnBackClicked()
        {
            if (detailOpen)
            {
                CloseDetail();
                RenderPage();
            }
            else
            {
                UIManager.Instance.ShowIngame();
            }
        }
    }
}
