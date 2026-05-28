using UnityEngine;

// Requires: Rigidbody (isKinematic=true, useGravity=false) + Collider (isTrigger=true)
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed    = 22f;
    [SerializeField] private float lifetime = 4f;

    private float  _damage;
    private string _ownerTag;

    public void Initialize(float damage, string ownerTag)
    {
        _damage   = damage;
        _ownerTag = ownerTag;
        Destroy(gameObject, lifetime);
    }

    private void Update() =>
        transform.position += transform.forward * (speed * Time.deltaTime);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_ownerTag) || other.isTrigger) return;

        HealthSystem hp = other.GetComponentInParent<HealthSystem>()
                       ?? other.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
