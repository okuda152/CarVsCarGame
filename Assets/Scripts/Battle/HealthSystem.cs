using System;
using System.Collections;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHP = 100f;

    public float MaxHP   => maxHP;
    public float Current { get; private set; }
    public bool  IsDead  => Current <= 0f;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action               OnDied;

    private void Awake() => Current = maxHP;

    /// <summary>CarBuilder から動的に HP を設定するときに使う。</summary>
    public void Initialize(float hp)
    {
        maxHP   = hp;
        Current = hp;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0f, Current - amount);
        OnHealthChanged?.Invoke(Current, maxHP);
        if (IsDead) Die();
    }

    private void Die()
    {
        OnDied?.Invoke();
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / 0.4f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
