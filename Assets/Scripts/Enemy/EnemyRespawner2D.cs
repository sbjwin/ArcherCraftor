using System.Collections;
using UnityEngine;
using ArcherCraftor.Combat;

namespace ArcherCraftor.Enemy
{
    /// <summary>
    /// 적이 사망한 뒤 지정된 시간(기본 3초) 후 동일 위치에 다시 스폰하는 2D 리스폰 매니저
    /// </summary>
    public class EnemyRespawner2D : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject currentEnemy;
        [SerializeField] private float respawnDelay = 3.0f;

        private Health trackedHealth;
        private bool isRespawning;

        private void Start()
        {
            if (currentEnemy != null)
            {
                BindEnemy(currentEnemy);
            }
            else
            {
                SpawnEnemy();
            }
        }

        private void OnDestroy()
        {
            UnbindCurrent();
        }

        private void BindEnemy(GameObject enemyObj)
        {
            UnbindCurrent();
            currentEnemy = enemyObj;

            if (currentEnemy != null && currentEnemy.TryGetComponent<Health>(out trackedHealth))
            {
                trackedHealth.OnDied += HandleEnemyDied;
            }
        }

        private void UnbindCurrent()
        {
            if (trackedHealth != null)
            {
                trackedHealth.OnDied -= HandleEnemyDied;
                trackedHealth = null;
            }
        }

        private void HandleEnemyDied()
        {
            UnbindCurrent();
            if (!isRespawning && gameObject.activeInHierarchy)
            {
                StartCoroutine(RespawnAfterDelay());
            }
        }

        private IEnumerator RespawnAfterDelay()
        {
            isRespawning = true;
            yield return new WaitForSeconds(respawnDelay);
            isRespawning = false;

            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null) return;

            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);
            GameObject spawned = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawned.name = enemyPrefab.name;
            BindEnemy(spawned);
        }
    }
}
