using UnityEngine;

namespace ArcherCraftor.Combat
{
    /// <summary>
    /// 활 조준 각도, 사거리, 탄속, 대미지, 관통력 공식 (순수 C# 도메인 유틸리티)
    /// </summary>
    public static class AimChargeMath
    {
        public const float MaxSpreadAngleDegrees = 120f;
        public const float MinSpreadAngleDegrees = 0f;
        public const float PierceBonusThreshold = 0.85f;

        public static float CalculateSpreadAngle(float chargeRatio)
        {
            return Mathf.Lerp(MaxSpreadAngleDegrees, MinSpreadAngleDegrees, Mathf.Clamp01(chargeRatio));
        }

        public static float CalculateAimRange(float chargeRatio, float minRange = 6f, float maxRange = 22f)
        {
            return Mathf.Lerp(minRange, maxRange, Mathf.Clamp01(chargeRatio));
        }

        public static (float speed, float damage, int pierce) CalculateArrowSpecs(
            float chargeRatio,
            float baseSpeed = 25f,
            float baseDamage = 30f,
            int basePierce = 1)
        {
            float c = Mathf.Clamp01(chargeRatio);
            float speed = Mathf.Lerp(baseSpeed * 0.7f, baseSpeed * 1.5f, c);
            float damage = Mathf.Lerp(baseDamage * 0.5f, baseDamage * 2.2f, c);
            int pierce = basePierce + (c >= PierceBonusThreshold ? 2 : 0);
            return (speed, damage, pierce);
        }
    }

    /// <summary>
    /// 특수 화살(충격/서리 등)의 군중 제어 효과를 받는 대상 인터페이스
    /// </summary>
    public interface ICrowdControlTarget
    {
        void ApplyKnockback(Vector2 impulse);
        void ApplyStun(float duration);
        void ApplySlow(float speedMultiplier, float duration);
        void ApplyFreeze(float duration);
    }
}
