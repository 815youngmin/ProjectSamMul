using UnityEngine;
using SamMul.GameClients.Stages.Characters;

public class BloodTrail : MonoBehaviour
{
    public static readonly string PREFAB_PATH = "Stages/ETCEffects/BloodTrail.prefab";

    private Character _owner;
    private Vector2 _startPosition;
    private Vector2 _curvePosition;
    private float _duration;
    private float _time;
    private bool _isEndPosition;

    public void Initialize(Character owner, Vector2 startPosition, Vector2 curvePosition, float duration)
    {
        _owner = owner;
        this.transform.position = startPosition;
        _startPosition = startPosition;
        _curvePosition = curvePosition;
        _duration = duration;
        _time = 0;
        this.GetComponent<TrailRenderer>().Clear();
        _isEndPosition = false;
    }

    public void UpdateLogic(float deltaTime)
    {
        if (_isEndPosition)
        {
            return;
        }

        _time += deltaTime;
        float t = _time / _duration;
        this.transform.position = this.CalculateBezierPoint(t, _startPosition, _curvePosition, _owner.CenterPos);

        if (t >= 1)
        {
            _isEndPosition = true;
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 startPosition, Vector3 curvePosition, Vector3 destination)
    {
        if (t > 1)
        {
            t = 1f;
        }
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * startPosition;
        p += 2 * u * t * curvePosition;
        p += tt * destination;

        return p;
    }
}
