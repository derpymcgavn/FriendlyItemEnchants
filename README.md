# Friendly Item Enchants

Standalone ACE Harmony mod that lets beneficial item enchantments, such as Impenetrability and Banes, be cast directly on another player. The mod applies the enchantment to the target player's worn armor/clothing and shield pieces. Harmful item enchantments keep normal ACE behavior.

## Build

```powershell
dotnet build Source\Mods\FriendlyItemEnchants\FriendlyItemEnchants.csproj -c Release
```

## Install

Copy these files to the server mod folder:

```text
Mods\FriendlyItemEnchants\FriendlyItemEnchants.dll
Mods\FriendlyItemEnchants\Meta.json
```

On first load the mod creates:

```text
Mods\FriendlyItemEnchants\FriendlyItemEnchants.json
```

## Config

```json
{
  "enabled": true,
  "affectArmorAndClothing": true,
  "affectShields": true,
  "allowItemDispelsOnPlayersEquippedItems": false,
  "itemDispelsAffectArmorAndClothing": true,
  "itemDispelsAffectShields": false,
  "preserveHarmfulRetailBehavior": true,
  "notifyOnFailure": true
}
```

Set `allowItemDispelsOnPlayersEquippedItems` to `true` to let positive Item Magic dispels, such as Evaporate Item Magic, target a player and remove matching enchantments from their equipped items instead of the player object. By default this affects armor/clothing only and leaves shield enchantments alone; set `itemDispelsAffectShields` to `true` if shield enchantments should also be dispellable through this redirect.

`preserveHarmfulRetailBehavior` is intentionally present as a guardrail. This mod does not widen harmful item spell targeting.
