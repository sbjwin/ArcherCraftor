using System;
using UnityEngine;

namespace ArcherCraftor.Combat
{
    /// <summary>
    /// 플레이어 및 적의 체력, 피격, 무적 프레임, 독성 방어력 약화(취약), 사망 이벤트를 관리하는 컴포넌트
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private GameObject deathEffectPrefab;

        private bool isInvulnerable;
        private float vulnerabilityMultiplier = 1f;
        private float vulnerabilityTimer;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDied;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0f;
        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (vulnerabilityTimer > 0f)
            {
                vulnerabilityTimer -= Time.deltaTime;
                if (vulnerabilityTimer <= 0f)
                {
                    vulnerabilityMultiplier = 1f;
                }
            }
        }

        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        public void SetDestroyOnDeath(bool shouldDestroy)
        {
            destroyOnDeath = shouldDestroy;
        }

        /// <summary>
        /// 독성 화살 등으로 인해 일정 시간 동안 받는 피해 배율 증가 (예: 1.35f)
        /// </summary>
        public void ApplyVulnerability(float damageMultiplier, float duration)
        {
            vulnerabilityMultiplier = Mathf.Max(vulnerabilityMultiplier, damageMultiplier);
            vulnerabilityTimer = Mathf.Max(vulnerabilityTimer, duration);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (IsDead || isInvulnerable) return;

            float finalDamage = amount * vulnerabilityMultiplier;
            currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            OnDied?.Invoke();

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
