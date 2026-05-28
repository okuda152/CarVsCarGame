using System.Collections.Generic;
using UnityEngine;

// Aggregates all weapons on a car and provides a single FireAll() call.
public class WeaponMount : MonoBehaviour
{
    private readonly List<WeaponBase> _weapons = new();

    public void AddWeapon(WeaponBase weapon) => _weapons.Add(weapon);

    public void SetOwnerTag(string tag)
    {
        foreach (var w in _weapons) w.SetOwner(tag);
    }

    public void FireAll()
    {
        foreach (var w in _weapons) w.TryFire();
    }
}
