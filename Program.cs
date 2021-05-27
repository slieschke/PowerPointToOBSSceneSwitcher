﻿// Thanks to CSharpFritz and EngstromJimmy for their gists, snippets, and thoughts.

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
            Console.WriteLine("connected");

            Console.Write("Connecting to OBS...");
            obs = new OBS();
            await obs.Connect();
            Console.WriteLine("connected");

            obs.GetScenes();

            Console.ReadLine();
        }

        private static void App_SlideShowNextSlide(SlideShowWindow window) {
            if (window != null) {
                Console.WriteLine($"Moved to Slide Number {window.View.Slide.SlideNumber}");

                // Text starts at Index 2 ¯\_(ツ)_/¯
                var note = string.Empty;
                try {
                    note = window.View.Slide.NotesPage.Shapes[2].TextFrame.TextRange.Text;
                } catch { /*no notes*/
                }

                bool sceneHandled = false;

                var notereader = new StringReader(note);
                string line;
                while ((line = notereader.ReadLine()) != null) {
                    if (line.StartsWith("OBS:")) {
                        line = line.Substring(4).Trim();

                        if (!sceneHandled) {
                            Console.WriteLine($"  Switching to OBS Scene named \"{line}\"");
                            try {
                                sceneHandled = obs.ChangeScene(line);
                            } catch (Exception ex) {
                                Console.WriteLine($"  ERROR: {ex.Message}");
                            }
                        } else {
                            Console.WriteLine($"  WARNING: Multiple scene definitions found.  I used the first and have ignored \"{line}\"");
                        }
                    }

                    if (line.StartsWith("OBSDEF:")) {
                        obs.DefaultScene = line.Substring(7).Trim();
                        Console.WriteLine($"  Setting the default OBS Scene to \"{obs.DefaultScene}\"");
                    }

                    if (line.StartsWith("**START")) {
                        obs.StartRecording();
                    }

                    if (line.StartsWith("**STOP")) {
                        obs.StopRecording();
                    }

                    if (!sceneHandled) {
                        obs.ChangeScene(obs.DefaultScene);
                        Console.WriteLine($"  Switching to OBS Default Scene named \"{obs.DefaultScene}\"");
                    }
                }
            }
        }
    }
}