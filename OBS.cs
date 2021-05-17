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

        public OBS() {
        }

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: false);
        }

        public Task Connect() {
            this.webSocket = new ObsWebSocket();
            this.webSocket.Connect($"ws://127.0.0.1:4444", string.Empty);
            return Task.CompletedTask;
        }

        public bool ChangeScene(string scene, int delay = 0) {
            if (!this.validScenes.Contains(scene)) {
                Console.WriteLine($"OBS scene named \"{scene}\" does not exist");
                return false;
            }

            if (delay == 0) {
                this.webSocket.Api.SetCurrentScene(scene);
            } else {
                Task.Delay(delay).ContinueWith(t => this.webSocket.Api.SetCurrentScene(scene));
            }

            return true;
        }

        public void GetScenes() {
            this.validScenes = this.webSocket.Api.GetSceneList().Scenes.Select(s => s.Name).ToList();
            Console.WriteLine("Valid scenes:");
            foreach (var scene in this.validScenes) {
                Console.WriteLine(scene);
            }
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