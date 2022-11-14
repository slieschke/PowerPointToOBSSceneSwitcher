namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Flurl.Http;
    using Microsoft.Office.Interop.PowerPoint;

    internal class Program {
        private static readonly Application PowerPoint = new Application();

        private static bool skipPtzRequests;
        private static int currentSlideNumber;

        private static Config config;
        private static OBS obs;
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
            obs = new OBS(config.VariableAudioSources);
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

            int previousSlideNumber = currentSlideNumber;
            currentSlideNumber = window.View.Slide.SlideNumber;
            Console.WriteLine($"Moved to slide {currentSlideNumber}");

            IDictionary<string, string> commands = GetSlideCommands(window.View.Slide);
            if (currentSlideNumber < previousSlideNumber) {
                // Went back a slide
                int i = currentSlideNumber;

                // Find the previous slide that had commands
                while (!commands.Any()) {
                    commands = GetSlideCommands(PowerPoint.ActivePresentation.Slides[i]);
                    i--;
                }

                // If there is any OBS delay command, switch to that scene immediately, ignoring any OBS command
                string[] delayCommands = { "OBS-DELAY", "OBS-LONG-DELAY", "OBS-SHORT-DELAY" };
                if (commands.Keys.Intersect(delayCommands).Count() > 0) {
                    commands["OBS"] = commands["OBS-DELAY"] ?? commands["OBS-LONG-DELAY"] ?? commands["OBS-SHORT-DELAY"];
                    commands.Remove("OBS-DELAY");
                    commands.Remove("OBS-LONG-DELAY");
                    commands.Remove("OBS-SHORT-DELAY");
                }
            }

            foreach (var command in commands) {
                var argument = command.Value.Trim();
                switch (command.Key) {
                    case "AUDIO":
                        List<string> audioSources = ParseListArgument(argument);
                        Console.WriteLine($"  Setting audio sources to \"{string.Join(", ", audioSources)}\"");
                        obs.SetAudioSources(audioSources);
                        break;
                    case "OBS":
                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\"");
                        obs.ChangeScene(argument, 0);
                        break;
                    case "OBS-DELAY":
                    case "OBS-SHORT-DELAY":
                        if (config.PtzPresets.ContainsKey(argument)) {
                            PTZ(argument);
                        }

                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\" after short delay");
                        obs.ChangeScene(argument, config.ShortDelay);
                        break;
                    case "OBS-LONG-DELAY":
                        if (config.PtzPresets.ContainsKey(argument)) {
                            PTZ(argument);
                        }

                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\" after long delay");
                        obs.ChangeScene(argument, config.LongDelay);
                        break;
                    case "PTZ":
                        PTZ(argument);
                        break;
                    default:
                        Console.WriteLine($"  Skipping invalid command \"{command.Key}:{command.Value}\"");
                        break;
                }
            }
        }

        private static List<string> ParseListArgument(string argument) {
            return argument.Split(",").Select(s => s.Trim()).ToList();
        }

        private static string NormalizeWhitespace(string argument) {
            // Remove any zero width spaces, and normalize any remaining consecutive whitespace to a single space.
            var zeroWidthSpace = "\u200B";
            var consecutiveWhitespacePattern = @"\s+";
            return Regex.Replace(argument.Replace(zeroWidthSpace, string.Empty), consecutiveWhitespacePattern, " ");
        }

        private static IDictionary<string, string> GetSlideCommands(Slide slide) {
            // Text starts at index 2 ¯\_(ツ)_/¯
            string note;
            try {
                note = slide.NotesPage.Shapes[2].TextFrame.TextRange.Text;
            } catch {
                // Slide has no notes
                return ImmutableDictionary<string, string>.Empty;
            }

            string line;
            IDictionary<string, string> commands = new Dictionary<string, string>();
            var noteReader = new StringReader(note);
            while ((line = noteReader.ReadLine()) != null) {
                var parts = NormalizeWhitespace(line).Split(':', 2);
                if (parts.Length == 2) {
                    commands[parts[0]] = parts[1];
                } else {
                    Console.WriteLine($"  Skipping invalid command \"{line}\"");
                }
            }

            return commands;
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
                var responseBody = await httpCgiUrl.GetAsync();
                Console.WriteLine(responseBody);
            } catch (FlurlHttpException ex) {
                Console.WriteLine($"  ERROR: {ex.Message}");
            }
        }
    }
}