#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace SamMul.Animations.Placeholder
{
    /// <summary>
    /// UI counterpart of <see cref="SkeletonAnimation"/>: a tinted rectangle filling the RectTransform.
    /// Being a Graphic keeps <c>color</c>, masking and tween extensions working for ported UI code.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkeletonGraphic : MaskableGraphic
    {
        public SkeletonDataAsset? skeletonDataAsset;
        public string startingAnimation = string.Empty;
        public bool startingLoop = true;
        public float timeScale = 1f;
        public bool UnscaledTime;

        private AnimationState? _state;
        private Skeleton? _skeleton;
        private Color _tint = Color.white;

        public bool IsValid => _state != null;

        public AnimationState AnimationState
        {
            get
            {
                if (_state == null)
                {
                    Initialize(false);
                }
                return _state!;
            }
        }

        public Skeleton Skeleton
        {
            get
            {
                if (_skeleton == null)
                {
                    Initialize(false);
                }
                return _skeleton!;
            }
        }

        public SkeletonData SkeletonData => Skeleton.Data;

        public override Texture mainTexture => s_WhiteTexture;

        protected override void Awake()
        {
            base.Awake();
            Initialize(false);
        }

        public void Initialize(bool overwrite)
        {
            if (_state != null && !overwrite)
            {
                return;
            }

            var data = skeletonDataAsset != null ? skeletonDataAsset.GetSkeletonData(true) : SkeletonData.Create(null);
            _skeleton = new Skeleton(data);
            _state = new AnimationState(new AnimationStateData(data));
            if (!string.IsNullOrEmpty(startingAnimation))
            {
                _state.SetAnimation(0, startingAnimation, startingLoop);
            }
            RefreshTint();
        }

        private void Update()
        {
            if (_state == null || _skeleton == null)
            {
                return;
            }

            float deltaTime = UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _state.Update(deltaTime * timeScale);
            _state.Apply(_skeleton);
            RefreshTint();
        }

        private void RefreshTint()
        {
            if (_skeleton == null)
            {
                return;
            }

            Color tint = PlaceholderStyle.Tint(_state, _skeleton.Data.BodyColor) * _skeleton.GetColor();
            if (tint != _tint)
            {
                _tint = tint;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Color32 vertexColor = color * _tint;
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), vertexColor, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), vertexColor, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), vertexColor, Vector2.one);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), vertexColor, new Vector2(1f, 0f));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
