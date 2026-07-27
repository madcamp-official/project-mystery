using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.UI;

namespace Wake.Exploration
{
    public sealed class EvidenceLocationHotspotSpec
    {
        public EvidenceLocationHotspotSpec(
            string locationCode,
            string evidenceId,
            Rect normalizedRect,
            string availableFromScene,
            string requiredEnding = "")
        {
            LocationCode = locationCode;
            EvidenceId = CanonicalEvidenceCatalog.NormalizeId(evidenceId);
            NormalizedRect = normalizedRect;
            AvailableFromScene = availableFromScene ?? string.Empty;
            RequiredEnding = requiredEnding ?? string.Empty;
        }

        public string LocationCode { get; }
        public string EvidenceId { get; }
        public Rect NormalizedRect { get; }
        public string AvailableFromScene { get; }
        public string RequiredEnding { get; }
    }

    /// <summary>
    /// Canonical click targets for evidence painted directly into location backgrounds.
    /// Coordinates use normalized image space, bottom-left = (0, 0).
    /// </summary>
    public static class EvidenceLocationHotspotCatalog
    {
        private static readonly EvidenceLocationHotspotSpec[] Entries =
        {
            E("PORT", "C-01", R(.06f, .17f, .11f, .11f), "P-01"),
            E("PORT", "C-18", R(.23f, .20f, .13f, .13f), "D8-03", "A"),

            E("HORIZON", "C-02", R(.20f, .49f, .10f, .23f), "D1-06"),
            E("HORIZON", "C-03", R(.45f, .50f, .12f, .10f), "D2-01"),
            E("HORIZON", "C-04", R(.04f, .80f, .08f, .10f), "D2-01"),
            E("HORIZON", "C-05", R(.23f, .58f, .08f, .12f), "D2-01"),
            E("HORIZON", "C-07", R(.56f, .28f, .17f, .14f), "D1-06"),

            E("BALLAST_CONTROL_ANNEX", "C-06", R(.75f, .22f, .18f, .15f), "D6-03"),
            E("BALLAST_CONTROL_ANNEX", "C-12", R(.65f, .55f, .18f, .26f), "D6-03"),
            E("SECURITY", "C-08", R(.66f, .88f, .12f, .10f), "D2-04"),
            E("ENGINE_CONTROL", "C-09", R(.10f, .38f, .17f, .24f), "D6-01"),
            E("SERVICE_RAIL", "C-10", R(.31f, .48f, .15f, .22f), "D6-02"),
            E("MEDBAY", "C-11", R(.08f, .09f, .14f, .15f), "D2-03"),
            E("NEWS_LOUNGE", "C-13", R(.58f, .48f, .16f, .12f), "D2-06"),
            E("PROMENADE", "C-14", R(.23f, .36f, .18f, .18f), "D3-05"),
            E("MEDBAY", "C-15", R(.73f, .11f, .15f, .11f), "D3-04"),
            E("MEDBAY", "C-16", R(.73f, .51f, .18f, .16f), "D7-02"),
            E("ARCHIVE", "C-17", R(.67f, .31f, .20f, .20f), "D7-03")
        };

        public static IReadOnlyList<EvidenceLocationHotspotSpec> All => Entries;

        public static IReadOnlyList<EvidenceLocationHotspotSpec> GetForLocation(
            string locationCode) =>
            Entries
                .Where(entry => string.Equals(
                    entry.LocationCode,
                    locationCode?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

        public static bool IsAvailable(
            EvidenceLocationHotspotSpec entry,
            GameStateManager state)
        {
            if (entry == null)
            {
                return false;
            }

            if (state == null)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(entry.RequiredEnding) &&
                !string.Equals(
                    state.FinalEndingId,
                    entry.RequiredEnding,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.IsNullOrEmpty(entry.AvailableFromScene) ||
                   state.IsProductionSceneUnlocked(entry.AvailableFromScene) ||
                   state.HasCompletedScene(entry.AvailableFromScene) ||
                   state.CollectedEvidenceIds.Contains(entry.EvidenceId);
        }

        private static EvidenceLocationHotspotSpec E(
            string location,
            string evidence,
            Rect rect,
            string scene,
            string ending = "") =>
            new(location, evidence, rect, scene, ending);

        private static Rect R(float x, float y, float width, float height) =>
            new(x - width * .5f, y - height * .5f, width, height);
    }

    [DisallowMultipleComponent]
    public sealed class EvidenceLocationHotspotOverlay : MonoBehaviour
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
            {
                return;
            }

            foreach (EvidenceLocationHotspotSpec spec in
                     EvidenceLocationHotspotCatalog.GetForLocation(locationCode))
            {
                if (!EvidenceLocationHotspotCatalog.IsAvailable(
                        spec, GameStateManager.Instance))
                {
                    continue;
                }

                CreateButton(spec);
            }
        }

        private void CreateButton(EvidenceLocationHotspotSpec spec)
        {
            GameObject target = new(
                $"EvidenceHotspot_{spec.EvidenceId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            target.transform.SetParent(contentRect, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = spec.NormalizedRect.min;
            rect.anchorMax = spec.NormalizedRect.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = target.GetComponent<Image>();
            image.color = new Color(1f, .78f, .28f, .001f);
            image.raycastTarget = true;

            Button button = target.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .82f, .45f, .12f);
            colors.pressedColor = new Color(1f, .72f, .25f, .20f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.onClick.AddListener(() => Interact(spec));
            spawned.Add(target);
        }

        private static void Interact(EvidenceLocationHotspotSpec spec)
        {
            EvidenceInventory inventory = EvidenceInventory.Instance;
            if (inventory == null)
            {
                return;
            }

            CanonicalEvidenceCatalog.TryGet(spec.EvidenceId, out var entry);
            string displayName = entry?.DisplayName ?? spec.EvidenceId;
            if (inventory.Contains(spec.EvidenceId))
            {
                ToastController.Instance?.Show($"이미 확보한 단서: {displayName}");
                UIManager.Instance?.ShowEvidence(spec.EvidenceId);
                return;
            }

            if (!inventory.TryAddById(spec.EvidenceId))
            {
                ToastController.Instance?.Show($"단서를 확인할 수 없습니다: {displayName}");
                return;
            }

            ToastController.Instance?.Show($"단서 확보: {displayName}");
            UIManager.Instance?.ShowEvidence(spec.EvidenceId);
        }

        private void Clear()
        {
            foreach (GameObject target in spawned)
            {
                if (target != null)
                {
                    Destroy(target);
                }
            }
            spawned.Clear();
        }
    }
}
