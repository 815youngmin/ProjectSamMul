using DG.Tweening;
using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.IndicatorObjects
{
    public class SquareAttackRangeIndicator : IndicatorObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _duration;

        private static readonly string SquareAttackRangeInidicatorPath = "Stages/ETCEffects/Indicator/SquareAttackRangeIndicator.prefab";

        private float _createdAt;
        private float _duration;

        private GameObject _body;
        private Sequence _sequence;
        private SpriteRenderer[] _renderers;
        private Color[] _originColors;

        private static readonly float SacleChangeDuration = 0.1f;
        private static readonly float ScaleChangeWaitDuration = 0.2f;

        private static readonly float AlphaFadeinDuration = 0.2f;
        private static readonly float AlphaFadeoutDuration = 0.1f;

        private static readonly float AlphaChangeDuration = 0.1f;
        private static readonly float AlphaChangeWaitDuration = 0.2f;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(IndicatorType.SquareAttackRange);
            _body = ResourcePool.Instance.InstantiateFromResource(SquareAttackRangeInidicatorPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 0.1f;

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

        public void Initialize(Vector3 center, float width, float height, float angle, float duration)
        {
            float now = Time.time;
            _createdAt = now;
            _duration = duration;

            Vector2 resultScale = new Vector2(width, height); // 0.1 => UnitPerPixel
            this.transform.localPosition = center;
            this.transform.localScale = resultScale;
            this.transform.localRotation = Quaternion.Euler(0, 0, angle);

            int renderCount = _renderers.Length;

            Sequence sequence = DOTween.Sequence(this.transform);
            //알파 연출 시퀀스
            for (int i = 0; i < renderCount; ++i)
            {
                SpriteRenderer renderer = _renderers[i];
                renderer.color = _originColors[i];
                float minAlpha = _originColors[i].a * 0.4f;
                float middleAlpha = _originColors[i].a * 0.6f;
                Sequence alphaSequence = DOTween.Sequence();
                alphaSequence.Append(renderer.DOFade(_originColors[i].a, AlphaFadeinDuration).From(0));

                int loopCount = (int)((duration - AlphaFadeinDuration - AlphaFadeoutDuration) / (AlphaChangeWaitDuration + AlphaChangeDuration * 4f));

                if (loopCount > 0)
                {
                    Sequence alphaPumpingSequence = DOTween.Sequence();
                    alphaPumpingSequence.AppendInterval(AlphaChangeWaitDuration);// 1 cicle : 1 shakePeriod
                    alphaPumpingSequence.Append(renderer.DOFade(middleAlpha, AlphaChangeDuration));
                    alphaPumpingSequence.Append(renderer.DOFade(minAlpha, AlphaChangeDuration));
                    alphaPumpingSequence.Append(renderer.DOFade(middleAlpha, AlphaChangeDuration));
                    alphaPumpingSequence.Append(renderer.DOFade(_originColors[i].a, AlphaChangeDuration));
                    alphaPumpingSequence.SetLoops(loopCount);
                    alphaSequence.Append(alphaPumpingSequence);
                }

                alphaSequence.Insert(duration - AlphaFadeoutDuration, renderer.DOFade(0.0f, AlphaFadeoutDuration));

                if (i == 0)
                {
                    sequence.Append(alphaSequence);
                }
                else
                {
                    sequence.Join(alphaSequence);
                }
            }

            //사이즈 조절 시퀀스
            Sequence scaleSequence = DOTween.Sequence();
            scaleSequence.Append(transform.DOScale(resultScale, SacleChangeDuration * 2.0f).From(Vector3.zero));

            Sequence scalePumpingSequence = DOTween.Sequence();
            scalePumpingSequence.AppendInterval(ScaleChangeWaitDuration); // 1 cicle : 1 shakePeriod
            scalePumpingSequence.Append(transform.DOScale(resultScale * 1.2f, SacleChangeDuration));
            scalePumpingSequence.Append(transform.DOScale(resultScale * 1.0f, SacleChangeDuration));
            scalePumpingSequence.AppendInterval(ScaleChangeWaitDuration); // 1 cicle : 1 shakePeriod
            scalePumpingSequence.Append(transform.DOScale(resultScale * 1.2f, SacleChangeDuration));
            scalePumpingSequence.Append(transform.DOScale(resultScale * 1.0f, SacleChangeDuration));
            scalePumpingSequence.AppendInterval(ScaleChangeWaitDuration); // 1 cicle : 1 shakePeriod
            scalePumpingSequence.SetLoops(int.MaxValue);
            scaleSequence.Append(scalePumpingSequence);

            sequence.Join(scaleSequence);

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

            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = Vector3.one * 0.1f;
            _body.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < _renderers.Length; ++i)
            {
                _renderers[i].color = _originColors[i];
            }

            base.PuttingBackToPool();
        }
    }
}
