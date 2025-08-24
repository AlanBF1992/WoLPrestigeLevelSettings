using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Reflection;
using System.Reflection.Emit;

namespace WoLPrestigeLevelSettings.Patches.WoL
{
    internal static class LevelUpMenuPatch
    {
        internal readonly static IMonitor LogMonitor = ModEntry.LogMonitor;

        internal static IEnumerable<CodeInstruction> ctorTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                FieldInfo informationUpInfo = AccessTools.Field(typeof(LevelUpMenu), nameof(LevelUpMenu.informationUp));
                MethodInfo profChooserLvlInfo = AccessTools.Method(typeof(LevelUpMenuPatch), nameof(profChooserLvl));

                //from: isProfessionChooser = level % 5 == 0 && skill != 5)
                //to:   isProfessionChooser = profChooserLvl(level) == 0 && skill != 5)
                matcher
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Stfld, informationUpInfo)
                    )
                    .ThrowIfNotMatch("LevelUpMenuPatch.ctorTranspiler: IL code 1 not found")
                    .Advance(3)
                    .RemoveInstructions(2)
                    .Insert(
                        new CodeInstruction(OpCodes.Call, profChooserLvlInfo)
                    )
                ;

                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                LogMonitor.Log($"Failed in {nameof(ctorTranspiler)}:\n{ex}", LogLevel.Error);
                return instructions;
            }
        }

        internal static IEnumerable<CodeInstruction> updateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                MethodInfo firstPrestigeLevelInfo = AccessTools.Method(typeof(LevelUpMenuUpdatePrefixPatch), nameof(LevelUpMenuUpdatePrefixPatch.firstPrestigeLevel));

                var currentLvlInfo = AccessTools.Field(typeof(LevelUpMenu), "currentLevel");
                var originalAddInfo = AccessTools.Method(typeof(LevelUpMenuPatch), nameof(originalAdd));

                // from: if (currentLevel is 5 or 15)
                // to:   if (currentLevel is 5 or firstPrestigeLevel)
                matcher
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldfld, currentLvlInfo),
                        new CodeMatch(OpCodes.Ldc_I4_S)
                    )
                    .ThrowIfNotMatch("LevelUpMenuPatch.updateTranspiler: IL code 1 not found")
                    .Advance(1)
                    .Set(OpCodes.Call, firstPrestigeLevelInfo)
                ;

                // from: just adding things
                // to:   if lvl is not ready for prestige, then add profession as normal, not as WoL
                for (int i = 2; i < 4; i++)
                {
                    matcher
                        .MatchStartForward(
                            new CodeMatch(OpCodes.Call),
                            new CodeMatch(OpCodes.Ldfld),
                            new CodeMatch(OpCodes.Ldarg_0)
                        )
                        .ThrowIfNotMatch($"LevelUpMenuPatch.updateTranspiler: IL code {i} not  found")
                    ;

                    matcher.CreateLabelWithOffsets(23, out Label skipAddingWoL);

                    List<CodeInstruction> checkInstructions = matcher.InstructionsWithOffsets(2, 5);

                    checkInstructions.AddRange([
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, currentLvlInfo),
                        new CodeInstruction(OpCodes.Call, originalAddInfo),
                        new CodeInstruction(OpCodes.Brtrue, skipAddingWoL)
                    ]);

                    matcher.InsertAndAdvance(checkInstructions);
                    matcher.Advance(1);
                }

                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                LogMonitor.Log($"Failed in {nameof(updateTranspiler)}:\n{ex}", LogLevel.Error);
                return instructions;
            }
        }

        internal static int profChooserLvl(int level)
        {
            if (level is 5 or 10
                || level == ModEntry.Config.FirstPrestigeLevel
                || level == ModEntry.Config.SecondPrestigeLevel)
            {
                return 0;
            }
            return 1;
        }

        internal static bool originalAdd(int profession, int level)
        {
            if (level is 5 or 10
                || level == ModEntry.Config.FirstPrestigeLevel
                || level == ModEntry.Config.SecondPrestigeLevel)
            {
                return false;
            }
            Game1.player.professions.Add(profession);
            return true;
        }
    }
}
