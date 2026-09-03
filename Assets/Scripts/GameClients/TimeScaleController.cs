using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleController
{
    private float _baseTimeScale;

    //timeScale = 1.0f로 만들어준다.
    public void Initialize()
    {
        _baseTimeScale = 1.0f;
        Time.timeScale = 1.0f;
    }
    public void Initialize(float timeScale)
    {
        _baseTimeScale = timeScale;
        Time.timeScale = timeScale;
    }

    public void ChangeTimeScale(float timeScale)
    {
        _baseTimeScale = timeScale;
        Time.timeScale = timeScale;
    }

    public void PauseTimeScale()
    {
        Time.timeScale = 0;
    }
    public void ResumeTimeScale()
    {
        Time.timeScale = _baseTimeScale;
    }
}
