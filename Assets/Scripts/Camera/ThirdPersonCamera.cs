using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new(0f, 5f, -10f);
    [SerializeField] private float   smoothSpeed = 6f;
    [SerializeField] private Vector3 lookAtOffset = new(0f, 1f, 0f);

    private Transform _target;

    public void SetTarget(Transform t) => _target = t;

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desired = _target.TransformPoint(localOffset);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(_target.position + lookAtOffset);
    }
}
