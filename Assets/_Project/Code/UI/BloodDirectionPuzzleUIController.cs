using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class BloodDirectionPuzzleUIController :
        MonoBehaviour,
        IRuntimeModalController
    {
        private static readonly Color Ink = new(0.055f, 0.045f, 0.05f, 0.99f);
        private static readonly Color Paper = new(0.88f, 0.82f, 0.69f, 1f);
        private static readonly Color Brass = new(0.72f, 0.52f, 0.25f, 1f);
        private static readonly Color Burgundy = new(0.45f, 0.075f, 0.08f, 1f);

        private readonly List<BloodPuzzlePieceView> pieceViews = new();
        private readonly List<GameObject> postureViews = new();
        private readonly List<Button> conclusionButtons = new();
        private GameObject root;
        private RectTransform board;
        private RectTransform toolPanel;
        private TMP_Text stepText;
        private TMP_Text instructionText;
        private TMP_Text feedbackText;
        private GameObject markerA;
        private GameObject markerB;
        private GameObject postureOverlay;
        private GameObject directionOverlay;
        private Button markerAButton;
        private Button markerBButton;
        private Button directionToolButton;
        private Texture2D bloodTexture;
        private Texture2D markerTexture;
        private Texture2D postureTexture;
        private BloodDirectionPuzzleSession puzzle;
        private ProductionPuzzleSession productionSession;
        private bool placingWound = true;

        public bool IsOpen => root != null && root.activeSelf;
        public BloodDirectionPuzzleSession Puzzle => puzzle;

        private void Awake()
        {
            BuildUi();
        }

        public bool Open()
        {
            if (!ProductionPuzzleCatalog.TryGet(
                    ProductionPuzzleCatalog.BloodPattern,
                    out ProductionPuzzleDefinition definition) ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    GameStateManager.Instance,
                    definition.SceneId,
                    definition.Id))
            {
                return false;
            }

            productionSession = new ProductionPuzzleSession(
                definition,
                GameStateManager.Instance,
                id => EvidenceInventory.Instance != null &&
                      EvidenceInventory.Instance.Contains(id));
            puzzle = new BloodDirectionPuzzleSession();
            root.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            root?.SetActive(false);
        }

        public void RotatePiece(int slot)
        {
            puzzle?.Rotate(slot);
            Refresh();
        }

        public void SwapPieces(int first, int second)
        {
            puzzle?.Swap(first, second);
            Refresh();
        }

        public void SelectPosture(int index)
        {
            if (puzzle == null || !puzzle.SelectPosture(index))
            {
                return;
            }

            feedbackText.text =
                "Helena: “그 위치라면 벽 쪽에도 작은 비산 흔적이 남아야 해요.”";
            ShowPostureOverlay(index);
            if (puzzle.ShouldEmphasizeTails)
            {
                StartCoroutine(FlashDirectionLines());
            }
            RefreshStageVisibility();
        }

        public void SetMarkerTool(bool wound)
        {
            placingWound = wound;
            markerAButton.image.color = wound ? Burgundy : new Color(0.2f, 0.16f, 0.16f);
            markerBButton.image.color = wound ? new Color(0.2f, 0.16f, 0.16f) : Brass;
            instructionText.text = wound
                ? "바닥 사진에서 자상 위치를 지정하세요."
                : "큰 혈액 웅덩이의 중심을 지정하세요.";
        }

        public void PlaceMarker(Vector2 normalized)
        {
            if (puzzle == null || !puzzle.PlaceMarker(placingWound, normalized))
            {
                return;
            }

            RectTransform target = placingWound
                ? markerA.GetComponent<RectTransform>()
                : markerB.GetComponent<RectTransform>();
            target.anchorMin = normalized;
            target.anchorMax = normalized;
            target.anchoredPosition = Vector2.zero;
            target.gameObject.SetActive(true);
            if (puzzle.Stage == BloodDirectionStage.ChooseConclusion)
            {
                feedbackText.text =
                    "두 지점이 일치하지 않습니다. 혈흔의 생성 방식을 판단하세요.";
            }
            Refresh();
        }

        public void ChooseConclusion(int index)
        {
            if (puzzle == null)
            {
                return;
            }

            if (!puzzle.ChooseConclusion(index))
            {
                feedbackText.text = index == 0
                    ? "이 자세라면 벽 쪽에 방사형 비산 흔적이 남아야 해요."
                    : "걸어 들어왔다면 바닥에 연속된 이동 혈흔이 있어야 해요.";
                return;
            }

            productionSession.Select("no_spatter");
            productionSession.Select("center_mismatch");
            productionSession.Select("vertical_drop");
            productionSession.SetStep((int)BloodDirectionStage.Complete);
            PuzzleCompletionResult result = productionSession.TryComplete();
            if (!result.Completed)
            {
                InvestigationFeedback feedback =
                    InvestigationFeedbackCatalog.ForPuzzle(
                        productionSession.Definition,
                        result);
                feedbackText.text = feedback.Message;
                return;
            }

            stepText.text = "분석 완료 · 시신 투입 가설 1";
            instructionText.text =
                "Daniel은 이곳에서 공격당한 것이 아니라, 이미 부상당한 상태로 천장에서 투입됐다.";
            feedbackText.text =
                "방사형 비산혈흔 부재 · 상처와 웅덩이 중심 불일치 · 충격 후 수직 확산";
            foreach (Button button in conclusionButtons)
            {
                button.gameObject.SetActive(false);
            }
            StartCoroutine(CloseAfterDelay());
        }

        private void Refresh()
        {
            if (puzzle == null)
            {
                return;
            }

            for (int slot = 0; slot < pieceViews.Count; slot++)
            {
                int source = puzzle.Pieces[slot];
                pieceViews[slot].SetPiece(
                    source,
                    CreateGridSprite(bloodTexture, source),
                    puzzle.Rotations[slot]);
                pieceViews[slot].SetInteraction(
                    puzzle.Stage == BloodDirectionStage.Reconstruct);
            }

            if (puzzle.Stage == BloodDirectionStage.CompareBody &&
                productionSession.Step < 1)
            {
                productionSession.SetStep(1);
                feedbackText.text =
                    "복원 완료: 줄눈, 웅덩이 테두리, 방울 꼬리 방향이 모두 이어집니다.";
            }
            else if (puzzle.Stage == BloodDirectionStage.ChooseConclusion &&
                     productionSession.Step < 2)
            {
                productionSession.SetStep(2);
            }
            RefreshStageVisibility();
        }

        private void RefreshStageVisibility()
        {
            BloodDirectionStage stage = puzzle.Stage;
            stepText.text = stage switch
            {
                BloodDirectionStage.Reconstruct => "01 · 혈흔 조각 맞추기",
                BloodDirectionStage.CompareBody => "02 · 시신 위치 대조",
                BloodDirectionStage.ChooseConclusion => "03 · 혈흔 유형 선택",
                _ => "분석 완료"
            };
            instructionText.text = stage switch
            {
                BloodDirectionStage.Reconstruct =>
                    "조각을 드래그해 교환하고, 클릭해 90° 회전하세요.",
                BloodDirectionStage.CompareBody =>
                    "자세를 겹쳐 본 뒤 A(자상)와 B(웅덩이 중심)를 각각 표시하세요.",
                BloodDirectionStage.ChooseConclusion =>
                    "혈흔의 형태를 가장 잘 설명하는 결론을 선택하세요.",
                _ => instructionText.text
            };
            bool comparing = stage == BloodDirectionStage.CompareBody;
            bool choosing = stage == BloodDirectionStage.ChooseConclusion;
            foreach (GameObject posture in postureViews)
            {
                posture.SetActive(comparing);
            }
            markerAButton.gameObject.SetActive(comparing);
            markerBButton.gameObject.SetActive(comparing);
            directionToolButton.gameObject.SetActive(comparing);
            for (int index = 0; index < conclusionButtons.Count; index++)
            {
                conclusionButtons[index].gameObject.SetActive(choosing);
            }
        }

        private void BuildUi()
        {
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            bloodTexture = Resources.Load<Texture2D>(
                "Puzzles/BloodDirection/blood_stain");
            markerTexture = Resources.Load<Texture2D>(
                "Puzzles/BloodDirection/markers");
            postureTexture = Resources.Load<Texture2D>(
                "Puzzles/BloodDirection/postures");

            root = MakePanel("Blood Direction Puzzle", canvas, Ink);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.02f, 0.025f);
            rootRect.anchorMax = new Vector2(0.98f, 0.975f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            TMP_Text title = MakeText(root.transform, "피의 방향", 32f);
            SetRect(title.rectTransform, 0.035f, 0.91f, 0.62f, 0.985f);
            title.alignment = TextAlignmentOptions.Left;
            title.color = Paper;
            stepText = MakeText(root.transform, string.Empty, 20f);
            SetRect(stepText.rectTransform, 0.65f, 0.93f, 0.94f, 0.975f);
            stepText.alignment = TextAlignmentOptions.Right;
            stepText.color = Brass;
            instructionText = MakeText(root.transform, string.Empty, 19f);
            SetRect(instructionText.rectTransform, 0.035f, 0.855f, 0.94f, 0.91f);
            instructionText.alignment = TextAlignmentOptions.Left;
            feedbackText = MakeText(root.transform, string.Empty, 18f);
            SetRect(feedbackText.rectTransform, 0.035f, 0.035f, 0.94f, 0.10f);
            feedbackText.color = new Color(0.95f, 0.78f, 0.62f);

            GameObject boardObject = MakePanel(
                "Horizon Room Floor",
                root.transform,
                new Color(0.08f, 0.07f, 0.065f, 1f));
            board = boardObject.GetComponent<RectTransform>();
            SetRect(board, 0.035f, 0.12f, 0.70f, 0.84f);
            var boardClick = boardObject.AddComponent<BloodPuzzleBoardClick>();
            boardClick.Initialize(this, board);

            var gridObject = new GameObject(
                "Piece Grid",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            gridObject.transform.SetParent(board, false);
            RectTransform gridRect = gridObject.GetComponent<RectTransform>();
            SetRect(gridRect, 0f, 0f, 1f, 1f);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.spacing = new Vector2(3f, 3f);
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.cellSize = new Vector2(190f, 190f);
            for (int index = 0; index < BloodDirectionPuzzleSession.PieceCount; index++)
            {
                GameObject piece = MakePanel(
                    $"Blood Piece {index + 1}",
                    gridObject.transform,
                    Color.white);
                var view = piece.AddComponent<BloodPuzzlePieceView>();
                view.Initialize(this, index);
                pieceViews.Add(view);
            }

            toolPanel = MakePanel(
                    "Analysis Tools",
                    root.transform,
                    new Color(0.10f, 0.09f, 0.10f, 1f))
                .GetComponent<RectTransform>();
            SetRect(toolPanel, 0.72f, 0.12f, 0.965f, 0.84f);
            TMP_Text tools = MakeText(toolPanel, "분석 도구", 21f);
            SetRect(tools.rectTransform, 0.06f, 0.91f, 0.94f, 0.98f);
            BuildPostureTools();
            BuildMarkerTools();
            BuildConclusions();
            BuildDirectionOverlay();

            Button close = MakeButton(root.transform, "닫기", 18f);
            SetRect(close.GetComponent<RectTransform>(), 0.89f, 0.035f, 0.965f, 0.09f);
            close.onClick.AddListener(Close);
            FeatureTypography.ApplyPuzzle(
                root.transform,
                title,
                instructionText,
                feedbackText);
            root.SetActive(false);
        }

        private void BuildPostureTools()
        {
            string[] labels = { "자세 1", "자세 2", "자세 3" };
            for (int index = 0; index < 3; index++)
            {
                int captured = index;
                Button button = MakeImageButton(
                    toolPanel,
                    $"Posture {index + 1}",
                    CreateAtlasThird(postureTexture, index));
                SetRect(
                    button.GetComponent<RectTransform>(),
                    0.06f + index * 0.31f,
                    0.61f,
                    0.34f + index * 0.31f,
                    0.88f);
                TMP_Text label = MakeText(button.transform, labels[index], 14f);
                SetRect(label.rectTransform, 0f, 0f, 1f, 0.18f);
                button.onClick.AddListener(() => SelectPosture(captured));
                postureViews.Add(button.gameObject);
            }

            postureOverlay = new GameObject(
                "Posture Overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            postureOverlay.transform.SetParent(board, false);
            RectTransform rect = postureOverlay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = postureOverlay.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.7f);
            image.raycastTarget = false;
            postureOverlay.SetActive(false);
        }

        private void BuildMarkerTools()
        {
            markerAButton = MakeImageButton(
                toolPanel,
                "Marker A",
                CreateSprite(markerTexture, 510, 490, 455, 475));
            SetRect(markerAButton.GetComponent<RectTransform>(), 0.08f, 0.38f, 0.45f, 0.58f);
            markerAButton.onClick.AddListener(() => SetMarkerTool(true));
            markerBButton = MakeImageButton(
                toolPanel,
                "Marker B",
                CreateSprite(markerTexture, 960, 490, 455, 475));
            SetRect(markerBButton.GetComponent<RectTransform>(), 0.55f, 0.38f, 0.92f, 0.58f);
            markerBButton.onClick.AddListener(() => SetMarkerTool(false));

            directionToolButton = MakeImageButton(
                toolPanel,
                "Blood Direction Tool",
                CreateSprite(markerTexture, 845, 215, 530, 330));
            SetRect(
                directionToolButton.GetComponent<RectTransform>(),
                0.24f,
                0.12f,
                0.76f,
                0.34f);
            directionToolButton.onClick.AddListener(
                () => StartCoroutine(FlashDirectionLines()));

            markerA = MakeBoardMarker("A · 자상", Burgundy);
            markerB = MakeBoardMarker("B · 웅덩이", Brass);
        }

        private void BuildConclusions()
        {
            string[] choices =
            {
                "이 자리에서 찔린 뒤 쓰러졌다.",
                "부상자가 걸어 들어와 쓰러졌다.",
                "이미 상처 입은 신체가 위에서 떨어졌다."
            };
            for (int index = 0; index < choices.Length; index++)
            {
                int captured = index;
                Button button = MakeButton(toolPanel, choices[index], 16f);
                SetRect(
                    button.GetComponent<RectTransform>(),
                    0.06f,
                    0.58f - index * 0.19f,
                    0.94f,
                    0.73f - index * 0.19f);
                button.onClick.AddListener(() => ChooseConclusion(captured));
                conclusionButtons.Add(button);
            }
        }

        private void BuildDirectionOverlay()
        {
            directionOverlay = new GameObject(
                "Direction Tail Hint",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            directionOverlay.transform.SetParent(board, false);
            RectTransform rect = directionOverlay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.13f);
            rect.anchorMax = new Vector2(0.94f, 0.90f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = directionOverlay.GetComponent<Image>();
            image.sprite = CreateSprite(markerTexture, 845, 215, 530, 330);
            image.preserveAspect = true;
            image.color = new Color(1f, 0.35f, 0.30f, 0.78f);
            image.raycastTarget = false;
            directionOverlay.SetActive(false);
        }

        private GameObject MakeBoardMarker(string label, Color color)
        {
            GameObject target = MakePanel(label, board, color);
            target.GetComponent<Image>().raycastTarget = false;
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(92f, 32f);
            TMP_Text text = MakeText(target.transform, label, 13f);
            SetRect(text.rectTransform, 0f, 0f, 1f, 1f);
            text.raycastTarget = false;
            target.SetActive(false);
            return target;
        }

        private void ShowPostureOverlay(int index)
        {
            Image image = postureOverlay.GetComponent<Image>();
            image.sprite = CreateAtlasThird(postureTexture, index);
            postureOverlay.SetActive(true);
            postureOverlay.transform.SetAsLastSibling();
            markerA.transform.SetAsLastSibling();
            markerB.transform.SetAsLastSibling();
        }

        private IEnumerator FlashDirectionLines()
        {
            directionOverlay.SetActive(true);
            directionOverlay.transform.SetAsLastSibling();
            yield return new WaitForSecondsRealtime(1.4f);
            directionOverlay.SetActive(false);
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3.2f);
            Close();
        }

        private Sprite CreateGridSprite(Texture2D texture, int source)
        {
            if (texture == null)
            {
                return null;
            }
            int column = source % 3;
            int rowFromTop = source / 3;
            float width = texture.width / 3f;
            float height = texture.height / 3f;
            return Sprite.Create(
                texture,
                new Rect(
                    column * width,
                    texture.height - (rowFromTop + 1) * height,
                    width,
                    height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private Sprite CreateAtlasThird(Texture2D texture, int index)
        {
            if (texture == null)
            {
                return null;
            }
            float width = texture.width / 3f;
            return Sprite.Create(
                texture,
                new Rect(index * width, 0f, width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Sprite CreateSprite(
            Texture2D texture,
            float x,
            float y,
            float width,
            float height)
        {
            return texture == null
                ? null
                : Sprite.Create(
                    texture,
                    new Rect(x, y, width, height),
                    new Vector2(0.5f, 0.5f),
                    100f);
        }

        private static GameObject MakePanel(
            string name,
            Transform parent,
            Color color)
        {
            var target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            target.GetComponent<Image>().color = color;
            return target;
        }

        private static TMP_Text MakeText(
            Transform parent,
            string value,
            float fontSize)
        {
            var target = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            TMP_Text text = target.GetComponent<TMP_Text>();
            TypographyService.Apply(text, TypographyRole.Body);
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button MakeButton(
            Transform parent,
            string label,
            float fontSize)
        {
            GameObject target = MakePanel(
                label,
                parent,
                new Color(0.20f, 0.16f, 0.16f, 1f));
            Button button = target.AddComponent<Button>();
            TMP_Text text = MakeText(target.transform, label, fontSize);
            SetRect(text.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
            text.raycastTarget = false;
            return button;
        }

        private static Button MakeImageButton(
            Transform parent,
            string name,
            Sprite sprite)
        {
            GameObject target = MakePanel(name, parent, Color.white);
            Image image = target.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return target.AddComponent<Button>();
        }

        private static void SetRect(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class BloodPuzzlePieceView :
        MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        private BloodDirectionPuzzleUIController owner;
        private int slot;
        private int source;
        private Image image;
        private static int draggedSlot = -1;

        public void Initialize(BloodDirectionPuzzleUIController controller, int slotIndex)
        {
            owner = controller;
            slot = slotIndex;
            image = GetComponent<Image>();
            image.preserveAspect = true;
        }

        public void SetPiece(int sourceIndex, Sprite sprite, int quarterTurns)
        {
            source = sourceIndex;
            image.sprite = sprite;
            image.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, -90f * quarterTurns);
            gameObject.name = $"Blood Piece {source + 1} in Slot {slot + 1}";
        }

        public void SetInteraction(bool enabled)
        {
            image.raycastTarget = enabled;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                owner.RotatePiece(slot);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            draggedSlot = slot;
            image.color = new Color(1f, 1f, 1f, 0.55f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            image.color = Color.white;
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            BloodPuzzlePieceView destination =
                hit != null ? hit.GetComponent<BloodPuzzlePieceView>() : null;
            if (destination != null && draggedSlot >= 0)
            {
                owner.SwapPieces(draggedSlot, destination.slot);
            }
            draggedSlot = -1;
        }
    }

    public sealed class BloodPuzzleBoardClick :
        MonoBehaviour,
        IPointerClickHandler
    {
        private BloodDirectionPuzzleUIController owner;
        private RectTransform board;

        public void Initialize(
            BloodDirectionPuzzleUIController controller,
            RectTransform boardRect)
        {
            owner = controller;
            board = boardRect;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner.Puzzle == null ||
                owner.Puzzle.Stage != BloodDirectionStage.CompareBody ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    board,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 local))
            {
                return;
            }

            Rect rect = board.rect;
            owner.PlaceMarker(new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, local.y)));
        }
    }
}
