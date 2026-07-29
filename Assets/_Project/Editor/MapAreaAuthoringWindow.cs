using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Editor
{
    public sealed class MapAreaAuthoringWindow : EditorWindow
    {
        private enum InputMode
        {
            Polygon,
            Label,
            Entrance
        }

        private readonly List<Vector2> points = new();
        private Texture2D baseImage;
        private int deck = 8;
        private string areaId = "NEW_AREA";
        private string displayName = "새 제한 구역";
        private string revealCondition = "D1-04";
        private string accessCondition = string.Empty;
        private MapAreaVisualState initialState =
            MapAreaVisualState.Restricted;
        private MapLayerMode previewMode = MapLayerMode.Investigation;
        private InputMode inputMode;
        private Vector2 labelAnchor = new(.5f, .5f);
        private Vector2 entranceAnchor = new(.5f, .5f);
        private Vector2 scroll;

        [MenuItem("Wake/Map Area Authoring")]
        private static void Open()
        {
            MapAreaAuthoringWindow window =
                GetWindow<MapAreaAuthoringWindow>("Map Area Authoring");
            window.minSize = new Vector2(760f, 640f);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(
                "정규화 폴리곤 제작",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이미지를 클릭해 실제 벽 안쪽 꼭짓점, 이름 위치, 출입구를 " +
                "지정합니다. 좌표는 0~1 범위로 저장됩니다.",
                MessageType.Info);

            baseImage = (Texture2D)EditorGUILayout.ObjectField(
                "Base 이미지",
                baseImage,
                typeof(Texture2D),
                false);
            deck = EditorGUILayout.IntField("Deck", deck);
            areaId = EditorGUILayout.TextField("영역 ID", areaId);
            displayName = EditorGUILayout.TextField("표시 이름", displayName);
            revealCondition =
                EditorGUILayout.TextField("발견 조건", revealCondition);
            accessCondition =
                EditorGUILayout.TextField("접근 조건", accessCondition);
            initialState = (MapAreaVisualState)EditorGUILayout.EnumPopup(
                "초기 상태",
                initialState);
            previewMode = (MapLayerMode)EditorGUILayout.EnumPopup(
                "합성 미리보기",
                previewMode);
            inputMode = (InputMode)EditorGUILayout.EnumPopup(
                "클릭 입력",
                inputMode);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("마지막 꼭짓점 취소"))
                {
                    if (points.Count > 0)
                        points.RemoveAt(points.Count - 1);
                    Repaint();
                }
                if (GUILayout.Button("폴리곤 초기화"))
                {
                    points.Clear();
                    Repaint();
                }
                if (GUILayout.Button("현재 덱 Base 불러오기"))
                    LoadDeckBase();
            }

            Rect preview = GUILayoutUtility.GetRect(
                720f,
                540f,
                GUILayout.ExpandWidth(true));
            DrawPreview(preview);
            HandlePreviewInput(preview);

            MapAreaShape draft = CreateDraft();
            bool valid = MapAreaCatalog.IsValid(draft, out string error);
            EditorGUILayout.HelpBox(
                valid
                    ? $"검증 통과 · 꼭짓점 {points.Count}개"
                    : error,
                valid ? MessageType.Info : MessageType.Warning);
            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button("MapAreaCatalog 에셋에 저장", GUILayout.Height(34f)))
                    Save(draft);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreview(Rect available)
        {
            EditorGUI.DrawRect(available, new Color(.035f, .05f, .075f, 1f));
            Rect imageRect = Fit(available, 1448f / 1086f);
            if (baseImage != null)
                GUI.DrawTexture(imageRect, baseImage, ScaleMode.StretchToFill);

            Texture2D overlay = LoadPreviewOverlay();
            if (overlay != null && previewMode != MapLayerMode.Passenger)
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, .65f);
                GUI.DrawTexture(imageRect, overlay, ScaleMode.StretchToFill);
                GUI.color = previous;
            }

            Handles.BeginGUI();
            Handles.color = new Color(1f, .78f, .28f, 1f);
            for (int index = 0; index < points.Count; index++)
            {
                Vector3 current = ToGui(points[index], imageRect);
                Vector3 next = ToGui(points[(index + 1) % points.Count], imageRect);
                Handles.DrawSolidDisc(current, Vector3.forward, 4f);
                if (points.Count > 1)
                    Handles.DrawLine(current, next, 2f);
            }
            Handles.color = new Color(.3f, .9f, 1f, 1f);
            Handles.DrawSolidDisc(ToGui(labelAnchor, imageRect), Vector3.forward, 5f);
            Handles.color = new Color(1f, .35f, .25f, 1f);
            Handles.DrawSolidDisc(ToGui(entranceAnchor, imageRect), Vector3.forward, 5f);
            Handles.EndGUI();
        }

        private void HandlePreviewInput(Rect available)
        {
            Event current = Event.current;
            Rect imageRect = Fit(available, 1448f / 1086f);
            if (current.type != EventType.MouseDown ||
                current.button != 0 ||
                !imageRect.Contains(current.mousePosition))
            {
                return;
            }

            Vector2 normalized = new(
                Mathf.InverseLerp(
                    imageRect.xMin,
                    imageRect.xMax,
                    current.mousePosition.x),
                1f - Mathf.InverseLerp(
                    imageRect.yMin,
                    imageRect.yMax,
                    current.mousePosition.y));
            switch (inputMode)
            {
                case InputMode.Label:
                    labelAnchor = normalized;
                    break;
                case InputMode.Entrance:
                    entranceAnchor = normalized;
                    break;
                default:
                    points.Add(normalized);
                    break;
            }
            current.Use();
            Repaint();
        }

        private MapAreaShape CreateDraft()
        {
            var shape = new MapAreaShape();
            shape.SetAuthoringData(
                areaId,
                displayName,
                deck,
                points,
                labelAnchor,
                entranceAnchor,
                revealCondition,
                accessCondition,
                initialState);
            return shape;
        }

        private void Save(MapAreaShape shape)
        {
            const string defaultPath =
                "Assets/_Project/Resources/Maps/MapAreaCatalog.asset";
            MapAreaCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<MapAreaCatalogAsset>(defaultPath);
            if (catalog == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Map Area Catalog 저장",
                    "MapAreaCatalog",
                    "asset",
                    "Resources/Maps 아래에 저장하면 런타임이 자동으로 사용합니다.",
                    "Assets/_Project/Resources/Maps");
                if (string.IsNullOrEmpty(path))
                    return;
                catalog = CreateInstance<MapAreaCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            Undo.RecordObject(catalog, "Save Map Area");
            catalog.Replace(shape);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
        }

        private void LoadDeckBase()
        {
            string path =
                $"Assets/_Project/Resources/Maps/DeckLayers/Deck{deck:00}_Base.png";
            baseImage = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private Texture2D LoadPreviewOverlay()
        {
            string suffix = previewMode == MapLayerMode.Technical
                ? "Technical"
                : "Restricted";
            string path =
                $"Assets/_Project/Resources/Maps/DeckLayers/Deck{deck:00}_{suffix}.png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Rect Fit(Rect available, float aspect)
        {
            float width = available.width;
            float height = width / aspect;
            if (height > available.height)
            {
                height = available.height;
                width = height * aspect;
            }
            return new Rect(
                available.x + (available.width - width) * .5f,
                available.y + (available.height - height) * .5f,
                width,
                height);
        }

        private static Vector3 ToGui(Vector2 point, Rect rect) =>
            new(
                Mathf.Lerp(rect.xMin, rect.xMax, point.x),
                Mathf.Lerp(rect.yMax, rect.yMin, point.y),
                0f);
    }
}
