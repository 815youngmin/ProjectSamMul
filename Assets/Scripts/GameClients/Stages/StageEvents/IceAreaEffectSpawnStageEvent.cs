using DG.Tweening;
using Shared.StaticDatas;
using UnityEngine;
using Z.ResourcePools;

namespace Z.GameClients.Stages.StageEvents
{
    public class IceAreaEffectSpawnStageEvent : StageEventBase
    {
        private readonly float SlowDuration = 1.5f;
        private readonly float SlowTick = 0.5f;
        private readonly float SlowRate = 0.5f;
        private float _slowAt;

        //임시 연출 이미지
        private const string EFFECT_BLACKSPRITE_RESOURCE_PATH = "Stages/StatusEffects/TestSlowStageEvent.prefab";
        private SpriteRenderer _blackSprite;
        private float _visionRadius = 10f;

        public IceAreaEffectSpawnStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {
        }
        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            //연출 생성

            //테스트 연출 이미지(쉐이더 기능을 통해 가운데 구멍이 뚫려있다.)
            _blackSprite = ResourcePool.Instance.InstantiateFromResource(EFFECT_BLACKSPRITE_RESOURCE_PATH).GetComponent<SpriteRenderer>();
            _blackSprite.gameObject.SetActive(true);
            _blackSprite.transform.SetParent(stage.PC.transform);
            _blackSprite.transform.localPosition = Vector3.zero;
            _blackSprite.transform.localScale = Vector3.one * 40f;
            _blackSprite.material.SetVector("_MaskScale", new Vector4(0.03f * _visionRadius, 0.03f * _visionRadius, 0f, 0f));
            var fadeInSequence = DOTween.Sequence();
            fadeInSequence.Append(_blackSprite.DOFade(1f, 0.2f));
            fadeInSequence.Play();

            _slowAt = Time.time;

        }
        public override void Update(Stage stage, int stageEventTickNumber)
        {
            if (StageEventTickMaxNumber < stageEventTickNumber)
            {
                return;
            }

            float now = Time.time;
            if(_slowAt <= now)
            {
                stage.PC.StatusEffects.AddOrUpdateStatusEffect(stage, stage.PC, Characters.StatusEffects.StatusEffectType.SlowMove, SlowDuration, now, SlowRate);
                _slowAt = now + SlowTick;
            }
        }

        public override void End(Stage stage)
        {
                var fadeOutSequence = DOTween.Sequence();
                fadeOutSequence.Append(_blackSprite.DOFade(0f, 1));
                fadeOutSequence.Play();
                fadeOutSequence.OnComplete(() =>
                {
                    _blackSprite.transform.SetParent(null);
                    _blackSprite.gameObject.SetActive(false);
                    ResourcePool.Instance.PutBackInstance(EFFECT_BLACKSPRITE_RESOURCE_PATH, _blackSprite.gameObject);
                    _blackSprite = null;
                });

        }
    }
}
