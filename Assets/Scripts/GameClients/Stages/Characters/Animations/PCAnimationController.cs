using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Animations
{
    public class PCAnimationController : CharacterAnimationController
    {
        protected readonly SkeletonAnimation _body;
        public SkeletonAnimation Body => _body;
        public override float BodyLocalScale => 0.3333f;
        
        public override bool IsFlippedX => _body.skeleton.ScaleX < 0f; 

        #region Animations
        private readonly Animation _idleMovement;
        private readonly Animation _run;
        private readonly Animation _runBackward;
        private readonly Animation _hitted; // 영문법과는 맞지 않지만, 피격됨을 명시적으로 나타내기 위해 -ed를 붙인다.
        private readonly Animation _dead;
        private readonly Animation _appear;
        private readonly Animation _disappear;
        private readonly Animation _heal;
        private readonly EventData HitFrameEvent;

        private Animation _idleAction;
        private List<Animation> _attackActions = new List<Animation>();
        private Animation _specialAttackAction;
        private Animation _targeting;

        private Animation _face;
        private Animation _point;
        #endregion

        private Vector2 _moveVector;

        #region Bones
        private readonly Bone _aimBone;
        private Vector2 _aimBoneLocalIdlePosition;
        private Vector2 _aimBoneLocalTargetPosition;
        private Vector2 _targetWorldPosition;

        private bool? _isAimingToLeft;
        // 타겟을 잡고 있는 경우 true, 타겟 없으면 false
        private bool _isAiming;
        // 마지막으로 에이밍 마친 시간. 에이밍이 끝나도 일정시간 해당 타겟을 바라보도록 애니메이션 처리하기 위함.
        private float _lastAimingEndAt;    // Unity Time.time

        private bool _isMovingBackward;
        #endregion

        private Coroutine _rewindAttackRoutine;

        #region AnimationHelpers
        public override float AppearAnimationDuration => _appear?.Duration ?? 0.0f;
        public override float DeadAnimationDuration => _dead?.Duration ?? 0.0f;
        public override float DisappearAnimationDuration => _disappear?.Duration ?? 0.0f;
        public float SpecialAttackAnimationDuration => _specialAttackAction?.Duration ?? 0.001f;
        #endregion

        public override bool IsPlayingMovement
        {
            get
            {
                var currentAnimation = _body.AnimationState.GetCurrent((int)BodyAnimationTrack.Movement)?.Animation;
                return currentAnimation == _run ||
                    currentAnimation == _runBackward;
            }
        }

        public PCAnimationController(
            SkeletonAnimation body,
            Renderer renderer,
             bool isPlayerOrBoss,
            string idleActionAnimationName,
            string idleMovementAnimationName,
            string runAnimationName,
            string runBackwardAnimationName,
            string hittedAnimationName,
            string attackAnimationName,
            string specialAttackAnimationName,
            string targetingAnimationName,
            string deadAnimationName,
            string faceAnimationName,
            string appearAnimationName,
            string disappearAnimationName,
            string pointAnimationName,
            string healAnimationName
            ) : this(body, renderer,isPlayerOrBoss, idleActionAnimationName, idleMovementAnimationName,runAnimationName, runBackwardAnimationName, hittedAnimationName,
                        new List<string>() { attackAnimationName },
                        specialAttackAnimationName,targetingAnimationName, deadAnimationName,
                        faceAnimationName, appearAnimationName, disappearAnimationName, pointAnimationName, healAnimationName)
        {
        }

        public PCAnimationController(
            SkeletonAnimation body,
            Renderer renderer,
            bool isPlayerOrBoss,
            string idleActionAnimationName,
            string idleMovementAnimationName,
            string runAnimationName,
            string runBackwardAnimationName,
            string hittedAnimationName,
            List<string> attackAnimationNames,
            string specialAttackAnimationName,
            string targetingAnimationName,
            string deadAnimationName,
            string faceAnimationName,
            string appearAnimationName,
            string disappearAnimationName,
            string pointAnimationName,
            string healAnimationName
            ) : base(isPlayerOrBoss, renderer, "Spine/Skeleton Fill")
        {
            _body = body;
            _body.transform.localScale = new Vector3(BodyLocalScale, BodyLocalScale, 1.0f);
            _moveVector = Vector2.zero;

            _hitted = this.FindAnimation(hittedAnimationName);
            _dead = this.FindAnimation(deadAnimationName);
            _appear = this.FindAnimation(appearAnimationName);
            _disappear = this.FindAnimation(disappearAnimationName) ?? _dead;
            _run = this.FindAnimation(runAnimationName);
            _heal = this.FindAnimation(healAnimationName);
            Debug.Assert(_run != null);

            _runBackward = this.FindAnimation(runBackwardAnimationName);
            if (_runBackward == null)
            {
                _runBackward = FindAnimation(runAnimationName);
            }

            _idleMovement = this.FindAnimation(idleMovementAnimationName);

            _idleAction = this.FindAnimation(idleActionAnimationName);
            foreach (var attackActionName in attackAnimationNames)
            {
                Animation attack = this.FindAnimation(attackActionName);
                if (null == attack)
                {
                    continue;
                }

                _attackActions.Add(this.FindAnimation(attackActionName));
            }

            _specialAttackAction = this.FindAnimation(specialAttackAnimationName);
            _targeting = this.FindAnimation(targetingAnimationName);
            _face = this.FindAnimation(faceAnimationName);
            _point = this.FindAnimation(pointAnimationName);

            {
                SpineHelper.SetMixHelper(_body, _specialAttackAction, _specialAttackAction, 0.0f);
                SpineHelper.SetMixHelper(_body,_targeting, _specialAttackAction, 0.0f);
                SpineHelper.SetMixHelper(_body,_specialAttackAction, _targeting, 0.0f);
                SpineHelper.SetMixHelper(_body,_idleAction, _specialAttackAction, 0.005f);
                SpineHelper.SetMixHelper(_body,_specialAttackAction, _idleAction, 0.10f);
                SpineHelper.SetMixHelper(_body, _targeting, _idleAction, 0.33f);

                this.SetMixAttackAction();
            }

            this.InitializeSpineSkin();

            _body.skeleton.SetToSetupPose();
            _aimBone = _body.skeleton.FindBone("aim");
            if (_aimBone != null)
            {
                _isAimingToLeft = true;
            }
            
            _aimBoneLocalTargetPosition = Vector3.zero;
            _isAiming = false;
            _lastAimingEndAt = 0f;
            _isMovingBackward = false;

            _rewindAttackRoutine = null;

            if (_aimBone != null)
            {
                _aimBoneLocalIdlePosition = _aimBone.GetLocalPosition();
            }

            this.HitFrameEvent = _body.Skeleton.Data.FindEvent("hit");

            this.SetToInitialState();
        }

        private void InitializeSpineSkin()
        {
            Skin customSkin = new Skin("Character");
            customSkin.AddSkin(_body.Skeleton.Data.FindSkin("BasicBody"));
            customSkin.AddSkin(_body.Skeleton.Data.FindSkin("BasicHead"));
            _body.Skeleton.SetSkin(customSkin);
        }

        private void SetMixAttackAction()
        {
            foreach (var attackAction in _attackActions)
            {
                SpineHelper.SetMixHelper(_body, _specialAttackAction, attackAction, 0.000f);
                SpineHelper.SetMixHelper(_body, _targeting, attackAction, 0.0f);
                SpineHelper.SetMixHelper(_body, attackAction, _targeting, 0.0f);
                SpineHelper.SetMixHelper(_body, _idleAction, attackAction, 0.0f);
                SpineHelper.SetMixHelper(_body, attackAction, _idleAction, 0.0f);
            }

            foreach (var attackAction in _attackActions)
            {
                foreach (var targetAttackAction in _attackActions)
                {
                    SpineHelper.SetMixHelper(_body, attackAction, targetAttackAction, 0.005f);
                }
            }
        }

        public override void SetToInitialState()
        {
            _body.transform.rotation = Quaternion.identity;
            _body.AnimationState.ClearTracks();
            _body.skeleton.SetToSetupPose();

            _activeBodyEffects.Clear();
            this.ClearBodyEffectShader();

            if (_rewindAttackRoutine != null)
            {
                _body.StopCoroutine(_rewindAttackRoutine);
                _rewindAttackRoutine = null;
            }

            this.ResetAimBoneToSetupPose();
        }

        private float _playFaceAnimationAt = 0f;
        private float _playPointAnimationAt = 0f;
        public override void Update()
        {
            float now = Time.time;

            base.Update();
            if (_isAiming)
            {
                var currentAnimation = this.GetCurrentAnimation(BodyAnimationTrack.AttackAction);
                if (currentAnimation == _idleAction)
                {
                    this.SetAnimation(BodyAnimationTrack.AttackAction, _targeting, loop: true);
                }
            }

            // 주기적으로 눈깜빡임 재생시켜준다.
            if (_playFaceAnimationAt <= now)
            {
                _playFaceAnimationAt = now + Random.Range(1.0f, 3.5f);
                this.PlayFaceAnimation();
            }

            // 주기적으로 캐릭터 특징을 살려주는 애니메이션을 재생시켜준다.
            if(_playPointAnimationAt <= now)
            {
                _playPointAnimationAt = now + Random.Range(5.0f, 10f);
                this.PlayPointAnimation();
            }
        }
        public float FindHitTimeOnAttackAnimation() => FindHitTimeOnAnimation(_attackActions[0]);
        //attackSequence는 1부터 시작한다
        public float FindHitTimeOnAttackAnimation(int attackSequence) => FindHitTimeOnAnimation(_attackActions[(attackSequence-1) % _attackActions.Count]);
        public float FindHitTimeOnAnimation(Animation animation)
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

        public override void UpdateBodyDirectionByMoveDirection(Vector2 moveDir)
        {
            float previousBodyScaleX = _body.skeleton.ScaleX;
            bool isMovingToLeft = moveDir.x <= 0;
            bool wasMovingBackward = _isMovingBackward;

            if (_aimBone == null ||
                (!_isAiming && ((Time.time - _lastAimingEndAt) > 1.55f))) //0.66f)))
            {
                // 에이밍중이지 않으면 이동방향을 바라보도록 한다. 
                _body.skeleton.ScaleX = isMovingToLeft ? 1.0f : -1.0f;
                // 뒷걸음질 칠 이유도 없다
                _isMovingBackward = false;
            }
            else
            {
                // 에이밍 중이라면, 에이밍 방향으로 몸 방향을 결정한다.
                _body.skeleton.ScaleX = _isAimingToLeft.Value ? 1.0f : -1.0f;
                // 에이밍 방향과 걷는 방향이 다르면 뒷걸음질 친다.
                _isMovingBackward = _isAimingToLeft != isMovingToLeft;
            }

            if (previousBodyScaleX != _body.skeleton.ScaleX &&
                _aimBone != null &&
                _isAiming)
            {
                // 플립되고 나면 에이밍본 위치도 다시 설정해줘야한다.
                var targetSkeletonSpacePoint = _body.transform.InverseTransformPoint(_targetWorldPosition.x, _targetWorldPosition.y, 0f);
                targetSkeletonSpacePoint.x *= _body.Skeleton.ScaleX;
                targetSkeletonSpacePoint.y *= _body.Skeleton.ScaleY;
                _aimBoneLocalTargetPosition = targetSkeletonSpacePoint;

                _aimBone.SetLocalPosition(_aimBoneLocalTargetPosition);
                _aimBone.UpdateWorldTransform();
            }

            if (wasMovingBackward != _isMovingBackward &&
                _moveVector != Vector2.zero)
            {
                // 앞으로걷기/뒤로 걷기 바뀌었다면, 바로 걷기 애니메이션에 바로 반영해야 한다. 
                if (this.IsPlayingMovement)
                {
                    this.PlayMoveInfinitely(_moveVector);
                }
            }
        }

        /// <summary>
        /// Aim본의 위치를 타겟위치로 변경한다.
        /// </summary>
        public void BeginAiming(Vector2 targetWorldPosition)
        {
            if (_aimBone == null)
            {
                return;
            }

            if (_rewindAttackRoutine != null)
            {
                _body.StopCoroutine(_rewindAttackRoutine);
                _rewindAttackRoutine = null;
            }

            _isAiming = true;
            this.UpdateAimBoneToTargetWorldPosition(targetWorldPosition);

            var currentAnimation = this.GetCurrentAnimation(BodyAnimationTrack.AttackAction);
            if (!_attackActions.Contains(currentAnimation) &&
                currentAnimation != _specialAttackAction)
            {
                this.SetAnimation(BodyAnimationTrack.AttackAction, _targeting, loop: true);
            }
        }

        public void UpdateAimBoneToTargetWorldPosition(Vector2 targetWorldPosition)
        {
            if (_aimBone == null)
            {
                return;
            }

            if (!_isAiming)
            {
                Debug.LogWarning($"aiming중이 아닌데 UpdateAimBone호출. 무시함.");
                return;
            }
            _targetWorldPosition = targetWorldPosition;
            var targetSkeletonSpacePoint = _body.transform.InverseTransformPoint(targetWorldPosition.x, targetWorldPosition.y, 0f);
            targetSkeletonSpacePoint.x *= _body.Skeleton.ScaleX;
            targetSkeletonSpacePoint.y *= _body.Skeleton.ScaleY;
            _aimBoneLocalTargetPosition = targetSkeletonSpacePoint;

            _aimBone.SetLocalPosition(_aimBoneLocalTargetPosition);
            _aimBone.UpdateWorldTransform();

            _isAimingToLeft = targetWorldPosition.x <= _body.transform.position.x;
        }

        public void EndAiming()
        {
            if (_aimBone == null)
            {
                return;
            }

            if (_isAiming)
            {
                _lastAimingEndAt = Time.time;
                _isAiming = false;
                _targetWorldPosition = Vector2.zero;
            }

            if (_rewindAttackRoutine != null)
            {
                _body.StopCoroutine(_rewindAttackRoutine);
            }
            _rewindAttackRoutine = _body.StartCoroutine(RewindAimBoneToIdleRoutine());
            if(null == _idleAction)
            {
                return;
            }

            var currentAnimation = this.GetCurrentAnimation(BodyAnimationTrack.AttackAction);
            if (currentAnimation == _targeting)
            {
                this.SetAnimation(BodyAnimationTrack.AttackAction, _idleAction, loop: true);
            }
            else if (currentAnimation != _idleAction)
            {
                this.ContinueAnimation(BodyAnimationTrack.AttackAction, _idleAction, loop: true, delay: 0f);
            }
        }

        private void ResetAimBoneToSetupPose()
        {
            if (null != _aimBone)
            {
                _aimBone.SetLocalPosition(_aimBoneLocalIdlePosition);
                _aimBone.UpdateAppliedTransform();
            }
            _aimBoneLocalTargetPosition = _aimBoneLocalIdlePosition;
        }

        private IEnumerator RewindAimBoneToIdleRoutine()
        {
            const float totalDuration = 0.55f;
            float deltaTime = 0f;

            yield return new WaitForSeconds(0.3f);

            if (null != _aimBone)
            {
                while (deltaTime < totalDuration)
                {
                    var position = new Vector2(
                        Mathf.Lerp(_aimBoneLocalTargetPosition.x, _aimBoneLocalIdlePosition.x, deltaTime / totalDuration),
                        Mathf.Lerp(_aimBoneLocalTargetPosition.y, _aimBoneLocalIdlePosition.y, deltaTime / totalDuration)
                        );
                    _aimBone.SetLocalPosition(position);
                    _aimBone.UpdateAppliedTransform();

                    yield return null;
                    deltaTime += Time.deltaTime;
                }

                _aimBone.SetLocalPosition(_aimBoneLocalIdlePosition);
                _aimBone.UpdateAppliedTransform();
            }
            _aimBoneLocalTargetPosition = _aimBoneLocalIdlePosition;

            _rewindAttackRoutine = null;
            yield break;
        }

        //-----
        public override void StopMovement()
        {
            _moveVector = Vector2.zero;

            if (_idleMovement == null)
            {
                return;
            }
            this.SetAnimation(BodyAnimationTrack.Movement, _idleMovement, loop: true);
        }

        // Idle 액션은 상체 애니메이션으로 처리한다. 공격하지 않으면 Idle이다. 이동과 무관하다.
        // 이동종료는 Idle재생이 아니라 StopWalk로 처리한다.
        public override void PlayIdleAttackActionInfinitely()
        {
            if (_idleAction == null)
            {
                return;
            }

            var currentAnimation = this.GetCurrentAnimation(BodyAnimationTrack.AttackAction);
            if (currentAnimation == _idleAction ||
                currentAnimation == _targeting)
            {
                return;
            }

            if (!_isAiming)
            {
                this.SetAnimation(BodyAnimationTrack.AttackAction, _idleAction, loop: true);
                this.ResetAimBoneToSetupPose();
            }
            else
            {
                this.SetAnimation(BodyAnimationTrack.AttackAction, _targeting, loop: true);
            }

        }

        public override void PlayMoveInfinitely(Vector2 moveVector)
        {
            if (null == _run)
            {
                return;
            }

            _moveVector = moveVector;
            Animation desiredAnimation = null;

            float speed = moveVector.magnitude;
            float timeScale = 1f;
            desiredAnimation = _isMovingBackward ? _runBackward : _run;
            if (speed > 0.75f)
            {
                // 1.0f , 0.8f, 0.55f, 0.35f
                timeScale = speed; // speed / 1.0f;
            }
            else
            {
                timeScale = (speed / 0.5f) - 0.2f;
                if (timeScale < 0.3f)
                {
                    timeScale = 0.3f;
                }
            }

            var currentEntry = _body.AnimationState.GetCurrent((int)BodyAnimationTrack.Movement);
            if (currentEntry?.Animation == desiredAnimation)
            {
                currentEntry.TimeScale = timeScale;
                return;
            }

            SetAnimation(BodyAnimationTrack.Movement, desiredAnimation, loop: true).TimeScale = timeScale;
        }

        public void PlayFaceAnimation()
        {
            if (_face == null)
            {
                return;
            }

            this.SetAnimation(BodyAnimationTrack.Face, _face, loop: false);
            this.ContinueEmptyAnimation(BodyAnimationTrack.Face, 0f);
        }

        public void PlayPointAnimation()
        {
            if(_point == null)
            {
                return;
            }
            this.SetAnimation(BodyAnimationTrack.OverBodyEffect, _point, loop: false);
            this.ContinueEmptyAnimation(BodyAnimationTrack.OverBodyEffect, 0f);
        }

        /// <summary>
        /// 공격 애니메이션을 재생한다.
        /// </summary>
        public override void PlayAttackForce(float attackDuration)
        {
            if(_attackActions.Count==0)
            {
                return;
            }

            // 공격액션은 시작하면 바로 시작한다. (이전에 공격하고 잇었더라도, 바로 새로운 공격액션을 시작)
            // -> 연발에서 공격이 너무 길어져서, 앞에 공격모션중이면 새로운 것 씹도록 수정해본다. 테스트중

            float timeScale = _attackActions[0].Duration / attackDuration;
            if (timeScale < 1f)
            {
                // 발포애니메이션을 느리게 재생하지는 않는다.
                // 발포를 정상속도로하고, 남은시간동안 그냥 그자세로 대기한다.
                timeScale = 1f;
            }

            var entry = SetAnimation(BodyAnimationTrack.AttackAction, _attackActions[0], loop: false);
            entry.TimeScale = timeScale;
            ContinueAnimation(BodyAnimationTrack.AttackAction, _targeting, loop: true, delay: 0f);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attackDuration"></param>
        /// <param name="attackSequence">1부터 시작한다</param>
        public void PlayAttackForce(float attackDuration, int attackSequence)
        {
            Debug.Assert(_attackActions.Count != 0);
            // 공격액션은 시작하면 바로 시작한다. (이전에 공격하고 잇었더라도, 바로 새로운 공격액션을 시작)
            // -> 연발에서 공격이 너무 길어져서, 앞에 공격모션중이면 새로운 것 씹도록 수정해본다. 테스트중

            float timeScale = _attackActions[attackSequence-1].Duration / attackDuration;
            if (timeScale < 1f)
            {
                // 발포애니메이션을 느리게 재생하지는 않는다.
                // 발포를 정상속도로하고, 남은시간동안 그냥 그자세로 대기한다.
                timeScale = 1f;
            }

            var entry = SetAnimation(BodyAnimationTrack.AttackAction, _attackActions[attackSequence-1], loop: false);
            entry.TimeScale = timeScale;
            ContinueAnimation(BodyAnimationTrack.AttackAction, _targeting, loop: true, delay: 0f);
        }

        public override void StopAttack()
        {
            var currentAnimation = GetCurrentAnimation(BodyAnimationTrack.AttackAction);
            if (!_attackActions.Contains(currentAnimation)||
                currentAnimation != _specialAttackAction)
            {
                return;
            }

            this.SetAnimation(BodyAnimationTrack.AttackAction, _targeting, loop: true);
        }

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
                previousAnimation = this.GetCurrentAnimation(BodyAnimationTrack.AttackAction);
                if (previousAnimation == _hitted)
                {
                    // 앞에 Hit가 재생되고 있었다면, 구태여 이어서 다시 재생하지 않는다.
                    previousAnimation = null;
                }
                if (previousAnimation != null)
                {
                    wasLoopped = _body.AnimationState.GetCurrent((int)BodyAnimationTrack.AttackAction).Loop;
                }
            }

            this.SetAnimation(BodyAnimationTrack.AttackAction, _hitted, loop: false);

            if (continuePreviousAnimation &&
                previousAnimation != null)
            {
                // NOTE: 여기가 이게 맞나? Hitted 재생한뒤에Idle이 아니라 이동이 나와야 하려나?
                // 몬스터일 경우에 대한 처리 다시 고민해야한다. 몬스터용 애니메이션컨트롤러와 캐릭터용 컨트롤러를 나눠야 할 것 같다 이쯤되면.
                this.ContinueAnimation(BodyAnimationTrack.AttackAction, previousAnimation, loop: wasLoopped, delay: 0f);
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
            this.SetAnimation(BodyAnimationTrack.Movement, _dead, loop: false);

            //update에서 처리되는 반복 애니메이션은 죽는 애니메이션 재생 후 나올 수 있도록 변경한다.
            //(부활시에는 잘 작동하고 죽는 연출동안은 재생 안되도록 처리)
            float now = Time.time;
            _playFaceAnimationAt = now + _dead.Duration + 1.5f;
            _playPointAnimationAt = now + _dead.Duration + 3f;
        }

        public override void PlayAppear(float appearDuration)
        {
            if (_appear == null)
            {
                return;
            }

            float timeScale = _appear.Duration / appearDuration;

            _body.AnimationState.ClearTracks();
            _body.Skeleton.SetToSetupPose();
            this.SetAnimation(BodyAnimationTrack.AttackAction, _appear, loop: false).TimeScale = timeScale;
            this.ContinueEmptyAnimation(BodyAnimationTrack.AttackAction, 0.0f);
        }

        public override void PlayDisappear()
        {
            if (_disappear == null)
            {
                return;
            }

            _body.AnimationState.ClearTracks();
            _body.Skeleton.SetToSetupPose();
            this.SetAnimation(BodyAnimationTrack.AttackAction, _disappear, loop: false);
        }

        public override void BeginHittedBodyEffect(bool isBigCharacter)
        {
            float duration = 0.15f;
            int previousHittedIndex = _activeBodyEffects.FindIndex(x => x.Type == CharacterBodyEffectType.Hitted);
            if (previousHittedIndex >= 0)
            {
                _activeBodyEffects.RemoveAt(previousHittedIndex);
            }

            int phase = 0;
            var hittedEffect = CharacterBodyEffect.Hitted(duration,
                bodyEffectApplier: () =>
                {
                    this.FillBody(0.8f, Color.white);
                },
                progressiveBodyEffect: (float leftTime, float progressedTime) =>
                {
                    switch (phase)
                    {
                        case 0:
                            {
                                if (progressedTime > 0.005f)
                                {
                                    phase = 1;
                                    this.FillBody(0.9f, Color.white);
                                }
                                break;
                            }
                        case 1:
                            {
                                if (leftTime < 0.01f)
                                {
                                    phase = 2;
                                    this.FillBody(0.9f, Color.black);
                                }
                                break;
                            }
                        default:
                            {
                                return;
                            }
                    }
                });
            // 하얀 기본 이펙트 대신, 까맣게 깔고 시작한다.
            this.FillBody(0.9f, Color.black);
            _activeBodyEffects.Add(hittedEffect);
        }

        public Animation FindAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return null;
            }
            return _body.Skeleton.Data.FindAnimation(animationName);
        }

        public Animation GetCurrentAnimation(BodyAnimationTrack track)
        {
            return _body.AnimationState.GetCurrent((int)track)?.Animation;
        }

        /// <summary>
        /// 애니매이션을 즉시 재생한다. 트랙에 현재 재생중인 애니메이션이 있으면, 해당 애니메이션은 즉시 중지된다.
        /// </summary>
        public TrackEntry SetAnimation(BodyAnimationTrack track, Animation animation, bool loop)
        {
            return _body.AnimationState.SetAnimation((int)track, animation, loop);
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

        public void SetEmptyAnimation(BodyAnimationTrack track, float mixDuration)
        {
            _body.AnimationState.SetEmptyAnimation((int)track, mixDuration);
        }

        /// <summary>
        /// 애니메이션 재생을 예약한다.
        /// 예약한 애니메이션은 현재 재생중인 애니메이션이 끝나면 바로 이어서 재생된다.
        /// </summary>
        public TrackEntry ContinueAnimation(BodyAnimationTrack track, Animation animation, bool loop, float delay)
        {
            return _body.AnimationState.AddAnimation((int)track, animation, loop, delay);
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

        public override void PlayHeal(float healDuration)
        {
            if (_heal == null)
            {
                return;
            }

            float timeScale = _heal.Duration / healDuration;

            _body.AnimationState.ClearTracks();
            _body.Skeleton.SetToSetupPose();
            this.SetAnimation(BodyAnimationTrack.AttackAction, _heal, loop: false).TimeScale = timeScale;
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
