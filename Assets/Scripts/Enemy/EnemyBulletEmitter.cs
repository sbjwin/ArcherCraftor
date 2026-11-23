using UnityEngine;
using ArcherCraftor.Combat;

namespace ArcherCraftor.Enemy
{
    /// <summary>
    /// 2D XY 평면에서 조준 탄환, 부채꼴 탄막, 360도 링 탄막을 발사하는 적 탄막 에미터
    /// </summary>
    public class EnemyBulletEmitter : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;

        private void Awake()
        {
            if (firePoint == null) firePoint = transform;
        }

        public void FireAimShot(Vector2 targetPosition, float bulletSpeed = 10f)
        {
            Vector2 dir = (targetPosition - (Vector2)firePoint.position).normalized;
            SpawnBullet(firePoint.position, dir, bulletSpeed);
        }

        public void FireSpread(Vector2 targetPosition, int count = 5, float spreadAngle = 45f, float bulletSpeed = 9.5f)
        {
            Vector2 baseDir = (targetPosition - (Vector2)firePoint.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            float startAngle = baseAngle - spreadAngle * 0.5f;
            float step = count > 1 ? spreadAngle / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = (startAngle + step * i) * Mathf.Deg2Rad;
                Vector2 shootDir = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
                SpawnBullet(firePoint.position, shootDir, bulletSpeed);
            }
        }

        public void FireRing(int bulletCount = 12, float bulletSpeed = 8f)
        {
            float stepAngle = 360f / bulletCount;
            for (int i = 0; i < bulletCount; i++)
            {
                float rad = stepAngle * i * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                SpawnBullet(firePoint.position, dir, bulletSpeed);
            }
        }

        private void SpawnBullet(Vector3 position, Vector2 direction, float speed)
        {
            if (bulletPrefab == null) return;

            position.z = 0f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.Euler(0f, 0f, angle));
            if (bulletObj.TryGetComponent<EnemyBullet>(out var bullet))
            {
                bullet.Initialize(direction, speed);
            }
        }
    }
}
