using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.Editor;

namespace Wake.Tests
{
    public sealed class TypographyDefaultsMigratorTests
    {
        private readonly List<Object> created = new();
        private TMP_FontAsset legacy;
        private TMP_FontAsset replacement;
        private TMP_FontAsset custom;

        [SetUp]
        public void SetUp()
        {
            legacy = CreateFont();
            replacement = CreateFont();
            custom = CreateFont();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }
            created.Clear();
        }

        [Test]
        public void ShouldReplace_AcceptsMissingFont()
        {
            Assert.That(
                TypographyDefaultsMigrator.ShouldReplace(
                    null,
                    legacy),
                Is.True);
        }

        [Test]
        public void ShouldReplace_AcceptsLegacyFont()
        {
            Assert.That(
                TypographyDefaultsMigrator.ShouldReplace(
                    legacy,
                    legacy),
                Is.True);
        }

        [Test]
        public void ShouldReplace_PreservesCustomFont()
        {
            Assert.That(
                TypographyDefaultsMigrator.ShouldReplace(
                    custom,
                    legacy),
                Is.False);
        }

        [Test]
        public void ShouldReplace_DoesNotTreatNullLegacyAsMatch()
        {
            Assert.That(
                TypographyDefaultsMigrator.ShouldReplace(
                    custom,
                    null),
                Is.False);
        }

        [Test]
        public void MigrateTexts_ReplacesLegacyFont()
        {
            TMP_Text old = CreateLabel("Legacy");
            old.font = legacy;

            int count = TypographyDefaultsMigrator.MigrateTexts(
                new[] { old },
                legacy,
                replacement);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(old.font, Is.SameAs(replacement));
        }

        [Test]
        public void MigrateTexts_PreservesExplicitCustomFont()
        {
            TMP_Text label = CreateLabel("Custom");
            label.font = custom;

            int count = TypographyDefaultsMigrator.MigrateTexts(
                new[] { label },
                legacy,
                replacement);

            Assert.That(count, Is.Zero);
            Assert.That(label.font, Is.SameAs(custom));
        }

        [Test]
        public void MigrateTexts_IgnoresNullLabels()
        {
            TMP_Text label = CreateLabel("Legacy");
            label.font = legacy;

            int count = TypographyDefaultsMigrator.MigrateTexts(
                new TMP_Text[] { null, label },
                legacy,
                replacement);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(label.font, Is.SameAs(replacement));
        }

        [Test]
        public void MigrateTexts_ReturnsZeroForMissingInputs()
        {
            Assert.That(
                TypographyDefaultsMigrator.MigrateTexts(
                    null,
                    legacy,
                    replacement),
                Is.Zero);
            Assert.That(
                TypographyDefaultsMigrator.MigrateTexts(
                    new TMP_Text[0],
                    legacy,
                    null),
                Is.Zero);
        }

        [Test]
        public void MigrateTexts_IsIdempotent()
        {
            TMP_Text label = CreateLabel("Legacy");
            label.font = legacy;

            int first = TypographyDefaultsMigrator.MigrateTexts(
                new[] { label },
                legacy,
                replacement);
            int second = TypographyDefaultsMigrator.MigrateTexts(
                new[] { label },
                legacy,
                replacement);

            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.Zero);
            Assert.That(label.font, Is.SameAs(replacement));
        }

        [Test]
        public void MigrateTexts_UpdatesAuthoredMaterialAtlas()
        {
            TMP_Text label = CreateLabel("Custom Material");
            label.font = legacy;
            Material authored = Track(
                new Material(legacy.material));
            label.fontSharedMaterial = authored;

            TypographyDefaultsMigrator.MigrateTexts(
                new[] { label },
                legacy,
                replacement);

            Assert.That(label.font, Is.SameAs(replacement));
            Assert.That(label.fontSharedMaterial, Is.SameAs(authored));
            Assert.That(
                authored.mainTexture,
                Is.SameAs(replacement.atlasTexture));
        }

        [Test]
        public void SetTmpDefault_RejectsNullReplacement()
        {
            Assert.That(
                () => TypographyDefaultsMigrator.SetTmpDefault(null),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void MigrateScene_RejectsMissingPath()
        {
            Assert.That(
                () => TypographyDefaultsMigrator.MigrateScene(
                    string.Empty,
                    legacy,
                    replacement),
                Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void MigrateScene_RejectsMissingReplacement()
        {
            Assert.That(
                () => TypographyDefaultsMigrator.MigrateScene(
                    TypographyDefaultsMigrator.UiScenePath,
                    legacy,
                    null),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void ProjectAssetsExistAtExpectedPaths()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                    TypographyDefaultsMigrator.TmpSettingsPath),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    TypographyDefaultsMigrator.LegacyFontPath),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    TypographyDefaultsMigrator.UiScenePath),
                Is.Not.Null);
        }

        private TMP_Text CreateLabel(string name)
        {
            GameObject target = Track(
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)));
            return target.GetComponent<TMP_Text>();
        }

        private TMP_FontAsset CreateFont()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            Assert.That(source, Is.Not.Null);
            return Track(TMP_FontAsset.CreateFontAsset(source));
        }

        private T Track<T>(T item)
            where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
