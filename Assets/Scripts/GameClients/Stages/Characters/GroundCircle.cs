using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Characters
{
    //캐릭터 아래 이동 방향성을 표기해주는 스크립트
    //할당 단계에서 오브젝트를 생성 해주고 
    //초기화 단계에서 타입에 맞는 이미지를 불러온다.
    //업데이트 단계에서 캐릭터 이동방향에 맞춰 이미지를 회전시켜준다.
    public class GroundCircle
    {
        public enum GroundCircleType
        {
            Player,
            Servant,
        }
        private GameObject _groundCircleObject;
        private SpriteRenderer _groundCircleBody;
        private GroundCircleType _type;

        public void Initialize(Character character, GroundCircleType type)
        {
            Debug.Assert(_groundCircleObject == null);
            _type = type;
            _groundCircleObject = new GameObject("groundCircle");
            _groundCircleObject.transform.SetParent(character.transform);
            _groundCircleObject.transform.localPosition = Vector3.zero;
            // y축으로 살짝 찌부시킨다.
            _groundCircleObject.transform.localScale = new Vector2(1f, 0.8f);

            // 기본 오브젝트의 Child로 body오브젝트를 하나더 추가하고, Child오브젝트에 SpriteRenderer를 붙인다.
            // 원형의 스프라이트를 제작하고, 실제 렌더링되는 이미지는 y축으로 살작 찌부시키기 위함.
            // 원형의 sprite에 rotation을 먹이면서, y축 찌부된 형상을 유지하기 위해 이런 구조를 가진다.
            var spriteBodyObject = new GameObject("groundCircleBody");
            spriteBodyObject.transform.SetParent(_groundCircleObject.transform);
            spriteBodyObject.transform.localPosition = Vector3.zero;
            var spriteRenderer = spriteBodyObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerID = SortingLayer.NameToID("LowParticle");
            _groundCircleBody = spriteRenderer;
            if (_type == GroundCircleType.Player)
            {
                spriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Characters/GroundCircleDummy.png");
                spriteRenderer.color = new Color(0.1f, 1f, 0.1f, 1.0f);
                spriteRenderer.gameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); 
            }
            else if(_type == GroundCircleType.Servant)
            {
                spriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Characters/GroundCircleDummy.png");
                spriteRenderer.color = new Color(0.5f, 1f, 0.1f, 1.0f);
                spriteRenderer.gameObject.transform.localScale = new Vector3(0.165f, 0.165f, 0.165f);
            }
        }
        public void Clear()
        {
            if(_groundCircleObject != null)
            {
                GameObject.Destroy(_groundCircleObject);
                _groundCircleObject = null;
            }
        }

        public void RotateToMoveDirection(Vector2 moveDir)
        {
            if (_groundCircleBody == null ||
                moveDir.sqrMagnitude <= 0)
            {
                return;
            }

            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            _groundCircleBody.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

}
