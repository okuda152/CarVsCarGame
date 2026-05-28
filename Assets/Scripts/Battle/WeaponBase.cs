using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected float      damage          = 15f;
    [SerializeField] protected float      fireRate        = 1f; // shots/sec
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform  firePoint;

    protected string OwnerTag;
    private float _nextFireTime;

    public void SetOwner(string tag) => OwnerTag = tag;

    public void TryFire()
    {
        if (Time.time < _nextFireTime) return;
        _nextFireTime = Time.time + 1f / fireRate;
        Fire();
    }

    protected abstract void Fire();

    protected void SpawnProjectile(Transform origin)
    {
        if (projectilePrefab == null || origin == null) return;
        GameObject go = Instantiate(projectilePrefab, origin.position, origin.rotation);
        go.GetComponent<Projectile>()?.Initialize(damage, OwnerTag);
    }
}
