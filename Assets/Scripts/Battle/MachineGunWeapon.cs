using UnityEngine;

// Fires rapidly in the car's forward direction.
public class MachineGunWeapon : WeaponBase
{
    private void Awake()
    {
        damage   = 2f;
        fireRate = 4f; // shots/sec
    }

    protected override void Fire() => SpawnProjectile(firePoint);
}
