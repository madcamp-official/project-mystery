using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Exploration
{
    public sealed class AmbientInspectableSpec
    {
        public AmbientInspectableSpec(
            string id,
            string location,
            string title,
            string description,
            Rect hotspot,
            Rect imageUv)
        {
            Id = id;
            Location = location;
            Title = title;
            Description = description;
            Hotspot = hotspot;
            ImageUv = imageUv;
        }

        public string Id { get; }
        public string Location { get; }
        public string Title { get; }
        public string Description { get; }
        public Rect Hotspot { get; }
        public Rect ImageUv { get; }
    }

    public static class AmbientInspectableCatalog
    {
        private static readonly AmbientInspectableSpec[] Entries =
        {
            I("PROP_CHAMPAGNE","ATRIUM","마시다 만 샴페인","립스틱 자국이 선명하다. 파티가 시작되기도 전에 자리를 옮긴 승객이 있었던 모양이다. 수사와는 관계없다.",R(.18f,.30f),Cell(0,0)),
            I("PROP_BROCHURE","PORT","승선 안내서","접힌 모서리마다 오늘 행사에 대한 기대가 묻어 있다. 누군가는 공연보다 선상 수영장을 더 기다린 듯하다.",R(.32f,.25f),Cell(1,0)),
            I("PROP_BELL","VIP_LOUNGE","서비스 벨","두 번 누르자 멀리서 승무원이 '곧 가겠습니다'라고 답한다. 바쁜 밤이다.",R(.72f,.36f),Cell(2,0)),
            I("PROP_COFFEE","NEWS_LOUNGE","식어 버린 커피","향보다 탄 맛이 먼저 올라온다. 대니얼이 기사 마감 때마다 이것을 세 잔씩 마셨다는 직원의 푸념이 떠오른다.",R(.23f,.32f),Cell(0,1)),
            I("PROP_ROBOT","PROMENADE","와인 서비스 로봇","정중한 전자음과 함께 잔을 권한다. 목적지를 물어보니 '가장 가까운 목마른 손님'이라고 답한다.",R(.68f,.32f),Cell(1,1)),
            I("PROP_LUGGAGE","GANGWAY","주인 잃은 여행가방","여행 태그가 여러 장 붙어 있다. 직원은 주인이 기념품 가게에서 시간을 잊었을 뿐이라고 말한다.",R(.18f,.22f),Cell(2,1)),
            I("PROP_MASK","BALLROOM","갈라 가면","화려하지만 값싼 장식이다. 에벌린이라면 의상 소품의 금박이 고르지 않다며 사용하지 않았을 것이다.",R(.78f,.34f),Cell(0,2)),
            I("PROP_CLIPBOARD","ENGINE_CONTROL","정비 체크보드","기관실 교대자가 그린 배의 단면 옆에 커피 얼룩이 번져 있다. 오언은 '중요한 도면에는 절대 잔을 올리지 않는다'고 강조한다.",R(.35f,.42f),Cell(1,2)),
            I("PROP_TOKEN","OPEN_DECK","구명정 점검 토큰","매일 점검을 마친 승무원이 뒤집어 두는 황동 표식이다. 오늘 날짜 면도 정상적으로 뒤집혀 있다.",R(.82f,.25f),Cell(2,2))
        };

        public static IReadOnlyList<AmbientInspectableSpec> All => Entries;

        public static IReadOnlyList<AmbientInspectableSpec> GetForLocation(
            string locationCode) =>
            Entries.Where(item => string.Equals(
                item.Location,
                locationCode?.Trim(),
                StringComparison.OrdinalIgnoreCase)).ToArray();

        private static AmbientInspectableSpec I(
            string id, string location, string title, string description,
            Rect hotspot, Rect imageUv) =>
            new(id, location, title, description, hotspot, imageUv);

        private static Rect R(float x, float y) =>
            new(x - .075f, y - .075f, .15f, .15f);

        private static Rect Cell(int column, int rowFromTop) =>
            new(column / 3f, (2 - rowFromTop) / 3f, 1f / 3f, 1f / 3f);
    }

    [DisallowMultipleComponent]
    public sealed class AmbientInspectableOverlay : MonoBehaviour
    {
        private readonly List<GameObject> spawned = new();
        private RectTransform contentRect;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
        }

        public void Show(string locationCode)
        {
            Clear();
            foreach (AmbientInspectableSpec spec in
                     AmbientInspectableCatalog.GetForLocation(locationCode))
            {
                GameObject target = new(
                    $"AmbientInspectable_{spec.Id}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(Outline));
                target.transform.SetParent(contentRect, false);
                RectTransform rect = target.GetComponent<RectTransform>();
                Rect hotspot =
                    AmbientInteractionPresentation.ClampHotspot(spec.Hotspot);
                if (RuntimeUiLayoutRegistry.TryGetNormalizedRect(
                        $"location.{locationCode}.inspectable.{spec.Id}",
                        out Rect placeholder))
                {
                    hotspot = placeholder;
                }
                rect.anchorMin = hotspot.min;
                rect.anchorMax = hotspot.max;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image image = target.GetComponent<Image>();
                image.color = Color.white;
                Outline outline = target.GetComponent<Outline>();
                outline.effectColor = new Color32(87, 202, 212, 120);
                outline.effectDistance = new Vector2(2f, -2f);
                Button button = target.GetComponent<Button>();
                button.colors = AmbientInteractionPresentation.HotspotColors();
                button.onClick.AddListener(() =>
                    AmbientInspectablePopup.Show(spec));
                CreateHotspotLabel(target.transform, spec.Title);
                spawned.Add(target);
            }
        }

        private static void CreateHotspotLabel(
            Transform parent,
            string title)
        {
            GameObject labelObject = new(
                "State Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(190f, 30f);

            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            TypographyService.Apply(label, TypographyRole.TechnicalStrong);
            label.text = AmbientInteractionPresentation.HotspotLabel(title);
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(226, 238, 224, 235);
            label.raycastTarget = false;
        }

        private void Clear()
        {
            foreach (GameObject target in spawned)
            {
                if (target != null)
                    Destroy(target);
            }
            spawned.Clear();
        }
    }

    public static class AmbientInspectablePopup
    {
        private static GameObject active;

        public static void Show(AmbientInspectableSpec spec)
        {
            if (spec == null || DialogueController.Instance?.IsBusy == true)
                return;
            if (active != null)
                UnityEngine.Object.Destroy(active);

            active = new GameObject(
                "Ambient Inspectable Popup",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(Image),
                typeof(Button));
            Canvas canvas = active.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            CanvasScaler scaler = active.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            Image shade = active.GetComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, .72f);
            active.GetComponent<Button>().onClick.AddListener(Close);

            GameObject panel = new(
                "Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(Button));
            panel.transform.SetParent(active.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta =
                AmbientInteractionPresentation.PopupSize(
                    scaler.referenceResolution);
            panel.GetComponent<Image>().color = new Color32(24, 29, 43, 252);
            Button inputBlocker = panel.GetComponent<Button>();
            inputBlocker.transition = Selectable.Transition.None;
            Outline outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color32(200, 155, 76, 255);
            outline.effectDistance = new Vector2(3f, -3f);

            Texture2D sheet =
                Resources.Load<Texture2D>("InspectableProps/ambient_prop_sheet");
            GameObject artObject = new(
                "Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            artObject.transform.SetParent(panel.transform, false);
            RectTransform artRect = artObject.GetComponent<RectTransform>();
            artRect.anchorMin = artRect.anchorMax = new Vector2(.5f, 1f);
            artRect.pivot = new Vector2(.5f, 1f);
            artRect.anchoredPosition = new Vector2(0f, -42f);
            artRect.sizeDelta = new Vector2(520f, 520f);
            RawImage art = artObject.GetComponent<RawImage>();
            art.texture = sheet;
            art.uvRect = spec.ImageUv;
            art.raycastTarget = false;
            artObject.SetActive(sheet != null);

            CreateText(panel.transform, spec.Title, 32f, FontStyles.Bold,
                TypographyRole.HeadingStrong,
                new Vector2(40f, 158f), new Vector2(-40f, 220f));
            CreateText(panel.transform, spec.Description, 22f, FontStyles.Normal,
                TypographyRole.BodyRegular,
                new Vector2(54f, 42f), new Vector2(-54f, 150f));
            CreateCloseButton(panel.transform);
        }

        private static void CreateText(
            Transform parent,
            string text,
            float size,
            FontStyles style,
            TypographyRole role,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject textObject = new(
                "Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            TMP_Text label = textObject.GetComponent<TMP_Text>();
            TypographyService.Apply(label, role);
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
        }

        private static void CreateCloseButton(Transform parent)
        {
            GameObject target = new(
                "Close",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(96f, 48f);
            target.GetComponent<Image>().color = Color.white;

            Button button = target.GetComponent<Button>();
            button.colors = AmbientInteractionPresentation.PanelButtonColors();
            button.onClick.AddListener(Close);

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(target.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            TypographyService.Apply(label, TypographyRole.Choice);
            label.text = "닫기";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private static void Close()
        {
            if (active != null)
                UnityEngine.Object.Destroy(active);
            active = null;
        }
    }
}
