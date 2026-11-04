namespace _Project.Gameplay.Features.LightningCharge
{
    public struct LightningChargeAbility
    {
        public double NextActivationTime;
        public double ChargeExpiresAt;
        public float CooldownDuration;
        public float ChargeDuration;
        public float TransferRadius;
        public int ChargesPerActivation;
        public int RemainingCharges;
        public int DamageHits;
        public int MaxTargetsPerChain;
    }
}
