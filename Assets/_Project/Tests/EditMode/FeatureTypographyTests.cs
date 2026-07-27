using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class FeatureTypographyTests
    {
        private readonly List<Object> created = new();
        private TypographyCatalog catalog;
        private TMP_FontAsset body;
        private TMP_FontAsset bodyRegular;
        private TMP_FontAsset choice;
        private TMP_FontAsset heading;
        private TMP_FontAsset headingStrong;
        private TMP_FontAsset technical;

        [SetUp]
        public void SetUp()
        {
            catalog = Track(
                ScriptableObject.CreateInstance<TypographyCatalog>());
            body = CreateFont();
            bodyRegular = CreateFont();
            choice = CreateFont();
            heading = CreateFont();
            headingStrong = CreateFont();
            technical = CreateFont();
            SetFont("body", body);
            SetFont("bodyRegular", bodyRegular);
            SetFont("choice", choice);
            SetFont("heading", heading);
            SetFont("headingStrong", headingStrong);
            SetFont("technical", technical);
            TypographyService.SetCatalogForTests(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            TypographyService.SetCatalogForTests(null);
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
        public void ApplyPuzzle_AssignsSemanticRoles()
        {
            GameObject root = CreateRoot("Puzzle");
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text objective = CreateLabel("Objective", root.transform);
            TMP_Text hint = CreateLabel("Hint", root.transform);

            int count = FeatureTypography.ApplyPuzzle(
                root.transform,
                title,
                objective,
                hint);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(title.font, Is.SameAs(headingStrong));
            Assert.That(objective.font, Is.SameAs(body));
            Assert.That(hint.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplyPuzzle_UsesChoiceForDynamicButtons()
        {
            GameObject root = CreateRoot("Puzzle");
            TMP_Text first = CreateLabel("First Choice", root.transform);
            TMP_Text second = CreateLabel("Second Choice", root.transform);

            int count = FeatureTypography.ApplyPuzzle(
                root.transform,
                null,
                null,
                null);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(first.font, Is.SameAs(choice));
            Assert.That(second.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyPuzzle_IncludesInactiveButtons()
        {
            GameObject root = CreateRoot("Puzzle");
            TMP_Text label = CreateLabel("Inactive", root.transform);
            label.gameObject.SetActive(false);

            FeatureTypography.ApplyPuzzle(
                root.transform,
                null,
                null,
                null);

            Assert.That(label.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyEnding_AssignsSemanticRoles()
        {
            GameObject root = CreateRoot("Ending");
            TMP_Text route = CreateLabel("Route", root.transform);
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text epilogue = CreateLabel("Epilogue", root.transform);
            TMP_Text reason = CreateLabel("Reason", root.transform);

            int count = FeatureTypography.ApplyEnding(
                root.transform,
                route,
                title,
                epilogue,
                reason);

            Assert.That(count, Is.EqualTo(4));
            Assert.That(route.font, Is.SameAs(technical));
            Assert.That(title.font, Is.SameAs(headingStrong));
            Assert.That(epilogue.font, Is.SameAs(bodyRegular));
            Assert.That(reason.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplyEnding_UsesBodyForActionLabels()
        {
            GameObject root = CreateRoot("Ending");
            TMP_Text action = CreateLabel("Return", root.transform);

            FeatureTypography.ApplyEnding(
                root.transform,
                null,
                null,
                null,
                null);

            Assert.That(action.font, Is.SameAs(body));
        }

        [Test]
        public void ApplyEnding_RolesRemainDistinct()
        {
            GameObject root = CreateRoot("Ending");
            TMP_Text route = CreateLabel("Route", root.transform);
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text epilogue = CreateLabel("Epilogue", root.transform);

            FeatureTypography.ApplyEnding(
                root.transform,
                route,
                title,
                epilogue,
                null);

            Assert.That(route.font, Is.Not.SameAs(title.font));
            Assert.That(route.font, Is.Not.SameAs(epilogue.font));
            Assert.That(title.font, Is.Not.SameAs(epilogue.font));
        }

        [Test]
        public void ApplyMethods_ReturnZeroForMissingSurface()
        {
            Assert.That(
                FeatureTypography.ApplyPuzzle(null, null, null, null),
                Is.Zero);
            Assert.That(
                FeatureTypography.ApplyEnding(
                    null, null, null, null, null),
                Is.Zero);
        }

        [Test]
        public void ExplicitLabelsOutsideRootAreCounted()
        {
            GameObject root = CreateRoot("Ending");
            TMP_Text route = CreateLabel("Route", null);
            TMP_Text title = CreateLabel("Title", null);

            int count = FeatureTypography.ApplyEnding(
                root.transform,
                route,
                title,
                null,
                null);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(route.font, Is.SameAs(technical));
            Assert.That(title.font, Is.SameAs(headingStrong));
        }

        [Test]
        public void ApplyTheoryBoard_AssignsSemanticRoles()
        {
            GameObject root = CreateRoot("Theory Board");
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text progress = CreateLabel("Progress", root.transform);
            TMP_Text status = CreateLabel("Status", root.transform);
            TMP_Text deduction = CreateLabel("Deduction", root.transform);

            int count = FeatureTypography.ApplyTheoryBoard(
                root.transform,
                title,
                progress,
                status);

            Assert.That(count, Is.EqualTo(4));
            Assert.That(title.font, Is.SameAs(headingStrong));
            Assert.That(progress.font, Is.SameAs(technical));
            Assert.That(status.font, Is.SameAs(bodyRegular));
            Assert.That(deduction.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyTheoryBoard_UsesChoiceForButtons()
        {
            GameObject root = CreateRoot("Theory Board");
            TMP_Text first = CreateLabel("First Theory", root.transform);
            TMP_Text second = CreateLabel("Second Theory", root.transform);
            TMP_Text close = CreateLabel("Close", root.transform);

            int count = FeatureTypography.ApplyTheoryBoard(
                root.transform,
                null,
                null,
                null);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(first.font, Is.SameAs(choice));
            Assert.That(second.font, Is.SameAs(choice));
            Assert.That(close.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyTheoryBoard_IncludesInactiveLabels()
        {
            GameObject root = CreateRoot("Theory Board");
            TMP_Text label = CreateLabel("Inactive", root.transform);
            label.gameObject.SetActive(false);

            FeatureTypography.ApplyTheoryBoard(
                root.transform,
                null,
                null,
                null);

            Assert.That(label.font, Is.SameAs(choice));
        }

        [Test]
        public void ApplyTheoryBoard_CountsExternalOverrides()
        {
            GameObject root = CreateRoot("Theory Board");
            TMP_Text title = CreateLabel("External Title", null);
            TMP_Text progress = CreateLabel("External Progress", null);
            TMP_Text status = CreateLabel("External Status", null);

            int count = FeatureTypography.ApplyTheoryBoard(
                root.transform,
                title,
                progress,
                status);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(title.font, Is.SameAs(headingStrong));
            Assert.That(progress.font, Is.SameAs(technical));
            Assert.That(status.font, Is.SameAs(bodyRegular));
        }

        [Test]
        public void ApplyTheoryBoard_ReturnsZeroForMissingSurface()
        {
            Assert.That(
                FeatureTypography.ApplyTheoryBoard(
                    null,
                    null,
                    null,
                    null),
                Is.Zero);
        }

        [Test]
        public void ApplyMenuAction_UsesHeadingRole()
        {
            GameObject button = CreateRoot("Start");
            TMP_Text label = CreateLabel("Label", button.transform);

            bool applied = FeatureTypography.ApplyMenuAction(
                button.transform);

            Assert.That(applied, Is.True);
            Assert.That(label.font, Is.SameAs(heading));
        }

        [Test]
        public void ApplyMenuAction_FindsInactiveNestedLabel()
        {
            GameObject button = CreateRoot("Continue");
            GameObject wrapper = CreateRoot("Wrapper");
            wrapper.transform.SetParent(button.transform, false);
            TMP_Text label = CreateLabel("Label", wrapper.transform);
            wrapper.SetActive(false);

            FeatureTypography.ApplyMenuAction(button.transform);

            Assert.That(label.font, Is.SameAs(heading));
        }

        [Test]
        public void ApplyMenuAction_ReturnsFalseWithoutTarget()
        {
            Assert.That(
                FeatureTypography.ApplyMenuAction(null),
                Is.False);
        }

        [Test]
        public void ApplyMenuAction_ReturnsFalseWithoutLabel()
        {
            GameObject button = CreateRoot("Empty");

            Assert.That(
                FeatureTypography.ApplyMenuAction(button.transform),
                Is.False);
        }

        [Test]
        public void RuntimeSurfaceRolesRemainDistinct()
        {
            GameObject root = CreateRoot("Theory Board");
            TMP_Text title = CreateLabel("Title", root.transform);
            TMP_Text progress = CreateLabel("Progress", root.transform);
            TMP_Text status = CreateLabel("Status", root.transform);
            TMP_Text action = CreateLabel("Action", root.transform);

            FeatureTypography.ApplyTheoryBoard(
                root.transform,
                title,
                progress,
                status);

            Assert.That(title.font, Is.Not.SameAs(progress.font));
            Assert.That(title.font, Is.Not.SameAs(status.font));
            Assert.That(title.font, Is.Not.SameAs(action.font));
            Assert.That(progress.font, Is.Not.SameAs(status.font));
            Assert.That(progress.font, Is.Not.SameAs(action.font));
            Assert.That(status.font, Is.Not.SameAs(action.font));
        }

        private GameObject CreateRoot(string name)
        {
            return Track(new GameObject(name, typeof(RectTransform)));
        }

        private TMP_Text CreateLabel(string name, Transform parent)
        {
            GameObject target = Track(
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)));
            if (parent != null)
            {
                target.transform.SetParent(parent, false);
            }
            return target.GetComponent<TMP_Text>();
        }

        private TMP_FontAsset CreateFont()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            Assert.That(source, Is.Not.Null);
            return Track(TMP_FontAsset.CreateFontAsset(source));
        }

        private void SetFont(string fieldName, TMP_FontAsset value)
        {
            FieldInfo field = typeof(TypographyCatalog).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(catalog, value);
        }

        private T Track<T>(T item)
            where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
