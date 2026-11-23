using NUnit.Framework;
using UnityEngine;

namespace ArcherCraftor.Tests.EditMode
{
    [TestFixture]
    public class ArrowParryMathTests
    {
        // 2D XY 평면 기준 탄막 상쇄(Counter-Shot) 거리 판정 수식 검증
        private static bool IsInParryRange2D(Vector2 arrowPos, float arrowParryRadius, Vector2 bulletPos, float bulletRadius)
        {
            float maxDistance = arrowParryRadius + bulletRadius;
            float sqrDist = (arrowPos - bulletPos).sqrMagnitude;
            return sqrDist <= (maxDistance * maxDistance);
        }

        [Test]
        public void BulletInsideMagneticRadius_IsIntercepted()
        {
            Vector2 arrowPos = new Vector2(5f, 5f);
            float arrowParryRadius = 0.8f;

            Vector2 bulletPos = new Vector2(5.5f, 5.2f); // 거리 약 0.538m
            float bulletRadius = 0.3f;

            bool parried = IsInParryRange2D(arrowPos, arrowParryRadius, bulletPos, bulletRadius);
            Assert.IsTrue(parried, "자석 반경(0.8) + 탄환 반경(0.3) = 1.1m 내의 탄환은 상쇄되어야 함");
        }

        [Test]
        public void BulletOutsideMagneticRadius_IsNotIntercepted()
        {
            Vector2 arrowPos = new Vector2(5f, 5f);
            float arrowParryRadius = 0.8f;

            Vector2 bulletPos = new Vector2(7f, 5f); // 거리 2.0m
            float bulletRadius = 0.3f;

            bool parried = IsInParryRange2D(arrowPos, arrowParryRadius, bulletPos, bulletRadius);
            Assert.IsFalse(parried, "범위 밖의 탄환은 상쇄 판정을 받지 않아야 함");
        }

        [Test]
        public void DiagonalDistance2D_RespectsExactBoundary()
        {
            Vector2 arrowPos = Vector2.zero;
            float arrowParryRadius = 0.8f;
            float bulletRadius = 0.2f; // 합산 반경 = 1.0f

            Assert.IsTrue(IsInParryRange2D(arrowPos, arrowParryRadius, new Vector2(0.7f, 0.7f), bulletRadius)); // sqrt(0.98) < 1.0
            Assert.IsFalse(IsInParryRange2D(arrowPos, arrowParryRadius, new Vector2(0.8f, 0.8f), bulletRadius)); // sqrt(1.28) > 1.0
        }
    }
}
