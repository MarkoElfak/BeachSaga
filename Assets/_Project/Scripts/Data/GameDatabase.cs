using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Root asset that references every definition and config. GameBootstrap holds one.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        [Header("Configs")]
        public EconomyConfig economy;
        public CameraConfig cameraConfig;
        public InputConfig input;
        public WorldLayoutConfig world;
        public PresentationConfig presentation;
        public PaletteDefinition palette;
        public VisitorConfig visitors;
        public TerrainConfig terrain;
        public NeighbourConfig neighbours;
        public MonetizationConfig monetization;
        public TutorialConfig tutorial;
        public SoundLibrary sounds;
        public PlacementConfig placement;
        public DailyRewardConfig dailyRewards;
        public OnlineConfig online;
        public DayNightConfig dayNight;
        public BeachEventConfig beachEvents;

        [Header("Content")]
        [Tooltip("Ordered: element 0 is tier 1 (Tent).")]
        public TierDefinition[] tiers;
        public ProductDefinition[] products;
        public VisitorTypeDefinition[] visitorTypes;
        public QuestDefinition[] quests;
        public DecorationDefinition[] decorations;
        public AchievementDefinition[] achievements;
        public MiniGameDefinition[] miniGames;

        public int MaxTier => tiers != null ? tiers.Length : 0;

        /// <summary>Tier numbers are 1-based to match ProductDefinition.requiredTier.</summary>
        public TierDefinition GetTier(int tierNumber)
        {
            if (tiers == null || tierNumber < 1 || tierNumber > tiers.Length)
            {
                return null;
            }

            return tiers[tierNumber - 1];
        }

        public QuestDefinition FindQuest(string questId)
        {
            if (quests != null)
            {
                foreach (QuestDefinition quest in quests)
                {
                    if (quest != null && quest.id == questId) return quest;
                }
            }

            return null;
        }

        public DecorationDefinition FindDecoration(string decorationId)
        {
            if (decorations != null)
            {
                foreach (DecorationDefinition decoration in decorations)
                {
                    if (decoration != null && decoration.id == decorationId) return decoration;
                }
            }

            return null;
        }

        public ProductDefinition FindProduct(string productId)
        {
            if (products == null)
            {
                return null;
            }

            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] != null && products[i].id == productId)
                {
                    return products[i];
                }
            }

            return null;
        }
    }
}
