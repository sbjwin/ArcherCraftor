using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcherCraftor.Crafting
{
    /// <summary>
    /// Unity 엔진 의존성 없이 EditMode에서도 즉시 검증 가능한 순수 현장 즉석 제작 도메인 모델
    /// </summary>
    public class FieldCraftingCore
    {
        public static readonly ArrowType[] SpecialArrowOrder =
        {
            ArrowType.Impact,
            ArrowType.Blast,
            ArrowType.Frost,
            ArrowType.Spark,
            ArrowType.Toxic
        };

        public static readonly Dictionary<ArrowType, ResourceType> RecipeTable = new Dictionary<ArrowType, ResourceType>
        {
            { ArrowType.Impact, ResourceType.FlintStone },
            { ArrowType.Blast,  ResourceType.FlameBerry },
            { ArrowType.Frost,  ResourceType.FrostMushroom },
            { ArrowType.Spark,  ResourceType.SparkCrystal },
            { ArrowType.Toxic,  ResourceType.ToxicSac }
        };

        private readonly Dictionary<ResourceType, int> inventory = new Dictionary<ResourceType, int>();
        private readonly Dictionary<ArrowType, int> quiver = new Dictionary<ArrowType, int>();
        private readonly int maxPerSlot;

        public int MaxPerSlot => maxPerSlot;

        public FieldCraftingCore(int maxPerSlot = 10)
        {
            this.maxPerSlot = Mathf.Max(1, maxPerSlot);

            foreach (ResourceType res in Enum.GetValues(typeof(ResourceType)))
            {
                inventory[res] = 0;
            }

            foreach (ArrowType arrow in SpecialArrowOrder)
            {
                quiver[arrow] = 0;
            }
        }

        public int GetResourceCount(ResourceType resource)
        {
            return inventory.TryGetValue(resource, out int count) ? count : 0;
        }

        public int GetArrowCount(ArrowType arrowType)
        {
            if (arrowType == ArrowType.Standard) return int.MaxValue;
            return quiver.TryGetValue(arrowType, out int count) ? count : 0;
        }

        public void AddResource(ResourceType resource, int amount)
        {
            if (amount <= 0) return;
            inventory[resource] = GetResourceCount(resource) + amount;
        }

        public void AddArrowDirect(ArrowType arrowType, int amount)
        {
            if (arrowType == ArrowType.Standard || amount <= 0) return;
            quiver[arrowType] = Mathf.Min(maxPerSlot, GetArrowCount(arrowType) + amount);
        }

        public bool TryCraft(ArrowType arrowType)
        {
            if (arrowType == ArrowType.Standard) return true;
            if (!RecipeTable.TryGetValue(arrowType, out ResourceType required)) return false;

            int available = GetResourceCount(required);
            if (available < 1) return false;

            int currentArrows = GetArrowCount(arrowType);
            if (currentArrows >= maxPerSlot) return false;

            inventory[required] = available - 1;
            quiver[arrowType] = currentArrows + 1;
            return true;
        }

        public int CraftAsManyAsPossible(ArrowType arrowType)
        {
            int crafted = 0;
            while (TryCraft(arrowType))
            {
                crafted++;
            }
            return crafted;
        }

        /// <summary>
        /// 발사 시 특수 화살을 1발 소모합니다. 잔여 탄약이 0발일 때 재료가 있으면 즉시 1발 제작 후 발사(Auto-Craft on Fire)합니다.
        /// </summary>
        public bool TryConsumeForFire(ArrowType arrowType, out bool autoCrafted)
        {
            autoCrafted = false;
            if (arrowType == ArrowType.Standard) return true;

            int current = GetArrowCount(arrowType);
            if (current > 0)
            {
                quiver[arrowType] = current - 1;
                return true;
            }

            if (TryCraft(arrowType))
            {
                autoCrafted = true;
                quiver[arrowType] = Mathf.Max(0, GetArrowCount(arrowType) - 1);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 씬 내에서 플레이어의 재료 인벤토리, 퀴버 탄약, 휠 스크롤 탄종 선택 및 즉석 제작 이벤트를 관리하는 매니저
    /// </summary>
    public class FieldCraftingManager : MonoBehaviour
    {
        public static FieldCraftingManager Instance { get; private set; }

        [Header("Quiver & Starting Loadout")]
        [Tooltip("특수 화살 슬롯당 최대 소지 탄약 수")]
        [Range(1, 30)]
        [SerializeField] private int maxArrowsPerSlot = 10;
        [Tooltip("테스트 시작 시 기본 지급될 각 재료 수량")]
        [SerializeField] private int startingResourceEach = 2;
        [Tooltip("테스트 시작 시 각 특수 화살에 기본 장전될 수량")]
        [SerializeField] private int startingSpecialArrowsEach = 2;

        private FieldCraftingCore core;
        private int selectedIndex;

        public event Action<ArrowType> OnSelectedArrowChanged;
        public event Action OnInventoryOrQuiverUpdated;
        public event Action<ArrowType, int> OnArrowCrafted;

        public ArrowType SelectedSpecialArrow => FieldCraftingCore.SpecialArrowOrder[selectedIndex];
        public int MaxArrowsPerSlot => core.MaxPerSlot;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeCore();
        }

        private void InitializeCore()
        {
            core = new FieldCraftingCore(maxArrowsPerSlot);

            if (startingResourceEach > 0)
            {
                foreach (ResourceType res in Enum.GetValues(typeof(ResourceType)))
                {
                    core.AddResource(res, startingResourceEach);
                }
            }

            if (startingSpecialArrowsEach > 0)
            {
                foreach (ArrowType arrow in FieldCraftingCore.SpecialArrowOrder)
                {
                    core.AddArrowDirect(arrow, startingSpecialArrowsEach);
                }
            }
        }

        public int GetResourceCount(ResourceType resource) => core.GetResourceCount(resource);
        public int GetArrowCount(ArrowType arrowType) => core.GetArrowCount(arrowType);

        public void CycleSpecialArrow(int direction)
        {
            if (direction == 0) return;

            int len = FieldCraftingCore.SpecialArrowOrder.Length;
            selectedIndex = (selectedIndex + (direction > 0 ? 1 : -1) + len) % len;
            OnSelectedArrowChanged?.Invoke(SelectedSpecialArrow);
            OnInventoryOrQuiverUpdated?.Invoke();
        }

        public void SelectSpecialArrow(ArrowType arrowType)
        {
            for (int i = 0; i < FieldCraftingCore.SpecialArrowOrder.Length; i++)
            {
                if (FieldCraftingCore.SpecialArrowOrder[i] == arrowType)
                {
                    selectedIndex = i;
                    OnSelectedArrowChanged?.Invoke(SelectedSpecialArrow);
                    OnInventoryOrQuiverUpdated?.Invoke();
                    return;
                }
            }
        }

        public void AddResource(ResourceType resource, int amount = 1)
        {
            core.AddResource(resource, amount);
            OnInventoryOrQuiverUpdated?.Invoke();
        }

        public bool TryCraftSelectedSingle()
        {
            ArrowType target = SelectedSpecialArrow;
            if (core.TryCraft(target))
            {
                OnArrowCrafted?.Invoke(target, 1);
                OnInventoryOrQuiverUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public int CraftMaxSelected()
        {
            ArrowType target = SelectedSpecialArrow;
            int crafted = core.CraftAsManyAsPossible(target);
            if (crafted > 0)
            {
                OnArrowCrafted?.Invoke(target, crafted);
                OnInventoryOrQuiverUpdated?.Invoke();
            }
            return crafted;
        }

        public bool TryConsumeSelectedForFire(out ArrowType firedType, out bool autoCrafted)
        {
            firedType = SelectedSpecialArrow;
            bool consumed = core.TryConsumeForFire(firedType, out autoCrafted);
            if (consumed)
            {
                if (autoCrafted)
                {
                    OnArrowCrafted?.Invoke(firedType, 1);
                }
                OnInventoryOrQuiverUpdated?.Invoke();
            }
            return consumed;
        }
    }
}
