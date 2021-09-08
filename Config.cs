namespace SceneSwitcher {
    using System.Collections.Generic;

    public class Config {
        public int ObsDelayPeriod { get; set; } = 2500;

        public Dictionary<string, string> PtzPresets { get; set; }

        public List<TallyLight> TallyLights { get; set; }
    }
}