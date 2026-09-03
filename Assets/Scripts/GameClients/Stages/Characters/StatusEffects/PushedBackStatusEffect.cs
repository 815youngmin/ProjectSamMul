using UnityEngine;

namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class PushedBackStatusEffect : StunStatusEffect
    {
        private readonly Vector2 _pushedBackVelocity;
        private Rect _rect;

        /// <summary>
        /// <see cref="PushedBackStatusEffect"/> 객체를 생성합니다.
        /// </summary>
        /// <param name="duration">
        /// 밀쳐지는 시간입니다.
        /// </param>
        /// <param name="angle">
        /// 밀쳐지는 각도입니다. 오른쪽은 0, 위쪽은 90, 왼쪽은 180, 아래쪽은 270으로 반시계 방향으로 돌아갑니다.
        /// </param>
        /// <param name="speed">
        /// 밀쳐지는 속력입니다.
        /// </param>
        public PushedBackStatusEffect(float duration, float angle, float speed) : base(duration)
        {
            _pushedBackVelocity = speed * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public override void Begin(Stage stage, Character owner, float now)
        {
            base.Begin(stage, owner, now);

            _rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
        }

        public override void Update(Stage stage, Character owner, float now)
        {
            base.Update(stage, owner, now);

            var translation = Time.deltaTime * _pushedBackVelocity;

            if (!_rect.Contains(owner.Pos + translation))
            {
                return;
            }

            owner.transform.Translate(translation, Space.World);
        }
    }
}
