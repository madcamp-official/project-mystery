using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AmbientCharacterHotspotOverlay : MonoBehaviour
    {
        private sealed class WorldCharacterView
        {
            public string Speaker;
            public string InteractionId;
            public bool IsMainCharacter;
            public bool IsFocusParticipant;
            public GameObject Target;
            public RectTransform Rect;
            public RawImage Image;
            public Shadow SilhouetteShadow;
            public Button Button;
            public GameObject GroundShadowObject;
            public RectTransform GroundShadowRect;
            public RawImage GroundShadowImage;
            public AmbientWorldCharacterAsset Asset;
            public Rect AtlasUvRect;
            public Material BlendMaterial;
            public Color BaseTint;
        }

        private static readonly Dictionary<string, Texture2D> TextureCache =
            new();
        private static Texture2D groundShadowTexture;
        private static Shader ambientBlendShader;

        private readonly List<WorldCharacterView> spawned = new();
        private RectTransform contentRect;
        private Vector2 lastContentSize;
        private string currentLocationCode = string.Empty;
        private string currentSceneId = string.Empty;
        private DialogueController boundDialogue;
        private bool dialoguePresentationVisible;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
            BindDialogue();
        }

        public void Show(string locationCode)
        {
            Clear();
            if (contentRect == null)
                return;

            currentLocationCode =
                locationCode?.Trim().ToUpperInvariant() ?? string.Empty;
            currentSceneId = AmbientBarkCatalog.ResolveCurrentSceneId(
                Wake.Core.GameStateManager.Instance,
                DialogueController.Instance?.ActiveProductionSceneId);
            IReadOnlyList<AmbientBarkRecord> barks =
                AmbientBarkCatalog.GetAvailable(
                    currentLocationCode,
                    Wake.Core.GameStateManager.Instance,
                    currentSceneId,
                    maximum: 1);
            for (int index = 0; index < barks.Count; index++)
            {
                CreateAmbientCharacter(barks[index]);
            }

            if (ScenePresenceCatalog.TryGet(
                    currentSceneId,
                    out ScenePresenceRecord scene))
            {
                int mainLimit = Mathf.Max(0, 3 - spawned.Count);
                IReadOnlyList<SceneWorldCharacter> characters =
                    ScenePresencePresentationPolicy.SelectVisible(
                        scene,
                        currentLocationCode,
                        mainLimit);
                foreach (SceneWorldCharacter character in characters)
                    CreateMainCharacter(character);
            }

            RefreshLayout();
            ApplyDialogueVisibility();
        }

        private void CreateAmbientCharacter(AmbientBarkRecord bark)
        {
            string interactionId = CreateInteractionId(
                "bark",
                currentSceneId,
                currentLocationCode,
                bark.Id);
            CreateWorldCharacter(
                bark.Speaker,
                bark.Id,
                interactionId,
                isMainCharacter: false,
                isFocusParticipant: false,
                onClick: () => StartAmbientCharacterDialogue(
                    bark,
                    interactionId));
        }

        private void CreateMainCharacter(SceneWorldCharacter character)
        {
            string interactionId = CreateInteractionId(
                "world",
                currentSceneId,
                currentLocationCode,
                character.CharacterId);
            CreateWorldCharacter(
                character.CharacterId,
                currentSceneId,
                interactionId,
                isMainCharacter: true,
                isFocusParticipant: character.IsFocusParticipant,
                onClick: () => StartMainCharacterDialogue(character));
        }

        private void CreateWorldCharacter(
            string speaker,
            string instanceId,
            string interactionId,
            bool isMainCharacter,
            bool isFocusParticipant,
            UnityEngine.Events.UnityAction onClick)
        {
            if (!AmbientWorldCharacterCatalog.TryGetAsset(
                    speaker,
                    out AmbientWorldCharacterAsset asset))
            {
                return;
            }

            Texture2D texture = LoadTexture(asset.ResourcePath);
            if (texture == null)
                return;

            GameObject groundShadow =
                CreateGroundShadow(speaker, instanceId);
            GameObject target = new(
                $"AmbientCharacter_{speaker}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(Shadow),
                typeof(Button));
            target.transform.SetParent(contentRect, false);
            target.transform.SetAsLastSibling();

            RawImage image = target.GetComponent<RawImage>();
            image.texture = texture;
            image.uvRect = asset.UvRect;
            image.color = Color.white;
            image.raycastTarget = true;
            Material blendMaterial = CreateBlendMaterial(asset.UvRect);
            if (blendMaterial != null)
                image.material = blendMaterial;

            Shadow shadow = target.GetComponent<Shadow>();
            shadow.useGraphicAlpha = true;

            Button button = target.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(onClick);

            target.name += $"_{spawned.Count}_{instanceId}";
            var view = new WorldCharacterView
            {
                Speaker = speaker,
                InteractionId = interactionId,
                IsMainCharacter = isMainCharacter,
                IsFocusParticipant = isFocusParticipant,
                Target = target,
                Rect = target.GetComponent<RectTransform>(),
                Image = image,
                SilhouetteShadow = shadow,
                Button = button,
                GroundShadowObject = groundShadow,
                GroundShadowRect =
                    groundShadow.GetComponent<RectTransform>(),
                GroundShadowImage = groundShadow.GetComponent<RawImage>(),
                Asset = asset,
                AtlasUvRect = asset.UvRect,
                BlendMaterial = blendMaterial,
                BaseTint = Color.white
            };
            spawned.Add(view);
        }

        private void StartMainCharacterDialogue(
            SceneWorldCharacter character)
        {
            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null)
                return;

            Wake.Core.GameStateManager state =
                Wake.Core.GameStateManager.Instance;
            if (character.IsFocusParticipant)
            {
                if (state?.HasCompletedScene(currentSceneId) == true)
                {
                    dialogue.StartAmbientLine(
                        character.CharacterId,
                        MainCharacterWorldLineCatalog.GetCompleted(
                            character.CharacterId,
                            character.State),
                        MainCharacterWorldLineCatalog.GetEmotion(
                            character.State));
                    return;
                }

                Wake.Core.ProductionDialogueCheckpoint checkpoint =
                    state?.DialogueCheckpoint;
                if (checkpoint != null &&
                    string.Equals(
                        checkpoint.activeSceneId,
                        currentSceneId,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    dialogue.RestoreProductionScene(checkpoint);
                    return;
                }

                if (dialogue.CanStartProductionScene(currentSceneId))
                {
                    dialogue.StartProductionScene(currentSceneId);
                    return;
                }
            }

            string interactionId = CreateInteractionId(
                "world",
                currentSceneId,
                currentLocationCode,
                character.CharacterId);
            if (state?.HasCompletedNpcInteraction(interactionId) == true)
            {
                dialogue.StartAmbientLine(
                    character.CharacterId,
                    MainCharacterWorldLineCatalog.GetCompleted(
                        character.CharacterId,
                        character.State),
                    MainCharacterWorldLineCatalog.GetEmotion(character.State));
                return;
            }

            if (dialogue.StartAmbientLine(
                    character.CharacterId,
                    MainCharacterWorldLineCatalog.Get(
                        character.CharacterId,
                        character.State),
                    MainCharacterWorldLineCatalog.GetEmotion(character.State)))
            {
                state?.RecordCompletedNpcInteraction(interactionId);
                RefreshCompletionPresentation();
            }
        }

        private void StartAmbientCharacterDialogue(
            AmbientBarkRecord bark,
            string interactionId)
        {
            DialogueController dialogue = DialogueController.Instance;
            if (dialogue == null)
                return;

            Wake.Core.GameStateManager state =
                Wake.Core.GameStateManager.Instance;
            bool completed =
                state?.HasCompletedNpcInteraction(interactionId) == true;
            string text = completed
                ? "앞서 말씀드린 내용이 전부입니다."
                : bark.Text;
            if (dialogue.StartAmbientLine(
                    bark.Speaker,
                    text,
                    completed ? "neutral" : bark.Emotion) &&
                !completed)
            {
                state?.RecordCompletedNpcInteraction(interactionId);
                RefreshCompletionPresentation();
            }
        }

        private void LateUpdate()
        {
            BindDialogue();
            if (contentRect != null &&
                spawned.Count > 0 &&
                contentRect.rect.size != lastContentSize)
            {
                RefreshLayout();
            }
            RefreshCompletionPresentation();
        }

        private void BindDialogue()
        {
            DialogueController dialogue = DialogueController.Instance;
            if (boundDialogue == dialogue)
                return;

            if (boundDialogue != null)
                boundDialogue.PresentationChanged -=
                    HandleDialoguePresentationChanged;

            boundDialogue = dialogue;
            if (boundDialogue == null)
                return;

            boundDialogue.PresentationChanged +=
                HandleDialoguePresentationChanged;
            dialoguePresentationVisible =
                boundDialogue.ActivePresentation.IsVisible;
            ApplyDialogueVisibility();
        }

        private void HandleDialoguePresentationChanged(
            DialoguePresentationSpec presentation)
        {
            dialoguePresentationVisible = presentation.IsVisible;
            ApplyDialogueVisibility();
        }

        private void ApplyDialogueVisibility()
        {
            bool visible = !dialoguePresentationVisible;
            foreach (WorldCharacterView view in spawned)
            {
                view?.Target?.SetActive(visible);
                view?.GroundShadowObject?.SetActive(visible);
            }
        }

        private void RefreshLayout()
        {
            if (contentRect == null)
                return;

            Vector2 contentSize = contentRect.rect.size;
            if (contentSize.x <= 0f || contentSize.y <= 0f)
                return;

            for (int index = 0; index < spawned.Count; index++)
            {
                WorldCharacterView view = spawned[index];
                if (view?.Target == null)
                    continue;

                ApplyStage(view, index, spawned.Count);
            }

            lastContentSize = contentSize;
        }

        private void ApplyStage(
            WorldCharacterView view,
            int index,
            int count)
        {
            AmbientWorldStageProfile stage;
            if (count == 1 &&
                AmbientWorldStageCatalog.TryGet(
                    currentLocationCode,
                    view.Speaker,
                    out stage))
            {
                // Preserve the hand-authored single-actor composition.
            }
            else
            {
                stage = AmbientWorldStageCatalog.GetLocationProfile(
                    currentLocationCode,
                    index,
                    count,
                    view.IsMainCharacter);
            }

            if (RuntimeUiLayoutRegistry.TryGetNormalizedRect(
                    $"location.{currentLocationCode}.character.{view.Speaker}",
                    out Rect placeholder))
            {
                stage = new AmbientWorldStageProfile(
                    placeholder.center,
                    placeholder.height,
                    stage.Mirror,
                    stage.LightTint,
                    stage.ShadowDirection,
                    stage.ShadowOpacity,
                    stage.GroundShadowScale,
                    stage.Saturation,
                    stage.Exposure,
                    stage.Contrast,
                    stage.Softness);
            }

            Vector2 anchor = stage.Anchor;
            view.Rect.anchorMin = anchor;
            view.Rect.anchorMax = anchor;
            view.Rect.pivot = new Vector2(0.5f, 0f);

            Vector2 contentSize = contentRect.rect.size;
            AmbientWorldLayoutMetrics geometry =
                AmbientWorldGeometry.Calculate(
                    contentSize,
                    stage,
                    view.Asset);
            view.Rect.anchoredPosition = new Vector2(
                0f,
                geometry.AnchoredOffsetY);
            view.Rect.sizeDelta = geometry.RectSize;

            Rect uv = view.Image.uvRect;
            float width = Mathf.Abs(uv.width);
            float baseX = uv.width < 0f ? uv.x + uv.width : uv.x;
            view.Image.uvRect = stage.Mirror
                ? new Rect(baseX + width, uv.y, -width, uv.height)
                : new Rect(baseX, uv.y, width, uv.height);
            view.Image.color = stage.LightTint;
            view.BaseTint = stage.LightTint;
            view.Button.colors =
                AmbientInteractionPresentation.CharacterSpriteColors(
                    stage.LightTint);
            ApplyBlendMaterial(view, stage);

            view.SilhouetteShadow.effectColor =
                new Color(0f, 0f, 0f, stage.ShadowOpacity);
            view.SilhouetteShadow.effectDistance = new Vector2(
                stage.ShadowDirection.x * contentSize.x,
                stage.ShadowDirection.y * contentSize.y);

            view.GroundShadowRect.anchorMin = anchor;
            view.GroundShadowRect.anchorMax = anchor;
            view.GroundShadowRect.pivot = new Vector2(0.5f, 0.5f);
            view.GroundShadowRect.anchoredPosition = new Vector2(
                stage.ShadowDirection.x * contentSize.x * 0.3f,
                0f);
            view.GroundShadowRect.sizeDelta =
                geometry.GroundShadowSize;
            view.GroundShadowImage.color =
                new Color(0f, 0f, 0f, stage.ShadowOpacity * 0.72f);
        }

        private void RefreshCompletionPresentation()
        {
            Wake.Core.GameStateManager state =
                Wake.Core.GameStateManager.Instance;
            if (state == null)
                return;

            foreach (WorldCharacterView view in spawned)
            {
                if (view?.Image == null || view.Button == null)
                    continue;

                bool completed =
                    (view.IsFocusParticipant &&
                     state.HasCompletedScene(currentSceneId)) ||
                    state.HasCompletedNpcInteraction(view.InteractionId);
                Color tint = completed
                    ? Color.Lerp(
                        view.BaseTint,
                        new Color(
                            0.58f,
                            0.58f,
                            0.58f,
                            view.BaseTint.a),
                        0.32f)
                    : view.BaseTint;
                view.Image.color = tint;
                view.Button.colors =
                    AmbientInteractionPresentation.CharacterSpriteColors(tint);
            }
        }

        private static string CreateInteractionId(
            string kind,
            string sceneId,
            string locationCode,
            string sourceId)
        {
            return string.Join(
                ":",
                "npc",
                kind ?? string.Empty,
                sceneId ?? string.Empty,
                locationCode ?? string.Empty,
                sourceId ?? string.Empty);
        }

        private static Material CreateBlendMaterial(Rect uvRect)
        {
            ambientBlendShader ??=
                Resources.Load<Shader>(
                    "Shaders/AmbientCharacterBlend");
            if (ambientBlendShader == null)
                return null;

            var material = new Material(ambientBlendShader)
            {
                name = "Ambient Character Blend (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetVector(
                "_UvRect",
                new Vector4(
                    uvRect.x,
                    uvRect.y,
                    Mathf.Abs(uvRect.width),
                    Mathf.Abs(uvRect.height)));
            return material;
        }

        private static void ApplyBlendMaterial(
            WorldCharacterView view,
            AmbientWorldStageProfile stage)
        {
            Material material = view.BlendMaterial;
            if (material == null)
                return;

            material.SetFloat("_Saturation", stage.Saturation);
            material.SetFloat("_Exposure", stage.Exposure);
            material.SetFloat("_Contrast", stage.Contrast);
            material.SetFloat("_Softness", stage.Softness);
            Vector2 lightDirection =
                -stage.ShadowDirection.normalized;
            if (stage.Mirror)
                lightDirection.x *= -1f;
            material.SetVector(
                "_LightDirection",
                new Vector4(
                    lightDirection.x,
                    lightDirection.y,
                    0f,
                    0f));
            material.SetVector(
                "_UvRect",
                new Vector4(
                    view.AtlasUvRect.x,
                    view.AtlasUvRect.y,
                    Mathf.Abs(view.AtlasUvRect.width),
                    Mathf.Abs(view.AtlasUvRect.height)));
        }

        private GameObject CreateGroundShadow(
            string speaker,
            string instanceId)
        {
            GameObject shadow = new(
                $"AmbientGroundShadow_{speaker}_{instanceId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            shadow.transform.SetParent(contentRect, false);
            RawImage image = shadow.GetComponent<RawImage>();
            image.texture = GetGroundShadowTexture();
            image.raycastTarget = false;
            return shadow;
        }

        private static Texture2D LoadTexture(string resourcePath)
        {
            if (TextureCache.TryGetValue(
                    resourcePath,
                    out Texture2D cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            TextureCache[resourcePath] = texture;
            return texture;
        }

        private static Texture2D GetGroundShadowTexture()
        {
            if (groundShadowTexture != null)
                return groundShadowTexture;

            const int width = 96;
            const int height = 32;
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false)
            {
                name = "Ambient Ground Shadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float ny = (y + 0.5f) / height * 2f - 1f;
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f) / width * 2f - 1f;
                    float distance = nx * nx + ny * ny;
                    byte alpha = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(1f - distance) *
                        Mathf.Clamp01(1f - distance) *
                        255f);
                    pixels[y * width + x] =
                        new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            groundShadowTexture = texture;
            return groundShadowTexture;
        }

        private void Clear()
        {
            foreach (WorldCharacterView view in spawned)
            {
                if (view?.Target != null)
                    Destroy(view.Target);
                if (view?.GroundShadowObject != null)
                    Destroy(view.GroundShadowObject);
                if (view?.BlendMaterial != null)
                    Destroy(view.BlendMaterial);
            }
            spawned.Clear();
        }

        private void OnDestroy()
        {
            if (boundDialogue != null)
            {
                boundDialogue.PresentationChanged -=
                    HandleDialoguePresentationChanged;
            }
        }
    }
}
