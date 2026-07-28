using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Editor;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class ScreenShellPrefabIntegrityTests
    {
        private static readonly string[] ExtendedSlotIds =
        {
            ScreenShellSlotIds.SafeArea,
            ScreenShellSlotIds.PortraitLeft,
            ScreenShellSlotIds.PortraitRight,
            ScreenShellSlotIds.Choices,
            ScreenShellSlotIds.ModalDim,
            ScreenShellSlotIds.ModalPanel
        };

        [Test]
        public void AllSevenShellPrefabs_ExistAndSatisfyContract()
        {
            ScreenShellType[] shellTypes =
                (ScreenShellType[])Enum.GetValues(
                    typeof(ScreenShellType));
            Assert.That(shellTypes, Has.Length.EqualTo(7));

            foreach (ScreenShellType type in shellTypes)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    GetPrefabPath(type));
                Assert.That(
                    prefab,
                    Is.Not.Null,
                    $"Missing prefab for {type}.");

                ScreenShellLayout shell =
                    prefab.GetComponent<ScreenShellLayout>();
                Assert.That(shell, Is.Not.Null);
                Assert.That(shell.ShellType, Is.EqualTo(type));
                Assert.That(shell.IsComplete, Is.True);

                RuntimeUiLayoutSlot[] slots =
                    prefab.GetComponentsInChildren<RuntimeUiLayoutSlot>(true);
                HashSet<string> ids =
                    slots.Select(slot => slot.SlotId).ToHashSet();
                Assert.That(ids, Is.SupersetOf(ScreenRegionIds.All));
                Assert.That(ids, Is.SupersetOf(ExtendedSlotIds));
                Assert.That(
                    slots.GroupBy(slot => slot.SlotId)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key),
                    Is.Empty,
                    $"{type} contains duplicate slot IDs.");
            }
        }

        [Test]
        public void AllShellSlots_StayInsideNormalizedSafeArea()
        {
            foreach (ScreenShellType type in Enum.GetValues(
                         typeof(ScreenShellType)))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    GetPrefabPath(type));
                foreach (RuntimeUiLayoutSlot slot in
                         prefab.GetComponentsInChildren<
                             RuntimeUiLayoutSlot>(true))
                {
                    RectTransform rect =
                        (RectTransform)slot.transform;
                    Assert.That(
                        rect.anchorMin.x,
                        Is.InRange(0f, 1f),
                        slot.SlotId);
                    Assert.That(
                        rect.anchorMin.y,
                        Is.InRange(0f, 1f),
                        slot.SlotId);
                    Assert.That(
                        rect.anchorMax.x,
                        Is.InRange(0f, 1f),
                        slot.SlotId);
                    Assert.That(
                        rect.anchorMax.y,
                        Is.InRange(0f, 1f),
                        slot.SlotId);
                    Assert.That(
                        rect.anchorMax.x,
                        Is.GreaterThanOrEqualTo(rect.anchorMin.x),
                        slot.SlotId);
                    Assert.That(
                        rect.anchorMax.y,
                        Is.GreaterThanOrEqualTo(rect.anchorMin.y),
                        slot.SlotId);
                }
            }
        }

        [Test]
        public void ShellPolicies_HideGameplayChromeWhereRequired()
        {
            ScreenShellLayout system = Load(ScreenShellType.System);
            ScreenShellLayout ending = Load(ScreenShellType.Ending);
            ScreenShellLayout exploration =
                Load(ScreenShellType.Exploration);
            ScreenShellLayout modal =
                Load(ScreenShellType.ModalOverlay);

            Assert.That(system.Policy.BlocksGameplayHud, Is.True);
            Assert.That(system.Policy.ShowsGlobalNavigation, Is.False);
            Assert.That(ending.Policy.BlocksGameplayHud, Is.True);
            Assert.That(exploration.Policy.ShowsGlobalNavigation, Is.True);
            Assert.That(modal.Policy.CapturesInput, Is.True);
            Assert.That(modal.Policy.ShowsGlobalNavigation, Is.False);
        }

        private static ScreenShellLayout Load(ScreenShellType type)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                    GetPrefabPath(type))
                .GetComponent<ScreenShellLayout>();
        }

        private static string GetPrefabPath(ScreenShellType type)
        {
            string name = type == ScreenShellType.ModalOverlay
                ? "ModalOverlayShell"
                : $"{type}ScreenShell";
            return $"{ScreenShellPrefabAuthoring.PrefabFolder}/{name}.prefab";
        }
    }
}
