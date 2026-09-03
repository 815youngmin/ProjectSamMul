using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.Characters.PCs;
using Z.ObjectPools;

namespace Z.GameClients.Stages.Characters
{
    public class CharacterPool
    {
        private readonly ObjectPool<CharacterType, Character> _characterPool;
        private readonly StaticDataRepository _staticDataRepository;
        private readonly GameObject _charactersRoot;

        public CharacterPool(StaticDataRepository staticDataRepository)
        {
            _characterPool = new ObjectPool<CharacterType, Character>(objectFactory: AllocateCharacter);
            _staticDataRepository = staticDataRepository;
            _charactersRoot = new GameObject("@CharactersRoot");
        }

        // 씬이 정리될 때 호출된다.
        // 씬을 넘어갈 때 유효하지 않은 Pooling Object들을 모두 Destroy해준다. (prefab은 남김)
        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _characterPool.DestroyAll(relatedScene, destroyFunction: (Character element) =>
            {
                GameObject.Destroy(element.gameObject);
            });
        }

        public Monster TakeOneMonsterFromPool(CharacterType characterType)
        {
            var monster = _characterPool.TakeOne<Monster>(characterType);
            monster.transform.SetParent(_charactersRoot.transform);
            return monster;
        }

        public PlayerCharacter TakeOnePlayerCharacterFromPool(HeroType heroType)
        {
            var playerCharacter = _characterPool.TakeOne<PlayerCharacter>(heroType.ToCharacterType());
            playerCharacter.transform.SetParent(_charactersRoot.transform);
            return playerCharacter;
        }

        public void Reserve(CharacterType key, int count)
        {
            _characterPool.Reserve(key, count);
        }

        private Character AllocateCharacter(CharacterType characterType, IPoolObjectInitialDataBase initialData)
        {
            var characterObject = new GameObject(characterType.ToString());
            if (characterType.IsHeroType())
            {
                var heroType = characterType.ToHeroType();
                var pcStaticData = _staticDataRepository.Heroes.Get(heroType);
                var player = characterObject.AddComponent<PlayerCharacter>();
                player.AllocateSharedResources(
                    characterType,
                    pcStaticData.SkeletonDataPath,
                    pcStaticData.ColliderRadius,
                    new Vector2(pcStaticData.HitBoxOffset[0], pcStaticData.HitBoxOffset[1]),
                    new Vector2(pcStaticData.HitBoxSize[0], pcStaticData.HitBoxSize[1]),
                    pcStaticData.ColliderRadius * 2.0f);
                return player;
            }
            else
            {
                var monsterStaticData = _staticDataRepository.Monsters.Get(characterType);
                var monster = characterObject.AddComponent<Monster>();

                monster.AllocateSharedResources(
                    characterType,
                    monsterStaticData.SkeletonDataPath,
                    monsterStaticData.ColliderRadius,
                    new Vector2(monsterStaticData.HitBoxOffset[0], monsterStaticData.HitBoxOffset[1]),
                    new Vector2(monsterStaticData.HitBoxSize[0], monsterStaticData.HitBoxSize[1]),
                    monsterStaticData.ShadowSize);
                monster.SetBodyPixelYOffset(monsterStaticData.BodyPixelYOffset);
                return monster;
            }
        }

        public void PutBackCharacter(Character character)
        {
            _characterPool.PutBack(character);
        }
    }
}
