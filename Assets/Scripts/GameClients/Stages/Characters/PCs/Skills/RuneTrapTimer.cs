using UnityEngine;
using UnityEngine.UI;

public class RuneTrapTimer : MonoBehaviour
{
    [SerializeField] private Image _maskImage;

    public void FillAmount(float amount)
    {
        _maskImage.fillAmount = amount;
    }
}
