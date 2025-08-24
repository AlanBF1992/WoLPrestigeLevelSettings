namespace WoLPrestigeLevelSettings
{
    /// <summary>The mod configuration class from the player.</summary>
    public sealed class ModConfig
    {
        // Level at which the first prestige is unlocked.
        public int FirstPrestigeLevel { get; set; } = 15;
        // Level at which the second prestige is unlocked.
        public int SecondPrestigeLevel { get; set; } = 20;
    }
}
