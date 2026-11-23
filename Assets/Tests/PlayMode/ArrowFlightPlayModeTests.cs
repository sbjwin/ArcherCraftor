using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ArcherCraftor.Combat;
using ArcherCraftor.Tests.Harness;

namespace ArcherCraftor.Tests.PlayMode
{
    public class ArrowFlightPlayModeTests
    {
        private GameObject arrowObject;
        private ArrowProjectile arrowProjectile;

        [TearDown]
        public void TearDown()
        {
            TestHarnessHelper.Cleanup();
        }

        [UnityTest]
        public IEnumerator ArrowMovesForward_Along2DDirection()
        {
            arrowObject = TestHarnessHelper.CreateTestObject("TestArrow2D");
            arrowObject.AddComponent<CircleCollider2D>();
            arrowProjectile = arrowObject.AddComponent<ArrowProjectile>();

            Vector3 startPos = Vector3.zero;
            Vector2 shootDirection = Vector2.up;
            float baseSpeed = 20f;

            arrowObject.transform.position = startPos;
            arrowProjectile.Initialize(shootDirection, 0.5f, baseSpeed, 30f);

            yield return new WaitForSeconds(0.2f);

            Assert.Greater(arrowObject.transform.position.y, startPos.y + 1.0f, "2D 화살이 발사 방향(+Y)으로 정상 이동해야 함");
            Assert.AreEqual(0f, arrowObject.transform.position.z, 0.01f, "2D XY 평면에서 Z축 좌표가 0으로 유지되어야 함");
        }
    }
}
