using DG.Tweening;
using Shared.GameDataTypes;
using System.Linq;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class InvisibleStatusEffect : StatusEffect
    {
        private Sequence _fadeOutSequence;
        private Sequence _fadeInSequence;
        private float _createAt;

        private bool _isFadeIn;

        public InvisibleStatusEffect(float duration) : base(StatusEffectType.Invisible, duration)
        {
        }
        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);

            owner.SetImmuneToHit();
            owner.SetImmuneToKnockBack();

            _fadeOutSequence = DOTween.Sequence();
            _fadeOutSequence.Append(DOTween.To(() => 1f, x => owner.AnimationController.SetBodyAlpha(x), 0.05f, 0.3f));

            _fadeInSequence = DOTween.Sequence();
            _fadeInSequence.Append(DOTween.To(() => 0.05f, x => owner.AnimationController.SetBodyAlpha(x), 1f, 0.3f));
            _fadeInSequence.Pause();

            _isFadeIn = false;

            _createAt = now;
        }

        public override void Cancel(Character owner, Stage stage)
        {
            owner.UnsetImmuneToHit();
            owner.UnsetImmuneToKnockBack();
            if (_fadeOutSequence != null)
            {
                _fadeOutSequence.Kill();
                _fadeOutSequence = null;

                _fadeInSequence.Kill();
                _fadeInSequence = null;

                owner.AnimationController.SetBodyAlpha(1f);
            }
        }

        public override void End(Stage stage, Character owner, float now)
        {
            owner.UnsetImmuneToHit();
            owner.UnsetImmuneToKnockBack();
            if (_fadeOutSequence != null)
            {
                _fadeOutSequence.Kill();
                _fadeOutSequence = null;

                _fadeInSequence.Kill();
                _fadeInSequence = null;

                owner.AnimationController.SetBodyAlpha(1f);
            }
        }

        public override void Update(Stage stage, Character owner, float now)
        {
            //투명화 끝나기 전 연출 재생
            if(now > _createAt + Duration - 0.3f && 
                !_isFadeIn)
            {
                _fadeInSequence.Restart();
                _isFadeIn = true;
            }
        }

    }
}
