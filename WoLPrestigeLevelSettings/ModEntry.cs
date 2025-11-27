using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using WoLPrestigeLevelSettings.Compatibility.GMCM;
using WoLPrestigeLevelSettings.Compatibility.MasteryExtended;
using WoLPrestigeLevelSettings.Patches.WoL;

namespace WoLPrestigeLevelSettings
{
    public class ModEntry : Mod
    {
        /// <summary>Monitoring and logging for the mod.</summary>
        public static IMonitor LogMonitor { get; internal set; } = null!;

        /// <summary>Simplified APIs for writing mods.</summary>
        public static IModHelper ModHelper { get; internal set; } = null!;

        /// <summary>Simplified APIs for writing mods.</summary>
        new public static IManifest ModManifest { get; internal set; } = null!;

        /// <summary>The mod configuration from the player.</summary>
        public static ModConfig Config { get; internal set; } = null!;

        public override void Entry(IModHelper helper)
        {
            LogMonitor = Monitor;
            ModHelper = Helper;
            ModManifest = base.ModManifest;
            Config = helper.ReadConfig<ModConfig>();

            Harmony harmony = new(ModManifest.UniqueID);
            helper.Events.GameLoop.GameLaunched += GMCMConfigVanilla;

            Patches(harmony);

            if (ModHelper.ModRegistry.IsLoaded("AlanBF.MasteryExtended"))
            {
                MELoader.Loader(helper, harmony);
            }
        }

        public static void Patches(Harmony harmony)
        {
            // Change when the level up menus is a Profession Chooser
            harmony.Patch(
                original: AccessTools.Constructor(typeof(LevelUpMenu), [typeof(int), typeof(int)]),
                transpiler: new HarmonyMethod(typeof(LevelUpMenuPatch), nameof(LevelUpMenuPatch.ctorTranspiler))
            );

            // Make WoL use those levels
            harmony.Patch(
                original: AccessTools.Method("DaLion.Professions.Framework.Patchers.Prestige.LevelUpMenuUpdatePatcher:LevelUpMenuUpdatePrefix"),
                transpiler: new HarmonyMethod(typeof(LevelUpMenuUpdatePrefixPatch), nameof(LevelUpMenuUpdatePrefixPatch.LevelUpMenuUpdatePrefixTranspiler))
            );

            // Change the check of when
            harmony.Patch(
                original: AccessTools.Method(typeof(LevelUpMenu), nameof(LevelUpMenu.update)),
                transpiler: new HarmonyMethod(typeof(LevelUpMenuPatch), nameof(LevelUpMenuPatch.updateTranspiler))
            );
        }

        private static void GMCMConfigVanilla(object? _1, GameLaunchedEventArgs _2)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = ModHelper.ModRegistry.GetApi<IGMCMApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => ModHelper.WriteConfig(Config)
            );

            /**************
             * Profession *
             **************/
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => ModHelper.Translation.Get("profession-config-title")
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.FirstPrestigeLevel,
                setValue: (value) =>
                {
                    if (ModHelper.ModRegistry.IsLoaded("AlanBF.MasteryExtended"))
                    {
                        MELoader.adjustLevels(Config.FirstPrestigeLevel, value);
                    }
                    Config.FirstPrestigeLevel = value;
                },
                name: () => ModHelper.Translation.Get("profession-unlock-prestige-1"),
                tooltip: () => "Default: 15",
                min: 11,
                max: 19,
                interval: 1
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.SecondPrestigeLevel,
                setValue: (value) =>
                {
                    if (value <= Config.FirstPrestigeLevel)
                    {
                        if(ModHelper.ModRegistry.IsLoaded("AlanBF.MasteryExtended"))
                        {
                            MELoader.adjustLevels(Config.FirstPrestigeLevel, 15);
                            MELoader.adjustLevels(Config.SecondPrestigeLevel, 20);
                        }

                        Config.FirstPrestigeLevel = 15;
                        Config.SecondPrestigeLevel = 20;
                        LogMonitor.Log(ModHelper.Translation.Get("profession-unlock-prestige-reset"), LogLevel.Warn);

                    }
                    else
                    {
                        if (ModHelper.ModRegistry.IsLoaded("AlanBF.MasteryExtended"))
                        {
                            MELoader.adjustLevels(Config.SecondPrestigeLevel, value);
                        }
                        Config.SecondPrestigeLevel = value;
                    }
                },
                name: () => ModHelper.Translation.Get("profession-unlock-prestige-2"),
                tooltip: () => "Default: 20",
                min: 12,
                max: 20,
                interval: 1
            );

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => ModHelper.Translation.Get("profession-unlock-prestige-warning")
            );
        }
    }
}
