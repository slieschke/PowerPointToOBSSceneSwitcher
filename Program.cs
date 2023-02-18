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
            obs = new OBS(config);
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
                // Went back a slide, or jumped to a slide; figure out the previous video and audio that was used
                IDictionary<string, string> previousSlideCommands = GetSlideCommands(PowerPoint.ActivePresentation.Slides[previousSlideNumber]);

                string[] videoCommands = { "VIDEO-LONG-DELAY", "VIDEO-SHORT-DELAY", "VIDEO" };

                bool ContainsVideoCommand(IDictionary<string, string> commands) => commands.Keys.FirstOrDefault(videoCommands.Contains) != null;
                bool ContainsAudioCommand(IDictionary<string, string> commands) => commands.Keys.FirstOrDefault(key => key == "AUDIO") != null;

                // If going back one slide, the video or audio will already be correct if the previous slide didn't change video or audio respectively.
                bool wentBackOneSlide = currentSlideNumber == previousSlideNumber - 1;
                bool foundVideoCommand = wentBackOneSlide && !ContainsVideoCommand(previousSlideCommands);
                bool foundAudioCommand = wentBackOneSlide && !ContainsAudioCommand(previousSlideCommands);

                int i = currentSlideNumber;
                IDictionary<string, string> backCommands = new Dictionary<string, string>();
                while (i > 0 && !(foundVideoCommand && foundAudioCommand)) {
                    commands = GetSlideCommands(PowerPoint.ActivePresentation.Slides[i--]);

                    if (!foundVideoCommand && ContainsVideoCommand(commands)) {
                        foundVideoCommand = true;
                        string lastVideoCommand = videoCommands.First(commands.Keys.Contains);

                        // Use the last video command immediately.
                        backCommands["VIDEO"] = commands[lastVideoCommand];
                    }

                    if (!foundAudioCommand && ContainsAudioCommand(commands)) {
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
                        ExecuteVideoCommand(command.Key, argument, commands, currentSlideNumber);
                        break;
                    case "VIDEO-SHORT-DELAY":
                        Task.Delay(config.ShortDelay).ContinueWith(t => {
                            Console.WriteLine($"  (short delay)");
                            ExecuteVideoCommand(command.Key, argument, commands, currentSlideNumber);
                        });
                        break;
                    case "VIDEO-LONG-DELAY":
                        Task.Delay(config.LongDelay).ContinueWith(t => {
                            Console.WriteLine($"  (long delay)");
                            ExecuteVideoCommand(command.Key, argument, commands, currentSlideNumber);
                        });
                        break;
                    default:
                        WriteError($"Skipping invalid command \"{command.Key}:{command.Value}\"");
                        break;
                }
            }
        }

        private static List<string> ParseListArgument(string argument) {
            return argument.Split(",").Select(s => s.Trim()).ToList();
        }

        private static void ExecuteVideoCommand(string command, string argument, IDictionary<string, string> currentSlideCommands, int currentSlideNumber) {
            Console.WriteLine($"  Switching video to \"{argument}\"");

            string nextVideoCommandArgument = GetNextVideoCommandArgument(command, currentSlideCommands, currentSlideNumber);

            string currentPtzCamera = GetPtzCamera(argument);
            string nextPtzCamera = nextVideoCommandArgument != null ? GetPtzCamera(nextVideoCommandArgument) : null;

            if (nextPtzCamera != null && nextPtzCamera != currentPtzCamera) {
                // The next scene is from a PTZ camera, which is not used by the current scene.
                // Prime the PTZ camera with the next scene it will display to avoid camera movement getting livestreamed.
                PTZ(nextPtzCamera, nextVideoCommandArgument);
            }

            string scene = currentPtzCamera ?? argument;
            if (currentPtzCamera != null) {
                PTZ(currentPtzCamera, argument);
            }

            if (obs.HasScene(scene)) {
                obs.ChangeScene(scene);
            } else {
                WriteError($"No video scene named \"{scene}\" exists");
            }
        }

        private static string GetNextVideoCommandArgument(string currentVideoCommand, IDictionary<string, string> currentSlideCommands, int currentSlideNumber) {
            string[] remainingVideoCommands;
            if (currentVideoCommand == "VIDEO") {
                remainingVideoCommands = new string[] { "VIDEO-SHORT-DELAY", "VIDEO-LONG-DELAY" };
            } else if (currentVideoCommand == "VIDEO-SHORT-DELAY") {
                remainingVideoCommands = new string[] { "VIDEO-LONG-DELAY" };
            } else if (currentVideoCommand == "VIDEO-LONG-DELAY") {
                remainingVideoCommands = Array.Empty<string>();
            } else {
                remainingVideoCommands = new string[] { "VIDEO", "VIDEO-SHORT-DELAY", "VIDEO-LONG-DELAY" };
            }

            foreach (var command in remainingVideoCommands) {
                if (currentVideoCommand != command && currentSlideCommands.TryGetValue(command, out string argument)) {
                    return argument;
                }
            }

            if (currentSlideNumber == PowerPoint.ActivePresentation.Slides.Count) {
                return null;
            }

            return GetNextVideoCommandArgument(null, GetSlideCommands(PowerPoint.ActivePresentation.Slides[currentSlideNumber + 1]), currentSlideNumber + 1);
        }

        private static string GetPtzCamera(string preset) {
            return config.PtzScenes.Keys.FirstOrDefault(scene => config.PtzScenes[scene].ContainsKey(preset));
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
                    WriteError($"Invalid command \"{line}\" on slide {slide.SlideNumber}");
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
            var liveTallyLight = config.TallyLights.FirstOrDefault(tallyLight => tallyLight.ObsScene == scene);
            if (activeTallyLight == liveTallyLight) {
                return;
            }

            activeTallyLight?.TurnOff();
            liveTallyLight?.TurnOn();

            activeTallyLight = liveTallyLight;
        }

        private static async void PTZ(string camera, string preset) {
            var httpCgiUrl = config.PtzScenes[camera][preset];

            Console.WriteLine($"  Setting \"{camera}\" camera to \"{preset}\" preset");

            if (skipPtzRequests) {
                return;
            }

            try {
                await httpCgiUrl.GetAsync();
            } catch (FlurlHttpException ex) {
                WriteError(ex.Message);
            }
        }

        private static void WriteError(string message) {
            Console.Error.WriteLine($"  ERROR: {message}");
        }
    }
}