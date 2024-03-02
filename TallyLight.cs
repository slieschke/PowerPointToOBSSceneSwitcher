namespace SceneSwitcher {
    using System;

    using Flurl;
    using Flurl.Http;

    public class TallyLight {
        private static bool skipRequests;

        public string BaseUrl { get; set; }

        public float Brightness { get; set; }

        public string LiveColor { get; set; }

        public string ObsScene { get; set; }

        public static void SetSkipRequests(bool skip) {
            skipRequests = skip;
        }

        public void TurnOn() {
            Toggle("on", new {
                brightness = Brightness,
                color = LiveColor,
                state = "live",
            });
        }

        public void TurnOff() {
            Toggle("off", new { state = "off" });
        }

        private async void Toggle(string state, object queryParams) {
            var url = BaseUrl.SetQueryParams(queryParams);
            Console.WriteLine($"  Turning {state} tally light for \"{ObsScene}\" camera");

            if (skipRequests) {
                return;
            }

            try {
                await url.GetAsync();
            } catch (FlurlHttpException ex) {
                Console.Error.WriteLine($"  ERROR: {ex.Message}");
            }
        }
    }
}