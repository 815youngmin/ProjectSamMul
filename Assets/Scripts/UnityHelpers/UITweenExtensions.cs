#nullable enable
using DG.Tweening;
using UnityEngine;

namespace Z.UnityHelpers
{
    public static class UITweenExtension
    {
        /// <summary>버튼이 등장할 때 살짝 커졌다가 원래 크기로 돌아오는 연출.</summary>
        public static void DOButtonInitialTween(this Transform buttonTransform, float delay, Vector3 scaleUpOffset)
        {
            var originalScale = buttonTransform.localScale;
            buttonTransform.localScale = Vector3.zero;
            DOTween.Sequence()
                .AppendInterval(delay)
                .Append(buttonTransform.DOScale(originalScale + scaleUpOffset, 0.12f).SetEase(Ease.OutQuad))
                .Append(buttonTransform.DOScale(originalScale, 0.08f).SetEase(Ease.InQuad))
                .SetUpdate(isIndependentUpdate: true)
                .SetLink(buttonTransform.gameObject);
        }

        public static void DOButtonInitialTween(this Transform buttonTransform)
        {
            buttonTransform.DOButtonInitialTween(0f, Vector3.one * 0.1f);
        }
    }
}
