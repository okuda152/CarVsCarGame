using UnityEngine;

// Slowly rotates its head toward the nearest enemy and fires on demand.
public class TurretWeapon : WeaponBase
{
    [SerializeField] private Transform turretHead;
    [SerializeField] private float     rotateSpeed    = 120f; // deg/sec
    [SerializeField] private float     detectionRange = 35f;

    private Transform _target;

    private void Awake()
    {
        // Turret has high damage, slow fire rate
        damage   = 10f;
        fireRate = 0.6f;
    }

    private void Update()
    {
        _target = FindTarget();
        if (_target != null && turretHead != null)
            RotateHeadToward(_target.position);
    }

    protected override void Fire() => SpawnProjectile(firePoint);

    private Transform FindTarget()
    {
        string targetTag = OwnerTag == "Player" ? "Enemy" : "Player";
        GameObject obj = GameObject.FindWithTag(targetTag);
        if (obj == null) return null;
        return Vector3.Distance(transform.position, obj.transform.position) <= detectionRange
            ? obj.transform : null;
    }

    private void RotateHeadToward(Vector3 worldPos)
    {
        Vector3 dir = worldPos - turretHead.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        turretHead.rotation = Quaternion.RotateTowards(
            turretHead.rotation, target, rotateSpeed * Time.deltaTime);
    }
}
