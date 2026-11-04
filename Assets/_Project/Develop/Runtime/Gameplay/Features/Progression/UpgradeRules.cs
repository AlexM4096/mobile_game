namespace _Project.Gameplay.Features.Progression
{
    internal static class UpgradeRules
    {
        public static int GetRank(in UpgradeRanks ranks, UpgradeId id) => id switch
        {
            UpgradeId.Spear => ranks.Spear,
            UpgradeId.AdditionalBlade => ranks.AdditionalBlade,
            UpgradeId.WhirlingBlades => ranks.WhirlingBlades,
            UpgradeId.SplitThrow => ranks.SplitThrow,
            UpgradeId.Quickdraw => ranks.Quickdraw,
            UpgradeId.HeavySpear => ranks.HeavySpear,
            UpgradeId.ReinforcedShaft => ranks.ReinforcedShaft,
            UpgradeId.LightningCharge => ranks.LightningCharge,
            UpgradeId.Capacitor => ranks.Capacitor,
            _ => 0
        };

        public static void SetRank(ref UpgradeRanks ranks, UpgradeId id, int value)
        {
            switch (id)
            {
                case UpgradeId.Spear:
                    ranks.Spear = value;
                    break;
                case UpgradeId.AdditionalBlade:
                    ranks.AdditionalBlade = value;
                    break;
                case UpgradeId.WhirlingBlades:
                    ranks.WhirlingBlades = value;
                    break;
                case UpgradeId.SplitThrow:
                    ranks.SplitThrow = value;
                    break;
                case UpgradeId.Quickdraw:
                    ranks.Quickdraw = value;
                    break;
                case UpgradeId.HeavySpear:
                    ranks.HeavySpear = value;
                    break;
                case UpgradeId.ReinforcedShaft:
                    ranks.ReinforcedShaft = value;
                    break;
                case UpgradeId.LightningCharge:
                    ranks.LightningCharge = value;
                    break;
                case UpgradeId.Capacitor:
                    ranks.Capacitor = value;
                    break;
            }
        }

        public static bool RequiresSpear(UpgradeId id) =>
            id == UpgradeId.SplitThrow ||
            id == UpgradeId.Quickdraw ||
            id == UpgradeId.HeavySpear ||
            id == UpgradeId.ReinforcedShaft;

        public static bool RequiresLightningCharge(UpgradeId id) =>
            id == UpgradeId.Capacitor;

        public static bool IsAbilityUnlock(UpgradeId id) =>
            id == UpgradeId.Spear || id == UpgradeId.LightningCharge;
    }
}
