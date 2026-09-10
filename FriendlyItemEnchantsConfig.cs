namespace FriendlyItemEnchants
{
    public sealed class FriendlyItemEnchantsConfig
    {
        public bool Enabled { get; set; } = true;
        public bool AffectArmorAndClothing { get; set; } = true;
        public bool AffectShields { get; set; } = true;
        public bool AllowItemDispelsOnPlayersEquippedItems { get; set; } = false;
        public bool ItemDispelsAffectArmorAndClothing { get; set; } = true;
        public bool ItemDispelsAffectShields { get; set; } = false;
        public bool PreserveHarmfulRetailBehavior { get; set; } = true;
        public bool NotifyOnFailure { get; set; } = true;
    }
}
