using DG.Tweening;
using System.Linq;
using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.IndicatorObjects
{
    public class BlinkCircularAttackRangeIndicator : IndicatorObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _duration;

        private static string BlinkCircularAttackRangeInidicatorPath = "Stages/ETCEffects/Indicator/BlinkCircleIndicator.prefab";

        private float _createdAt;
        private float _duration;

        private GameObject _body;
        private Sequence _sequence;
        private SpriteRenderer[] _renderers;
        private Color[] _originColors;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(IndicatorType.BlinkCircularAttackRange);
            _body = ResourcePool.Instance.InstantiateFromResource(BlinkCircularAttackRangeInidicatorPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _renderers = _body.GetComponentsInChildren<SpriteRenderer>();
            _originColors = new Color[_renderers.Length];
            int renderCount = _renderers.Length;
            for (int i = 0; i < renderCount; ++i)
            {
                _originColors[i] = _renderers[i].color;
                _renderers[i].sortingLayerID = SortingLayer.NameToID("HighParticle");
                if (_renderers[i].material.name == "M_fxt_Multiply")
                {
                    _renderers[i].sortingOrder = 9997;
                }
                else
                {
                    _renderers[i].sortingOrder = 9998;
                }
            }
        }

        public void Initialize(Vector2 position, float radius, float duration)
        {
            float now = Time.time;
            _createdAt = now;
            _duration = duration;

            Vector3 resultScale = Vector3.one * radius * 2f;
            this.transform.localPosition = position;

            Sequence sequence = DOTween.Sequence(this.transform);

            Sequence outLineSequence = DOTween.Sequence();
            outLineSequence.Append(_renderers[0].transform.DOScale(resultScale, 0.15f).From(0));
            outLineSequence.Join(_renderers[0].DOFade(1f, 0.15f).From(0.3f));
            outLineSequence.Append(_renderers[0].DOFade(0f, 0.35f));
            outLineSequence.SetLoops(int.MaxValue);

            Sequence innerSequence = DOTween.Sequence();
            innerSequence.Append(_renderers[1].DOFade(1f, 0.35f).From(0.2f));
            innerSequence.Join(_renderers[1].transform.DOScale(resultScale * 0.8f, 0.5f).From(0));
            innerSequence.Insert(0.35f, _renderers[1].DOFade(0f, 0.15f));
            innerSequence.SetLoops(int.MaxValue);

            sequence.Append(outLineSequence);
            sequence.Join(innerSequence);

            _sequence = sequence;
            _sequence.Play();
        }

        public override void PuttingBackToPool()
        {
            if (_sequence != null)
            {
                _sequence.Kill();
                _sequence = null;
            }

            for (int i = 0; i < _renderers.Length; ++i)
            {
                _renderers[i].color = _originColors[i];
            }

            base.PuttingBackToPool();
        }
    }
}
