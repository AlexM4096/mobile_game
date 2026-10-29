using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Gameplay.Features.Progression
{
    [CreateAssetMenu(
        fileName = "ProgressionConfig",
        menuName = "Configs/Gameplay/Progression Config")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int baseExperience = 5;
        [SerializeField, Min(1)] private int experienceIncreasePerLevel = 5;
        [SerializeField, Min(2)] private int maximumLevel = 23;
        [SerializeField, Range(1, 3)] private int offeredCardCount = 3;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private VisualTreeAsset visualTree;
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private List<UpgradeCardDefinition> cards = new();

        public int BaseExperience => baseExperience;
        public int ExperienceIncreasePerLevel => experienceIncreasePerLevel;
        public int MaximumLevel => maximumLevel;
        public int OfferedCardCount => offeredCardCount;
        public int RandomSeed => randomSeed;
        public VisualTreeAsset VisualTree => visualTree;
        public PanelSettings PanelSettings => panelSettings;
        public IReadOnlyList<UpgradeCardDefinition> Cards => cards;

        public UpgradeCardDefinition GetCard(UpgradeId id)
        {
            for (var index = 0; index < cards.Count; index++)
            {
                if (cards[index].Id == id)
                {
                    return cards[index];
                }
            }

            return null;
        }

        public bool TryValidate(out string error)
        {
            if (baseExperience < 1 || experienceIncreasePerLevel < 1)
            {
                error = "Progression experience thresholds must be positive.";
                return false;
            }

            if (maximumLevel != 23 || offeredCardCount != 3)
            {
                error = "The prototype progression requires maximum level 23 and three offered cards.";
                return false;
            }

            if (visualTree == null || panelSettings == null)
            {
                error = "Progression UI requires both a VisualTreeAsset and PanelSettings.";
                return false;
            }

            if (cards.Count != Enum.GetValues(typeof(UpgradeId)).Length)
            {
                error = "Progression requires exactly one card for every UpgradeId.";
                return false;
            }

            var seen = new HashSet<UpgradeId>();
            var totalRanks = 0;
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                if (card == null || !seen.Add(card.Id))
                {
                    error = "Progression cards must be non-null and have unique upgrade IDs.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(card.Title) ||
                    string.IsNullOrWhiteSpace(card.Description) ||
                    card.Icon == null ||
                    card.MaximumRank < 1)
                {
                    error = $"Progression card '{card.Id}' has incomplete presentation or rank data.";
                    return false;
                }

                if (card.Id != UpgradeId.Spear && card.Magnitude <= 0f)
                {
                    error = $"Progression card '{card.Id}' requires a positive effect magnitude.";
                    return false;
                }

                totalRanks += card.MaximumRank;
            }

            if (totalRanks != maximumLevel - 1)
            {
                error = "The configured upgrade ranks must equal maximum level minus one.";
                return false;
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            baseExperience = Mathf.Max(1, baseExperience);
            experienceIncreasePerLevel = Mathf.Max(1, experienceIncreasePerLevel);
            maximumLevel = Mathf.Max(2, maximumLevel);
            offeredCardCount = Mathf.Clamp(offeredCardCount, 1, 3);
        }
    }

    [Serializable]
    public sealed class UpgradeCardDefinition
    {
        [SerializeField] private UpgradeId id;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maximumRank = 1;
        [SerializeField, Min(0f)] private float magnitude;

        public UpgradeId Id => id;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaximumRank => maximumRank;
        public float Magnitude => magnitude;
    }
}
