namespace SceneSwitcher {
    using System;
    using Flurl;
    using Flurl.Http;

    public class TallyLight {
        private static bool skipRequests;

        public string BaseUrl { get; set; }

        public float Brightness { get; set; }

        public string LiveColor { get; set; }

        public string ObsSource { get; set; }

        public static void SetSkipRequests(bool skip) {
            skipRequests = skip;
        }

        public void TurnOn() {
            this.Toggle("on", new {
                brightness = this.Brightness,
                color = this.LiveColor,
                state = "live",
            });
        }

        public void TurnOff() {
            this.Toggle("off", new { state = "off" });
        }

        private async void Toggle(string state, object queryParams) {
            var url = this.BaseUrl.SetQueryParams(queryParams);
            Console.WriteLine($"  Turning {state} tally light for camera \"{this.ObsSource}\" - {url}");

            if (skipRequests) {
                return;
            }

            try {
                await url.GetAsync();
            } catch (FlurlHttpException ex) {
                Console.WriteLine($"  ERROR: {ex.Message}");
            }
        }
    }
}