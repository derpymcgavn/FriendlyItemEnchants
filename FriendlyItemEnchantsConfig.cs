namespace FriendlyItemEnchants
{
    public sealed class FriendlyItemEnchantsConfig
    {
        public bool Enabled { get; set; } = true;
        public bool AffectArmorAndClothing { get; set; } = true;
        public bool AffectShields { get; set; } = true;
        public bool PreserveHarmfulRetailBehavior { get; set; } = true;
        public bool NotifyOnFailure { get; set; } = true;
    }
}
