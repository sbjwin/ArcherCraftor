using UnityEngine;
using UnityEngine.UI;
using ArcherCraftor.Combat;
using ArcherCraftor.Core;
using ArcherCraftor.Crafting;

namespace ArcherCraftor.UI
{
    /// <summary>
    /// 체력바, 선택된 특수 화살(듀얼 퀴버), 보유 재료 수량, 탄막 상쇄/처치 통계, 게임오버 패널을 갱신하는 인게임 HUD
    /// </summary>
    public class InGameHUD : MonoBehaviour
    {
        [Header("Health Bar")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Health playerHealth;

        [Header("Dual Quiver & Crafting UI")]
        [SerializeField] private Image selectedArrowColorBadge;
        [SerializeField] private Text selectedArrowText;
        [SerializeField] private Text resourceSummaryText;
        [SerializeField] private Text combatStatsText;

        [Header("Panels")]
        [SerializeField] private GameObject guidePanel;
        [SerializeField] private GameObject gameOverPanel;

        private void Start()
        {
            if (playerHealth == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerHealth = player.GetComponent<Health>();
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (FieldCraftingManager.Instance != null)
            {
                FieldCraftingManager.Instance.OnSelectedArrowChanged += OnSelectedArrowChanged;
                FieldCraftingManager.Instance.OnInventoryOrQuiverUpdated += RefreshQuiverAndResources;
                RefreshQuiverAndResources();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnParryCountChanged += OnStatsChanged;
                GameManager.Instance.OnEnemyKilled += OnStatsChanged;
                GameManager.Instance.OnPlayerDied += ShowGameOver;
                RefreshCombatStats();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
            }

            if (FieldCraftingManager.Instance != null)
            {
                FieldCraftingManager.Instance.OnSelectedArrowChanged -= OnSelectedArrowChanged;
                FieldCraftingManager.Instance.OnInventoryOrQuiverUpdated -= RefreshQuiverAndResources;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnParryCountChanged -= OnStatsChanged;
                GameManager.Instance.OnEnemyKilled -= OnStatsChanged;
                GameManager.Instance.OnPlayerDied -= ShowGameOver;
            }
        }

        private void UpdateHealthBar(float current, float max)
        {
            if (healthSlider != null && max > 0f)
            {
                healthSlider.value = current / max;
            }

            if (healthText != null)
            {
                healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void OnSelectedArrowChanged(ArrowType _)
        {
            RefreshQuiverAndResources();
        }

        private void RefreshQuiverAndResources()
        {
            var crafting = FieldCraftingManager.Instance;
            if (crafting == null) return;

            ArrowType selected = crafting.SelectedSpecialArrow;
            int arrowCount = crafting.GetArrowCount(selected);

            if (selectedArrowColorBadge != null)
            {
                selectedArrowColorBadge.color = ArrowVisualProfile.GetArrowColor(selected);
            }

            if (selectedArrowText != null)
            {
                selectedArrowText.text = $"[우클릭] {ArrowVisualProfile.GetDisplayName(selected)} : {arrowCount} / {crafting.MaxArrowsPerSlot}발";
            }

            if (resourceSummaryText != null)
            {
                resourceSummaryText.text =
                    $"재료 | 충격석:{crafting.GetResourceCount(ResourceType.FlintStone)}  " +
                    $"화염열매:{crafting.GetResourceCount(ResourceType.FlameBerry)}  " +
                    $"서리버섯:{crafting.GetResourceCount(ResourceType.FrostMushroom)}  " +
                    $"정전기:{crafting.GetResourceCount(ResourceType.SparkCrystal)}  " +
                    $"독낭:{crafting.GetResourceCount(ResourceType.ToxicSac)}";
            }
        }

        private void OnStatsChanged(int _)
        {
            RefreshCombatStats();
        }

        private void RefreshCombatStats()
        {
            if (combatStatsText == null || GameManager.Instance == null) return;
            combatStatsText.text = $"탄막 상쇄: {GameManager.Instance.ParriedBulletsCount}   적 처치: {GameManager.Instance.EnemiesDefeatedCount}";
        }

        private void ShowGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }
    }
}
