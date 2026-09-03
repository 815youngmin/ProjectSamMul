using UnityEngine;

namespace SamMul.GameClients.Stages.ProjectileObjects.ProjectileBody
{
    public class ProjectileSpriteBody : ProjectileBodyBase
    {
        private SpriteRenderer _spriteRenderer;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        private TrailRenderer[] _trailRenderers;

        public void AllocateSharedResources(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
            _trailRenderers = spriteRenderer.GetComponentsInChildren<TrailRenderer>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void PuttingBackToPool()
        {
            if (null != _trailRenderers)
            {
                foreach (var trailRenderer in _trailRenderers)
                {
                    trailRenderer.Clear();
                }
            }
        }

        public override void Fade(float value)
        {
            Color color = _spriteRenderer.color;
            color.a = value;
            _spriteRenderer.color = color;
        }

    }
}
