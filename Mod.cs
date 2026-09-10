using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Mods;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

using HarmonyLib;

using log4net;

namespace FriendlyItemEnchants
{
    public class Mod : IHarmonyMod
    {
        internal const string HarmonyId = "derpace.friendly_item_enchants";
        internal static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal static FriendlyItemEnchantsConfig Config { get; private set; } = new FriendlyItemEnchantsConfig();

        private Harmony _harmony;

        public void Initialize()
        {
            Config = LoadConfig();
            if (!Config.Enabled)
            {
                Log.Info("Friendly Item Enchants is disabled by config.");
                return;
            }

            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll();
            Log.Info("Friendly Item Enchants loaded.");
        }

        public void Dispose()
        {
            _harmony?.UnpatchAll(HarmonyId);
            _harmony = null;
            Log.Info("Friendly Item Enchants unloaded.");
        }

        private static FriendlyItemEnchantsConfig LoadConfig()
        {
            try
            {
                var folder = Path.Combine(ModManager.ModPath, "FriendlyItemEnchants");
                var path = Path.Combine(folder, "FriendlyItemEnchants.json");
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(folder);
                    var json = JsonSerializer.Serialize(new FriendlyItemEnchantsConfig(), ConfigManager.SerializerOptions);
                    File.WriteAllText(path, json);
                    return new FriendlyItemEnchantsConfig();
                }

                return JsonSerializer.Deserialize<FriendlyItemEnchantsConfig>(File.ReadAllText(path), ConfigManager.SerializerOptions)
                    ?? new FriendlyItemEnchantsConfig();
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to load FriendlyItemEnchants.json; using defaults. {ex}");
                return new FriendlyItemEnchantsConfig();
            }
        }
    }

    internal static class FriendlyItemEnchantHelpers
    {
        private static readonly MethodInfo HandleCastSpellMethod = AccessTools.Method(
            typeof(WorldObject),
            "HandleCastSpell",
            new[]
            {
                typeof(Spell),
                typeof(WorldObject),
                typeof(WorldObject),
                typeof(WorldObject),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(float),
                typeof(WorldObject)
            });

        private static readonly MethodInfo DoSpellEffectsMethod = AccessTools.Method(
            typeof(WorldObject),
            "DoSpellEffects",
            new[] { typeof(Spell), typeof(WorldObject), typeof(WorldObject) });

        public static bool IsFriendlyImpenBaneForOtherPlayer(WorldObject caster, Spell spell, WorldObject target, out Player targetPlayer)
        {
            targetPlayer = target as Player;
            return Mod.Config.Enabled
                && targetPlayer != null
                && targetPlayer != caster
                && spell != null
                && spell.School == MagicSchool.ItemEnchantment
                && spell.IsImpenBaneType
                && spell.IsBeneficial;
        }

        public static List<WorldObject> GetAffectedItems(Creature target)
        {
            return target.EquippedObjects.Values
                .Where(item => item.IsEnchantable && IsAllowedTargetItem(item))
                .ToList();
        }

        public static bool IsAllowedTargetItem(WorldObject item)
        {
            if (Mod.Config.AffectShields && item.IsShield)
                return true;

            return Mod.Config.AffectArmorAndClothing && item.WeenieType == WeenieType.Clothing;
        }

        public static void ApplySpellToItem(WorldObject caster, Spell spell, WorldObject item)
        {
            if (HandleCastSpellMethod == null)
                throw new MissingMethodException(typeof(WorldObject).FullName, "HandleCastSpell");

            HandleCastSpellMethod.Invoke(caster, new object[] { spell, item, null, null, false, false, false, 1.0f, null });
        }

        public static void PlayCreatureEffect(WorldObject caster, Spell spell, WorldObject target)
        {
            if (DoSpellEffectsMethod == null)
                return;

            DoSpellEffectsMethod.Invoke(caster, new object[] { spell, caster, target });
        }

        public static void SendFailure(WorldObject caster, Player targetPlayer, Spell spell)
        {
            if (!Mod.Config.NotifyOnFailure)
                return;

            if (caster is Player player)
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You fail to affect {targetPlayer.Name} with {spell.Name}", ChatMessageType.Magic));

            if (!targetPlayer.SquelchManager.Squelches.Contains(caster, ChatMessageType.Magic))
                targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"{caster.Name} fails to affect you with {spell.Name}", ChatMessageType.Magic));
        }
    }

    [HarmonyPatch(typeof(WorldObject), "TryCastSpell_WithRedirects")]
    internal static class TryCastSpellWithRedirectsPatch
    {
        private static bool Prefix(WorldObject __instance, Spell spell, WorldObject target, WorldObject itemCaster, WorldObject weapon, bool isWeaponSpell, bool fromProc, bool tryResist, ref bool __result)
        {
            if (!FriendlyItemEnchantHelpers.IsFriendlyImpenBaneForOtherPlayer(__instance, spell, target, out _))
                return true;

            __instance.TryCastSpell(spell, target, itemCaster, weapon, isWeaponSpell, fromProc, tryResist);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldObject), "TryCastItemEnchantment_WithRedirects")]
    internal static class TryCastItemEnchantmentWithRedirectsPatch
    {
        private static bool Prefix(WorldObject __instance, Spell spell, WorldObject target, WorldObject itemCaster)
        {
            if (!FriendlyItemEnchantHelpers.IsFriendlyImpenBaneForOtherPlayer(__instance, spell, target, out var targetPlayer))
                return true;

            var items = FriendlyItemEnchantHelpers.GetAffectedItems(targetPlayer);
            if (items.Count == 0)
            {
                FriendlyItemEnchantHelpers.SendFailure(__instance, targetPlayer, spell);
                return false;
            }

            foreach (var item in items)
                FriendlyItemEnchantHelpers.ApplySpellToItem(__instance, spell, item);

            FriendlyItemEnchantHelpers.PlayCreatureEffect(__instance, spell, targetPlayer);
            return false;
        }
    }
}
