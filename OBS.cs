namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using OBSWebsocketDotNet;

    public class OBS(Config config) : IDisposable {
        private readonly Config config = config;

        private OBSWebsocket websocket;
        private List<string> scenes;
        private Dictionary<string, HashSet<string>> sceneSources;
        private bool disposedValue;

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public event EventHandler<string> SceneChanged;

        public void Connect() {
            websocket = new OBSWebsocket();
            websocket.Connected += OnConnect;
            websocket.CurrentProgramSceneChanged += (_, args) => SceneChanged?.Invoke(this, args.SceneName);

            WebSocketConfig wsc = config.WebSocketConfig;
            string url = $"ws://{wsc.Host}:{wsc.Port}";
            try {
                websocket.ConnectAsync(url, wsc.Password);
            } catch (Exception) {
                ExitOnConnectError();
            }

            // No exception appears to be thrown on failed connections. Workaround by
            // timing out if not connected after some amount of time.
            Task.Run(async () => {
                await Task.Delay(10000);
                if (!websocket.IsConnected) {
                    ExitOnConnectError();
                }
            });
        }

        public bool HasScene(string scene) {
            return scenes.Contains(scene);
        }

        public void ChangeScene(string scene) {
            websocket.SetCurrentProgramScene(scene);
        }

        public void SetAudioSources(List<string> sources) {
            config.VariableAudioSources.ForEach(source => websocket.SetInputMute(source, !sources.Contains(source)));
        }

        public ISet<string> GetSceneSources(string scene) {
            return sceneSources[scene];
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                websocket.Disconnect();
                websocket = null;
                disposedValue = true;
            }
        }

        private static void ExitOnConnectError() {
            Console.Error.WriteLine($"\nFailed to connect to OBS Studio. Check it has been started, it can be reached via the network, and the WebSocketConfig has been correctly configured.\nPress any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }

        private void OnConnect(object sender, EventArgs args) {
            LoadScenes();

            Console.WriteLine("\nValid scenes:");
            scenes.ForEach(scene => Console.WriteLine($"  {scene}"));
            Console.WriteLine();
            Console.WriteLine($"Current OBS scene is \"{websocket.GetCurrentProgramScene()}\"");
        }

        private void LoadScenes() {
            var scenes = websocket.GetSceneList().Scenes;
            this.scenes = new List<string>(scenes.Select(s => s.Name).ToList());
            sceneSources = scenes.ToDictionary(
                s => s.Name,
                s => websocket.GetSceneItemList(s.Name).Select(s => s.SourceName).ToHashSet());
        }
    }
}