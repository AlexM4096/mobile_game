namespace _Project.Gameplay.Features.Health
{
    public readonly struct EnemyHitResult
    {
        public readonly BrokenDefenseLayers BrokenLayers;
        public readonly bool IsDefeated;

        public EnemyHitResult(BrokenDefenseLayers brokenLayers, bool isDefeated)
        {
            BrokenLayers = brokenLayers;
            IsDefeated = isDefeated;
        }
    }

    public static class EnemyDefenseRules
    {
        public static int GetHelmetDurability(HelmetType helmetType, bool isEnchanted)
        {
            var baseDurability = helmetType switch
            {
                HelmetType.None => 0,
                HelmetType.Cloth => 1,
                HelmetType.Wood => 2,
                HelmetType.Iron => 3,
                HelmetType.DragonScale => 4,
                _ => 0
            };

            return baseDurability == 0
                ? 0
                : baseDurability + (isEnchanted ? 1 : 0);
        }

        public static HelmetVisualState GetHelmetVisualState(
            int currentDurability,
            int maxDurability)
        {
            if (currentDurability <= 0 || maxDurability <= 0)
            {
                return HelmetVisualState.None;
            }

            if (currentDurability >= maxDurability)
            {
                return HelmetVisualState.FullDurability;
            }

            return currentDurability == 1
                ? HelmetVisualState.OneHitRemaining
                : HelmetVisualState.Damaged;
        }

        public static EnemyHitResult ApplyHits(ref EnemyDefense defense, int hitCount)
        {
            if (hitCount <= 0)
            {
                return new EnemyHitResult(BrokenDefenseLayers.None, false);
            }

            var brokenLayers = BrokenDefenseLayers.None;
            var isDefeated = false;

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                if (defense.HelmetDurability > 0)
                {
                    defense.HelmetDurability--;
                    if (defense.HelmetDurability == 0)
                    {
                        defense.HelmetType = HelmetType.None;
                        brokenLayers |= BrokenDefenseLayers.Helmet;
                    }

                    continue;
                }

                if (defense.HasWeapon)
                {
                    defense.HasWeapon = false;
                    brokenLayers |= BrokenDefenseLayers.Weapon;
                    continue;
                }

                brokenLayers |= BrokenDefenseLayers.Body;
                isDefeated = true;
                break;
            }

            return new EnemyHitResult(brokenLayers, isDefeated);
        }
    }
}