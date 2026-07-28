using UnityEngine;

namespace Wake.UI
{
    // Stylised god-ray beams that live under the lobby "water" object, on the
    // same "Water" layer so the existing Water Camera -> RenderTexture ->
    // RawImage compositing pipeline picks them up for free. Idle: dim,
    // slow shimmer. SaveSlotSelectionController drives SetIntensity() with
    // the same t it already uses for the reveal/hide position lerp, so the
    // beams brighten in sync with water rising into view.
    public sealed class LightShaftEffect : MonoBehaviour
    {
        private const int BeamCount = 5;
        private const float IdleAlpha = 0.12f;
        private const float PeakAlpha = 0.5f;
        private const float ScrollSpeed = 0.05f;

        private static readonly float[] BeamAngles = { -28f, -14f, 0f, 14f, 28f };

        [SerializeField] private Material beamMaterialTemplate;

        private Material[] beamMaterials;
        private float intensity;

        private void Awake()
        {
            EnsureBuilt();
        }

        public void SetIntensity(float t)
        {
            intensity = Mathf.Clamp01(t);
            ApplyIntensity();
        }

        private void Update()
        {
            if (beamMaterials == null)
            {
                return;
            }
            float offset = Time.unscaledTime * ScrollSpeed;
            foreach (Material material in beamMaterials)
            {
                material.mainTextureOffset = new Vector2(0f, offset);
            }
        }

        private void ApplyIntensity()
        {
            if (beamMaterials == null)
            {
                return;
            }
            float alpha = Mathf.Lerp(IdleAlpha, PeakAlpha, intensity);
            foreach (Material material in beamMaterials)
            {
                Color color = material.GetColor("_BaseColor");
                color.a = alpha;
                material.SetColor("_BaseColor", color);
            }
        }

        private void EnsureBuilt()
        {
            if (beamMaterials != null)
            {
                return;
            }

            if (beamMaterialTemplate == null)
            {
                Debug.LogError("LightShaftEffect requires beamMaterialTemplate to be assigned.");
                return;
            }
            int waterLayer = LayerMask.NameToLayer("Water");
            beamMaterials = new Material[BeamCount];

            for (int i = 0; i < BeamCount; i++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
                beam.name = $"Beam {i}";
                beam.layer = waterLayer >= 0 ? waterLayer : beam.layer;
                Object.Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(transform, false);
                const float beamWidth = 0.22f;
                const float beamHeight = 2.2f;
                Vector3 origin = new Vector3(0f, 0.1f, -0.05f);
                Quaternion rotation = Quaternion.Euler(0f, 0f, BeamAngles[i]);
                Vector3 up = rotation * Vector3.up;
                beam.transform.localRotation = rotation;
                // Quad meshes are centered on their own pivot, so shift each
                // beam along its own rotated up-axis by half its height -
                // that pins the *base* of the beam at the shared origin and
                // lets it fan outward from there, instead of straddling the
                // origin symmetrically (which looks like a bowtie).
                beam.transform.localPosition = origin + up * (beamHeight / 2f);
                beam.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);

                MeshRenderer renderer = beam.GetComponent<MeshRenderer>();
                Material instance = new Material(beamMaterialTemplate) { name = $"LightShaftMaterial (Beam {i})" };
                renderer.sharedMaterial = instance;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                beamMaterials[i] = instance;
            }

            ApplyIntensity();
        }
    }
}
