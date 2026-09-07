using SamMul.Animations.Placeholder;
using System;
using UnityEngine;
using SamMul.GameClients.Stages.ProjectileObjects.ProjectileBody;

namespace SamMul.GameClients.Stages.ProjectileObjects
{
    public abstract class ProjectileBodyBase
    {
        protected GameObject _body;

        public virtual void Initialize()
        {
            this.Fade(1.0f);
        }

        public abstract void Fade(float value);
        /// <summary>
        /// 이미지의 방향을 설정하기 위한 함수.
        /// _bodyImage.transform.right 를 설정한다.
        /// </summary>
        /// <param name="right"></param>
        public void SetImageRight(Vector3 right)
        {
            _body.transform.right = right;
        }

        public virtual void UpdateLogic(float deltaTime)
        {

        }

        public abstract void PuttingBackToPool();

        public static ProjectileBodyBase AllocateProjectileBody(GameObject gameObject)
        {
            // 공용 공격 비주얼처럼 스프라이트가 자식에 있는 프리팹도 받는다.
            SpriteRenderer spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer)
            {
                ProjectileSpriteBody spriteBody = new ProjectileSpriteBody();
                spriteBody._body = gameObject;
                spriteBody.AllocateSharedResources(spriteRenderer);
                return spriteBody;
            }

            SkeletonAnimation skeletonAnimation = gameObject.GetComponent<SkeletonAnimation>();
            if(skeletonAnimation)
            {
                ProjectileSkeletonAnimationBody skeltonAnimationBody = new ProjectileSkeletonAnimationBody();
                skeltonAnimationBody._body = gameObject;
                skeltonAnimationBody.AllocateSharedResources(skeletonAnimation);
                return skeltonAnimationBody;
            }

            throw new NotImplementedException("구현 안됨. 구현해주세요.");
        }
    }
}
