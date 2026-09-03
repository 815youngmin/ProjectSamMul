using DG.Tweening;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.IndicatorObjects
{
    public class DirectionalSquareRangeIndicator : IndicatorObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _duration;

        private static string SquareAttackRangeInidicatorPath = "Stages/ETCEffects/Indicator/SquareAttackRangeIndicator.prefab";
        private static string DirectionalIndicatorPath = "Stages/ETCEffects/Indicator/DirectionalIndicator.prefab";

        private float _thickness;
        private float _createdAt;
        private float _duration;
        private float _distance;
        private Vector2 _startPosition;
        private Vector2 _direction;

        private GameObject _directionalObject;
        private GameObject _squareObject;

        private Sequence _sequence;
        private SpriteRenderer[] _squareRenderers;
        private SpriteRenderer[] _directionalRenderers;
        private Color[] _squareOriginColors;
        private Color[] _directionalOriginColors;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(IndicatorType.DirectionalSquareRange);
            _squareObject= new GameObject("square");
            _squareObject.transform.SetParent(this.transform);
            _squareObject.transform.localPosition = Vector3.zero;
            _squareObject.transform.localScale = Vector3.one;
            _squareObject.transform.localRotation = Quaternion.identity;

            GameObject square = ResourcePool.Instance.InstantiateFromResource(SquareAttackRangeInidicatorPath);
            square.transform.SetParent(_squareObject.transform);
            square.transform.localPosition = Vector2.zero;
            square.transform.localScale = Vector2.one * 0.1f;

            _squareRenderers = square.GetComponentsInChildren<SpriteRenderer>();
            _squareOriginColors = new Color[_squareRenderers.Length];
            int renderCount = _squareRenderers.Length;
            for (int i = 0; i < renderCount; ++i)
            {
                _squareOriginColors[i] = _squareRenderers[i].color;
                _squareRenderers[i].sortingLayerID = SortingLayer.NameToID("HighParticle");
                if (_squareRenderers[i].material.name == "M_fxt_Multiply")
                {
                    _squareRenderers[i].sortingOrder = 9997;
                }
                else
                {
                    _squareRenderers[i].sortingOrder = 9998;
                }
            }

            _directionalObject = ResourcePool.Instance.InstantiateFromResource(DirectionalIndicatorPath);
            _directionalObject.transform.SetParent(this.transform);
            _directionalObject.transform.localScale = Vector3.one;
            _directionalObject.transform.localPosition = Vector3.zero;
            _directionalObject.transform.localRotation = Quaternion.identity;

            _directionalRenderers = _directionalObject.GetComponentsInChildren<SpriteRenderer>();
            _directionalOriginColors = new Color[_directionalRenderers.Length];
            for (int i = 0; i < _directionalRenderers.Length; ++i)
            {
                _directionalOriginColors[i] = _directionalRenderers[i].color;  
                _directionalRenderers[i].sortingLayerID = SortingLayer.NameToID("HighParticle");
                _directionalRenderers[i].sortingOrder = 9999;
            }
        }

        public void Initialize(Vector2 startPosition, Vector2 direction, float thickness, float distance, float duration)
        {
            float now = Time.time;
            _startPosition = startPosition;
            _direction = direction;
            _createdAt = now;
            _duration = duration;
            _distance = distance;
            _thickness = thickness;

            _squareObject.gameObject.SetActive(false);
            _directionalObject.gameObject.SetActive(false);

            _sequence = DOTween.Sequence(this.transform);

            _sequence.AppendCallback(() =>
            {
                _squareObject.gameObject.SetActive(true);
                _directionalObject.gameObject.SetActive(true);
            });
            Sequence transformSequence = DOTween.Sequence(this.transform);
            transformSequence.Append(DOTween.To(() => 0f, currentDistance =>
            {
                Vector2 direction = _direction.normalized;
                Vector2 resultScale = new Vector2(currentDistance, _thickness);
                Vector2 resultPosition = _startPosition + 0.5f * currentDistance * direction;
                float angle = Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x);

                _squareObject.transform.localScale = resultScale;
                _squareObject.transform.localPosition = resultPosition;
                _squareObject.transform.localRotation = Quaternion.Euler(0, 0, angle);

                _directionalObject.transform.localScale = Vector3.one * _thickness;
                float halfDistance = _distance * 0.5f;
                float adjustedValue = currentDistance % halfDistance;
                Vector2 currentArrowPosition = _startPosition +  (currentDistance-1) * direction;
                _directionalObject.transform.localPosition = Vector2.Lerp(_startPosition, currentArrowPosition, adjustedValue / halfDistance);
                _directionalObject.transform.localRotation = Quaternion.Euler(0, 0, angle);

            }, distance, duration));
            _sequence.Append(transformSequence);

            Sequence alphaSequence = DOTween.Sequence(this.transform);
            for (int i = 0; i < _squareRenderers.Length; ++i)
            { 
                if(i == 0)
                {
                    alphaSequence.Append(_squareRenderers[i].DOFade(_squareRenderers[i].color.a, duration).From(_squareRenderers[i].color.a * 0.5f));
                }
                else
                {
                    alphaSequence.Join(_squareRenderers[i].DOFade(_squareRenderers[i].color.a, duration).From(_squareRenderers[i].color.a * 0.5f));
                }
            }
            for (int i = 0; i < _directionalRenderers.Length; ++i)
            {
                alphaSequence.Join(_directionalRenderers[i].DOFade(_directionalRenderers[i].color.a, duration).From(_directionalRenderers[i].color.a * 0.5f));
            }

            _sequence.Join(alphaSequence);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);
        }


        public override void PuttingBackToPool()
        {
            if( _sequence != null )
            {
                _sequence.Kill();
                _sequence = null;
            }

            _squareObject.transform.localPosition = Vector3.zero;
            _squareObject.transform.localScale = Vector3.one;
            _squareObject.transform.localRotation = Quaternion.identity;

            _directionalObject.transform.localPosition = Vector3.zero;
            _directionalObject.transform.localScale = Vector3.one;
            _directionalObject.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < _squareRenderers.Length; ++i)
            {
                _squareRenderers[i].color = _squareOriginColors[i];
            }

            for (int i = 0; i < _directionalRenderers.Length; ++i)
            {
                _directionalRenderers[i].color = _directionalOriginColors[i];
            }

            base.PuttingBackToPool();
        }
    }
}
