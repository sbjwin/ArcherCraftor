using UnityEngine;

namespace ArcherCraftor.Combat
{
    /// <summary>
    /// 화염 장판(Blast) 및 독성 안개(Toxic)를 처리하는 2D 지속 광역(DoT) 존
    /// </summary>
    public class HazardZone2D : MonoBehaviour
    {
        public enum ZoneKind
        {
            FirePool,
            ToxicCloud
        }

        private ZoneKind zoneKind;
        private float radius = 2.4f;
        private float duration = 3.5f;
        private float tickInterval = 0.5f;
        private float damagePerTick = 8f;
        private float vulnerabilityMultiplier = 1.35f;

        private float lifeTimer;
        private float tickTimer;

        private readonly Collider2D[] overlapBuffer = new Collider2D[24];
        private ContactFilter2D noFilter;

        public void Initialize(ZoneKind kind, float zoneRadius, float zoneDuration, float tickDamage, SpriteRenderer visualRenderer = null)
        {
            zoneKind = kind;
            radius = zoneRadius;
            duration = zoneDuration;
            damagePerTick = tickDamage;

            noFilter = ContactFilter2D.noFilter;

            transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            if (visualRenderer != null)
            {
                visualRenderer.color = kind == ZoneKind.FirePool
                    ? new Color(1f, 0.35f, 0.1f, 0.42f)
                    : new Color(0.45f, 0.95f, 0.3f, 0.38f);
            }
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= duration)
            {
                Destroy(gameObject);
                return;
            }

            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = tickInterval;
                ApplyZoneTick();
            }
        }

        private void ApplyZoneTick()
        {
            int count = Physics2D.OverlapCircle((Vector2)transform.position, radius, noFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null || !col.CompareTag("Enemy")) continue;

                if (col.TryGetComponent<Health>(out var health))
                {
                    if (zoneKind == ZoneKind.ToxicCloud)
                    {
                        health.ApplyVulnerability(vulnerabilityMultiplier, 2.0f);
                    }
                    health.TakeDamage(damagePerTick, col.transform.position, Vector3.zero);
                }
            }
        }
    }
}
