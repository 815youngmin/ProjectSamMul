#nullable enable
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaIndicators
{

    public class AreaIndicatorManager
    {
        #region AreaIndicator struct
        private readonly struct AreaIndicator
        {
            public readonly AreaIndicatorInstanceID InstanceID;
            public readonly GameObject AreaIndicatorGroup;
            public readonly GameObject InnerIndicator;
            public readonly GameObject OutlineIndicator;
            public readonly GameObject HoleIndicator;
            public readonly GameObject HoleLineIndicator;
            public readonly Sequence TweeningSequence;

            public AreaIndicator(AreaIndicatorInstanceID instanceID)//, GameObject inner, GameObject outline, Sequence tweeningSequence)
            {
                this.InstanceID = instanceID;
                this.AreaIndicatorGroup = new GameObject("AreaIndicatorGroup");

                this.InnerIndicator = new GameObject("innerIndicator");
                this.InnerIndicator.transform.SetParent(this.AreaIndicatorGroup.transform, false);

                this.OutlineIndicator = new GameObject("outlineIndicator");
                this.OutlineIndicator.transform.SetParent(this.AreaIndicatorGroup.transform, false);

                this.HoleIndicator = new GameObject("holeIndicator");
                this.HoleIndicator.transform.SetParent(this.AreaIndicatorGroup.transform, false);

                this.HoleLineIndicator = new GameObject("holeLineIndicator");
                this.HoleLineIndicator.transform.SetParent(this.AreaIndicatorGroup.transform, false);

                this.TweeningSequence = DOTween.Sequence(this.InnerIndicator.gameObject);
            }

            public void Destroy()
            {
                this.TweeningSequence.Kill();
                this.InnerIndicator.SetActive(false);
                this.OutlineIndicator.SetActive(false);
                this.HoleIndicator.SetActive(false);
                this.HoleLineIndicator.SetActive(false);
                this.AreaIndicatorGroup.SetActive(false);
                GameObject.Destroy(this.InnerIndicator);
                GameObject.Destroy(this.OutlineIndicator);
                GameObject.Destroy(this.HoleIndicator);
                GameObject.Destroy(this.HoleLineIndicator);
                GameObject.Destroy(this.AreaIndicatorGroup);
            }
        }
        #endregion

        private long _nextInstanceID;
        private Dictionary<AreaIndicatorInstanceID, AreaIndicator> _aliveIndicators;
        private readonly AttackAreaFlashManager _attackAreaFlashes;

        public AreaIndicatorManager(AttackAreaFlashManager attackAreaFlashes)
        {
            _attackAreaFlashes = attackAreaFlashes;
            this._nextInstanceID = 1;
            this._aliveIndicators = new Dictionary<AreaIndicatorInstanceID, AreaIndicator>();
        }

        private AreaIndicatorInstanceID IssueNextID()
        {
            return new AreaIndicatorInstanceID(_nextInstanceID++);
        }

        private AreaIndicator CreateIndicator()
        {
            var instanceId = this.IssueNextID();
            var indicator = new AreaIndicator(instanceId);
            this._aliveIndicators.Add(indicator.InstanceID, indicator);
            return indicator;
        }

        public void ForceRemoveIndicator(AreaIndicatorInstanceID indicatorID)
        {
            if (!this._aliveIndicators.Remove(indicatorID, out var indicator))
            {
                Debug.LogWarning($"인디케이터가 없는데 ForceRemoveIndicator호출했어요. 뭔가 이상합니다. 코드 다시 보세요.");
                return;
            }

            indicator.Destroy();
        }


        private static string _squareInidicatorPath = "Stage/Indicator/SquareIndicator.prefab";
        /// <summary>
        /// 공격 범위 사각형 형태의 인디케이터를 불러옵니다.
        /// 지속 시간 이후에 인디케이터는 사라집니다.
        /// </summary>
        /// <param name="center">중앙 위치값</param>
        /// <param name="width">가로 길이</param>
        /// <param name="height">세로 길이</param>
        /// <param name="angle">center를 기준으로 회전된 각도(degree)</param>
        /// <param name="duration">지속 시간</param>
        public AreaIndicatorInstanceID CreateSquareAttackRangeIndicator(
            Vector3 center, float width, float height, float angle, float duration)
        {
            var indicator = this.CreateIndicator();

            GameObject IndicatorObject = ResourcePool.Instance.InstantiateFromResource(_squareInidicatorPath);
            SpriteRenderer[] renderers = IndicatorObject.GetComponentsInChildren<SpriteRenderer>();
            IndicatorObject.transform.SetParent(indicator.AreaIndicatorGroup.transform);

            int renderCount = renderers.Length;
            Color[] originColors = new Color[renderCount];
            for (int i = 0; i < renderCount; ++i)
            {
                originColors[i] = renderers[i].color;
                renderers[i].sortingLayerID = SortingLayer.NameToID("HighParticle");
                if (renderers[i].material.name == "M_fxt_Multiply")
                {
                    renderers[i].sortingOrder = 9997;
                }
                else
                {
                    renderers[i].sortingOrder = 9998;
                }
            }
            Vector2 resultScale = new Vector2(width, height); // 0.1 => UnitPerPixel
            IndicatorObject.transform.localPosition = Vector3.zero;
            IndicatorObject.transform.localScale = Vector3.one * 0.1f;
            IndicatorObject.transform.localRotation = Quaternion.identity; //Quaternion.AngleAxis(angle, Vector3.back);

            indicator.AreaIndicatorGroup.transform.localPosition = center;
            indicator.AreaIndicatorGroup.transform.localScale = resultScale;
            indicator.AreaIndicatorGroup.transform.localRotation = Quaternion.Euler(0, 0, angle); //Quaternion.AngleAxis(angle, Vector3.back);

            float shakePeriod = duration / 3.0f;

            float trackIntervalRate = 0.04f; // 2.5초 기준 : 0.1
            float trackIntervalTimeUnit = duration * trackIntervalRate;

            for (int i = 0; i < renderCount; ++i)
            {
                SpriteRenderer renderer = renderers[i];
                Sequence alphaSequence = DOTween.Sequence();
                float alphaInterval = (shakePeriod - (trackIntervalTimeUnit * 3.0f)) * 0.5f;
                renderer.color = originColors[i];
                float minAlpha = originColors[i].a * 0.3f;
                float middleAlpha = originColors[i].a * 0.5f;
                alphaSequence.Append(renderer.DOFade(minAlpha, alphaInterval));
                alphaSequence.Append(renderer.DOFade(middleAlpha, alphaInterval));
                alphaSequence.AppendInterval(trackIntervalTimeUnit * 3.0f);// 1 cicle : 1 shakePeriod
                alphaSequence.Append(renderer.DOFade(originColors[i].a, trackIntervalTimeUnit));
                alphaSequence.Append(renderer.DOFade(middleAlpha, trackIntervalTimeUnit));
                alphaSequence.Append(renderer.DOFade(minAlpha, alphaInterval));
                alphaSequence.Append(renderer.DOFade(middleAlpha, alphaInterval));
                alphaSequence.AppendInterval(trackIntervalTimeUnit); // 2 cicle : 2 shakePeriod
                alphaSequence.Append(renderer.DOFade(originColors[i].a, trackIntervalTimeUnit));
                alphaSequence.Append(renderer.DOFade(middleAlpha, trackIntervalTimeUnit));
                alphaSequence.Append(renderer.DOFade(minAlpha, alphaInterval));
                alphaSequence.Append(renderer.DOFade(middleAlpha, alphaInterval));
                alphaSequence.Insert(duration - trackIntervalTimeUnit * 2.0f, renderer.DOFade(0.0f, trackIntervalTimeUnit));
                alphaSequence.OnComplete(() =>
                {
                    renderer.color = Color.clear;
                });
            }

            Sequence scaleSequence = DOTween.Sequence();
            scaleSequence.Append(indicator.AreaIndicatorGroup.transform.DOScale(resultScale, trackIntervalTimeUnit * 2.0f));
            scaleSequence.AppendInterval(shakePeriod - trackIntervalTimeUnit * 2.0f); // 1 cicle : 1 shakePeriod
            scaleSequence.Append(indicator.AreaIndicatorGroup.transform.DOScale(resultScale * 1.2f, trackIntervalTimeUnit));
            scaleSequence.Append(indicator.AreaIndicatorGroup.transform.DOScale(resultScale * 1.0f, trackIntervalTimeUnit));
            scaleSequence.AppendInterval(shakePeriod - trackIntervalTimeUnit * 2.0f); // 2 cicle : 2 shakePeriod
            scaleSequence.Append(indicator.AreaIndicatorGroup.transform.DOScale(resultScale * 1.2f, trackIntervalTimeUnit));
            scaleSequence.Append(indicator.AreaIndicatorGroup.transform.DOScale(resultScale * 1.0f, trackIntervalTimeUnit));
            scaleSequence.AppendInterval(shakePeriod - trackIntervalTimeUnit * 2.0f); // 해당 시퀀서는 투명 시퀀서보다 늦게 끝나야합니다.
            scaleSequence.OnComplete(() =>
            {
                for (int i = 0; i < renderCount; ++i)
                {
                    renderers[i].color = originColors[i];
                }

                ResourcePool.Instance.PutBackInstance(_squareInidicatorPath, IndicatorObject);
                this.ForceRemoveIndicator(indicator.InstanceID);
            });

            return indicator.InstanceID;
        }



        public AreaIndicatorInstanceID CreateSquareAttackRangeIndicator(SquareTargetArea squareArea, float duration)
        {
            return this.CreateSquareAttackRangeIndicator(squareArea.Center, squareArea.Size.x, squareArea.Size.y, squareArea.Angle, duration);
        }

        private static string _arrowIndicatorPath = "Stage/Indicator/ArrowIndicator.prefab";
        public AreaIndicatorInstanceID CreateDirectionalIndicator(Vector3 startPosition, Vector3 direction, float distance, float scale, float duration)
        {
            return this.CreateDirectionalIndicator(startPosition, direction, distance, speed: distance * 2f, scale, duration);
        }
        /// <summary>
        /// 방향성 표시 인디케이터 입니다.
        /// </summary>
        /// <param name="startPosition">시작 위치</param>
        /// <param name="direction">방향</param>
        /// <param name="distance">이동 거리</param>
        /// <param name="speed">방향성 화살표 속도</param>
        /// <param name="scale">방향성 화살표 크기</param>
        /// <param name="duration">인디케이터 지속시간</param>
        /// <returns></returns>
        public AreaIndicatorInstanceID CreateDirectionalIndicator(Vector3 startPosition,Vector3 direction, float distance, float speed, float scale, float duration)
        {
            var indicator = this.CreateIndicator();

            GameObject arrowObject = ResourcePool.Instance.InstantiateFromResource(_arrowIndicatorPath);
            SpriteRenderer[] arrowRenderers = arrowObject.GetComponentsInChildren<SpriteRenderer>();
            arrowObject.transform.SetParent(indicator.AreaIndicatorGroup.transform);
 
            arrowObject.transform.localScale = Vector3.one * scale;
            arrowObject.transform.right = direction;


            for (int i = 0; i < arrowRenderers.Length; ++i)
            {
                arrowRenderers[i].sortingLayerID = SortingLayer.NameToID("HighParticle");
                arrowRenderers[i].sortingOrder = 9999;
            }

            Vector3 indicatorStartPosition = startPosition + direction * scale * 0.1f;
            Vector3 indicatorEndPosition = startPosition + direction * (distance - scale *  0.1f);

            if(speed == 0)
            {
                Debug.LogError("speed가 0일수가 없는데.. 확인이 필요합니다.");
                return AreaIndicatorInstanceID.Invalid;
            }
            float arrowSequenceDuration = distance / speed;


            Sequence arrowSequence = DOTween.Sequence();
            arrowSequence.Append(arrowObject.transform.DOLocalMove(indicatorEndPosition, arrowSequenceDuration).From(indicatorStartPosition));
            for(int i = 0; i < arrowRenderers.Length; i++)
            {
                arrowSequence.Insert(0f, arrowRenderers[i].DOFade(0.8f, arrowSequenceDuration * 0.25f).From(0f));
            }
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                arrowSequence.Insert(arrowSequenceDuration * 0.25f, arrowRenderers[i].DOFade(0f, arrowSequenceDuration*0.5f));
            }
            arrowSequence.AppendInterval(arrowSequenceDuration * 0.1f);
            arrowSequence.SetLoops(-1);


            //종료 시퀀스
            Sequence fadeOutSequence = DOTween.Sequence();
            fadeOutSequence.AppendInterval(duration - 0.1f);
            fadeOutSequence.InsertCallback(duration - 0.1f,() =>
            {
                arrowSequence.Kill();
            });
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                fadeOutSequence.Join(arrowRenderers[i].DOFade(0f, 0.1f));
            }
            fadeOutSequence.OnComplete(() =>
            {

                ResourcePool.Instance.PutBackInstance(_arrowIndicatorPath, arrowObject);
                this.ForceRemoveIndicator(indicator.InstanceID);
            });


            return indicator.InstanceID;
        }


        /// <summary>
        /// 공격 범위를 알리는 원형 깜빡임 인디케이터. 전용 프리팹 없이 AttackAreaFlashManager 로 붉은 원을 duration 동안 깜빡이며 그린다.
        /// </summary>
        public AreaIndicatorInstanceID CreateBlinkCircularAttackRangeIndicator(Vector2 position, float radius, float duration)
        {
            const float BLINK_PERIOD = 0.5f;

            var indicator = this.CreateIndicator();
            var area = new CircularTargetArea(position, radius);
            var sequence = indicator.TweeningSequence;
            sequence.AppendInterval(duration);
            sequence.OnUpdate(() =>
            {
                float blink = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(sequence.Elapsed() / BLINK_PERIOD * Mathf.PI));
                var color = AttackAreaFlashManager.ENEMY_COLOR;
                color.a = blink;
                _attackAreaFlashes.Show(area, color);
            });
            sequence.OnComplete(() => this.ForceRemoveIndicator(indicator.InstanceID));

            return indicator.InstanceID;
        }

        /// <summary>
        /// 공격 방향성을 알려주는 원형 인디케이터 제작 함수입니다.
        /// </summary>
        /// <param name="position">인디케이터 생성 위치</param>
        /// <param name="radius">인디케이터 반지름</param>
        /// <param name="startDirection">시작 방향 </param>
        /// <param name="directionalRotateAmount">지속시간동안 돌 각도 값</param>
        /// <param name="duration">지속시간</param>
        /// <returns></returns>

        public AreaIndicatorInstanceID CreateCircularSectorAttackRangeIndicator(
            CircularSectorTargetArea targetArea, float duration)
        {
            return this.CreateCircularSectorAttackRangeIndicator(
                targetArea.Center, targetArea.Direction, targetArea.Angle, targetArea.Radius, duration);
        }

        public AreaIndicatorInstanceID CreateCircularSectorAttackRangeIndicator(Vector3 position, Vector3 direction, float angle, float radius, float duration)
        {
            var indicator = this.CreateIndicator();
            indicator.OutlineIndicator.transform.localScale = Vector3.one;
            indicator.InnerIndicator.transform.localScale = Vector3.zero;

            indicator.OutlineIndicator.transform.position = position;
            indicator.InnerIndicator.transform.position = position;

            MeshRenderer meshRenderer = AddMeshRender(indicator.InnerIndicator);
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;
            meshRenderer.material = ResourcePool.Instance.LoadResource<Material>("Stages/ETCEffects/CircleInner.mat");
            meshRenderer.material.color = new Color(1f, 1f, 1f, 0.8f);
            meshRenderer.sortingLayerID = SortingLayer.NameToID("HighParticle");

            int segments = (int)(64.0f * (angle / 360.0f));
            List<Vector3> vertces = new List<Vector3>(segments);
            List<Vector2> uvs = new List<Vector2>(segments);

            float x;
            float y;

            float dotValue = Vector3.Dot(Vector3.up, direction);
            float sinValue = Vector3.Cross(Vector3.up, direction).z;
            float diffAngle = Mathf.Acos(dotValue) * Mathf.Rad2Deg;
            diffAngle *= (0 < sinValue ? -1 : 1);
            diffAngle = diffAngle < 0 ? 360.0f + diffAngle : diffAngle;
            float calcAngle = diffAngle - (angle * 0.5f);
            float addAngle = (angle / (segments - 2));

            vertces.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 1; i < segments; ++i)
            {
                x = Mathf.Sin(Mathf.Deg2Rad * calcAngle);
                y = Mathf.Cos(Mathf.Deg2Rad * calcAngle);

                vertces.Add(new Vector3(x * radius, y * radius));
                uvs.Add(new Vector2((x +1)*0.5f, (y + 1)*0.5f));
                calcAngle += addAngle;
            }

            ushort count = 0;
            int indexSize = (segments - 2) * 3;
            int[] indices = new int[indexSize];
            for (int i = 0; i < indexSize - 1; i += 3)
            {
                indices[i] = 0;
                indices[i + 1] = (count + 1);
                indices[i + 2] = (count + 2);
                ++count;
            }

            mesh.vertices = vertces.ToArray();
            mesh.triangles = indices;
            mesh.uv = uvs.ToArray();

            vertces.Add(Vector3.zero);
            var lineRenderer = AddLineRenderer(indicator.OutlineIndicator, new Color(0.784313f, 0f, 0f, 1f));
            lineRenderer.enabled = true;
            lineRenderer.widthMultiplier = 0.15f;
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = segments + 1;
            lineRenderer.SetPositions(vertces.ToArray());

            indicator.TweeningSequence.Append(indicator.InnerIndicator.transform.DOScale(Vector3.one, duration)).OnComplete(() =>
            {
                this.ForceRemoveIndicator(indicator.InstanceID);
            });

            return indicator.InstanceID;
        }

        /// <summary>
        /// 스프라이트를 출력하기 위한 용도.
        /// </summary>
        /// <param name="setTarget"></param>
        /// <param name="spritePath"></param>
        /// <returns></returns>
        SpriteRenderer AddSpriteRenderer(GameObject setTarget, string spritePath)
        {
            var spriteRenderer = setTarget.AddComponent<SpriteRenderer>();
            Sprite sprite = ResourcePool.Instance.LoadResource<Sprite>(spritePath);
            spriteRenderer.sortingLayerID = SortingLayer.NameToID("LowShadow");
            spriteRenderer.sprite = sprite;

            return spriteRenderer;
        }

        SpriteRenderer AddSpriteRenderer(GameObject setTarget, string spritePath, float width, float height, Color color)
        {
            var spriteRenderer = setTarget.AddComponent<SpriteRenderer>();
            // NOTE: Indicator class 구현하고, 각 인디케이터마다 SpriteRender를 멤버로 가지게 한다. 인디케이터 Destroy할 때에 sprite 를 리소스 풀에 반환하도록 한다.
            // 리소스 자원관리를 이런식으로 하면 안된다. 객체지향에 맞춰서, 리소스 획득한 측에서 리소스 해제할 수 있게 구현해야 한다.
            Sprite sprite = ResourcePool.Instance.LoadResource<Sprite>(spritePath);
            spriteRenderer.sortingLayerID = SortingLayer.NameToID("LowShadow");
            spriteRenderer.sprite = sprite;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(width, height);
            spriteRenderer.color = color;

            return spriteRenderer;
        }

        LineRenderer AddLineRenderer(GameObject setTarget, Color color)
        {
            var lineRenderer = setTarget.AddComponent<LineRenderer>();

            lineRenderer.sortingLayerID = SortingLayer.NameToID("HighParticle");
            Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material = defaultMaterial;
            lineRenderer.material.color = color;

            return lineRenderer;
        }

        SpriteMask AddSpriteMask(GameObject setTarget, string maskSpritePath)
        {
            SpriteMask spriteMask = setTarget.AddComponent<SpriteMask>();
            // NOTE: Indicator class 구현하고, 각 인디케이터마다 SpriteRender를 멤버로 가지게 한다. 인디케이터 Destroy할 때에 sprite 를 리소스 풀에 반환하도록 한다.
            // 리소스 자원관리를 이런식으로 하면 안된다. 객체지향에 맞춰서, 리소스 획득한 측에서 리소스 해제할 수 있게 구현해야 한다.
            Sprite sprite = ResourcePool.Instance.LoadResource<Sprite>(maskSpritePath);
            spriteMask.sprite = sprite;

            return spriteMask;
        }

        MeshRenderer AddMeshRender(GameObject setTarget)
        {
            MeshRenderer meshRenderer = setTarget.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = setTarget.AddComponent<MeshFilter>();

            meshFilter.mesh = new Mesh();
            meshRenderer.sortingLayerID = SortingLayer.NameToID("LowShadow");

            return meshRenderer;
        }
    }
}
