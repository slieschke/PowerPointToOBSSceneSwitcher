namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::OBS.WebSocket.NET;

    public class OBS : IDisposable {
        private bool disposedValue;
        private ObsWebSocket webSocket;
        private List<string> validScenes;
        private string defaultScene;

        public OBS() {
        }

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: false);
        }

        public string DefaultScene {
            get => this.defaultScene;

            set {
                if (this.validScenes.Contains(value)) {
                    this.defaultScene = value;
                } else {
                    Console.WriteLine($"Scene named {value} does not exist and cannot be set as default");
                }
            }
        }

        public Task Connect() {
            this.webSocket = new ObsWebSocket();
            this.webSocket.Connect($"ws://127.0.0.1:4444", string.Empty);
            return Task.CompletedTask;
        }

        public bool ChangeScene(string scene) {
            if (!this.validScenes.Contains(scene)) {
                Console.WriteLine($"Scene named {scene} does not exist");
                if (string.IsNullOrEmpty(this.defaultScene)) {
                    Console.WriteLine("No default scene has been set!");
                    return false;
                }

                scene = this.defaultScene;
            }

            this.webSocket.Api.SetCurrentScene(scene);

            return true;
        }

        public void GetScenes() {
            var allScene = this.webSocket.Api.GetSceneList();
            var list = allScene.Scenes.Select(s => s.Name).ToList();
            Console.WriteLine("Valid Scenes:");
            foreach (var l in list) {
                Console.WriteLine(l);
            }

            this.validScenes = list;
        }

        public bool StartRecording() {
            try {
                this.webSocket.Api.StartRecording();
            } catch { /* Recording already started */
            }

            return true;
        }

        public bool StopRecording() {
            try {
                this.webSocket.Api.StopRecording();
            } catch { /* Recording already stopped */
            }

            return true;
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!this.disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                this.webSocket.Disconnect();
                this.webSocket = null;
                this.disposedValue = true;
            }
        }
    }
}