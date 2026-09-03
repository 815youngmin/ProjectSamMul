#nullable enable
using System;
using UnityEngine;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.GroundEffects
{
    /// <summary>
    /// A sprite animation placed under a character. Whether it loops or plays once is decided by the animation
    /// clip itself; the owner polls <see cref="IsExpired"/> and calls <see cref="PutResourceBackToPool"/>.
    /// </summary>
    public readonly struct CharacterGroundEffect
    {
        private const float ROTATION_DEGREES_PER_SECOND = -360f / 3.8f;

        public readonly CharacterGroundEffectType GroundEffectType;
        private readonly GameObject _body;
        private readonly SpriteAnimationHandler _animation;

        public bool IsExpired => !_animation.IsAlive;
        public string ResourcePath => GetResourcePath(this.GroundEffectType);

        public static CharacterGroundEffect CreateInfiniteLoop(CharacterGroundEffectType effectType, Transform ownerTransform)
        {
            return new CharacterGroundEffect(effectType, ownerTransform);
        }
        private CharacterGroundEffect(CharacterGroundEffectType effectType, Transform ownerTransform)
        {
            this.GroundEffectType = effectType;

            _body = new GameObject("GroundEffectBody");
            _body.transform.SetParent(ownerTransform, worldPositionStays: false);

            _animation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(GetResourcePath(effectType));
            _animation.transform.SetParent(_body.transform, worldPositionStays: false);
            _animation.transform.localPosition = Vector3.zero;
            _animation.transform.localScale = Vector3.one;
            _animation.gameObject.SetActive(true);

            var (scale, sortingOrder, rotates) = GetVisualSettings(effectType);
            _animation.SpriteRenderer.sortingLayerID = SortingLayer.NameToID("LowParticle");
            _animation.SpriteRenderer.sortingOrder = sortingOrder;
            _body.transform.localScale = new Vector3(scale, scale * 0.8f, 1f);
            if (rotates)
            {
                _animation.gameObject.AddComponent<AutoTransformRotate>().RotateEulers = new Vector3(0f, 0f, ROTATION_DEGREES_PER_SECOND);
            }

            _animation.InitializeAndPlay();
        }

        public void PutResourceBackToPool()
        {
            _animation.StopAndReserveToDestroy();
            if (_animation.TryGetComponent<AutoTransformRotate>(out var rotator))
            {
                UnityEngine.Object.Destroy(rotator);
            }
            _animation.transform.SetParent(null);
            _animation.transform.localRotation = Quaternion.identity;
            _animation.gameObject.SetActive(false);
            ResourcePool.Instance.PutBackInstance(this.ResourcePath, _animation.gameObject);

            UnityEngine.Object.Destroy(_body);
        }

        // (body scale, sorting order, keeps rotating)
        private static (float, int, bool) GetVisualSettings(CharacterGroundEffectType type)
        {
            return type switch
            {
                CharacterGroundEffectType.SummonerRecoverSource => (0.5f, 10, false),
                CharacterGroundEffectType.SummonerRecoverTarget => (0.4f, 10, false),
                CharacterGroundEffectType.SummonerSummonSource => (0.5f, 0, false),
                CharacterGroundEffectType.SummonerSummonTarget => (0.4f, 0, false),
                CharacterGroundEffectType.StimulationPackSource => (0.5f, 8, true),
                CharacterGroundEffectType.StimulationPackTarget => (0.4f, 8, true),
                CharacterGroundEffectType.ProteinSupplementSource => (0.5f, 8, true),
                CharacterGroundEffectType.ProteinSupplementTarget => (0.4f, 8, true),
                _ => throw new NotImplementedException($"{type} is not supported."),
            };
        }

        public static string GetResourcePath(CharacterGroundEffectType type)
        {
            return type switch
            {
                CharacterGroundEffectType.SummonerRecoverSource => "Stages/Characters/GroundEffects/Summoners/fxt_HealingCircleAnimation.prefab",
                CharacterGroundEffectType.SummonerRecoverTarget => "Stages/Characters/GroundEffects/Summoners/fxt_HealingCircleAnimation.prefab",
                CharacterGroundEffectType.SummonerSummonSource => "Stages/Characters/GroundEffects/Summoners/fxt_SummonCircleAnimation.prefab",
                CharacterGroundEffectType.SummonerSummonTarget => "Stages/Characters/GroundEffects/Summoners/fxt_SummonCircleAnimation.prefab",
                CharacterGroundEffectType.StimulationPackSource => "Stages/Characters/GroundEffects/Summoners/fxt_AttackSpeedBuffAnimation.prefab",
                CharacterGroundEffectType.StimulationPackTarget => "Stages/Characters/GroundEffects/Summoners/fxt_AttackSpeedBuffAnimation.prefab",
                CharacterGroundEffectType.ProteinSupplementSource => "Stages/Characters/GroundEffects/Summoners/fxt_AttackBuffAnimation.prefab",
                CharacterGroundEffectType.ProteinSupplementTarget => "Stages/Characters/GroundEffects/Summoners/fxt_AttackBuffAnimation.prefab",
                _ => throw new NotImplementedException($"{type} is not supported."),
            };
        }
    }
}
