using UnityEngine;
using ArcherCraftor.Crafting;

namespace ArcherCraftor.Combat
{
    /// <summary>
    /// 2D XY 평면에서 비행하며 적 탄막 상쇄(Counter-Shot)와 5종 특수 화살 기믹을 수행하는 투사체
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class ArrowProjectile : MonoBehaviour
    {
        [Header("Flight Settings")]
        [SerializeField] private float speed = 25f;
        [SerializeField] private float damage = 30f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float maxRange = 22f;
        [SerializeField] private int pierceCount = 1;

        [Header("Counter-shot / Parry (2D)")]
        [Tooltip("적 탄막을 요격하는 자석 반경(속사 시 조준각에 비례해 더 넓게 보정 가능)")]
        [SerializeField] private float magneticParryRadius = 0.85f;
        [SerializeField] private LayerMask enemyBulletLayer;

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject parrySparkPrefab;
        [SerializeField] private GameObject hitFleshPrefab;
        [SerializeField] private GameObject hitObstaclePrefab;
        [SerializeField] private Sprite zoneSprite;

        private Vector2 moveDirection;
        private ArrowType arrowType = ArrowType.Standard;
        private float chargeRatio;
        private float traveledDistance;
        private float lifeTimer;
        private int remainingPierce;
        private bool isDisposed;

        private readonly Collider2D[] overlapBuffer = new Collider2D[32];
        private ContactFilter2D bulletFilter;
        private ContactFilter2D allFilter;

        private void Awake()
        {
            remainingPierce = pierceCount;
            if (enemyBulletLayer.value != 0)
            {
                bulletFilter = new ContactFilter2D();
                bulletFilter.SetLayerMask(enemyBulletLayer);
                bulletFilter.useLayerMask = true;
            }
            else
            {
                bulletFilter = ContactFilter2D.noFilter;
            }

            allFilter = ContactFilter2D.noFilter;
        }

        public void Initialize(
            Vector2 direction,
            float charge,
            float baseSpeed = 25f,
            float baseDamage = 30f,
            int basePierce = 1,
            ArrowType type = ArrowType.Standard,
            float minRange = 7f,
            float maxAimRange = 22f)
        {
            moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            chargeRatio = Mathf.Clamp01(charge);
            arrowType = type;

            var specs = AimChargeMath.CalculateArrowSpecs(chargeRatio, baseSpeed, baseDamage, basePierce);
            speed = specs.speed;
            damage = specs.damage;
            remainingPierce = specs.pierce;
            maxRange = AimChargeMath.CalculateAimRange(chargeRatio, minRange, maxAimRange);

            // 속사(차징 전)일수록 넓은 조준 부채꼴을 반영해 근접 요격 판정을 소폭 확대
            if (chargeRatio < 0.25f)
            {
                magneticParryRadius = Mathf.Max(magneticParryRadius, 1.15f);
            }

            if (arrowType == ArrowType.Toxic)
            {
                remainingPierce += 1;
            }

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = ArrowVisualProfile.GetArrowColor(arrowType);
            }
        }

        private void Update()
        {
            if (isDisposed) return;

            float stepDist = speed * Time.deltaTime;
            transform.position += (Vector3)(moveDirection * stepDist);
            traveledDistance += stepDist;

            CheckParryEnemyBullets();

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime || traveledDistance >= maxRange)
            {
                if (arrowType == ArrowType.Blast || arrowType == ArrowType.Toxic)
                {
                    TriggerSpecialImpact(transform.position, null, hitObstacle: false);
                }
                DestroySelf();
            }
        }

        private void CheckParryEnemyBullets()
        {
            int count = Physics2D.OverlapCircle((Vector2)transform.position, magneticParryRadius, bulletFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<EnemyBullet>(out var bullet) && bullet.CanBeParried && !bullet.IsDestroyed)
                {
                    bullet.OnParried(col.transform.position);
                    SpawnEffect(parrySparkPrefab, col.transform.position);
                    Core.GameManager.Instance?.AddParryCount();

                    if (arrowType == ArrowType.Spark)
                    {
                        TriggerSparkChain(col.transform.position);
                    }
                    else if (arrowType == ArrowType.Blast)
                    {
                        TriggerBlastExplosion(col.transform.position, chargeRatio >= 0.5f);
                    }

                    remainingPierce--;
                    if (remainingPierce <= 0)
                    {
                        DestroySelf();
                        return;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDisposed) return;
            if (other.CompareTag("Player")) return;

            if (other.CompareTag("Enemy") || other.GetComponent<IDamageable>() != null)
            {
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage, transform.position, moveDirection);
                }

                TriggerSpecialImpact(transform.position, other, hitObstacle: false);
                SpawnEffect(hitFleshPrefab, transform.position);

                remainingPierce--;
                if (remainingPierce <= 0)
                {
                    DestroySelf();
                }
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || other.CompareTag("Obstacle"))
            {
                TriggerSpecialImpact(transform.position, other, hitObstacle: true);
                SpawnEffect(hitObstaclePrefab, transform.position);
                DestroySelf();
            }
        }

        private void TriggerSpecialImpact(Vector3 impactPoint, Collider2D directTarget, bool hitObstacle)
        {
            bool isCharged = chargeRatio >= 0.5f;

            switch (arrowType)
            {
                case ArrowType.Impact:
                    if (directTarget != null && directTarget.TryGetComponent<ICrowdControlTarget>(out var cc))
                    {
                        cc.ApplyKnockback(moveDirection * (isCharged ? 16f : 10f));
                        if (isCharged) cc.ApplyStun(1.4f);
                    }
                    if (hitObstacle && isCharged)
                    {
                        ApplyRadialCrowdControl(impactPoint, 3.2f, stunDuration: 1.6f, knockbackForce: 12f);
                    }
                    break;

                case ArrowType.Blast:
                    TriggerBlastExplosion(impactPoint, isCharged);
                    break;

                case ArrowType.Frost:
                    if (directTarget != null && directTarget.TryGetComponent<ICrowdControlTarget>(out var frostTarget))
                    {
                        frostTarget.ApplySlow(0.3f, 3.0f); // 70% 감속
                    }
                    if (isCharged)
                    {
                        ApplyRadialFreeze(impactPoint, 3.4f, 2.2f);
                    }
                    break;

                case ArrowType.Spark:
                    TriggerSparkChain(impactPoint);
                    break;

                case ArrowType.Toxic:
                    if (directTarget != null && directTarget.TryGetComponent<Health>(out var targetHealth))
                    {
                        targetHealth.ApplyVulnerability(1.35f, 4.5f);
                    }
                    SpawnHazardZone(impactPoint, HazardZone2D.ZoneKind.ToxicCloud, isCharged ? 3.2f : 1.9f, isCharged ? 4.5f : 2.8f, 7f);
                    break;
            }
        }

        private void TriggerBlastExplosion(Vector3 center, bool isCharged)
        {
            float radius = isCharged ? 3.4f : 2.0f;
            float aoeDamage = isCharged ? damage * 0.9f : damage * 0.5f;

            int count = Physics2D.OverlapCircle((Vector2)center, radius, allFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<EnemyBullet>(out var bullet) && bullet.CanBeParried && !bullet.IsDestroyed)
                {
                    bullet.OnParried(col.transform.position);
                    Core.GameManager.Instance?.AddParryCount();
                }
                else if (col.CompareTag("Enemy") && col.TryGetComponent<IDamageable>(out var enemyDmg))
                {
                    enemyDmg.TakeDamage(aoeDamage, center, Vector2.zero);
                }
            }

            SpawnEffect(parrySparkPrefab, center);
            if (isCharged)
            {
                SpawnHazardZone(center, HazardZone2D.ZoneKind.FirePool, 2.8f, 3.5f, 9f);
            }
        }

        private void TriggerSparkChain(Vector3 center)
        {
            bool isCharged = chargeRatio >= 0.5f;
            float chainRadius = isCharged ? 5.0f : 3.4f;
            int maxBulletsCleared = isCharged ? 10 : 4;
            int cleared = 0;

            int count = Physics2D.OverlapCircle((Vector2)center, chainRadius, allFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null) continue;

                if (cleared < maxBulletsCleared && col.TryGetComponent<EnemyBullet>(out var bullet) && bullet.CanBeParried && !bullet.IsDestroyed)
                {
                    bullet.OnParried(col.transform.position);
                    Core.GameManager.Instance?.AddParryCount();
                    cleared++;
                }
                else if (col.CompareTag("Enemy") && col.TryGetComponent<IDamageable>(out var enemyDmg))
                {
                    enemyDmg.TakeDamage(isCharged ? damage * 0.85f : damage * 0.45f, col.transform.position, Vector2.zero);
                }
            }
        }

        private void ApplyRadialCrowdControl(Vector3 center, float radius, float stunDuration, float knockbackForce)
        {
            int count = Physics2D.OverlapCircle((Vector2)center, radius, allFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null || !col.CompareTag("Enemy")) continue;

                if (col.TryGetComponent<ICrowdControlTarget>(out var cc))
                {
                    Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)center).normalized;
                    cc.ApplyKnockback(pushDir * knockbackForce);
                    cc.ApplyStun(stunDuration);
                }
            }
        }

        private void ApplyRadialFreeze(Vector3 center, float radius, float freezeDuration)
        {
            int count = Physics2D.OverlapCircle((Vector2)center, radius, allFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null || !col.CompareTag("Enemy")) continue;

                if (col.TryGetComponent<ICrowdControlTarget>(out var cc))
                {
                    cc.ApplyFreeze(freezeDuration);
                }
            }
        }

        private void SpawnHazardZone(Vector3 position, HazardZone2D.ZoneKind kind, float radius, float duration, float tickDamage)
        {
            GameObject zoneObj = new GameObject($"Zone_{kind}");
            zoneObj.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer sr = null;
            if (zoneSprite != null)
            {
                sr = zoneObj.AddComponent<SpriteRenderer>();
                sr.sprite = zoneSprite;
                sr.sortingLayerName = "GroundDecal";
                sr.sortingOrder = 1;
            }

            var zone = zoneObj.AddComponent<HazardZone2D>();
            zone.Initialize(kind, radius, duration, tickDamage, sr);
        }

        private void SpawnEffect(GameObject prefab, Vector3 position)
        {
            if (prefab != null)
            {
                position.z = 0f;
                Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private void DestroySelf()
        {
            isDisposed = true;
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, magneticParryRadius);
        }
    }
}
