namespace SceneSwitcher {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using OBSWebsocketDotNet;

    public class OBS : IDisposable {
        private bool disposedValue;
        private OBSWebsocket websocket;
        private List<string> validScenes;

        public OBS() {
        }

        ~OBS() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: false);
        }

        public Task Connect() {
            this.websocket = new OBSWebsocket();
            this.websocket.Connect($"ws://127.0.0.1:4444", string.Empty);
            return Task.CompletedTask;
        }

        public bool ChangeScene(string scene, int delay = 0) {
            if (!this.validScenes.Contains(scene)) {
                Console.WriteLine($"OBS scene named \"{scene}\" does not exist");
                return false;
            }

            if (delay == 0) {
                this.websocket.SetCurrentScene(scene);
            } else {
                Task.Delay(delay).ContinueWith(t => this.websocket.SetCurrentScene(scene));
            }

            return true;
        }

        public void GetScenes() {
            this.validScenes = this.websocket.GetSceneList().Scenes.Select(s => s.Name).ToList();
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

                this.websocket.Disconnect();
                this.websocket = null;
                this.disposedValue = true;
            }
        }
    }
}