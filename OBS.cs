namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using OBSWebsocketDotNet;

    public class OBS : IDisposable {
        private OBSWebsocket websocket;
        private List<string> validScenes;
        private Dictionary<string, HashSet<string>> sceneSources;
        private bool disposedValue;

        public OBS() {
        }

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: false);
        }

        public event EventHandler<string> SceneChanged;

        public Task Connect() {
            this.websocket = new OBSWebsocket();
            this.websocket.Connect($"ws://127.0.0.1:4444", string.Empty);
            this.LoadScenes();
            this.websocket.SceneChanged += (_, scene) => this.SceneChanged?.Invoke(this, scene);

            return Task.CompletedTask;
        }

        public string GetCurrentScene() {
            return this.websocket.GetCurrentScene().Name;
        }

        public void ChangeScene(string scene, int delay = 0) {
            if (!this.validScenes.Contains(scene)) {
                Console.WriteLine($"  OBS scene named \"{scene}\" does not exist");
                return;
            }

            if (delay == 0) {
                this.websocket.SetCurrentScene(scene);
            } else {
                Task.Delay(delay).ContinueWith(t => this.websocket.SetCurrentScene(scene));
            }
        }

        public ISet<string> GetSceneSources(string scene) {
            return this.sceneSources[scene];
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

                this.websocket.Disconnect();
                this.websocket = null;
                this.disposedValue = true;
            }
        }

        private void LoadScenes() {
            var scenes = this.websocket.GetSceneList().Scenes;
            this.validScenes = scenes.Select(s => s.Name).ToList();
            this.sceneSources = scenes.ToDictionary(s => s.Name, s => s.Items.Select(s => s.SourceName).ToHashSet());

            Console.WriteLine();
            Console.WriteLine("Valid scenes:");
            foreach (var scene in this.websocket.GetSceneList().Scenes) {
                Console.WriteLine($"  {scene.Name}");
            }

            Console.WriteLine();
        }
    }
}