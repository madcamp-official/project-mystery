using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.Evidence
{
    [DisallowMultipleComponent]
    public sealed class EvidenceRecordDetailView : MonoBehaviour
    {
        [SerializeField] private Image recordImage;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text acquisitionPlace;
        [SerializeField] private TMP_Text relatedPeople;
        [SerializeField] private TMP_Text reliability;
        [SerializeField] private TMP_Text description;
        [SerializeField] private ScrollRect descriptionScroll;

        public void Configure(
            Image image,
            TMP_Text titleLabel,
            TMP_Text acquisitionLabel,
            TMP_Text relatedPeopleLabel,
            TMP_Text reliabilityLabel,
            TMP_Text descriptionLabel,
            ScrollRect scroll)
        {
            recordImage = image;
            title = titleLabel;
            acquisitionPlace = acquisitionLabel;
            relatedPeople = relatedPeopleLabel;
            reliability = reliabilityLabel;
            description = descriptionLabel;
            descriptionScroll = scroll;
        }

        public void Bind(EvidencePanelItem? item)
        {
            bool hasItem = item.HasValue;
            EvidencePanelItem record = item.GetValueOrDefault();
            Set(title, hasItem ? record.Title : "조사 기록");
            Set(
                acquisitionPlace,
                hasItem
                    ? $"획득 장소  {record.AcquisitionPlace}"
                    : "아직 확인한 기록이 없습니다.");
            Set(
                relatedPeople,
                hasItem
                    ? $"관련 인물  {record.RelatedPeople}"
                    : string.Empty);
            Set(
                reliability,
                hasItem ? record.Reliability : string.Empty);
            Set(
                description,
                hasItem
                    ? record.Detail
                    : "확보한 증거가 없습니다. 배경의 인물과 사물을 조사하면 기록이 추가됩니다.");

            if (recordImage != null)
            {
                recordImage.sprite = null;
                recordImage.gameObject.SetActive(
                    hasItem && record.HasImage);
            }

            Canvas.ForceUpdateCanvases();
            if (descriptionScroll != null)
            {
                descriptionScroll.verticalNormalizedPosition = 1f;
            }
        }

        public void SetImage(Sprite sprite)
        {
            if (recordImage == null)
            {
                return;
            }
            recordImage.sprite = sprite;
            recordImage.gameObject.SetActive(sprite != null);
        }

        private static void Set(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
