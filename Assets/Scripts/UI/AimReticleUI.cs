using UnityEngine;
using UnityEngine.UI;
using ArcherCraftor.Combat;
using ArcherCraftor.Crafting;
using ArcherCraftor.Player;

namespace ArcherCraftor.UI
{
    /// <summary>
    /// 2D 조준선(크로스헤어), 원형 차징 게이지, 실시간 120°->0° 조준 부채꼴 가이드라인, 사거리 링 UI
    /// </summary>
    public class AimReticleUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBow playerBow;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private RectTransform crosshairRect;
        [SerializeField] private Image chargeFillImage;
        [SerializeField] private Image specialBadgeImage;
        [SerializeField] private LineRenderer rangeRingLine;
        [SerializeField] private LineRenderer aimConeLine;

        [Header("Settings")]
        [SerializeField] private float optimalRangeRadius = 8f;
        [SerializeField] private int ringSegments = 48;
        [SerializeField] private int coneArcSegments = 16;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color fullChargeColor = new Color(0.25f, 1f, 0.45f, 0.95f);

        private float currentChargeRatio;

        private void Start()
        {
            if (playerBow == null) playerBow = FindFirstObjectByType<PlayerBow>();
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();

            if (playerBow != null)
            {
                playerBow.OnChargeUpdated += UpdateChargeDisplay;
            }

            if (FieldCraftingManager.Instance != null)
            {
                FieldCraftingManager.Instance.OnSelectedArrowChanged += UpdateSpecialBadge;
                UpdateSpecialBadge(FieldCraftingManager.Instance.SelectedSpecialArrow);
            }

            SetupRangeRing();
            SetupAimCone();
        }

        private void OnDestroy()
        {
            if (playerBow != null)
            {
                playerBow.OnChargeUpdated -= UpdateChargeDisplay;
            }

            if (FieldCraftingManager.Instance != null)
            {
                FieldCraftingManager.Instance.OnSelectedArrowChanged -= UpdateSpecialBadge;
            }
        }

        private void Update()
        {
            if (crosshairRect != null)
            {
                crosshairRect.position = Input.mousePosition;
            }

            if (playerController != null)
            {
                Vector3 playerPos = playerController.transform.position;
                playerPos.z = 0f;

                if (rangeRingLine != null)
                {
                    rangeRingLine.transform.position = playerPos;
                }

                UpdateAimConeGeometry(playerPos);
            }
        }

        private void UpdateChargeDisplay(float chargeRatio)
        {
            currentChargeRatio = chargeRatio;
            if (chargeFillImage != null)
            {
                chargeFillImage.fillAmount = chargeRatio;
                chargeFillImage.color = chargeRatio >= 0.85f ? fullChargeColor : normalColor;
            }
        }

        private void UpdateSpecialBadge(ArrowType arrowType)
        {
            if (specialBadgeImage != null)
            {
                specialBadgeImage.color = ArrowVisualProfile.GetArrowColor(arrowType);
            }
        }

        private void SetupRangeRing()
        {
            if (rangeRingLine == null) return;

            rangeRingLine.positionCount = ringSegments + 1;
            rangeRingLine.useWorldSpace = false;
            rangeRingLine.loop = true;

            float angleStep = 360f / ringSegments;
            for (int i = 0; i <= ringSegments; i++)
            {
                float rad = Mathf.Deg2Rad * (angleStep * i);
                rangeRingLine.SetPosition(i, new Vector3(Mathf.Cos(rad) * optimalRangeRadius, Mathf.Sin(rad) * optimalRangeRadius, 0f));
            }
        }

        private void SetupAimCone()
        {
            if (aimConeLine == null) return;

            aimConeLine.useWorldSpace = true;
            aimConeLine.loop = false;
            aimConeLine.positionCount = coneArcSegments + 3;
        }

        private void UpdateAimConeGeometry(Vector3 origin)
        {
            if (aimConeLine == null || playerBow == null) return;

            if (!playerBow.IsCharging)
            {
                if (aimConeLine.enabled) aimConeLine.enabled = false;
                return;
            }

            if (!aimConeLine.enabled) aimConeLine.enabled = true;

            float spreadAngle = AimChargeMath.CalculateSpreadAngle(currentChargeRatio);
            float aimRange = AimChargeMath.CalculateAimRange(currentChargeRatio, playerBow.MinAimRange, playerBow.MaxAimRange);
            float centerAngle = playerController.AimAngleDegrees;

            aimConeLine.SetPosition(0, origin);

            if (spreadAngle <= 1.5f)
            {
                float rad = centerAngle * Mathf.Deg2Rad;
                Vector3 tip = origin + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * aimRange;
                for (int i = 1; i < aimConeLine.positionCount; i++)
                {
                    aimConeLine.SetPosition(i, tip);
                }
                aimConeLine.startColor = fullChargeColor;
                aimConeLine.endColor = fullChargeColor;
                return;
            }

            float halfSpread = spreadAngle * 0.5f;
            float startDeg = centerAngle - halfSpread;
            float stepDeg = spreadAngle / coneArcSegments;

            for (int i = 0; i <= coneArcSegments; i++)
            {
                float rad = (startDeg + stepDeg * i) * Mathf.Deg2Rad;
                Vector3 pt = origin + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * aimRange;
                aimConeLine.SetPosition(i + 1, pt);
            }

            aimConeLine.SetPosition(coneArcSegments + 2, origin);
            Color coneColor = playerBow.IsChargingSpecial
                ? ArrowVisualProfile.GetArrowColor(FieldCraftingManager.Instance != null ? FieldCraftingManager.Instance.SelectedSpecialArrow : ArrowType.Impact)
                : new Color(0.85f, 0.98f, 1f, 0.45f);
            aimConeLine.startColor = coneColor;
            aimConeLine.endColor = coneColor;
        }
    }
}
