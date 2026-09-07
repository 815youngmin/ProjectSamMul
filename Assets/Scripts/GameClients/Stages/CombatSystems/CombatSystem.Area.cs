using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.AreaIndicators;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.ItemObjects;

namespace SamMul.GameClients.Stages.CombatSystems
{
    public static partial class CombatSystem
    {
        private static bool TryHitObject(Character attacker, float damage, Collider2D collider, Stage stage)
        {
            var breakableItemObject = collider.GetComponent<BreakableItemObject>();
            if (breakableItemObject != null)
            {
                if (attacker.Alliance == AllianceType.Players)
                {
                    // 플레이어의 프로젝타일은 부술수있는 아이템을 부술 수 있다.
                    // 몬스터의 프로젝타일은 못부숨
                    breakableItemObject.OnBroken(attacker, damage, stage);
                    return true;
                }
                return false;
            }

            // TODO : 또 다른 충돌가능한 오브젝트 대응
            return false;
        }

        #region Find In Zone
        public enum KnockBackType
        {
            Pivot,
            Direction
        }

        private static void TryHitCharacter(Stage stage, Character attacker, IReadOnlyList<Character> findedCharacters, float damage, KnockBackType knockBackType, Vector2 knockBackPivot, float knockBackPower, HashSet<Character> hittedCharacterCollector, HashSet<Character> exceptedCharacters, string hitSoundPrefabPath)
        {
            foreach (var character in findedCharacters)
            {
                if (null != exceptedCharacters && exceptedCharacters.Contains(character))
                {
                    continue;
                }

                Vector2 knockbackVector;
                switch (knockBackType)
                {
                    case KnockBackType.Pivot:
                        knockbackVector = character.Pos - knockBackPivot;
                        break;
                    case KnockBackType.Direction:
                        knockbackVector = knockBackPivot;
                        break;
                    default:
                        throw new NotImplementedException($"구현되지 않은 KnockBackType({knockBackType}) 입니다. 추가해주세요.");
                }

                if (knockBackPower == 0f)
                {
                    knockbackVector = Vector2.zero;
                }
                else
                {
                    knockbackVector = knockbackVector.normalized * knockBackPower;
                }
                character.Hitted(stage, attacker, damage, knockbackVector, character.Pos, hitSoundPrefabPath);
                hittedCharacterCollector?.Add(character);
            }
        }

        /// <summary>
        /// Zone을 이용한 탐색으로 Hit처리를 합니다.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="targetArea"></param>
        /// <param name="attacker"></param>
        /// <param name="damage"></param>
        /// <param name="knockBackType">넉백 처리 방법 (Pivot : 해당 값 중심으로 방향을 계산해 처리합니다.) (Direction : 해당 값 방향으로 넉백 시킵니다.) </param>
        /// <param name="knockBackPivot">넉백 처리할때 기준일 될 좌표, knockBackType이 Direction이면 방향으로 사용됩니다.</param>
        /// <param name="knockBackPower">넉백 처리 힘</param>
        /// <param name="hittedCharacterCollector">이 함수가 리턴하면, 이번 공격으로 공격된 대상을 여기에 담아서 돌려준다.</param>
        /// <param name="exceptedCharacters">공격처 하면 안되는 리스트</param>
        public static void HitOnTargetArea(Stage stage, CircularTargetArea targetArea, Character attacker, float damage, KnockBackType knockBackType, Vector2 knockBackPivot, float knockBackPower, HashSet<Character> hittedCharacterCollector, HashSet<Character> exceptedCharacters, string hitSoundPrefabPath)
        {
            // 전용 이펙트가 없는 데모용: 공격의 실제 판정 범위를 잠깐 표시한다. 플레이어는 파란색, 적은 빨간색.
            stage.AttackAreaFlashes.Show(targetArea, attacker.Alliance == AllianceType.Players ? AttackAreaFlashManager.PLAYER_COLOR : AttackAreaFlashManager.ENEMY_COLOR);

            List<Character> findedCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(attacker.Alliance.ToEnemyAlliance(), targetArea, findedCharacters);
            TryHitCharacter(stage, attacker, findedCharacters, damage, knockBackType, knockBackPivot, knockBackPower, hittedCharacterCollector, exceptedCharacters, hitSoundPrefabPath);

            if (attacker.Alliance == AllianceType.Players)
            {
                IReadOnlyList<BreakableItemObject> breakableItemObjects = stage.BreakableItemObjects;
                foreach (var breakableItemObject in breakableItemObjects)
                {
                    if (targetArea.Contains(breakableItemObject.transform.position, BreakableItemObject.ITEM_COLLIDER_RADIUS))
                    {
                        breakableItemObject.OnBroken(attacker, damage, stage);
                    }
                }
            }
        }

        /// <summary>
        /// Zone을 이용한 탐색으로 Hit처리를 합니다.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="targetArea"></param>
        /// <param name="attacker"></param>
        /// <param name="damage"></param>
        /// <param name="knockBackType">넉백 처리 방법 (Pivot : 해당 값 중심으로 방향을 계산해 처리합니다.) (Direction : 해당 값 방향으로 넉백 시킵니다.) </param>
        /// <param name="knockBackPivot">넉백 처리할때 기준일 될 좌표, knockBackType이 Direction이면 방향으로 사용됩니다.</param>
        /// <param name="knockBackPower">넉백 처리 힘</param>
        /// <param name="hittedCharacterCollector">공격된 대상의 </param>
        /// <param name="exceptedCharacters"></param>
        public static void HitOnTargetArea(Stage stage, SquareTargetArea targetArea, Character attacker, float damage, KnockBackType knockBackType, Vector2 knockBackPivot, float knockBackPower, HashSet<Character> hittedCharacterCollector, HashSet<Character> exceptedCharacters, string hitSoundPrefabPath)
        {
            // 전용 이펙트가 없는 데모용: 공격의 실제 판정 범위를 잠깐 표시한다. 플레이어는 파란색, 적은 빨간색.
            stage.AttackAreaFlashes.Show(targetArea, attacker.Alliance == AllianceType.Players ? AttackAreaFlashManager.PLAYER_COLOR : AttackAreaFlashManager.ENEMY_COLOR);

            List<Character> findedCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(attacker.Alliance.ToEnemyAlliance(), targetArea, findedCharacters);
            TryHitCharacter(stage, attacker, findedCharacters, damage, knockBackType, knockBackPivot, knockBackPower, hittedCharacterCollector, exceptedCharacters, hitSoundPrefabPath);

            if (attacker.Alliance == AllianceType.Players)
            {
                IReadOnlyList<BreakableItemObject> breakableItemObjects = stage.BreakableItemObjects;
                foreach (var breakableItemObject in breakableItemObjects)
                {
                    if (targetArea.Contains(breakableItemObject.transform.position, BreakableItemObject.ITEM_COLLIDER_RADIUS))
                    {
                        breakableItemObject.OnBroken(attacker, damage, stage);
                    }
                }
            }
        }

        /// <summary>
        /// Zone을 이용한 탐색으로 Hit처리를 합니다.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="targetArea"></param>
        /// <param name="attacker"></param>
        /// <param name="damage"></param>
        /// <param name="knockBackType">넉백 처리 방법 (Pivot : 해당 값 중심으로 방향을 계산해 처리합니다.) (Direction : 해당 값 방향으로 넉백 시킵니다.) </param>
        /// <param name="knockBackPivot">넉백 처리할때 기준일 될 좌표, knockBackType이 Direction이면 방향으로 사용됩니다.</param>
        /// <param name="knockBackPower">넉백 처리 힘</param>
        /// <param name="hittedCharacterCollector">공격된 대상의 </param>
        /// <param name="exceptedCharacters"></param>
        public static void HitOnTargetArea(Stage stage, CircularSectorTargetArea targetArea, Character attacker, float damage, KnockBackType knockBackType, Vector2 knockBackPivot, float knockBackPower, HashSet<Character> hittedCharacterCollector, HashSet<Character> exceptedCharacters, string hitSoundPrefabPath)
        {
            // 전용 이펙트가 없는 데모용: 공격의 실제 판정 범위를 잠깐 표시한다. 플레이어는 파란색, 적은 빨간색.
            stage.AttackAreaFlashes.Show(targetArea, attacker.Alliance == AllianceType.Players ? AttackAreaFlashManager.PLAYER_COLOR : AttackAreaFlashManager.ENEMY_COLOR);

            List<Character> findedCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(attacker.Alliance.ToEnemyAlliance(), targetArea, findedCharacters);
            TryHitCharacter(stage, attacker, findedCharacters, damage, knockBackType, knockBackPivot, knockBackPower, hittedCharacterCollector, exceptedCharacters, hitSoundPrefabPath);

            if (attacker.Alliance == AllianceType.Players)
            {
                IReadOnlyList<BreakableItemObject> breakableItemObjects = stage.BreakableItemObjects;
                foreach (var breakableItemObject in breakableItemObjects)
                {
                    if (targetArea.Contains(breakableItemObject.transform.position))
                    {
                        breakableItemObject.OnBroken(attacker, damage, stage);
                    }
                }
            }
        }
        #endregion

        public static void HandleAreaEffectCollisionWithCharacter(
            AreaEffectObjectBase areaEffectObject,
            Vector2 position,
            float radius,
            Vector2 direction,
            float speed,
            float deltaTime,
            Collider2D[] overlappedColliders,
            RaycastHit2D[] raycastHits,
            Action<Character> onHitCharacter)
        {
            if (!areaEffectObject.IsAlive)
            {
                return;
            }

            int characterHitBoxLayer = LayerMask.NameToLayer("CharacterHitBox");
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            int collisionMask = Physics2D.GetLayerCollisionMask(projectileLayer);

            // 콜라이더로 충돌 처리.
            int overlappedCollidersCount = Physics2D.OverlapCircleNonAlloc(position, radius, overlappedColliders, collisionMask);
            for (int i = 0; i < overlappedCollidersCount && areaEffectObject.IsAlive; ++i)
            {
                TryHit(overlappedColliders[i]);
            }

            if (!areaEffectObject.IsAlive)
            {
                return;
            }

            // 레이캐스트로 충돌 처리.
            int raycastHitsCount = Physics2D.RaycastNonAlloc(position, direction, raycastHits, deltaTime * speed, collisionMask);
            for (int i = 0; i < raycastHitsCount && areaEffectObject.IsAlive; ++i)
            {
                TryHit(raycastHits[i].collider);
            }

            void TryHit(Collider2D collider)
            {
                if (!areaEffectObject.IsAlive)
                {
                    return;
                }

                if (collider == null)
                {
                    return;
                }

                if (collider.gameObject.layer == characterHitBoxLayer)
                {
                    var character = collider.transform.parent.GetComponent<Character>();
                    if (character.Alliance == areaEffectObject.Alliance)
                    {
                        return;
                    }

                    onHitCharacter.Invoke(character);
                }
            }
        }
    }
}
