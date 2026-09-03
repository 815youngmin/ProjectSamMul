using System.Collections.Generic;
using UnityEngine;

namespace SamMul.GameClients.Stages.Characters.Animations
{
    public abstract class MonsterAnimationController : CharacterAnimationController
    {
        public abstract float AttackAnimationDuration { get; }

        public MonsterAnimationController(bool isPlayerOrBoss, Renderer renderer, string shaderPath) : base(isPlayerOrBoss, renderer, shaderPath)
        {
        }

        public override void Update()
        {
            base.Update();
        }

        public override void StopMovement()
        {
            // 몬스터는 상하체 애니메이션 분리를 하지 않고,
            // 이동도 Action으로 다루기 때문에, 이동 종료에 따른 별다른 처리가 필요 없다.
            // 몬스터의 Idle애니메이션 재생의 책임은 IdleAction에서 가져간다.
            // 여기서 따로 처리하지 않는다. 
        }

        public abstract void PlayAttackForceSlowwing(float attackDuration);

        public abstract float FindHitTimeOnAttackAnimation();

        public override void BeginHittedBodyEffect(bool isBigCharacter)
        {
            // FillBody FillingRate
            float minRate = 0.55f;
            float midRate = 0.75f;
            float maxRate = 0.95f;
            
            if (isBigCharacter)
            {
                minRate = 0.05f;
                midRate = 0.20f;
                maxRate = 0.45f;
            }

            float duration = 0.16f;
            int previousHittedIndex = _activeBodyEffects.FindIndex(x => x.Type == CharacterBodyEffectType.Hitted);
            if (previousHittedIndex >= 0)
            {
                _activeBodyEffects.RemoveAt(previousHittedIndex);
            }

            int phase = 0;
            var hittedEffect = CharacterBodyEffect.Hitted(duration,
                bodyEffectApplier: () =>
                {
                    this.FillBody(minRate, Color.white);
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
                                    this.FillBody(maxRate, Color.white);
                                }
                                break;
                            }
                        case 1:
                            {
                                if (progressedTime > 0.015f)
                                {
                                    phase = 2;
                                    this.FillBody(minRate, Color.white);
                                }
                                break;
                            }
                        case 2:
                            {
                                if (progressedTime > 0.025f)
                                {
                                    phase = 3;
                                    this.FillBody(midRate, Color.white);
                                }
                                break;
                            }
                        case 3:
                            {
                                if (leftTime < 0.015f)
                                {
                                    this.FillBody(0.25f, Color.black);
                                    phase = 4;
                                }
                                break;
                            }
                        default:
                            {
                                return;
                            }
                    }
                });

            hittedEffect.Apply();
            _activeBodyEffects.Add(hittedEffect);
        }

        public abstract void PlayAnimationDevToolMode(string animationName);
        public abstract void GetAnimationNames(out List<string> animationNames);
    }
}
