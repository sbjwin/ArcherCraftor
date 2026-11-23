using System;
using UnityEngine;
using ArcherCraftor.Combat;
using ArcherCraftor.Crafting;

namespace ArcherCraftor.Player
{
    /// <summary>
    /// 듀얼 퀴버(좌클릭 기본 무한 화살 / 우클릭 특수 즉석 제작 화살 / 휠 탄종 전환) 활 발사 컨트롤러
    /// </summary>
    public class PlayerBow : MonoBehaviour
    {
        [Header("Prefabs & Spawn")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform firePoint;

        [Header("Charging Settings (차징 & 속사)")]
        [Tooltip("완충까지 걸리는 시간(초)")]
        [Range(0.2f, 3.0f)]
        [SerializeField] private float maxChargeTime = 1.0f;
        [Tooltip("이 시간 이하로 클릭 후 떼면 즉발 퀵샷 판정")]
        [SerializeField] private float quickShotThreshold = 0.2f;
        [Tooltip("차징 중 이동속도 배율")]
        [SerializeField] private float chargingMoveSpeedMultiplier = 0.55f;

        [Header("Fire Rate & Range")]
        [SerializeField] private float fireCooldown = 0.25f;
        [SerializeField] private float minAimRange = 7f;
        [SerializeField] private float maxAimRange = 22f;

        [Header("Arrow Specs")]
        [SerializeField] private float baseSpeed = 26f;
        [SerializeField] private float baseDamage = 35f;

        private PlayerController playerController;
        private float currentCharge;
        private float chargeTimer;
        private float cooldownTimer;
        private bool isCharging;
        private bool chargingSpecial;

        public event Action<float> OnChargeUpdated; // 0.0 ~ 1.0
        public event Action OnFired;

        public float CurrentChargeRatio => currentCharge;
        public bool IsCharging => isCharging;
        public bool IsChargingSpecial => chargingSpecial;
        public float MinAimRange => minAimRange;
        public float MaxAimRange => maxAimRange;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            if (firePoint == null) firePoint = transform;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            HandleCraftingAndScrollInput();
            HandleBowInput();
        }

        private void HandleCraftingAndScrollInput()
        {
            var crafting = FieldCraftingManager.Instance;
            if (crafting == null) return;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                crafting.CycleSpecialArrow(scroll > 0f ? 1 : -1);
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                crafting.CraftMaxSelected();
            }

            if (Input.GetMouseButtonDown(2))
            {
                crafting.TryCraftSelectedSingle();
            }
        }

        private void HandleBowInput()
        {
            // 차징 시작 (좌클릭 = 기본 화살, 우클릭 = 특수 화살)
            if (!isCharging && cooldownTimer <= 0f)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    BeginCharge(isSpecial: false);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    if (CanStartSpecialCharge())
                    {
                        BeginCharge(isSpecial: true);
                    }
                }
            }

            if (!isCharging) return;

            int activeMouseButton = chargingSpecial ? 1 : 0;

            if (Input.GetMouseButton(activeMouseButton))
            {
                chargeTimer += Time.deltaTime;
                currentCharge = chargeTimer <= quickShotThreshold * 0.5f
                    ? 0f
                    : Mathf.Clamp01((chargeTimer - quickShotThreshold * 0.5f) / Mathf.Max(0.1f, maxChargeTime - quickShotThreshold * 0.5f));
                OnChargeUpdated?.Invoke(currentCharge);
            }

            if (Input.GetMouseButtonUp(activeMouseButton))
            {
                FireBow();
            }
        }

        private bool CanStartSpecialCharge()
        {
            var crafting = FieldCraftingManager.Instance;
            if (crafting == null) return false;

            ArrowType selected = crafting.SelectedSpecialArrow;
            if (crafting.GetArrowCount(selected) > 0) return true;

            if (FieldCraftingCore.RecipeTable.TryGetValue(selected, out ResourceType req))
            {
                return crafting.GetResourceCount(req) > 0;
            }
            return false;
        }

        private void BeginCharge(bool isSpecial)
        {
            isCharging = true;
            chargingSpecial = isSpecial;
            chargeTimer = 0f;
            currentCharge = 0f;
            playerController.SetSpeedMultiplier(chargingMoveSpeedMultiplier);
            OnChargeUpdated?.Invoke(0f);
        }

        private void FireBow()
        {
            bool wasSpecial = chargingSpecial;
            float releasedCharge = chargeTimer <= quickShotThreshold ? 0f : currentCharge;

            isCharging = false;
            chargingSpecial = false;
            playerController.SetSpeedMultiplier(1.0f);
            cooldownTimer = fireCooldown;

            ArrowType arrowTypeToFire = ArrowType.Standard;
            if (wasSpecial)
            {
                var crafting = FieldCraftingManager.Instance;
                if (crafting == null || !crafting.TryConsumeSelectedForFire(out arrowTypeToFire, out _))
                {
                    currentCharge = 0f;
                    OnChargeUpdated?.Invoke(0f);
                    return;
                }
            }

            Vector2 shootDirection = playerController.AimDirection;
            if (shootDirection.sqrMagnitude < 0.001f)
            {
                shootDirection = Vector2.right;
            }

            Vector3 spawnPosition = firePoint != null
                ? firePoint.position
                : transform.position + (Vector3)(shootDirection * 0.6f);
            spawnPosition.z = 0f;

            if (arrowPrefab != null)
            {
                float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                GameObject arrowObj = Instantiate(arrowPrefab, spawnPosition, Quaternion.Euler(0f, 0f, angle));
                if (arrowObj.TryGetComponent<ArrowProjectile>(out var arrow))
                {
                    arrow.Initialize(
                        shootDirection,
                        releasedCharge,
                        baseSpeed,
                        baseDamage,
                        basePierce: 1,
                        type: arrowTypeToFire,
                        minRange: minAimRange,
                        maxAimRange: maxAimRange);
                }
            }

            OnFired?.Invoke();
            currentCharge = 0f;
            OnChargeUpdated?.Invoke(0f);
        }

        public void CancelCharge()
        {
            if (isCharging)
            {
                isCharging = false;
                chargingSpecial = false;
                currentCharge = 0f;
                playerController.SetSpeedMultiplier(1.0f);
                OnChargeUpdated?.Invoke(0f);
            }
        }
    }
}
