namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using OBSWebsocketDotNet;

    public class OBS : IDisposable {
        private readonly List<string> variableAudioSources;

        private OBSWebsocket websocket;
        private List<string> scenes;
        private Dictionary<string, HashSet<string>> sceneSources;
        private bool disposedValue;

        public OBS(List<string> variableAudioSources) {
            this.variableAudioSources = variableAudioSources;
        }

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: false);
        }

        public event EventHandler<string> SceneChanged;

        public Task Connect() {
            this.websocket = new OBSWebsocket();
            this.websocket.Connect($"ws://127.0.0.1:4444", string.Empty);

            try {
                this.LoadScenes();
            } catch (System.InvalidOperationException) {
                Console.Error.WriteLine("\nFailed to connect to OBS Studio; check it is running and obs-websocket is correctly configured.\nPress any key to exit.");
                Console.ReadKey();
                Environment.Exit(1);
            }

            Console.WriteLine("\nValid scenes:");
            this.scenes.ForEach(scene => Console.WriteLine($"  {scene}"));
            Console.WriteLine();

            this.websocket.SceneChanged += (_, scene) => this.SceneChanged?.Invoke(this, scene);

            return Task.CompletedTask;
        }

        public string GetCurrentScene() {
            return this.websocket.GetCurrentScene().Name;
        }

        public bool HasScene(string scene) {
            return this.scenes.Contains(scene);
        }

        public void ChangeScene(string scene) {
            this.websocket.SetCurrentScene(scene);
        }

        public void SetAudioSources(List<string> sources) {
            this.variableAudioSources.ForEach(source => this.websocket.SetMute(source, !sources.Contains(source)));
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
            this.scenes = new List<string>(scenes.Select(s => s.Name).ToList());
            this.sceneSources = scenes.ToDictionary(s => s.Name, s => s.Items.Select(s => s.SourceName).ToHashSet());
        }
    }
}