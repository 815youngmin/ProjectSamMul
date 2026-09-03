using UnityEngine;

/// <summary>
/// Rotates the transform continuously by <see cref="RotateEulers"/> degrees per second.
/// </summary>
public class AutoTransformRotate : MonoBehaviour
{
    public Vector3 RotateEulers;

    private void Update()
    {
        this.transform.Rotate(RotateEulers * Time.deltaTime);
    }
}
