using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public enum BackgroundSemanticCharacterRole
    {
        Context,
        Main,
        Focus
    }

    [Serializable]
    public sealed class BackgroundSemanticCharacterSlotBinding
    {
        [SerializeField] private string characterId = string.Empty;
        [SerializeField] private string slotId = string.Empty;
        [SerializeField] private BackgroundSemanticCharacterRole role;
        [SerializeField] private bool hardProtectionOverlap;

        public BackgroundSemanticCharacterSlotBinding()
        {
        }

        public BackgroundSemanticCharacterSlotBinding(
            string characterId,
            string slotId,
            BackgroundSemanticCharacterRole role =
                BackgroundSemanticCharacterRole.Context,
            bool hardProtectionOverlap = false)
        {
            Initialize(
                characterId,
                slotId,
                role,
                hardProtectionOverlap);
        }

        public string CharacterId => NormalizeCode(characterId);
        public string SlotId => slotId?.Trim() ?? string.Empty;
        public BackgroundSemanticCharacterRole Role => role;
        public bool HardProtectionOverlap => hardProtectionOverlap;

        public void Initialize(
            string valueCharacterId,
            string valueSlotId,
            BackgroundSemanticCharacterRole valueRole =
                BackgroundSemanticCharacterRole.Context,
            bool valueHardProtectionOverlap = false)
        {
            characterId = NormalizeCode(valueCharacterId);
            slotId = valueSlotId?.Trim() ?? string.Empty;
            role = valueRole;
            hardProtectionOverlap = valueHardProtectionOverlap;
        }

        internal static string NormalizeCode(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    [Serializable]
    public sealed class BackgroundSemanticSlotVisualGrade
    {
        [SerializeField] private string slotId = string.Empty;
        [SerializeField] private Color lightTintMultiplier = Color.white;
        [SerializeField, Min(0f)] private float saturationMultiplier = 1f;
        [SerializeField, Min(0f)] private float exposureMultiplier = 1f;
        [SerializeField, Min(0f)] private float contrastMultiplier = 1f;
        [SerializeField] private float softnessOffset;
        [SerializeField, Min(0f)] private float shadowOpacityMultiplier = 1f;
        [SerializeField, Min(.25f)] private float groundShadowScale = .62f;
        [SerializeField, Min(0f)] private float shadowDistance = .018f;

        public BackgroundSemanticSlotVisualGrade()
        {
        }

        public BackgroundSemanticSlotVisualGrade(
            string slotId,
            Color lightTintMultiplier,
            float saturationMultiplier = 1f,
            float exposureMultiplier = 1f,
            float contrastMultiplier = 1f,
            float softnessOffset = 0f,
            float shadowOpacityMultiplier = 1f,
            float groundShadowScale = .62f,
            float shadowDistance = .018f)
        {
            Initialize(
                slotId,
                lightTintMultiplier,
                saturationMultiplier,
                exposureMultiplier,
                contrastMultiplier,
                softnessOffset,
                shadowOpacityMultiplier,
                groundShadowScale,
                shadowDistance);
        }

        public string SlotId => slotId?.Trim() ?? string.Empty;
        public Color LightTintMultiplier => lightTintMultiplier;
        public float SaturationMultiplier => saturationMultiplier;
        public float ExposureMultiplier => exposureMultiplier;
        public float ContrastMultiplier => contrastMultiplier;
        public float SoftnessOffset => softnessOffset;
        public float ShadowOpacityMultiplier => shadowOpacityMultiplier;
        public float GroundShadowScale => groundShadowScale;
        public float ShadowDistance => shadowDistance;

        public void Initialize(
            string valueSlotId,
            Color valueLightTintMultiplier,
            float valueSaturationMultiplier = 1f,
            float valueExposureMultiplier = 1f,
            float valueContrastMultiplier = 1f,
            float valueSoftnessOffset = 0f,
            float valueShadowOpacityMultiplier = 1f,
            float valueGroundShadowScale = .62f,
            float valueShadowDistance = .018f)
        {
            slotId = valueSlotId?.Trim() ?? string.Empty;
            lightTintMultiplier = valueLightTintMultiplier;
            saturationMultiplier = Mathf.Max(
                0f,
                valueSaturationMultiplier);
            exposureMultiplier = Mathf.Max(0f, valueExposureMultiplier);
            contrastMultiplier = Mathf.Max(0f, valueContrastMultiplier);
            softnessOffset = valueSoftnessOffset;
            shadowOpacityMultiplier = Mathf.Max(
                0f,
                valueShadowOpacityMultiplier);
            groundShadowScale = Mathf.Max(.25f, valueGroundShadowScale);
            shadowDistance = Mathf.Max(0f, valueShadowDistance);
        }
    }

    [Serializable]
    public sealed class ApprovedBackgroundSemanticBinding
    {
        [SerializeField] private string locationCode = string.Empty;
        [SerializeField] private string variantKey = string.Empty;
        [SerializeField] private Sprite sourceSprite;
        [SerializeField] private string assetPath = string.Empty;
        [SerializeField] private string sourceImageHash = string.Empty;
        [SerializeField] private string semanticContentHash = string.Empty;
        [SerializeField] private bool approved;
        [SerializeField] private string reviewer = string.Empty;
        [SerializeField, Min(0)] private int approvalRevision;
        [SerializeField] private BackgroundSemanticProfile profile = new();
        [SerializeField] private List<BackgroundSemanticSlotVisualGrade>
            slotVisualGrades = new();

        public ApprovedBackgroundSemanticBinding()
        {
        }

        public ApprovedBackgroundSemanticBinding(
            string locationCode,
            string variantKey,
            Sprite sourceSprite,
            string sourceImageHash,
            bool approved,
            BackgroundSemanticProfile profile,
            IEnumerable<BackgroundSemanticSlotVisualGrade>
                slotVisualGrades = null,
            string reviewer = "",
            int approvalRevision = 0,
            string assetPath = "",
            string semanticContentHash = "")
        {
            Initialize(
                locationCode,
                variantKey,
                sourceSprite,
                sourceImageHash,
                approved,
                profile,
                slotVisualGrades,
                reviewer,
                approvalRevision,
                assetPath,
                semanticContentHash);
        }

        public string LocationCode =>
            BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                locationCode);
        public string VariantKey => variantKey?.Trim() ?? string.Empty;
        public Sprite SourceSprite => sourceSprite;
        public string AssetPath =>
            assetPath?.Trim().Replace('\\', '/') ?? string.Empty;
        public string SourceImageHash =>
            sourceImageHash?.Trim().ToLowerInvariant() ?? string.Empty;
        public string SourceSha256 => SourceImageHash;
        public string SemanticContentHash =>
            semanticContentHash?.Trim().ToLowerInvariant() ??
            string.Empty;
        public bool Approved => approved;
        public string Reviewer => reviewer?.Trim() ?? string.Empty;
        public int ApprovalRevision => approvalRevision;
        public BackgroundSemanticProfile Profile => profile;
        public IReadOnlyList<BackgroundSemanticSlotVisualGrade>
            SlotVisualGrades =>
                slotVisualGrades ??=
                    new List<BackgroundSemanticSlotVisualGrade>();

        public bool IsApproved
        {
            get
            {
                return approved &&
                       approvalRevision >= 0 &&
                       profile != null &&
                       profile.Status.State ==
                       BackgroundSemanticProfileState.Approved &&
                       !profile.IsUnused &&
                       !string.IsNullOrEmpty(LocationCode) &&
                       !string.IsNullOrEmpty(VariantKey) &&
                       sourceSprite != null &&
                       !string.IsNullOrEmpty(AssetPath) &&
                       IsSha256(SourceImageHash) &&
                       IsSha256(SemanticContentHash);
            }
        }

        public void Initialize(
            string valueLocationCode,
            string valueVariantKey,
            Sprite valueSourceSprite,
            string valueSourceImageHash,
            bool valueApproved,
            BackgroundSemanticProfile valueProfile,
            IEnumerable<BackgroundSemanticSlotVisualGrade>
                valueSlotVisualGrades = null,
            string valueReviewer = "",
            int valueApprovalRevision = 0,
            string valueAssetPath = "",
            string valueSemanticContentHash = "")
        {
            locationCode =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    valueLocationCode);
            variantKey = valueVariantKey?.Trim() ?? string.Empty;
            sourceSprite = valueSourceSprite;
            assetPath =
                valueAssetPath?.Trim().Replace('\\', '/') ??
                string.Empty;
            sourceImageHash =
                valueSourceImageHash?.Trim().ToLowerInvariant() ??
                string.Empty;
            semanticContentHash =
                valueSemanticContentHash?.Trim().ToLowerInvariant() ??
                string.Empty;
            approved = valueApproved;
            profile = valueProfile;
            slotVisualGrades = valueSlotVisualGrades != null
                ? new List<BackgroundSemanticSlotVisualGrade>(
                    valueSlotVisualGrades)
                : new List<BackgroundSemanticSlotVisualGrade>();
            reviewer = valueReviewer?.Trim() ?? string.Empty;
            approvalRevision = Mathf.Max(0, valueApprovalRevision);
        }

        public bool TryGetVisualGrade(
            string slotId,
            out BackgroundSemanticSlotVisualGrade grade)
        {
            string normalizedSlot = slotId?.Trim() ?? string.Empty;
            grade = SlotVisualGrades.FirstOrDefault(candidate =>
                candidate != null &&
                string.Equals(
                    candidate.SlotId,
                    normalizedSlot,
                    StringComparison.OrdinalIgnoreCase));
            return grade != null;
        }

        private static bool IsSha256(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length != 64)
                return false;

            return normalized.All(character =>
                character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f' ||
                character >= 'A' && character <= 'F');
        }
    }

    [Serializable]
    public sealed class ApprovedBackgroundSemanticSceneLayout
    {
        [SerializeField] private string sceneId = string.Empty;
        [SerializeField] private string locationCode = string.Empty;
        [SerializeField] private string variantKey = string.Empty;
        [SerializeField] private string sourceImageHash = string.Empty;
        [SerializeField] private string backgroundProfileId = string.Empty;
        [SerializeField] private string castFingerprint = string.Empty;
        [SerializeField] private bool approved;
        [SerializeField] private bool enforceMeasuredAlphaBounds;
        [SerializeField] private List<BackgroundSemanticCharacterSlotBinding>
            assignments = new();
        [SerializeField] private List<string> offCameraCharacterIds = new();

        public ApprovedBackgroundSemanticSceneLayout()
        {
        }

        public ApprovedBackgroundSemanticSceneLayout(
            string sceneId,
            string locationCode,
            string variantKey,
            string sourceImageHash,
            bool approved,
            IEnumerable<BackgroundSemanticCharacterSlotBinding>
                assignments,
            IEnumerable<string> offCameraCharacterIds = null,
            string backgroundProfileId = "",
            string castFingerprint = "",
            bool enforceMeasuredAlphaBounds = false)
        {
            Initialize(
                sceneId,
                locationCode,
                variantKey,
                sourceImageHash,
                approved,
                assignments,
                offCameraCharacterIds,
                backgroundProfileId,
                castFingerprint,
                enforceMeasuredAlphaBounds);
        }

        public string SceneId =>
            BackgroundSemanticCharacterSlotBinding.NormalizeCode(sceneId);
        public string LocationCode =>
            BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                locationCode);
        public string VariantKey => variantKey?.Trim() ?? string.Empty;
        public string SourceImageHash =>
            sourceImageHash?.Trim().ToLowerInvariant() ?? string.Empty;
        public string BackgroundProfileId =>
            backgroundProfileId?.Trim() ?? string.Empty;
        public string CastFingerprint =>
            castFingerprint?.Trim().ToLowerInvariant() ?? string.Empty;
        public bool Approved => approved;
        public bool EnforceMeasuredAlphaBounds =>
            enforceMeasuredAlphaBounds;
        public IReadOnlyList<BackgroundSemanticCharacterSlotBinding>
            Assignments =>
                assignments ??=
                    new List<BackgroundSemanticCharacterSlotBinding>();
        public IReadOnlyList<string> OffCameraCharacterIds =>
            offCameraCharacterIds ??= new List<string>();

        public void Initialize(
            string valueSceneId,
            string valueLocationCode,
            string valueVariantKey,
            string valueSourceImageHash,
            bool valueApproved,
            IEnumerable<BackgroundSemanticCharacterSlotBinding>
                valueAssignments,
            IEnumerable<string> valueOffCameraCharacterIds = null,
            string valueBackgroundProfileId = "",
            string valueCastFingerprint = "",
            bool valueEnforceMeasuredAlphaBounds = false)
        {
            sceneId =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    valueSceneId);
            locationCode =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    valueLocationCode);
            variantKey = valueVariantKey?.Trim() ?? string.Empty;
            sourceImageHash =
                valueSourceImageHash?.Trim().ToLowerInvariant() ??
                string.Empty;
            backgroundProfileId =
                valueBackgroundProfileId?.Trim() ?? string.Empty;
            castFingerprint =
                valueCastFingerprint?.Trim().ToLowerInvariant() ??
                string.Empty;
            approved = valueApproved;
            enforceMeasuredAlphaBounds =
                valueEnforceMeasuredAlphaBounds;
            assignments = valueAssignments != null
                ? new List<BackgroundSemanticCharacterSlotBinding>(
                    valueAssignments)
                : new List<BackgroundSemanticCharacterSlotBinding>();
            offCameraCharacterIds = valueOffCameraCharacterIds != null
                ? valueOffCameraCharacterIds
                    .Select(
                        BackgroundSemanticCharacterSlotBinding
                            .NormalizeCode)
                    .Where(value => !string.IsNullOrEmpty(value))
                    .ToList()
                : new List<string>();
        }

        public bool Matches(
            string valueSceneId,
            string valueLocationCode,
            string valueVariantKey,
            string valueSourceImageHash)
        {
            return string.Equals(
                       SceneId,
                       BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                           valueSceneId),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       LocationCode,
                       BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                           valueLocationCode),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       VariantKey,
                       valueVariantKey?.Trim() ?? string.Empty,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       SourceImageHash,
                       valueSourceImageHash?.Trim() ?? string.Empty,
                       StringComparison.OrdinalIgnoreCase);
        }

        public bool IsValidFor(
            BackgroundSemanticProfile profile,
            string expectedCastFingerprint = "")
        {
            if (!approved ||
                profile == null ||
                string.IsNullOrEmpty(SceneId) ||
                !string.Equals(
                    BackgroundProfileId,
                    profile.ProfileId?.Trim() ?? string.Empty,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    LocationCode,
                    profile.LocationCode?.Trim().ToUpperInvariant(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    VariantKey,
                    profile.VariantId?.Trim() ?? string.Empty,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    SourceImageHash,
                    profile.SourceImageHash?.Trim() ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase) ||
                !IsSha256(CastFingerprint))
            {
                return false;
            }

            string expected =
                expectedCastFingerprint?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(expected) &&
                !string.Equals(
                    CastFingerprint,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var characterIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var slotIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var profileSlots = new HashSet<string>(
                profile.Slots
                    .Where(slot => slot != null)
                    .Select(slot => slot.Id),
                StringComparer.OrdinalIgnoreCase);

            foreach (BackgroundSemanticCharacterSlotBinding assignment in
                     Assignments)
            {
                if (assignment == null ||
                    string.IsNullOrEmpty(assignment.CharacterId) ||
                    string.IsNullOrEmpty(assignment.SlotId) ||
                    !characterIds.Add(assignment.CharacterId) ||
                    !slotIds.Add(assignment.SlotId) ||
                    !profileSlots.Contains(assignment.SlotId))
                {
                    return false;
                }
            }

            foreach (string characterId in OffCameraCharacterIds)
            {
                string normalized =
                    BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                        characterId);
                if (string.IsNullOrEmpty(normalized) ||
                    !characterIds.Add(normalized))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSha256(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length != 64)
                return false;

            return normalized.All(character =>
                character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f' ||
                character >= 'A' && character <= 'F');
        }
    }

    [CreateAssetMenu(
        fileName = "ApprovedBackgroundSemanticCatalog",
        menuName =
            "Wake/Exploration/Approved Background Semantic Catalog")]
    public sealed class ApprovedBackgroundSemanticCatalog :
        ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField, Min(1)] private int schemaVersion =
            CurrentSchemaVersion;
        [SerializeField] private bool approved;
        [SerializeField] private string reviewer = string.Empty;
        [SerializeField, Min(0)] private int revision;
        [SerializeField] private string approvedAtUtc = string.Empty;
        [SerializeField] private bool approvedWarnings;
        [SerializeField, Min(0)] private int approvedWarningCount;
        [SerializeField] private string sourceInventoryGeneratedAtUtc =
            string.Empty;
        [SerializeField] private List<ApprovedBackgroundSemanticBinding>
            bindings = new();
        [SerializeField] private List<ApprovedBackgroundSemanticSceneLayout>
            sceneLayouts = new();

        public int SchemaVersion => schemaVersion;
        public bool Approved => approved;
        public bool IsUsable =>
            approved && schemaVersion == CurrentSchemaVersion;
        public string Reviewer => reviewer?.Trim() ?? string.Empty;
        public int Revision => revision;
        public string ApprovedAtUtc =>
            approvedAtUtc?.Trim() ?? string.Empty;
        public bool ApprovedWarnings => approvedWarnings;
        public int ApprovedWarningCount => approvedWarningCount;
        public string SourceInventoryGeneratedAtUtc =>
            sourceInventoryGeneratedAtUtc?.Trim() ?? string.Empty;
        public IReadOnlyList<ApprovedBackgroundSemanticBinding> Bindings =>
            bindings ??= new List<ApprovedBackgroundSemanticBinding>();
        public IReadOnlyList<ApprovedBackgroundSemanticSceneLayout>
            SceneLayouts =>
                sceneLayouts ??=
                    new List<ApprovedBackgroundSemanticSceneLayout>();

        public void Initialize(
            IEnumerable<ApprovedBackgroundSemanticBinding> valueBindings,
            IEnumerable<ApprovedBackgroundSemanticSceneLayout>
                valueSceneLayouts = null,
            bool valueApproved = true,
            int valueSchemaVersion = CurrentSchemaVersion,
            string valueReviewer = "",
            int valueRevision = 0,
            string valueApprovedAtUtc = "",
            bool valueApprovedWarnings = false,
            int valueApprovedWarningCount = 0,
            string valueSourceInventoryGeneratedAtUtc = "")
        {
            schemaVersion = valueSchemaVersion;
            approved = valueApproved;
            reviewer = valueReviewer?.Trim() ?? string.Empty;
            revision = Mathf.Max(0, valueRevision);
            approvedAtUtc = valueApprovedAtUtc?.Trim() ?? string.Empty;
            approvedWarnings = valueApprovedWarnings;
            approvedWarningCount = Mathf.Max(
                0,
                valueApprovedWarningCount);
            sourceInventoryGeneratedAtUtc =
                valueSourceInventoryGeneratedAtUtc?.Trim() ??
                string.Empty;
            bindings = valueBindings != null
                ? new List<ApprovedBackgroundSemanticBinding>(
                    valueBindings)
                : new List<ApprovedBackgroundSemanticBinding>();
            sceneLayouts = valueSceneLayouts != null
                ? new List<ApprovedBackgroundSemanticSceneLayout>(
                    valueSceneLayouts)
                : new List<ApprovedBackgroundSemanticSceneLayout>();
        }
    }
}
