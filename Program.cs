namespace SceneSwitcher {
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Office.Interop.PowerPoint;

    internal class Program {
        private static readonly Application PowerPoint = new Microsoft.Office.Interop.PowerPoint.Application();
        private static readonly HttpClient HttpClient;
        private static readonly JsonElement Config;

        private static OBS obs;
        private static bool testMode;

        static Program() {
            Console.Write("Reading configuration...");
            Config = JsonDocument.Parse(File.ReadAllText("config.json")).RootElement;
            Console.WriteLine(" read");
        }

        private static async Task Main(string[] args) {
            testMode = args.Length != 0 && args[0] == "test";

            Console.Write("Connecting to PowerPoint...");
            PowerPoint.SlideShowNextSlide += App_SlideShowNextSlide;
            Console.WriteLine(" connected");

            Console.Write("Connecting to OBS...");
            obs = new OBS();
            await obs.Connect();
            Console.WriteLine(" connected");

            obs.GetScenes();

            Console.ReadLine();
        }

        private static async void App_SlideShowNextSlide(SlideShowWindow window) {
            if (window != null) {
                Console.WriteLine($"Moved to slide number {window.View.Slide.SlideNumber}");

                // Text starts at index 2 ¯\_(ツ)_/¯
                var note = string.Empty;
                try {
                    note = window.View.Slide.NotesPage.Shapes[2].TextFrame.TextRange.Text;
                } catch {
                    // Slide has no notes
                }

                bool sceneHandled = false;

                var noteReader = new StringReader(note);
                string line;
                while ((line = noteReader.ReadLine()) != null) {
                    if (line.StartsWith("OBS:")) {
                        line = line.Substring(4).Trim();

                        if (!sceneHandled) {
                            Console.WriteLine($"  Switching to OBS scene named \"{line}\"");
                            try {
                                sceneHandled = obs.ChangeScene(line);
                            } catch (Exception ex) {
                                Console.WriteLine($"  ERROR: {ex.Message}");
                            }
                        } else {
                            Console.WriteLine($"  WARNING: Multiple scene definitions found. I used the first and have ignored \"{line}\"");
                        }
                    }

                    if (line.StartsWith("PTZ:")) {
                        line = line.Substring(4).Trim();

                        await PTZ(line);
                    }
                }
            }
        }

        private static async Task PTZ(string line) {
            Console.WriteLine($"  Switching to PTZ camera scene named \"{line}\"");

            string httpCgiUrl = Config.GetProperty(line).GetString();

            if (testMode) {
                // For testing at home
                return;
            }

            Console.WriteLine(httpCgiUrl);

            try {
                string responseBody = await HttpClient.GetStringAsync(httpCgiUrl);
                Console.WriteLine(responseBody);
            } catch (HttpRequestException ex) {
                Console.WriteLine($"  ERROR: {ex.Message}");
            }
        }
    }
}