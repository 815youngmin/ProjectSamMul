#nullable enable
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressBar : MonoBehaviour
{
    [SerializeField] private Slider _progressBar = null!;

    public void Initialize() => Initialize(0f, 1f, 0f);

    public void Initialize(float minValue, float maxValue, float currentValue)
    {
        _progressBar.minValue = minValue;
        _progressBar.maxValue = maxValue;
        _progressBar.value = currentValue;
    }

    public void SetProgressValue(float value) => _progressBar.value = value;
}
