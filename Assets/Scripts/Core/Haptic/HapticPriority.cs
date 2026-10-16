namespace Template.Core.Haptic
{
    /// <summary>Common priority values on RichHaptic's 0-256 scale, where 0 is the most important.</summary>
    public static class HapticPriority
    {
        public const int Critical = 0;
        public const int High = 64;
        public const int Default = RichHaptic.Haptics.DefaultPriority;
        public const int Low = 200;
    }
}
