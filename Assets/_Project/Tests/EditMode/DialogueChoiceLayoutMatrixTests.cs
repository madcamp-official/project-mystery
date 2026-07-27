using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class DialogueChoiceLayoutMatrixTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";
        private const string FontPath =
            "Assets/_Project/Resources/Typography/Pretendard Medium SDF.asset";
        private const float ChoiceLeft = 840f;
        private const float ChoiceRight = 280f;
        private const float Tolerance = 0.5f;

        private IReadOnlyList<DialogueRecord> records;
        private IReadOnlyList<ChoiceGroup> groups;
        private TMP_FontAsset font;

        private static IEnumerable LayoutCases
        {
            get
            {
                yield return Case(1280, 720, 0, 0, 1280, 720, "HD_16_9");
                yield return Case(1421, 888, 0, 0, 1421, 888, "Reported_1421_888");
                yield return Case(1920, 1080, 0, 0, 1920, 1080, "FHD_16_9");
                yield return Case(1920, 1200, 0, 0, 1920, 1200, "WUXGA_16_10");
                yield return Case(1440, 1080, 0, 0, 1440, 1080, "Four_Three");
                yield return Case(2560, 1080, 0, 0, 2560, 1080, "Ultrawide");
                yield return Case(1920, 1080, 80, 40, 1740, 980, "FHD_Inset");
                yield return Case(1440, 1080, 64, 36, 1312, 1000, "Four_Three_Inset");
                yield return Case(2560, 1080, 96, 32, 2368, 1016, "Ultrawide_Inset");
            }
        }

        [OneTimeSetUp]
        public void LoadProductionChoices()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(source, Is.Not.Null, DialoguePath);
            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(source.text);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            records = parsed.Records;
            groups = CollectGroups(records);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Assert.That(font, Is.Not.Null, FontPath);
        }

        [Test]
        public void ProductionSource_PreservesCompleteChoiceInventory()
        {
            DialogueRecord[] choices = records.Where(IsChoice).ToArray();

            Assert.That(choices, Has.Length.EqualTo(90));
            Assert.That(groups, Has.Count.EqualTo(33));
            Assert.That(groups.Sum(group => group.Rows.Count), Is.EqualTo(90));
            Assert.That(choices, Has.All.Matches<DialogueRecord>(row =>
                !string.IsNullOrWhiteSpace(row.ChoiceId)));
        }

        [Test]
        public void P01WarningGroup_PreservesBothExpectedChoices()
        {
            ChoiceGroup warning = groups.Single(group =>
                group.SceneId == "P-01" && group.BranchGroup == "P-01_WARN");

            Assert.That(warning.Rows.Select(row => row.LineId),
                Is.EqualTo(new[] { "P-01_020", "P-01_021" }));
            Assert.That(warning.Rows.Select(row => row.TextKo),
                Is.EqualTo(new[] { "그의 경고를 진지하게 듣기", "농담으로 넘기기" }));
            Assert.That(warning.Rows.Select(row => row.ChoiceId),
                Is.EqualTo(new[] { "P-01_C1", "P-01_C2" }));
            Assert.That(warning.Rows.Select(row => row.NextOrEffect),
                Is.EqualTo(new[]
                {
                    "trust_daniel:+1; flag:daniel_warning_taken",
                    "trust_daniel:-1; flag:daniel_warning_dismissed"
                }));
        }

        [Test]
        public void LargestGroups_PreserveEightAndFiveChoiceContracts()
        {
            ChoiceGroup largest = groups.OrderByDescending(
                group => group.Rows.Count).First();
            ChoiceGroup five = groups.Single(group =>
                group.SceneId == "D7-02" && group.BranchGroup == "D7-02_LOGIC");

            Assert.That(largest.SceneId, Is.EqualTo("D4-04"));
            Assert.That(largest.BranchGroup, Is.EqualTo("D4-04_Q"));
            Assert.That(largest.Rows.Count, Is.EqualTo(8));
            Assert.That(five.Rows.Count, Is.EqualTo(5));
            Assert.That(groups.Max(group => group.Rows.Count),
                Is.EqualTo(ProductionDialogueFlow.ChoiceCapacity));
        }

        [Test]
        public void LongestChoice_WrapsWithinMaximumCellHeight()
        {
            DialogueRecord longest = records.Where(IsChoice)
                .OrderByDescending(row => row.TextKo.Length).First();
            Assert.That(longest.LineId, Is.EqualTo("D8-01_062"));
            Assert.That(longest.TextKo, Has.Length.EqualTo(37));

            float available = ResponsiveDialogueLayout.ReferenceResolution.x -
                ChoiceLeft - ChoiceRight;
            DialogueChoiceLayoutSpec initial =
                DialogueChoiceLayoutPolicy.Calculate(available, 4);
            GameObject host = new(
                "Longest Choice Probe",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            try
            {
                TMP_Text label = host.GetComponent<TMP_Text>();
                label.font = font;
                label.fontSize = DialogueTypographyMetrics.ChoiceMaximum;
                label.enableAutoSizing = false;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.lineSpacing = DialogueTypographyMetrics.ChoiceLineSpacing;
                label.text = longest.TextKo;
                float preferred = label.GetPreferredValues(
                    label.text,
                    DialogueChoiceLayoutPolicy.GetLabelWidth(initial),
                    Mathf.Infinity).y;
                DialogueChoiceLayoutSpec measured =
                    DialogueChoiceLayoutPolicy.Calculate(available, 4, preferred);

                Assert.That(measured.CellSize.y,
                    Is.LessThanOrEqualTo(DialogueChoiceLayoutPolicy.MaximumCellHeight));
                Assert.That(measured.CellSize.y,
                    Is.GreaterThanOrEqualTo(DialogueChoiceLayoutPolicy.MinimumCellHeight));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [TestCaseSource(nameof(LayoutCases))]
        public void EveryProductionGroup_FitsSafeAreaAndAvoidsHud(
            Vector2 screenSize,
            Rect safeArea)
        {
            float scale = DialogueTypographyMetrics.CalculateCanvasScale(screenSize);
            float available = safeArea.width / scale - ChoiceLeft - ChoiceRight;
            float worstLabelHeight =
                DialogueChoiceLayoutPolicy.MaximumCellHeight -
                DialogueChoiceLayoutPolicy.LabelVerticalPadding;

            foreach (ChoiceGroup group in groups)
            {
                DialogueChoiceLayoutSpec spec =
                    DialogueChoiceLayoutPolicy.Calculate(
                        available,
                        group.Rows.Count,
                        worstLabelHeight);
                using var rig = new WorldCornerRig(
                    screenSize, safeArea, spec, group.Rows.Count);

                AssertInside(rig.Choices, safeArea, $"{group.BranchGroup} container");
                foreach (RectTransform button in rig.Buttons)
                    AssertInside(button, safeArea, $"{group.BranchGroup} button");
                foreach (RectTransform hud in rig.Hud)
                    AssertInside(hud, safeArea, $"{group.BranchGroup} {hud.name}");

                AssertSeparated(rig.Choices, rig.Portrait, group, "portrait");
                AssertSeparated(rig.Choices, rig.SpeakerPlate, group, "speaker");
                AssertSeparated(rig.Choices, rig.Evidence, group, "Evidence");
                AssertSeparated(rig.Choices, rig.Map, group, "Map");
                AssertSeparated(rig.Choices, rig.Settings, group, "Settings");
            }
        }

        [TestCaseSource(nameof(LayoutCases))]
        public void MinimumChoiceHeight_RemainsAtLeastFortyFourPixels(
            Vector2 screenSize,
            Rect safeArea)
        {
            float scale = DialogueTypographyMetrics.CalculateCanvasScale(screenSize);
            Assert.That(
                DialogueChoiceLayoutPolicy.MinimumCellHeight * scale,
                Is.GreaterThanOrEqualTo(44f),
                screenSize.ToString());
        }

        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(8, 4)]
        public void ProductionCounts_UseExpectedRows(int count, int rows)
        {
            float available = ResponsiveDialogueLayout.ReferenceResolution.x -
                ChoiceLeft - ChoiceRight;
            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(available, count);
            Assert.That(spec.Columns, Is.EqualTo(2));
            Assert.That(spec.Rows, Is.EqualTo(rows));
        }

        private static TestCaseData Case(
            float width, float height,
            float x, float y, float safeWidth, float safeHeight,
            string name) =>
            new TestCaseData(
                new Vector2(width, height),
                new Rect(x, y, safeWidth, safeHeight)).SetName(name);

        private static bool IsChoice(DialogueRecord row) =>
            row.Speaker == "PLAYER_CHOICE";

        private static IReadOnlyList<ChoiceGroup> CollectGroups(
            IReadOnlyList<DialogueRecord> source)
        {
            var result = new List<ChoiceGroup>();
            foreach (IGrouping<string, DialogueRecord> scene in
                     source.GroupBy(row => row.SceneId))
            {
                DialogueRecord[] ordered =
                    scene.OrderBy(row => row.Order).ToArray();
                for (int index = 0; index < ordered.Length;)
                {
                    if (!IsChoice(ordered[index]))
                    {
                        index++;
                        continue;
                    }

                    int start = index;
                    while (index < ordered.Length && IsChoice(ordered[index]))
                        index++;
                    DialogueRecord[] rows =
                        ordered.Skip(start).Take(index - start).ToArray();
                    result.Add(new ChoiceGroup(
                        scene.Key, rows[0].BranchGroup, rows));
                }
            }
            return result;
        }

        private static void AssertInside(
            RectTransform target,
            Rect safe,
            string context)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Assert.That(corner.x,
                    Is.InRange(safe.xMin - Tolerance, safe.xMax + Tolerance),
                    $"{context}: x={corner.x:0.00}");
                Assert.That(corner.y,
                    Is.InRange(safe.yMin - Tolerance, safe.yMax + Tolerance),
                    $"{context}: y={corner.y:0.00}");
            }
        }

        private static void AssertSeparated(
            RectTransform first,
            RectTransform second,
            ChoiceGroup group,
            string name)
        {
            Rect a = WorldRect(first);
            Rect b = WorldRect(second);
            float x = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float y = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            Assert.That(
                x <= Tolerance || y <= Tolerance,
                Is.True,
                $"{group.BranchGroup} overlaps {name}: {x:0.00}x{y:0.00}");
        }

        private static Rect WorldRect(RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                corners.Min(point => point.x),
                corners.Min(point => point.y),
                corners.Max(point => point.x),
                corners.Max(point => point.y));
        }

        private sealed class ChoiceGroup
        {
            public ChoiceGroup(
                string sceneId,
                string branchGroup,
                IReadOnlyList<DialogueRecord> rows)
            {
                SceneId = sceneId;
                BranchGroup = branchGroup;
                Rows = rows;
            }

            public string SceneId { get; }
            public string BranchGroup { get; }
            public IReadOnlyList<DialogueRecord> Rows { get; }
        }

        private sealed class WorldCornerRig : IDisposable
        {
            private readonly GameObject rootObject;

            public WorldCornerRig(
                Vector2 screen,
                Rect safe,
                DialogueChoiceLayoutSpec spec,
                int count)
            {
                float scale =
                    DialogueTypographyMetrics.CalculateCanvasScale(screen);
                rootObject = new GameObject("Safe Root", typeof(RectTransform));
                RectTransform root = rootObject.GetComponent<RectTransform>();
                root.pivot = Vector2.zero;
                root.sizeDelta = new Vector2(safe.width, safe.height) / scale;
                root.position = new Vector3(safe.xMin, safe.yMin, 0f);
                root.localScale = new Vector3(scale, scale, 1f);

                Evidence = TopLeft(
                    "Evidence", root, new Vector2(64f, -140f), new Vector2(210f, 82f));
                Map = TopLeft(
                    "Map", root, new Vector2(320f, -140f), new Vector2(210f, 82f));
                Settings = TopRight(
                    "Settings", root, new Vector2(-40f, -96f), new Vector2(210f, 82f));

                LinePanel = Rect("Line Panel", root);
                LinePanel.anchorMin = new Vector2(0f, 0f);
                LinePanel.anchorMax = new Vector2(1f, 0f);
                LinePanel.pivot = new Vector2(0.5f, 0f);
                LinePanel.anchoredPosition = new Vector2(0f, 36f);
                LinePanel.sizeDelta = new Vector2(0f, 480f);

                Portrait = TopLeft(
                    "Portrait", LinePanel, new Vector2(28f, 420f), new Vector2(320f, 400f));
                SpeakerPlate = TopLeft(
                    "Speaker", LinePanel, new Vector2(348f, 420f), new Vector2(460f, 68f));
                Next = BottomRight(
                    "Next", LinePanel, new Vector2(-44f, 44f), new Vector2(224f, 80f));
                LineText = TopLeft(
                    "Line Text", LinePanel, new Vector2(520f, -72f), new Vector2(1700f, 260f));

                Choices = Rect("Select Btn", LinePanel);
                Choices.anchorMin = new Vector2(0f, 1f);
                Choices.anchorMax = new Vector2(1f, 1f);
                Choices.pivot = new Vector2(0.5f, 0f);
                Choices.anchoredPosition = new Vector2(280f, 20f);
                Choices.sizeDelta = new Vector2(-1120f, spec.RequiredHeight);

                GridLayoutGroup grid =
                    Choices.gameObject.AddComponent<GridLayoutGroup>();
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = spec.Columns;
                grid.cellSize = spec.CellSize;
                grid.spacing = Vector2.one * DialogueChoiceLayoutPolicy.Spacing;
                grid.padding = new RectOffset(
                    DialogueChoiceLayoutPolicy.Padding,
                    DialogueChoiceLayoutPolicy.Padding,
                    DialogueChoiceLayoutPolicy.Padding,
                    DialogueChoiceLayoutPolicy.Padding);

                var buttons = new List<RectTransform>();
                for (int index = 0; index < count; index++)
                    buttons.Add(Rect($"Choice {index + 1}", Choices));
                Buttons = buttons;
                Hud = new[]
                {
                    LinePanel, LineText, Portrait, SpeakerPlate, Next,
                    Evidence, Map, Settings
                };

                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                LayoutRebuilder.ForceRebuildLayoutImmediate(Choices);
            }

            public RectTransform LinePanel { get; }
            public RectTransform LineText { get; }
            public RectTransform Portrait { get; }
            public RectTransform SpeakerPlate { get; }
            public RectTransform Next { get; }
            public RectTransform Evidence { get; }
            public RectTransform Map { get; }
            public RectTransform Settings { get; }
            public RectTransform Choices { get; }
            public IReadOnlyList<RectTransform> Buttons { get; }
            public IReadOnlyList<RectTransform> Hud { get; }

            public void Dispose() =>
                UnityEngine.Object.DestroyImmediate(rootObject);

            private static RectTransform TopLeft(
                string name, Transform parent, Vector2 position, Vector2 size)
            {
                RectTransform target = Rect(name, parent);
                target.anchorMin = target.anchorMax = new Vector2(0f, 1f);
                target.pivot = new Vector2(0f, 1f);
                target.anchoredPosition = position;
                target.sizeDelta = size;
                return target;
            }

            private static RectTransform TopRight(
                string name, Transform parent, Vector2 position, Vector2 size)
            {
                RectTransform target = Rect(name, parent);
                target.anchorMin = target.anchorMax = Vector2.one;
                target.pivot = Vector2.one;
                target.anchoredPosition = position;
                target.sizeDelta = size;
                return target;
            }

            private static RectTransform BottomRight(
                string name, Transform parent, Vector2 position, Vector2 size)
            {
                RectTransform target = Rect(name, parent);
                target.anchorMin = target.anchorMax = new Vector2(1f, 0f);
                target.pivot = new Vector2(1f, 0f);
                target.anchoredPosition = position;
                target.sizeDelta = size;
                return target;
            }

            private static RectTransform Rect(string name, Transform parent)
            {
                var host = new GameObject(name, typeof(RectTransform));
                host.transform.SetParent(parent, false);
                return host.GetComponent<RectTransform>();
            }
        }
    }
}
