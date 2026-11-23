using UnityEngine;

namespace ArcherCraftor.Combat
{
    /// <summary>
    /// 2D XY 평면에서 비행하며 화살과 충돌 시 상쇄(패링)되거나 플레이어/장애물에 적중하는 적 탄환
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float damage = 15f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private bool canBeParried = true;

        [Header("Effects")]
        [SerializeField] private GameObject parrySparkEffectPrefab;
        [SerializeField] private GameObject hitEffectPrefab;

        private Vector2 moveDirection;
        private float lifeTimer;
        private bool isDestroyed;

        public bool CanBeParried => canBeParried;
        public float Damage => damage;
        public bool IsDestroyed => isDestroyed;

        public void Initialize(Vector2 direction, float customSpeed = -1f, float customDamage = -1f)
        {
            moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            if (customSpeed > 0f) speed = customSpeed;
            if (customDamage > 0f) damage = customDamage;

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (isDestroyed) return;

            Vector3 delta = (Vector3)(moveDirection * (speed * Time.deltaTime));
            transform.position += delta;

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDestroyed) return;

            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage, transform.position, -moveDirection);
                }

                SpawnEffect(hitEffectPrefab);
                DestroyBullet();
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || other.CompareTag("Obstacle"))
            {
                SpawnEffect(hitEffectPrefab);
                DestroyBullet();
            }
        }

        /// <summary>
        /// 플레이어 화살에 요격되어 상쇄(패링)되었을 때 호출
        /// </summary>
        public void OnParried(Vector3 parryPoint)
        {
            if (isDestroyed || !canBeParried) return;

            SpawnEffect(parrySparkEffectPrefab, parryPoint);
            DestroyBullet();
        }

        private void SpawnEffect(GameObject effectPrefab, Vector3? position = null)
        {
            if (effectPrefab != null)
            {
                Vector3 spawnPos = position ?? transform.position;
                spawnPos.z = 0f;
                Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            }
        }

        private void DestroyBullet()
        {
            isDestroyed = true;
            Destroy(gameObject);
        }
    }
}
