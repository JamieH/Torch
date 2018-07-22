using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Timers;
using Newtonsoft.Json;
using NLog;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;

namespace Torch.Server.Managers
{
    public class AnalyticsManager : Manager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Timer _updateTimer; // 5 minutes
        
        /// <inheritdoc />
        public AnalyticsManager(ITorchBase torchInstance) : base(torchInstance) { }

        /// <inheritdoc />
        public override void Attach()
        {           
            Torch.GameStateChanged += TorchOnGameStateChanged;
        }

        private void TorchOnGameStateChanged(MySandboxGame game, TorchGameState newstate)
        {
            switch (newstate)
            {
                case TorchGameState.Loaded:
                {
                    _updateTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds)
                    {
                        Enabled = true,
                        AutoReset = true,
                    };
            
                    SendAnalytics(null, null);
                    _updateTimer.Elapsed += SendAnalytics;
                    break;
                }
                case TorchGameState.Unloading:
                {
                    _updateTimer?.Dispose();
                    _updateTimer = null;
                    break;
                }
            }
        }

        private void SendAnalytics(object sender, ElapsedEventArgs e)
        {
            Log.Info("Sending analytics");

            var instance = Torch.CurrentSession.Managers.GetManager<InstanceManager>();
            var mp = Torch.CurrentSession.Managers.GetManager<MultiplayerManagerDedicated>();
            var status = new ServerStatusItem
            {
                Name = instance.DedicatedConfig.ServerName,
                World = instance.DedicatedConfig.WorldName,
                MaxPlayers = instance.DedicatedConfig.SessionSettings.MaxPlayers,
                CurrentPlayers = mp.Players.Count,
                Address = $"{IPAddressExtensions.FromIPv4NetworkOrder(MyGameService.GameServer.GetPublicIP())}:{instance.DedicatedConfig.Port}"
            };

            var json = JsonConvert.SerializeObject(status);
            var content = new StringContent(json);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var client = new HttpClient();
            client.PostAsync("https://torchapi.net/api/serverlist/ping", content).Wait();
        }

        /// <inheritdoc />
        public override void Detach()
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
        }
        
        public class ServerStatusItem
        {
            public string Name { get; set; }

            public string World { get; set; }

            public int MaxPlayers { get; set; }

            public int CurrentPlayers { get; set; }

            public string Address { get; set; }
        }
    }
}