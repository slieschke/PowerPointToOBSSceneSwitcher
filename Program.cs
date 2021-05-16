namespace SceneSwitcher {
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Office.Interop.PowerPoint;

    internal class Program {
        private static readonly Application PowerPoint = new Microsoft.Office.Interop.PowerPoint.Application();
        private static OBS obs;

        private static async Task Main() {
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

        private static void App_SlideShowNextSlide(SlideShowWindow window) {
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
                }
            }
        }
    }
}