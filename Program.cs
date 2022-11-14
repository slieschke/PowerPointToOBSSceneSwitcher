namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Office.Interop.PowerPoint;

    internal class Program {
        private static readonly Application PowerPoint = new Application();
        private static readonly HttpClient HttpClient = new HttpClient();

        private static Config config;
        private static OBS obs;
        private static bool skipPtzRequests;
        private static TallyLight activeTallyLight;

        private static async Task Main(string[] args) {
            var argList = new List<string>(args);

            // For testing at home
            skipPtzRequests = argList.Contains("skipPtzRequests") || argList.Contains("skipAllRequests");
            TallyLight.SetSkipRequests(argList.Contains("skipTallyLightRequests") || argList.Contains("skipAllRequests"));

            Console.WriteLine("Reading configuration...");
            config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"), new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
            });

            Console.WriteLine("Connecting to PowerPoint...");
            PowerPoint.SlideShowNextSlide += NextSlide;

            Console.WriteLine("Connecting to OBS...");
            obs = new OBS();
            await obs.Connect();
            obs.SceneChanged += NextScene;
            var currentScene = obs.GetCurrentScene();

            Console.WriteLine($"Current OBS scene is \"{currentScene}\"");
            config.TallyLights.ForEach(tallyLight => tallyLight.TurnOff());

            Console.ReadLine();
        }

        private static void NextSlide(SlideShowWindow window) {
            if (window == null) {
                return;
            }

            Console.WriteLine($"Moved to slide {window.View.Slide.SlideNumber}");

            // Text starts at index 2 ¯\_(ツ)_/¯
            string note;
            try {
                note = window.View.Slide.NotesPage.Shapes[2].TextFrame.TextRange.Text;
            } catch {
                // Slide has no notes
                return;
            }

            string line;
            IDictionary<string, string> commands = new Dictionary<string, string>();
            var noteReader = new StringReader(note);
            while ((line = noteReader.ReadLine()) != null) {
                var parts = line.Split(':', 2);
                commands.Add(parts[0], parts[1]);
            }

            foreach (var command in commands) {
                var argument = command.Value.Trim();
                switch (command.Key) {
                    case "OBS":
                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\"");
                        obs.ChangeScene(argument, 0);
                        break;
                    case "OBS-DELAY":
                        if (config.PtzPresets.ContainsKey(argument)) {
                            PTZ(argument);
                        }

                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\" after delay");
                        obs.ChangeScene(argument, config.ObsDelayPeriod);
                        break;
                    case "PTZ":
                        PTZ(argument);
                        break;
                }
            }
        }

        private static void NextScene(object sender, string scene) {
            Console.WriteLine($"  OBS scene changed to \"{scene}\"");
            SetTallyLightScene(scene);
        }

        private static void SetTallyLightScene(string scene) {
            var sceneSources = obs.GetSceneSources(scene);
            var liveTallyLight = config.TallyLights.FirstOrDefault(tallyLight => sceneSources.Contains(tallyLight.ObsSource));
            if (activeTallyLight == liveTallyLight) {
                return;
            }

            if (activeTallyLight != null) {
                activeTallyLight.TurnOff();
            }

            if (liveTallyLight != null) {
                liveTallyLight.TurnOn();
            }

            activeTallyLight = liveTallyLight;
        }

        private static async void PTZ(string line) {
            Console.Write($"  Switching to PTZ camera preset named \"{line}\"");
            if (!config.PtzPresets.ContainsKey(line)) {
                Console.WriteLine();
                Console.WriteLine($"  PTZ preset named \"{line}\" does not exist");
                return;
            }

            var httpCgiUrl = config.PtzPresets[line];
            Console.WriteLine($" - {httpCgiUrl}");

            if (skipPtzRequests) {
                return;
            }

            try {
                var responseBody = await HttpClient.GetStringAsync(httpCgiUrl);
                Console.WriteLine(responseBody);
            } catch (HttpRequestException ex) {
                Console.WriteLine($"  ERROR: {ex.Message}");
            }
        }
    }
}