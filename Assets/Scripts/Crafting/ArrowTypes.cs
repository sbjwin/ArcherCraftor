using UnityEngine;

namespace ArcherCraftor.Crafting
{
    /// <summary>
    /// 아처크래프터의 기본 및 특수 화살 탄종 정의
    /// </summary>
    public enum ArrowType
    {
        Standard, // 기본 화살 (무한 탄약)
        Impact,   // 충격 화살 (넉백 / 벽 충돌 기절)
        Blast,    // 화염 폭발 화살 (탄막 소거 폭발 / 화염 장판)
        Frost,    // 서리 속박 화살 (70% 둔화 / 광역 빙결)
        Spark,    // 번개 연쇄 화살 (연쇄 방전 / 낙뢰)
        Toxic     // 독성 안개 화살 (독 안개 / 방어력 약화)
    }

    /// <summary>
    /// 월드 채집 및 적 드롭으로 획득하는 즉석 조합 재료
    /// </summary>
    public enum ResourceType
    {
        FlintStone,    // 충격석 / 부싯돌 -> Impact
        FlameBerry,    // 화염 열매 -> Blast
        FrostMushroom, // 서리 버섯 -> Frost
        SparkCrystal,  // 정전기 결정 -> Spark
        ToxicSac       // 독낭 분말 -> Toxic
    }

    /// <summary>
    /// 화살 및 재료 타입별 표시 이름과 시그니처 컬러 매핑 테이블
    /// </summary>
    public static class ArrowVisualProfile
    {
        public static Color GetArrowColor(ArrowType type)
        {
            switch (type)
            {
                case ArrowType.Impact: return new Color(1.0f, 0.72f, 0.2f);
                case ArrowType.Blast:  return new Color(1.0f, 0.32f, 0.15f);
                case ArrowType.Frost:  return new Color(0.35f, 0.85f, 1.0f);
                case ArrowType.Spark:  return new Color(0.95f, 0.95f, 0.3f);
                case ArrowType.Toxic:  return new Color(0.45f, 0.95f, 0.35f);
                default:               return new Color(0.75f, 0.96f, 1.0f);
            }
        }

        public static Color GetResourceColor(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.FlintStone:    return new Color(0.85f, 0.65f, 0.35f);
                case ResourceType.FlameBerry:    return new Color(1.0f, 0.35f, 0.2f);
                case ResourceType.FrostMushroom: return new Color(0.4f, 0.85f, 1.0f);
                case ResourceType.SparkCrystal:  return new Color(1.0f, 0.95f, 0.35f);
                case ResourceType.ToxicSac:      return new Color(0.5f, 0.9f, 0.3f);
                default:                         return Color.white;
            }
        }

        public static string GetDisplayName(ArrowType type)
        {
            switch (type)
            {
                case ArrowType.Impact: return "충격 화살 (Impact)";
                case ArrowType.Blast:  return "화염 폭발 (Blast)";
                case ArrowType.Frost:  return "서리 속박 (Frost)";
                case ArrowType.Spark:  return "번개 연쇄 (Spark)";
                case ArrowType.Toxic:  return "독성 안개 (Toxic)";
                default:               return "기본 화살 (Standard)";
            }
        }

        public static string GetShortName(ArrowType type)
        {
            switch (type)
            {
                case ArrowType.Impact: return "충격";
                case ArrowType.Blast:  return "화염";
                case ArrowType.Frost:  return "서리";
                case ArrowType.Spark:  return "번개";
                case ArrowType.Toxic:  return "독성";
                default:               return "기본";
            }
        }

        public static string GetResourceShortName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.FlintStone:    return "충격석";
                case ResourceType.FlameBerry:    return "화염열매";
                case ResourceType.FrostMushroom: return "서리버섯";
                case ResourceType.SparkCrystal:  return "정전기";
                case ResourceType.ToxicSac:      return "독낭";
                default:                         return type.ToString();
            }
        }
    }
}
