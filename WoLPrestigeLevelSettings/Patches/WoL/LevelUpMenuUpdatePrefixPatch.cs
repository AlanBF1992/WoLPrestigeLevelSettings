using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;
using System.Reflection;
using System.Reflection.Emit;

namespace WoLPrestigeLevelSettings.Patches.WoL
{
    internal static class LevelUpMenuUpdatePrefixPatch
    {
        internal readonly static IMonitor LogMonitor = ModEntry.LogMonitor;

        internal static IEnumerable<CodeInstruction> LevelUpMenuUpdatePrefixTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                MethodInfo firstPrestigeLevelInfo = AccessTools.Method(typeof(LevelUpMenuUpdatePrefixPatch), nameof(firstPrestigeLevel));
                MethodInfo secondPrestigeLevelInfo = AccessTools.Method(typeof(LevelUpMenuUpdatePrefixPatch), nameof(secondPrestigeLevel));

                for (int i = 1; i <= 3; i++)
                {
                    // from: case 15:
                    // to:   case firstPrestigeLevel():
                    matcher
                        .MatchStartForward(
                            new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)15)
                        )
                        .ThrowIfNotMatch($"LevelUpMenuUpdatePrefixPatch.LevelUpMenuUpdatePrefixTranspiler: IL code {i}a not found")
                        .Set(OpCodes.Call, firstPrestigeLevelInfo)
                    ;

                    // from: case 20:
                    // to:   case secondPrestigeLevel():
                    matcher
                        .MatchStartForward(
                            new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)20)
                        )
                        .ThrowIfNotMatch($"LevelUpMenuUpdatePrefixPatch.LevelUpMenuUpdatePrefixTranspiler: IL code {i}b not found")
                        .Set(OpCodes.Call, secondPrestigeLevelInfo)
                    ;
                }

                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                LogMonitor.Log($"Failed in {nameof(LevelUpMenuUpdatePrefixTranspiler)}:\n{ex}", LogLevel.Error);
                return instructions;
            }
        }

        internal static int firstPrestigeLevel() => ModEntry.Config.FirstPrestigeLevel;
        internal static int secondPrestigeLevel() => ModEntry.Config.SecondPrestigeLevel;
    }
}
