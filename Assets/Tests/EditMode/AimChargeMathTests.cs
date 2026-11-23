using NUnit.Framework;
using ArcherCraftor.Combat;

namespace ArcherCraftor.Tests.EditMode
{
    [TestFixture]
    public class AimChargeMathTests
    {
        [Test]
        public void TapShot_ReturnsMaxAngleAndMinRange()
        {
            float angle = AimChargeMath.CalculateSpreadAngle(0f);
            float range = AimChargeMath.CalculateAimRange(0f, 6f, 22f);

            Assert.AreEqual(120f, angle, 0.001f, "속사(탭) 시 각도는 최대 120도여야 함");
            Assert.AreEqual(6f, range, 0.001f, "속사(탭) 시 사거리는 최소치여야 함");
        }

        [Test]
        public void FullCharge_ReturnsZeroAngleAndMaxRange()
        {
            float angle = AimChargeMath.CalculateSpreadAngle(1.0f);
            float range = AimChargeMath.CalculateAimRange(1.0f, 6f, 22f);

            Assert.AreEqual(0f, angle, 0.001f, "풀차징 시 각도는 0도(완전 직선)여야 함");
            Assert.AreEqual(22f, range, 0.001f, "풀차징 시 사거리는 최대치여야 함");
        }

        [Test]
        public void HalfCharge_ReturnsMidpointAngleAndRange()
        {
            float angle = AimChargeMath.CalculateSpreadAngle(0.5f);
            float range = AimChargeMath.CalculateAimRange(0.5f, 6f, 22f);

            Assert.AreEqual(60f, angle, 0.001f, "50% 차징 시 각도는 60도여야 함");
            Assert.AreEqual(14f, range, 0.001f, "50% 차징 시 사거리는 14m여야 함");
        }

        [Test]
        public void ArrowSpecs_ScaleCorrectly_AtThresholds()
        {
            var tapSpecs = AimChargeMath.CalculateArrowSpecs(0f, 20f, 40f, 1);
            Assert.AreEqual(14f, tapSpecs.speed, 0.001f);
            Assert.AreEqual(20f, tapSpecs.damage, 0.001f);
            Assert.AreEqual(1, tapSpecs.pierce);

            var nearFullSpecs = AimChargeMath.CalculateArrowSpecs(0.84f, 20f, 40f, 1);
            Assert.AreEqual(1, nearFullSpecs.pierce, "0.85 미만에서는 관통력 보너스가 없어야 함");

            var thresholdSpecs = AimChargeMath.CalculateArrowSpecs(0.85f, 20f, 40f, 1);
            Assert.AreEqual(3, thresholdSpecs.pierce, "0.85 이상에서는 관통력 +2 보너스가 부여되어야 함");

            var fullSpecs = AimChargeMath.CalculateArrowSpecs(1.0f, 20f, 40f, 1);
            Assert.AreEqual(30f, fullSpecs.speed, 0.001f);
            Assert.AreEqual(88f, fullSpecs.damage, 0.001f);
            Assert.AreEqual(3, fullSpecs.pierce);
        }
    }
}
