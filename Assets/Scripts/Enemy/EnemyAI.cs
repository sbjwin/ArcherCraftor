using UnityEngine;
using ArcherCraftor.Combat;
using ArcherCraftor.Crafting;

namespace ArcherCraftor.Enemy
{
    /// <summary>
    /// 2D XY 평면에서 카이팅 거리를 유지하며 탄막을 발사하고 군중 제어(넉백/기절/둔화/빙결) 및 재료 드롭을 처리하는 적 AI
    /// </summary>
    [RequireComponent(typeof(EnemyBulletEmitter), typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour, ICrowdControlTarget
    {
        public enum AttackPattern
        {
            AimShot,
            SpreadShot,
            RingBurst
        }

        [Header("Detection & Movement (2D)")]
        [SerializeField] private bool isStationary;
        [SerializeField] private float detectionRadius = 16f;
        [SerializeField] private float attackRadius = 12f;
        [SerializeField] private float keepDistance = 6f;
        [SerializeField] private float moveSpeed = 3.2f;

        [Header("Combat Patterns")]
        [SerializeField] private float attackInterval = 2.0f;
        [SerializeField] private AttackPattern defaultPattern = AttackPattern.SpreadShot;

        [Header("Loot Drop")]
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private ResourceType dropResourceType = ResourceType.FlameBerry;

        private Transform playerTarget;
        private EnemyBulletEmitter emitter;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Health health;
        private Color baseColor = Color.white;

        private float attackTimer;
        private float retargetTimer;
        private float stunTimer;
        private float freezeTimer;
        private float slowTimer;
        private float slowMultiplier = 1f;
        private Vector2 knockbackVelocity;

        private const float RetargetInterval = 1.0f;

        public void SetTarget(Transform target)
        {
            playerTarget = target;
        }

        public void ConfigurePattern(AttackPattern pattern, ResourceType dropType)
        {
            defaultPattern = pattern;
            dropResourceType = dropType;
        }

        private void Awake()
        {
            emitter = GetComponent<EnemyBulletEmitter>();
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            if (isStationary)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) baseColor = spriteRenderer.color;

            health = GetComponent<Health>();
        }

        private void Start()
        {
            TryFindPlayerTarget();

            if (health != null)
            {
                health.OnDied += HandleEnemyDied;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDied -= HandleEnemyDied;
            }
        }

        private void TryFindPlayerTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        private void Update()
        {
            UpdateCrowdControlTimers();

            if (playerTarget == null)
            {
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f)
                {
                    retargetTimer = RetargetInterval;
                    TryFindPlayerTarget();
                }
                return;
            }

            if (freezeTimer > 0f || stunTimer > 0f)
            {
                return;
            }

            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist <= detectionRadius)
            {
                LookAtPlayer();

                if (dist <= attackRadius)
                {
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        ExecuteAttack();
                        attackTimer = attackInterval;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (isStationary)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (knockbackVelocity.sqrMagnitude > 0.05f)
            {
                rb.linearVelocity = knockbackVelocity;
                knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.fixedDeltaTime * 9f);
                return;
            }

            if (playerTarget == null || freezeTimer > 0f || stunTimer > 0f)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toPlayer = (Vector2)(playerTarget.position - transform.position);
            float dist = toPlayer.magnitude;

            if (dist > detectionRadius)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            float effectiveSpeed = moveSpeed * (slowTimer > 0f ? slowMultiplier : 1f);
            Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.zero;

            if (dist > keepDistance + 1f)
            {
                rb.linearVelocity = dir * effectiveSpeed;
            }
            else if (dist < keepDistance - 1f)
            {
                rb.linearVelocity = -dir * (effectiveSpeed * 0.7f);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void UpdateCrowdControlTimers()
        {
            if (stunTimer > 0f) stunTimer -= Time.deltaTime;
            if (freezeTimer > 0f) freezeTimer -= Time.deltaTime;
            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f) slowMultiplier = 1f;
            }

            if (spriteRenderer != null)
            {
                if (freezeTimer > 0f)
                    spriteRenderer.color = new Color(0.45f, 0.85f, 1f);
                else if (stunTimer > 0f)
                    spriteRenderer.color = new Color(1f, 0.88f, 0.35f);
                else if (slowTimer > 0f)
                    spriteRenderer.color = Color.Lerp(baseColor, new Color(0.4f, 0.75f, 1f), 0.55f);
                else
                    spriteRenderer.color = baseColor;
            }
        }

        private void LookAtPlayer()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = playerTarget.position.x < transform.position.x;
            }
        }

        private void ExecuteAttack()
        {
            Vector2 targetPos = playerTarget.position;
            switch (defaultPattern)
            {
                case AttackPattern.AimShot:
                    emitter.FireAimShot(targetPos);
                    break;
                case AttackPattern.SpreadShot:
                    emitter.FireSpread(targetPos, count: 5, spreadAngle: 40f);
                    break;
                case AttackPattern.RingBurst:
                    emitter.FireRing(bulletCount: 14);
                    break;
            }
        }

        private void HandleEnemyDied()
        {
            Core.GameManager.Instance?.AddEnemyKill();

            if (pickupPrefab != null)
            {
                Vector3 dropPos = transform.position;
                dropPos.z = 0f;
                GameObject drop = Instantiate(pickupPrefab, dropPos, Quaternion.identity);
                if (drop.TryGetComponent<PickupResource2D>(out var pickup))
                {
                    pickup.Configure(dropResourceType, 2);
                }
            }
        }

        public void ApplyKnockback(Vector2 impulse)
        {
            if (isStationary) return;
            knockbackVelocity = impulse;
        }

        public void ApplyStun(float duration)
        {
            stunTimer = Mathf.Max(stunTimer, duration);
        }

        public void ApplySlow(float speedMultiplier, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp(speedMultiplier, 0.1f, 1f));
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        public void ApplyFreeze(float duration)
        {
            freezeTimer = Mathf.Max(freezeTimer, duration);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
    }
}
