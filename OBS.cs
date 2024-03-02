namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using OBSWebsocketDotNet;

    public class OBS : IDisposable {
        private readonly Config config;

        private OBSWebsocket websocket;
        private List<string> scenes;
        private Dictionary<string, HashSet<string>> sceneSources;
        private bool disposedValue;

        public OBS(Config config) {
            this.config = config;
        }

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: false);
        }

        public event EventHandler<string> SceneChanged;

        public void Connect() {
            this.websocket = new OBSWebsocket();
            this.websocket.Connected += this.OnConnect;
            this.websocket.CurrentProgramSceneChanged += (_, args) => this.SceneChanged?.Invoke(this, args.SceneName);

            WebSocketConfig wsc = this.config.WebSocketConfig;
            string url = $"ws://{wsc.Host}:{wsc.Port}";
            try {
                this.websocket.ConnectAsync(url, wsc.Password);
            } catch (Exception) {
                this.ExitOnConnectError();
            }

            // No exception appears to be thrown on failed connections. Workaround by
            // timing out if not connected after some amount of time.
            Task.Run(async () => {
                await Task.Delay(10000);
                if (!this.websocket.IsConnected) {
                    this.ExitOnConnectError();
                }
            });
        }

        public bool HasScene(string scene) {
            return this.scenes.Contains(scene);
        }

        public void ChangeScene(string scene) {
            this.websocket.SetCurrentProgramScene(scene);
        }

        public void SetAudioSources(List<string> sources) {
            this.config.VariableAudioSources.ForEach(source => this.websocket.SetInputMute(source, !sources.Contains(source)));
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
                this.websocket.Disconnect();
                this.websocket = null;
                this.disposedValue = true;
            }
        }

        private void OnConnect(object sender, EventArgs args) {
            this.LoadScenes();

            Console.WriteLine("\nValid scenes:");
            this.scenes.ForEach(scene => Console.WriteLine($"  {scene}"));
            Console.WriteLine();
            Console.WriteLine($"Current OBS scene is \"{this.websocket.GetCurrentProgramScene()}\"");
        }

        private void ExitOnConnectError() {
            Console.Error.WriteLine($"\nFailed to connect to OBS Studio. Check it has been started, it can be reached via the network, and the WebSocketConfig has been correctly configured.\nPress any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }

        private void LoadScenes() {
            var scenes = this.websocket.GetSceneList().Scenes;
            this.scenes = new List<string>(scenes.Select(s => s.Name).ToList());
            this.sceneSources = scenes.ToDictionary(
                s => s.Name,
                s => this.websocket.GetSceneItemList(s.Name).Select(s => s.SourceName).ToHashSet());
        }
    }
}