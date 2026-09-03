using Shared.GameDataTypes;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;

namespace SamMul.GameClients.Stages.Characters.Animations
{
    public class SpineMonsterAnimationController : MonsterAnimationController
    {
        private readonly SkeletonAnimation _body;
        private readonly CharacterType _characterType;
        public SkeletonAnimation Body => _body;
        public override float BodyLocalScale => 1f;

        private Animation _walk;
        private readonly Animation _idle;
        private readonly Animation _dead;
        private readonly Animation _hitted; // 영문법과는 맞지 않지만, 피격됨을 명시적으로 나타내기 위해 -ed를 붙인다.

        private readonly Animation _attack;
        private readonly Animation _appear;
        private readonly Animation _disappear;
        private readonly Animation _heal;

        public override float AttackAnimationDuration => _attack?.Duration ?? 0f;
        public override float AppearAnimationDuration => _appear?.Duration ?? 0f;
        public override float DeadAnimationDuration => _dead?.Duration ?? 0.0f;
        public override float DisappearAnimationDuration => _disappear?.Duration ?? 0.0f;

        private readonly EventData HitFrameEvent;

        public override bool IsFlippedX => _body.skeleton.ScaleX < 0f;

        public override bool IsPlayingMovement
        {
            get
            {
                var currentAnimation = _body.AnimationState.GetCurrent((int)BodyAnimationTrack.WholeBody)?.Animation;
                return currentAnimation == _walk;
            }
        }

        public SpineMonsterAnimationController(
            SkeletonAnimation body,
            Renderer renderer,
            bool isPlayerOrBoss,
            CharacterType characterType,
            string idleAnimationName,
            string walkAnimationName,
            string hittedAnimationName,
            string attackAnimationName,
            string deadAnimationName,
            string appearAnimationName,
            string disappearAnimationName,
            string healAnimationName
            ) : base(isPlayerOrBoss, renderer, "Spine/Skeleton Fill")
        {
            _body = body;
            _body.transform.localScale = new Vector3(BodyLocalScale, BodyLocalScale, 1.0f);
            _characterType = characterType;
            _hitted = this.FindAnimation(hittedAnimationName);
            _dead = this.FindAnimation(deadAnimationName);
            _attack = this.FindAnimation(attackAnimationName);
            _appear = this.FindAnimation(appearAnimationName);
            _disappear = this.FindAnimation(disappearAnimationName) ?? _dead;
            _heal = this.FindAnimation(healAnimationName);
            _idle = this.FindAnimation(idleAnimationName);
            _walk = this.FindAnimation(walkAnimationName);

            this.SetToInitialState();

            if (null != _attack)
            {
                _body.AnimationState.Data.SetMix(_idle, _attack, 0.00f);
                _body.AnimationState.Data.SetMix(_attack, _idle, 0.000f);
            }

            this.HitFrameEvent = _body.Skeleton.Data.FindEvent("hit");

            this.SettingGraphicsToCharacterType(_characterType);
        }

        private void SettingGraphicsToCharacterType(CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Egypt_BossRa:
                    {
                        this.SetAnimationMix(this.FindAnimation("idle"), this.FindAnimation("move"), 0.5f);
                        this.SetAnimationMix(this.FindAnimation("move"), this.FindAnimation("idle"), 0.5f);
                        Body.tintBlack = true;
                    }
                    break;
                case CharacterType.Troy_BossTrojanHorse:
                    {
                        this.SetAnimationMix(this.FindAnimation("move_b"), this.FindAnimation("move_f"), 0.5f);
                        this.SetAnimationMix(this.FindAnimation("move_f"), this.FindAnimation("move_b"), 0.5f);
                    }
                    break;
                case CharacterType.ThreeKingdom_Dongzhuo:
                    {
                        Body.tintBlack = true;
                    }
                    break;
                case CharacterType.Spaceship_Boss:
                    {
                        this.SetAnimationMix(this.FindAnimation("attack02_ready"), this.FindAnimation("attack02_smash"), 0f);
                        this.SetAnimationMix(this.FindAnimation("attack02_smash"), this.FindAnimation("attack02_ready"), 0f);
                    }
                    break;
                case CharacterType.Viking_BossLoki:
                    {
                        this.SetAnimationMix(this.FindAnimation("die"), this.FindAnimation("attack02"), 0f);
                        this.SetAnimationMix(this.FindAnimation("attack02"), this.FindAnimation("die"), 0f);
                    }
                    break;
                case CharacterType.Viking_BossKraken:
                    {
                        this.SetAnimationMix(this.FindAnimation("attack02"), this.FindAnimation("attack02-03-04_ready"), 0f);
                        this.SetAnimationMix(this.FindAnimation("attack02-03-04_ready"), this.FindAnimation("attack02"), 0f);
                    }
                    break;
                case CharacterType.Viking_BossFreya:
                    {
                        Body.tintBlack = true;
                    }
                    break;
                case CharacterType.Crusades_EasternEmpireShip:
                    {
                        this.SetAnimationMix(this.FindAnimation("attack01_fire"), this.FindAnimation("attack01_normal"), 0.5f);
                        this.SetAnimationMix(this.FindAnimation("attack01_normal"), this.FindAnimation("attack01_fire"), 0.5f);
                    }
                    break;
                case CharacterType.EraMix_Boss_Freya:
                    {
                        Body.tintBlack = true;
                    }
                    break;
                default:
                    break;
            }
        }

        // Idle 액션은 상체 애니메이션으로 처리한다. 공격하지 않으면 Idle이다. 이동과 무관하다.
        // 이동종료는 Idle재생이 아니라 StopWalk로 처리한다.
        public override void PlayIdleAttackActionInfinitely()
        {
            // 몬스터는 여기서 Idle애니메이션 재생
            if (_idle == null)
            {
                return;
            }

            if (this.GetCurrentAnimation(BodyAnimationTrack.WholeBody) == _idle)
            {
                return;
            }

            this.SetAnimation(BodyAnimationTrack.WholeBody, _idle, loop: true);
        }

        public void ChangeMoveAnimation(Animation newMoveAnimation)
        {
            var previousWalkAnimation = _walk;
            _walk = newMoveAnimation;

            var currentEntry = this.GetCurrentEntry(BodyAnimationTrack.WholeBody);

            var currentAnimation = currentEntry?.Animation;
            if (currentAnimation == previousWalkAnimation)
            {
                this.SetEmptyAnimation(BodyAnimationTrack.WholeBody, 0f);
                this.SetAnimation(BodyAnimationTrack.WholeBody, _walk, loop: true);
            }

            if (currentAnimation == _hitted)
            {
                // 히트 재생중엔 히트끝난 뒤에 이동애니메이션 재생한다.
                _body.AnimationState.ClearNext(currentEntry);
                this.ContinueAnimation(BodyAnimationTrack.WholeBody, _walk, loop: true, delay: 0f);
                return;
            }
        }

        public override void PlayMoveInfinitely(Vector2 moveVector)
        {
            if (_walk == null)
            {
                return;
            }

            var currentAnimation = this.GetCurrentAnimation(BodyAnimationTrack.WholeBody);
            if (currentAnimation == _walk)
            {
                return;
            }

            if (currentAnimation == _hitted)
            {
                // 히트 재생중엔 히트끝난 뒤에 이동애니메이션 재생한다.
                this.ContinueAnimation(BodyAnimationTrack.WholeBody, _walk, loop: true, delay: 0f);
                return;
            }

            this.SetAnimation(BodyAnimationTrack.WholeBody, _walk, loop: true);
        }

        /// <summary>
        /// 공격 애니메이션을 재생한다.
        /// </summary>
        public override void PlayAttackForce(float attackDuration)
        {
            if (_attack == null)
            {
                return;
            }

            float timeScale = _attack.Duration / attackDuration;
            if (timeScale < 1f)
            {
                // 공격 애니메이션을 느리게 재생하지는 않는다.
                // 공격을 정상속도로하고, 남은시간동안 그냥 그자세로 대기한다.
                timeScale = 1f;
            }

            var trackEntry = this.SetAnimation(BodyAnimationTrack.WholeBody, _attack, loop: false);
            trackEntry.TimeScale = timeScale;
            this.ContinueAnimation(BodyAnimationTrack.WholeBody, _idle, true, 0.0f);
        }

        public override void StopAttack()
        {
            if (_attack == null)
            {
                return;
            }

            if (this.GetCurrentAnimation(BodyAnimationTrack.WholeBody) != _attack)
            {
                return;
            }

            this.SetAnimation(BodyAnimationTrack.WholeBody, _idle, loop: true);
        }

        /// <param name="continuePreviousAnimation">
        /// Hit 애니메이션은 임시 애니메이션으로, 동일 트랙에 앞서 재생중인 애니메이션이 있었다면, hit가 끝나면 다시 재생해준다.
        /// </param>
        public override void PlayHitted(bool continuePreviousAnimation)
        {
            if (this.IsSkipHitAnimation)
            {
                return;
            }

            if (_hitted == null)
            {
                return;
            }

            Animation previousAnimation = null;
            bool wasLoopped = false;
            if (continuePreviousAnimation)
            {
                previousAnimation = this.GetCurrentAnimation(BodyAnimationTrack.WholeBody);
                if (previousAnimation == _hitted)
                {
                    // 앞에 Hit가 재생되고 있었다면, 구태여 이어서 다시 재생하지 않는다.
                    previousAnimation = null;
                }
                if (previousAnimation != null)
                {
                    wasLoopped = _body.AnimationState.GetCurrent((int)BodyAnimationTrack.WholeBody).Loop;
                }
            }

            this.SetAnimation(BodyAnimationTrack.WholeBody, _hitted, loop: false);
            if (previousAnimation != null &&
                continuePreviousAnimation)
            {
                this.ContinueAnimation(BodyAnimationTrack.WholeBody, previousAnimation, wasLoopped, delay: 0f);
            }
        }

        public override void StopAllAndPlayDead()
        {
            if (_dead == null)
            {
                return;
            }

            _body.AnimationState.ClearTracks();
            _body.StopAllCoroutines();
            _body.Skeleton.SetToSetupPose();
            this.SetAnimation(BodyAnimationTrack.WholeBody, _dead, loop: false);

        }

        public override void PlayAppear(float appearDuration)
        {
            if (_appear == null)
            {
                return;
            }

            float timeScale = _appear.Duration / appearDuration;

            _body.AnimationState.ClearTracks();
            this.SetAnimation(BodyAnimationTrack.WholeBody, _appear, loop: false).TimeScale = timeScale;
        }

        public override void PlayDisappear()
        {
            if (_disappear == null)
            {
                return;
            }

            _body.AnimationState.ClearTracks();
            this.SetAnimation(BodyAnimationTrack.WholeBody, _disappear, loop: false);
        }

        public Animation FindAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return null;
            }
            return _body.Skeleton.Data.FindAnimation(animationName);
        }

        public override void UpdateBodyDirectionByMoveDirection(Vector2 moveDir)
        {
            bool isMovingToLeft = moveDir.x <= 0;

            _body.Skeleton.ScaleX = isMovingToLeft ? 1.0f : -1.0f;
        }

        public override void SetToInitialState()
        {
            _body.transform.rotation = Quaternion.identity;
            _body.AnimationState.ClearTracks();
            _body.skeleton.SetToSetupPose();

            _activeBodyEffects.Clear();
            this.ClearBodyEffectShader();
        }

        public Animation GetCurrentAnimation(BodyAnimationTrack track)
        {
            return this.GetCurrentEntry(track)?.Animation;
        }

        public TrackEntry GetCurrentEntry(BodyAnimationTrack track)
        {
            return _body.AnimationState.GetCurrent((int)track);
        }

        /// <summary>
        /// 애니매이션을 즉시 재생한다. 트랙에 현재 재생중인 애니메이션이 있으면, 해당 애니메이션은 즉시 중지된다.
        /// </summary>
        public TrackEntry SetAnimation(BodyAnimationTrack track, Animation animation, bool loop)
        {
            return _body.AnimationState.SetAnimation((int)track, animation, loop);
        }
        public TrackEntry SetAnimation(int track, Animation animation, bool loop)
        {
            return _body.AnimationState.SetAnimation(track, animation, loop);
        }
        /// <summary>
        /// 빈 애니매이션을 즉시 재생한다. 트랙에 현재 재생중인 애니메이션이 있으면, 해당 애니메이션은 즉시 중지된다.
        /// </summary>
        public TrackEntry SetEmptyAnimation(BodyAnimationTrack track, float mixDuration)
        {
            return _body.AnimationState.SetEmptyAnimation((int)track, mixDuration);
        }
        public TrackEntry SetEmptyAnimation(int track, float mixDuration)
        {
            return _body.AnimationState.SetEmptyAnimation(track, mixDuration);
        }
        /// <summary>
        /// 애니매이션을 즉시 재생한다
        /// 지속 시간과 애니메이션 재생 시간이 다르면 재생속도를 조절해 지속 시간동안 재생된다. 
        /// </summary>
        public TrackEntry SetAnimation(BodyAnimationTrack track, Animation animation, bool loop, float duration)
        {
            float timeScale = animation.Duration / duration;
            var entry = _body.AnimationState.SetAnimation((int)track, animation, loop);
            entry.TimeScale = timeScale;
            return entry;
        }

        public void SetAnimationMix(Animation from, Animation to, float duration)
        {
            _body.AnimationState.Data.SetMix(from, to, duration);
        }

        /// <summary>
        /// 지정한 트랙에 빈 애니메이션을 추가해준다. 앞선 애니메이션의 마지막 프레임 모습 이후 기본포즈와 블랜딩하여 기본포즈로 돌아갈 수 있게 한다.
        /// </summary>
        public TrackEntry ContinueEmptyAnimation(BodyAnimationTrack track, float mixDuration)
        {
            return _body.AnimationState.AddEmptyAnimation((int)track, mixDuration, 0f);
        }

        /// <summary>
        /// 애니메이션 재생을 예약한다.
        /// 예약한 애니메이션은 현재 재생중인 애니메이션이 끝나면 바로 이어서 재생된다.
        /// </summary>
        public TrackEntry ContinueAnimation(BodyAnimationTrack track, Animation animation, bool loop, float delay)
        {
            var trackEntry = _body.AnimationState.AddAnimation((int)track, animation, loop, delay);

            trackEntry.MixTime = 1f;
            return trackEntry;
        }

        public TrackEntry ContinueAnimation(int track, Animation animation, bool loop, float delay)
        {
            var trackEntry = _body.AnimationState.AddAnimation(track, animation, loop, delay);
            trackEntry.MixTime = 1f;
            return trackEntry;
        }

        /// <summary>
        /// 애니메이션 재생을 예약한다.
        /// 예약한 애니메이션은 현재 재생중인 애니메이션이 끝나면 바로 이어서 재생된다.
        /// 지속 시간과 애니메이션 재생 시간이 다르면 재생속도를 조절해 지속 시간동안 재생된다. 
        /// </summary>
        public TrackEntry ContinueAnimation(BodyAnimationTrack track, Animation animation, bool loop, float delay, float duration)
        {
            float timeScale = animation.Duration / duration;
            var entry = _body.AnimationState.AddAnimation((int)track, animation, loop, delay);
            entry.TimeScale = timeScale;
            return entry;
        }

        public override void PlayAttackForceSlowwing(float attackDuration)
        {
            if (_attack == null)
            {
                return;
            }

            float timeScale = _attack.Duration / attackDuration;

            var trackEntry = this.SetAnimation(BodyAnimationTrack.WholeBody, _attack, loop: false);
            trackEntry.TimeScale = timeScale;
        }

        public override float FindHitTimeOnAttackAnimation()
        {
            if (_attack == null)
            {
                return 0f;
            }
            return FindHitTime(_attack);
        }

        public float FindHitTime(Animation animation)
        {
            if (this.HitFrameEvent == null)
            {
                return 0f;
            }

            var hitEvent = FindEventInAnimationTimeline(animation, this.HitFrameEvent);
            if (hitEvent == null)
            {
                return 0f;
            }

            return hitEvent.Time;
        }

        public float FindEventTime(Animation animation, EventData eventData)
        {
            if (eventData == null)
            {
                return 0f;
            }
            var spineEvent = FindEventInAnimationTimeline(animation, eventData);
            if (spineEvent == null)
            {
                return 0f;
            }
            return spineEvent.Time;
        }

        public override void PlayAnimationDevToolMode(string animationName)
        {
            var animation = this.FindAnimation(animationName);
            if (animation == null)
            {
                return;
            }

            this.SetAnimation(BodyAnimationTrack.WholeBody, animation, loop: true);
        }

        public override void GetAnimationNames(out List<string> animationNames)
        {
            ExposedList<Animation> animations = _body.Skeleton.Data.Animations;
            animationNames = new List<string>(animations.Count);

            foreach (var animation in animations)
            {
                animationNames.Add(animation.Name);
            }
        }

        public override void PlayHeal(float healDuration)
        {
            if (_heal == null)
            {
                return;
            }

            float timeScale = _heal.Duration / healDuration;

            _body.AnimationState.ClearTracks();
            this.SetAnimation(BodyAnimationTrack.WholeBody, _heal, loop: false).TimeScale = timeScale;
        }

        public override void SetBodyAlpha(float alpha)
        {
            if (_body == null)
            {
                Debug.LogError("_body 가 null값이면 안됩니다. 로직 확인이 필요합니다.");
                return;
            }
            _body.skeleton.A = alpha;
        }

    }
}
