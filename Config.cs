namespace SceneSwitcher {
    using System.Collections.Generic;

    public class Config {
        public int LongDelay { get; set; } = 5000;

        public int ShortDelay { get; set; } = 2000;

        public Dictionary<string, string> PtzPresets { get; set; }

        public List<TallyLight> TallyLights { get; set; }

        public List<string> VariableAudioSources { get; set; }
    }
}