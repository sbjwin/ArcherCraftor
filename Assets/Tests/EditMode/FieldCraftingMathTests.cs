using NUnit.Framework;
using ArcherCraftor.Crafting;

namespace ArcherCraftor.Tests.EditMode
{
    [TestFixture]
    public class FieldCraftingMathTests
    {
        [Test]
        public void StandardArrow_AlwaysAvailableWithoutResource()
        {
            var core = new FieldCraftingCore(10);

            Assert.IsTrue(core.TryCraft(ArrowType.Standard), "기본 화살은 재료 소모 없이 언제나 무한이어야 함");
            Assert.IsTrue(core.TryConsumeForFire(ArrowType.Standard, out bool autoCrafted));
            Assert.IsFalse(autoCrafted);
        }

        [Test]
        public void SpecialArrow_CraftsWhenMaterialPresent()
        {
            var core = new FieldCraftingCore(10);
            core.AddResource(ResourceType.FlameBerry, 3);

            bool crafted = core.TryCraft(ArrowType.Blast);

            Assert.IsTrue(crafted);
            Assert.AreEqual(2, core.GetResourceCount(ResourceType.FlameBerry), "재료가 1개 차감되어야 함");
            Assert.AreEqual(1, core.GetArrowCount(ArrowType.Blast), "화살통에 화염 화살 1발이 생성되어야 함");
        }

        [Test]
        public void SpecialArrow_FailsWhenMaterialMissing()
        {
            var core = new FieldCraftingCore(10);

            bool crafted = core.TryCraft(ArrowType.Frost);

            Assert.IsFalse(crafted, "재료가 없으면 제작 실패해야 함");
            Assert.AreEqual(0, core.GetArrowCount(ArrowType.Frost));
        }

        [Test]
        public void QuiverCapacity_CapsAtMaxSlots()
        {
            var core = new FieldCraftingCore(10);
            core.AddResource(ResourceType.FlintStone, 15);
            core.AddArrowDirect(ArrowType.Impact, 10);

            bool crafted = core.TryCraft(ArrowType.Impact);

            Assert.IsFalse(crafted, "최대 슬롯(10발)을 초과하여 제작할 수 없어야 함");
            Assert.AreEqual(10, core.GetArrowCount(ArrowType.Impact));
            Assert.AreEqual(15, core.GetResourceCount(ResourceType.FlintStone), "실패 시 재료가 차감되면 안 됨");
        }

        [Test]
        public void AutoCraftOnFire_ConsumesMaterialWhenQuiverEmpty()
        {
            var core = new FieldCraftingCore(10);
            core.AddResource(ResourceType.ToxicSac, 2);

            bool fired = core.TryConsumeForFire(ArrowType.Toxic, out bool autoCrafted);

            Assert.IsTrue(fired, "탄약이 0발이어도 재료가 있으면 자동 제작 후 발사되어야 함");
            Assert.IsTrue(autoCrafted);
            Assert.AreEqual(1, core.GetResourceCount(ResourceType.ToxicSac));
            Assert.AreEqual(0, core.GetArrowCount(ArrowType.Toxic));
        }
    }
}
