using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AmbientCharacterHotspotOverlay : MonoBehaviour
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
            if (contentRect == null)
                return;

            IReadOnlyList<AmbientBarkRecord> barks =
                AmbientBarkCatalog.GetAvailable(
                    locationCode,
                    Wake.Core.GameStateManager.Instance);
            for (int index = 0; index < barks.Count; index++)
            {
                CreateCharacterButton(barks[index], index, barks.Count);
            }
        }

        private void CreateCharacterButton(
            AmbientBarkRecord bark,
            int index,
            int count)
        {
            GameObject target = new(
                $"AmbientCharacter_{bark.Speaker}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            target.transform.SetParent(contentRect, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            float center = .5f + (index - (count - 1) * .5f) * .15f;
            rect.anchorMin = new Vector2(center, .03f);
            rect.anchorMax = new Vector2(center, .03f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(150f, 64f);

            Image image = target.GetComponent<Image>();
            image.color = new Color32(24, 31, 46, 238);
            Outline outline = target.GetComponent<Outline>();
            outline.effectColor = new Color32(210, 164, 83, 230);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(target.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            TypographyService.Apply(label, TypographyRole.Choice);
            label.text =
                $"{DialoguePortraitCatalog.GetDisplayName(bark.Speaker)}\n대화하기";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            Button button = target.GetComponent<Button>();
            button.onClick.AddListener(() =>
                DialogueController.Instance?.StartAmbientLine(
                    bark.Speaker,
                    bark.Text,
                    bark.Emotion));
            spawned.Add(target);
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
}
