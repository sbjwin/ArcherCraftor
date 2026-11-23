using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArcherCraftor.Combat;

namespace ArcherCraftor.Core
{
    /// <summary>
    /// 게임 상태(상쇄 카운트, 처치 수, 플레이어 사망 및 R키 재시작)를 관리하는 전역 매니저
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isGameOver;
        [SerializeField] private int parriedBulletsCount;
        [SerializeField] private int enemiesDefeatedCount;

        private Health playerHealth;

        public event Action<int> OnParryCountChanged;
        public event Action<int> OnEnemyKilled;
        public event Action OnPlayerDied;

        public int ParriedBulletsCount => parriedBulletsCount;
        public int EnemiesDefeatedCount => enemiesDefeatedCount;
        public bool IsGameOver => isGameOver;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<Health>(out playerHealth))
            {
                playerHealth.SetDestroyOnDeath(false);
                playerHealth.OnDied += TriggerGameOver;
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDied -= TriggerGameOver;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCurrentScene();
            }
        }

        public void AddParryCount()
        {
            parriedBulletsCount++;
            OnParryCountChanged?.Invoke(parriedBulletsCount);
        }

        public void AddEnemyKill()
        {
            enemiesDefeatedCount++;
            OnEnemyKilled?.Invoke(enemiesDefeatedCount);
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;
            OnPlayerDied?.Invoke();
        }

        public void RestartCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
