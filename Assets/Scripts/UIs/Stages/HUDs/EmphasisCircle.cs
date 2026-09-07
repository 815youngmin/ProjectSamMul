#nullable enable
using System;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.ResourcePools;

namespace SamMul.UIs.Stages.HUDs
{
    /// <summary>
    /// 특별한 몬스터의 발밑에 표시하는 강조 원입니다.
    /// </summary>
    public class EmphasisCircle : MonoBehaviour
    {
        public static readonly string PREFAB_PATH = "Stage/UIs/EmphasisCircle/EmphasisCircle.prefab";

        public enum Color { Red, Blue }

        [SerializeField] private SpriteRenderer _spriteRenderer = null!;

        public void Initialize(Monster owner, Color color)
        {
            string spritePath = color switch
            {
                Color.Red => "Stage/UIs/EmphasisCircle/EmphasisCircleRed.png",
                Color.Blue => "Stage/UIs/EmphasisCircle/EmphasisCircleBlue.png",
                _ => throw new NotImplementedException($"{color} 색상의 스프라이트 경로가 정의되지 않았습니다."),
            };

            var sprite = ResourcePool.Instance.LoadResource<Sprite>(spritePath);
            _spriteRenderer.sprite = sprite;

            // 스프라이트 지름이 몬스터 콜라이더 지름과 같아지도록 맞추고, 바닥에 눕힌 느낌으로 세로를 절반으로 줄인다.
            float unitsPerSprite = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 1f;
            float scale = 2.0f * owner.ColliderRadius / unitsPerSprite;

            this.transform.SetParent(owner.transform);
            this.transform.localPosition = Vector3.zero;
            this.transform.localScale = new Vector3(scale, scale * 0.5f, 1.0f);
        }
    }
}
