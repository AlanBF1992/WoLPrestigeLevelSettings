using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace WoLPrestigeLevelSettings.Compatibility.MasteryExtended
{
    internal static class MELoader
    {
        internal static void Loader(IModHelper helper, Harmony _)
        {
            helper.Events.GameLoop.GameLaunched += fixOnGameLaunch;
        }

        [EventPriority(EventPriority.Low)]
        internal static void fixOnGameLaunch(object? sender, GameLaunchedEventArgs e)
        {
            adjustLevels(15, ModEntry.Config.FirstPrestigeLevel);
            adjustLevels(20, ModEntry.Config.SecondPrestigeLevel);
        }

        internal static void adjustLevels(int oldValue, int newValue)
        {
            IEnumerable<dynamic> skillList = (IEnumerable<dynamic>)AccessTools.PropertyGetter("MasteryExtended.Menu.Pages.MasterySkillsPage:skills").Invoke(null, null)!;

            foreach (var skill in skillList.Where(s => s.Id >= 0 && s.Id <= 4))
            {
                skill.ProfessionChooserLevels.Remove(oldValue);
                skill.ProfessionChooserLevels.Add(newValue);
            }
        }
    }
}
