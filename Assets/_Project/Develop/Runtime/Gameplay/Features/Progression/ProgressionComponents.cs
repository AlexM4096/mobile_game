using Arch.Core;

namespace _Project.Gameplay.Features.Progression
{
    public enum UpgradeId
    {
        Spear = 0,
        AdditionalBlade = 1,
        WhirlingBlades = 2,
        SplitThrow = 3,
        Quickdraw = 4,
        HeavySpear = 5,
        ReinforcedShaft = 6,
        LightningCharge = 7,
        Capacitor = 8
    }

    public struct CharacterProgress
    {
        public int Level;
        public int Experience;
        public int RequiredExperience;
        public int PendingSelections;
    }

    public struct UpgradeRanks
    {
        public int Spear;
        public int AdditionalBlade;
        public int WhirlingBlades;
        public int SplitThrow;
        public int Quickdraw;
        public int HeavySpear;
        public int ReinforcedShaft;
        public int LightningCharge;
        public int Capacitor;
    }

    public struct ExperienceReward
    {
        public Entity Recipient;
        public int Amount;
    }

    public struct ExperienceAwardedTag { }

    public struct GrantExperienceRequest
    {
        public Entity Target;
        public int Amount;
    }

    public struct ApplyUpgradeRequest
    {
        public Entity Target;
        public UpgradeId Upgrade;
    }
}
