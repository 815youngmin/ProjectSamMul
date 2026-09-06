#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaIndicators
{
    /// <summary>
    /// 플레이어 공격이 실제로 판정한 범위를 잠깐 보여 주고 사라지는 공용 이펙트.
    /// 전용 공격 이펙트 리소스가 없는 데모에서 공격 범위를 확인하기 위한 용도로, 판정 도형(원·사각·부채꼴)을 메시로 그린다.
    /// CombatSystem.HitOnTargetArea 에서 플레이어 진영의 공격일 때 호출된다.
    /// </summary>
    public class AttackAreaFlashManager
    {
        private const float DURATION = 0.15f;
        private const float START_ALPHA = 0.6f;
        private const int FULL_CIRCLE_SEGMENTS = 40;
        private const string SORTING_LAYER_NAME = "LowParticle";
        private static readonly Color FLASH_COLOR = new Color(0.15f, 0.4f, 1f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private class Flash
        {
            public readonly GameObject Object;
            public readonly MeshRenderer Renderer;
            public readonly Mesh Mesh;
            public float ElapsedTime;

            public Flash(Transform root, Material material)
            {
                this.Object = new GameObject("AttackAreaFlash");
                this.Object.transform.SetParent(root, false);
                this.Mesh = new Mesh { name = "AttackAreaFlash" };
                this.Mesh.MarkDynamic();
                this.Object.AddComponent<MeshFilter>().sharedMesh = this.Mesh;
                this.Renderer = this.Object.AddComponent<MeshRenderer>();
                this.Renderer.sharedMaterial = material;
                this.Renderer.sortingLayerName = SORTING_LAYER_NAME;
                this.Renderer.shadowCastingMode = ShadowCastingMode.Off;
                this.Renderer.receiveShadows = false;
                this.Renderer.lightProbeUsage = LightProbeUsage.Off;
                this.Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private static Material? s_material;
        private static Material SharedMaterial
        {
            get
            {
                if (s_material == null)
                {
                    s_material = new Material(Shader.Find("Sprites/Default"))
                    {
                        name = "AttackAreaFlash",
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                }
                return s_material;
            }
        }

        private readonly List<Flash> _aliveFlashes = new List<Flash>();
        private readonly Stack<Flash> _freeFlashes = new Stack<Flash>();
        private readonly List<Vector3> _vertices = new List<Vector3>(FULL_CIRCLE_SEGMENTS + 2);
        private readonly List<int> _triangles = new List<int>(FULL_CIRCLE_SEGMENTS * 3);
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private GameObject? _root;

        public void Show(CircularTargetArea area) => this.ShowFan(area.Center, Vector2.right, area.Radius, 360f);

        public void Show(CircularSectorTargetArea area) => this.ShowFan(area.Center, area.Direction, area.Radius, area.Angle);

        public void Show(SquareTargetArea area)
        {
            var flash = this.Take();
            var half = area.Size * 0.5f;
            _vertices.Clear();
            _triangles.Clear();
            _vertices.Add(new Vector3(-half.x, -half.y));
            _vertices.Add(new Vector3(-half.x, half.y));
            _vertices.Add(new Vector3(half.x, half.y));
            _vertices.Add(new Vector3(half.x, -half.y));
            _triangles.Add(0); _triangles.Add(1); _triangles.Add(2);
            _triangles.Add(0); _triangles.Add(2); _triangles.Add(3);
            this.Apply(flash, area.Center, Quaternion.AngleAxis(area.Angle, Vector3.forward));
        }

        /// <param name="angle">direction 기준으로 벌어진 전체 각도(degree). 360이면 원.</param>
        private void ShowFan(Vector2 center, Vector2 direction, float radius, float angle)
        {
            var flash = this.Take();
            int segments = Mathf.Max(3, Mathf.CeilToInt(FULL_CIRCLE_SEGMENTS * angle / 360f));
            float startDegree = Vector2.SignedAngle(Vector2.right, direction) - angle * 0.5f;

            _vertices.Clear();
            _triangles.Clear();
            _vertices.Add(Vector3.zero);
            for (int i = 0; i <= segments; ++i)
            {
                float radian = (startDegree + angle * i / segments) * Mathf.Deg2Rad;
                _vertices.Add(new Vector3(Mathf.Cos(radian), Mathf.Sin(radian)) * radius);
            }
            for (int i = 1; i <= segments; ++i)
            {
                _triangles.Add(0); _triangles.Add(i); _triangles.Add(i + 1);
            }
            this.Apply(flash, center, Quaternion.identity);
        }

        private void Apply(Flash flash, Vector2 center, Quaternion rotation)
        {
            flash.Mesh.Clear();
            flash.Mesh.SetVertices(_vertices);
            flash.Mesh.SetTriangles(_triangles, 0);
            flash.Mesh.RecalculateBounds();

            flash.Object.transform.SetPositionAndRotation(new Vector3(center.x, center.y, 0f), rotation);
            flash.ElapsedTime = 0f;
            this.SetAlpha(flash, START_ALPHA);
            flash.Object.SetActive(true);
            _aliveFlashes.Add(flash);
        }

        public void Update(float deltaTime)
        {
            for (int i = _aliveFlashes.Count - 1; i >= 0; --i)
            {
                var flash = _aliveFlashes[i];
                flash.ElapsedTime += deltaTime;
                if (flash.ElapsedTime >= DURATION)
                {
                    flash.Object.SetActive(false);
                    _aliveFlashes.RemoveAt(i);
                    _freeFlashes.Push(flash);
                    continue;
                }
                this.SetAlpha(flash, START_ALPHA * (1f - flash.ElapsedTime / DURATION));
            }
        }

        public void Clear()
        {
            _aliveFlashes.Clear();
            _freeFlashes.Clear();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        private Flash Take()
        {
            if (_freeFlashes.Count > 0)
            {
                return _freeFlashes.Pop();
            }
            _root ??= new GameObject("@AttackAreaFlashRoot");
            return new Flash(_root.transform, SharedMaterial);
        }

        private void SetAlpha(Flash flash, float alpha)
        {
            var color = FLASH_COLOR;
            color.a = alpha;
            _propertyBlock.SetColor(ColorId, color);
            flash.Renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
