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
    using Humanizer;
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
                // Went back a slide; figure out the previous video and audio that was used
                IDictionary<string, string> previousSlideCommands = GetSlideCommands(PowerPoint.ActivePresentation.Slides[previousSlideNumber]);

                string[] videoCommands = { "VIDEO-LONG-DELAY", "VIDEO-SHORT-DELAY", "VIDEO" };

                bool ContainsVideoCommand(IDictionary<string, string> commands) => commands.Keys.FirstOrDefault(videoCommands.Contains) != null;
                bool ContainsAudioCommand(IDictionary<string, string> commands) => commands.Keys.FirstOrDefault(key => key == "AUDIO") != null;

                // If the previous slide didn't change the video or audio the current audio or video is already correct.
                bool foundVideoCommand = !ContainsVideoCommand(previousSlideCommands);
                bool foundAudioCommand = !ContainsAudioCommand(previousSlideCommands);

                int i = currentSlideNumber;
                IDictionary<string, string> backCommands = new Dictionary<string, string>();
                while (!foundVideoCommand || !foundAudioCommand || i > 0) {
                    commands = GetSlideCommands(PowerPoint.ActivePresentation.Slides[i--]);

                    if (ContainsVideoCommand(commands)) {
                        foundVideoCommand = true;
                        string lastVideoCommand = videoCommands.First(commands.Keys.Contains);

                        // Use the last video command immediately.
                        backCommands["VIDEO"] = commands[lastVideoCommand];
                    }

                    if (ContainsAudioCommand(commands)) {
                        foundAudioCommand = true;
                        backCommands["AUDIO"] = commands["AUDIO"];
                    }
                }

                commands = backCommands;
            }

            foreach (var command in commands) {
                var argument = command.Value;
                switch (command.Key) {
                    case "AUDIO":
                        List<string> audioSources = ParseListArgument(argument);
                        Console.WriteLine($"  Switching audio to {audioSources.Humanize(source => $"\"{source}\"")}");
                        obs.SetAudioSources(audioSources);
                        break;
                    case "VIDEO":
                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\"");
                        obs.ChangeScene(argument, 0);
                        break;
                    case "VIDEO-SHORT-DELAY":
                        if (config.PtzPresets.ContainsKey(argument)) {
                            PTZ(argument);
                        }

                        Console.WriteLine($"  Switching to OBS scene named \"{argument}\" after short delay");
                        obs.ChangeScene(argument, config.ShortDelay);
                        break;
                    case "VIDEO-LONG-DELAY":
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
                    commands[parts[0].ToUpper().Trim()] = parts[1].Trim();
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

            activeTallyLight?.TurnOff();
            liveTallyLight?.TurnOn();

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