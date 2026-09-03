#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SamMul.Animations.Placeholder
{
    /// <summary>
    /// Placeholder stand-in for the skeleton renderer component. Draws a flat, tinted body quad through the
    /// object's own MeshRenderer (so ported sorting code written against MeshRenderer keeps working) plus a
    /// small marker on the facing side. The sign of <see cref="Placeholder.Skeleton.ScaleX"/> mirrors it.
    /// Hit/burn fills written by the animation controllers as _FillPhase/_FillColor in the renderer's
    /// property block are blended in here.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [DisallowMultipleComponent]
    public class SkeletonAnimation : MonoBehaviour
    {
        public SkeletonDataAsset? skeletonDataAsset;
        [SerializeField] private string _animationName = string.Empty;
        public bool loop;
        public float timeScale = 1f;
        public string initialSkinName = string.Empty;
        public bool tintBlack;
        public bool UnscaledTime;

        public AnimationState state = null!;
        public Skeleton skeleton = null!;

        public bool valid => state != null;

        public AnimationState AnimationState
        {
            get
            {
                if (!valid)
                {
                    Initialize(false);
                }
                return state;
            }
        }

        public Skeleton Skeleton
        {
            get
            {
                if (!valid)
                {
                    Initialize(false);
                }
                return skeleton;
            }
        }

        public SkeletonData SkeletonData => Skeleton.Data;

        /// <summary>Name of the clip on track 0. Setting it (re)starts that clip with <see cref="loop"/>.</summary>
        public string AnimationName
        {
            get
            {
                var entry = valid ? state.GetCurrent(0) : null;
                return entry != null ? entry.Animation.Name : _animationName;
            }
            set
            {
                _animationName = value ?? string.Empty;
                if (!valid)
                {
                    return;
                }
                if (string.IsNullOrEmpty(_animationName))
                {
                    state.ClearTrack(0);
                }
                else
                {
                    state.SetAnimation(0, _animationName, loop);
                }
            }
        }

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int FillPhaseId = Shader.PropertyToID("_FillPhase");
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        private static Material? s_sharedMaterial;

        private MeshRenderer? _renderer;
        private Mesh? _mesh;
        private MaterialPropertyBlock? _propertyBlock;
        private readonly List<Vector3> _vertices = new List<Vector3>(7);

        private static Material SharedMaterial
        {
            get
            {
                if (s_sharedMaterial == null)
                {
                    s_sharedMaterial = new Material(Shader.Find("Sprites/Default"))
                    {
                        name = "PlaceholderBody",
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                }
                return s_sharedMaterial;
            }
        }

        private void Awake()
        {
            Initialize(false);
        }

        public void Initialize(bool overwrite)
        {
            if (valid && !overwrite)
            {
                return;
            }

            var data = skeletonDataAsset != null ? skeletonDataAsset.GetSkeletonData(true) : SkeletonData.Create(null);
            skeleton = new Skeleton(data);
            state = new AnimationState(new AnimationStateData(data));

            if (!string.IsNullOrEmpty(initialSkinName))
            {
                skeleton.SetSkin(initialSkinName);
            }
            if (!string.IsNullOrEmpty(_animationName))
            {
                state.SetAnimation(0, _animationName, loop);
            }

            EnsureRenderer();
            ApplyVisual();
        }

        private void Update()
        {
            Update(UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        public void Update(float deltaTime)
        {
            if (!valid)
            {
                return;
            }
            state.Update(deltaTime * timeScale);
            state.Apply(skeleton);
            skeleton.UpdateWorldTransform();
        }

        private void LateUpdate()
        {
            ApplyVisual();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private void EnsureRenderer()
        {
            if (_renderer != null)
            {
                return;
            }

            _renderer = GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = SharedMaterial;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            _mesh = new Mesh { name = "PlaceholderBody" };
            _mesh.MarkDynamic();
            // 0..3: body quad (bottom-left, top-left, top-right, bottom-right), 4..6: facing marker.
            _mesh.vertices = new Vector3[7];
            _mesh.uv = new Vector2[7];
            var markerColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            _mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white, markerColor, markerColor, markerColor };
            _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6 };
            GetComponent<MeshFilter>().sharedMesh = _mesh;

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyVisual()
        {
            if (!valid || _renderer == null || _mesh == null || _propertyBlock == null)
            {
                return;
            }

            var data = skeleton.Data;
            float width = data.Width * skeleton.ScaleX;
            float height = data.Height * skeleton.ScaleY;
            float halfWidth = width * 0.5f;
            float bob = PlaceholderStyle.Bob(state) * data.Height;

            _vertices.Clear();
            _vertices.Add(new Vector3(-halfWidth, bob));
            _vertices.Add(new Vector3(-halfWidth, height + bob));
            _vertices.Add(new Vector3(halfWidth, height + bob));
            _vertices.Add(new Vector3(halfWidth, bob));
            // Marker on the facing side (negative X when ScaleX > 0, mirrored with it).
            _vertices.Add(new Vector3(-halfWidth, height * 0.8f + bob));
            _vertices.Add(new Vector3(-halfWidth - width * 0.2f, height * 0.7f + bob));
            _vertices.Add(new Vector3(-halfWidth, height * 0.6f + bob));
            _mesh.SetVertices(_vertices);
            _mesh.RecalculateBounds();

            Color color = PlaceholderStyle.Tint(state, data.BodyColor) * skeleton.GetColor();

            _renderer.GetPropertyBlock(_propertyBlock);
            float fillPhase = _propertyBlock.GetFloat(FillPhaseId);
            if (fillPhase > 0f)
            {
                Color fillColor = _propertyBlock.GetColor(FillColorId);
                color = new Color(
                    Mathf.Lerp(color.r, fillColor.r, fillPhase),
                    Mathf.Lerp(color.g, fillColor.g, fillPhase),
                    Mathf.Lerp(color.b, fillColor.b, fillPhase),
                    color.a);
            }
            _propertyBlock.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
