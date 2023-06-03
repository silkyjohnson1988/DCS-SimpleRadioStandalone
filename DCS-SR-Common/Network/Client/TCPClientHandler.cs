using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Caliburn.Micro;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Network.EventMessages;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Setting;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Singletons;
using Newtonsoft.Json;
using NLog;
using LogManager = NLog.LogManager;


namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network.Client
{
    public class TCPClientHandler : IHandle<DisconnectRequestMessage>, IHandle<UnitUpdateMessage>
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private volatile bool _stop = false;

        public static string ServerVersion = "Unknown";
        private readonly string _guid;
        private IPEndPoint _serverEndpoint;
        private TcpClient _tcpClient;
        
        private static readonly int MAX_DECODE_ERRORS = 5;

        private long _lastSent = -1;
        private System.Timers.Timer _idleTimeoutTimer;
        
        private readonly ConnectedClientsSingleton _clients = ConnectedClientsSingleton.Instance;
        private readonly SyncedServerSettings _serverSettings = SyncedServerSettings.Instance;

        private SRClient _playerUnitState;

        private long idleTimeOutTime = 0;

        public TCPClientHandler(string guid, SRClient _playerUnitState, long idleTimeOut=0)
        {
            _guid = guid;
            //TODO pass in LatLng as well
            this._playerUnitState = _playerUnitState;

            idleTimeOutTime = idleTimeOut;
            _idleTimeoutTimer= new System.Timers.Timer(10000);
            _idleTimeoutTimer.Elapsed += new ElapsedEventHandler(CheckIfIdleTimeOut);
            _idleTimeoutTimer.Interval = TimeSpan.FromSeconds(10).TotalMilliseconds;

        }

        private void CheckIfIdleTimeOut(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_lastSent != -1 && idleTimeOutTime > 0 && TimeSpan.FromTicks(DateTime.Now.Ticks - _lastSent).TotalSeconds > idleTimeOutTime)
            {
                Logger.Warn("Disconnecting - Idle Time out");
                Disconnect();
            }
        }
        
        public Task HandleAsync(DisconnectRequestMessage message, CancellationToken cancellationToken)
        {
            Disconnect();

            return Task.CompletedTask;
        }

        public Task HandleAsync(UnitUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.FullUpdate)
                ClientRadioUpdated(message.UnitUpdate);
            else
                ClientCoalitionUpdate(message.UnitUpdate);

            return Task.CompletedTask;
        }


        public void TryConnect(IPEndPoint endpoint)
        {
            EventBus.Instance.SubscribeOnBackgroundThread(this);
            _serverEndpoint = endpoint;

            var tcpThread = new Thread(Connect);
            tcpThread.Start();
        }

        //TODO
        // public void ConnectExternalAWACSMode(string password, ExternalAWACSModeConnectCallback callback)
        // {
        //     if (_clientStateSingleton.ExternalAWACSModelSelected)
        //     {
        //         return;
        //     }
        //
        //     _externalAWACSModeCallback = callback;
        //
        //     var sideInfo = _clientStateSingleton.PlayerCoaltionLocationMetadata;
        //     sideInfo.name = _clientStateSingleton.LastSeenName;
        //     SendToServer(new NetworkMessage
        //     {
        //         Client = new SRClient
        //         {
        //             Coalition = sideInfo.side,
        //             Name = sideInfo.name,
        //             LatLngPosition = sideInfo.LngLngPosition,
        //             ClientGuid = _guid,
        //             AllowRecord = GlobalSettingsStore.Instance.GetClientSettingBool(GlobalSettingsKeys.AllowRecording)
        //         },
        //         ExternalAWACSModePassword = password,
        //         MsgType = NetworkMessage.MessageType.EXTERNAL_AWACS_MODE_PASSWORD
        //     });
        // }
        //
        // public void DisconnectExternalAWACSMode()
        // {
        //     if (!_clientStateSingleton.ExternalAWACSModelSelected || _radioDCSSync == null)
        //     {
        //         return;
        //     }
        //
        //     _radioDCSSync.StopExternalAWACSModeLoop();
        //
        //     CallExternalAWACSModeOnMain(false, 0);
        // }

      //  [MethodImpl(MethodImplOptions.Synchronized)]
        private void Connect()
        {
            _lastSent = DateTime.Now.Ticks;
            _idleTimeoutTimer.Start();

            //TODO move out
            // if (_radioDCSSync != null)
            // {
            //     _radioDCSSync.Stop();
            //     _radioDCSSync = null;
            // }
            // if (_lotATCSync != null)
            // {
            //     _lotATCSync.Stop();
            //     _lotATCSync = null;
            // }
            // if (_vaicomSync != null)
            // {
            //     _vaicomSync.Stop();
            //     _vaicomSync = null;
            // }

            //TODO move out
            // _radioDCSSync = new DCSRadioSyncManager(ClientRadioUpdated, ClientCoalitionUpdate, _guid,_newAircraft);
            // _lotATCSync = new LotATCSyncHandler(ClientCoalitionUpdate, _guid);
            // _vaicomSync = new VAICOMSyncHandler();

            _lastSent = DateTime.Now.Ticks;

            var connectionError = false;

            using (_tcpClient = new TcpClient())
            {
                try
                {
                    _tcpClient.SendTimeout = 90000;
                    _tcpClient.NoDelay = true;

                    // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                    _tcpClient.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port)
                        .Wait(TimeSpan.FromSeconds(10));

                    if (_tcpClient.Connected)
                    {
                        _tcpClient.NoDelay = true;

                        ClientSyncLoop();
                    }
                    else
                    {
                        Logger.Error($"Failed to connect to server @ {_serverEndpoint}");

                        // Signal disconnect including an error
                        connectionError = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not connect to server");
                    connectionError = true;
                    Disconnect();
                }
            }
        }

        private void ClientRadioUpdated(SRClient updatedUnitState)
        {
            //disconnect AWACS
            // if (_clientStateSingleton.ExternalAWACSModelSelected && _clientStateSingleton.IsGameConnected)
            // {
            //     Logger.Debug("Disconnect AWACS Mode as Game Detected");
            //     DisconnectExternalAWACSMode();
            // }

            Logger.Debug("Sending Radio Update to Server");
            //var sideInfo = _clientStateSingleton.PlayerCoaltionLocationMetadata;

            var message = new NetworkMessage
            {
                Client = new SRClient
                {
                    Coalition = updatedUnitState.Coalition,
                    Name = updatedUnitState.Name,
                    Seat = updatedUnitState.Seat,
                    ClientGuid = _guid,
                    RadioInfo = updatedUnitState.RadioInfo,
                    AllowRecord = updatedUnitState.AllowRecord
                },
                MsgType = NetworkMessage.MessageType.RADIO_UPDATE
            };

            var needValidPosition = _serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) || _serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED);

            if (needValidPosition)
            {
                message.Client.LatLngPosition = updatedUnitState.LatLngPosition;
            }
            else
            {
                message.Client.LatLngPosition = new LatLngPosition();
            }

            SendToServer(message);    
        }

        private void ClientCoalitionUpdate(SRClient updatedmetadata)
        {

            var message =  new NetworkMessage
            {
                Client = new SRClient
                {
                    Coalition = updatedmetadata.Coalition,
                    Name = updatedmetadata.Name,
                    Seat = updatedmetadata.Seat,
                    ClientGuid = _guid,
                    AllowRecord = updatedmetadata.AllowRecord
                },
                MsgType = NetworkMessage.MessageType.UPDATE
            };

            var needValidPosition = _serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED) || _serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED);

            if (needValidPosition)
            {
                message.Client.LatLngPosition = updatedmetadata.LatLngPosition;
            }
            else
            {
                message.Client.LatLngPosition = new LatLngPosition();
            }

            SendToServer(message);
        }

  
        
        private void ClientSyncLoop()
        {
            //clear the clients list
            _clients.Clear();
            int decodeErrors = 0; //if the JSON is unreadable - new version likely

            using (var reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8))
            {
                try
                {
                   
                    //start the loop off by sending a SYNC Request
                    SendToServer(new NetworkMessage
                    {
                        Client = new SRClient
                        {
                            Coalition = _playerUnitState.Coalition,
                            //TODO the name should be set to the last known name when passed in
                            Name = _playerUnitState.Name.Length > 0 ? _playerUnitState.Name : "Unknown",
                            LatLngPosition = _playerUnitState.LatLngPosition,
                            ClientGuid = _guid,
                            RadioInfo = _playerUnitState.RadioInfo,
                            AllowRecord = _playerUnitState.AllowRecord
                        },
                        MsgType = NetworkMessage.MessageType.SYNC,
                    });

                    EventBus.Instance.PublishOnUIThreadAsync(new TCPClientStatusMessage(true, _serverEndpoint));

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        try
                        {
                            var serverMessage = JsonConvert.DeserializeObject<NetworkMessage>(line);
                            decodeErrors = 0; //reset counter
                            if (serverMessage != null)
                            {
                                //Logger.Debug("Received "+serverMessage.MsgType);
                                switch (serverMessage.MsgType)
                                {
                                    case NetworkMessage.MessageType.PING:
                                        // Do nothing for now
                                        break;
                                    case NetworkMessage.MessageType.RADIO_UPDATE:
                                    case NetworkMessage.MessageType.UPDATE:

                                        SRClient updated = null;
                                        if (serverMessage.ServerSettings != null)
                                        {
                                            _serverSettings.Decode(serverMessage.ServerSettings);
                                        }
                                        
                                        if (_clients.ContainsKey(serverMessage.Client.ClientGuid))
                                        {
                                            var srClient = _clients[serverMessage.Client.ClientGuid];
                                            var updatedSrClient = serverMessage.Client;
                                            if (srClient != null)
                                            {
                                                srClient.LastUpdate = DateTime.Now.Ticks;
                                                srClient.Name = updatedSrClient.Name;
                                                srClient.Coalition = updatedSrClient.Coalition;

                                                srClient.LatLngPosition = updatedSrClient.LatLngPosition;

                                                if (updatedSrClient.RadioInfo != null)
                                                {
                                                    srClient.RadioInfo = updatedSrClient.RadioInfo;
                                                    srClient.RadioInfo.LastUpdate = DateTime.Now.Ticks;
                                                }
                                                else
                                                {
                                                    //radio update but null RadioInfo means no change
                                                    if (serverMessage.MsgType ==
                                                        NetworkMessage.MessageType.RADIO_UPDATE &&
                                                        srClient.RadioInfo != null)
                                                    {
                                                        srClient.RadioInfo.LastUpdate = DateTime.Now.Ticks;
                                                    }
                                                }

                                                // Logger.Debug("Received Update Client: " + NetworkMessage.MessageType.UPDATE + " From: " +
                                                //             srClient.Name + " Coalition: " +
                                                //             srClient.Coalition + " Pos: " + srClient.LatLngPosition);

                                                updated = srClient;
                                            }
                                        }
                                        else
                                        {
                                            var connectedClient = serverMessage.Client;
                                            connectedClient.LastUpdate = DateTime.Now.Ticks;

                                            //init with LOS true so you can hear them incase of bad DCS install where
                                            //LOS isnt working
                                            connectedClient.LineOfSightLoss = 0.0f;
                                            //0.0 is NO LOSS therefore full Line of sight

                                            _clients[serverMessage.Client.ClientGuid] = connectedClient;

                                            // Logger.Debug("Received New Client: " + NetworkMessage.MessageType.UPDATE +
                                            //             " From: " +
                                            //             serverMessage.Client.Name + " Coalition: " +
                                            //             serverMessage.Client.Coalition);

                                            updated = connectedClient;
                                        }

                                        //TODO fix AWACS MODE
                                        // if (_clientStateSingleton.ExternalAWACSModelSelected &&
                                        //     !_serverSettings.GetSettingAsBool(Common.Setting.ServerSettingsKeys.EXTERNAL_AWACS_MODE))
                                        // {
                                        //     DisconnectExternalAWACSMode();
                                        // }

                                        EventBus.Instance.PublishOnUIThreadAsync(new SRClientUpdateMessage(updated));

                                        break;
                                    case NetworkMessage.MessageType.SYNC:
                                        // Logger.Info("Recevied: " + NetworkMessage.MessageType.SYNC);

                                        //check server version
                                        if (serverMessage.Version == null)
                                        {
                                            Logger.Error("Disconnecting Unversioned Server");
                                            Disconnect();
                                            break;
                                        }

                                        var serverVersion = Version.Parse(serverMessage.Version);
                                        var protocolVersion = Version.Parse(Constants.MINIMUM_PROTOCOL_VERSION);

                                        ServerVersion = serverMessage.Version;

                                        if (serverVersion < protocolVersion)
                                        {
                                            Logger.Error($"Server version ({serverMessage.Version}) older than minimum procotol version ({Constants.MINIMUM_PROTOCOL_VERSION}) - disconnecting");

                                            ShowVersionMistmatchWarning(serverMessage.Version);

                                            Disconnect();
                                            break;
                                        }

                                        if (serverMessage.Clients != null)
                                        {
                                            foreach (var client in serverMessage.Clients)
                                            {
                                                client.LastUpdate = DateTime.Now.Ticks;
                                                //init with LOS true so you can hear them incase of bad DCS install where
                                                //LOS isnt working
                                                client.LineOfSightLoss = 0.0f;
                                                //0.0 is NO LOSS therefore full Line of sight
                                                _clients[client.ClientGuid] = client;

                                            EventBus.Instance.PublishOnUIThreadAsync(
                                                new SRClientUpdateMessage(client));
                                            }
                                        }
                                        //add server settings
                                        _serverSettings.Decode(serverMessage.ServerSettings);

                                        //TODO
                                        // if (_clientStateSingleton.ExternalAWACSModelSelected &&
                                        //     !_serverSettings.GetSettingAsBool(Common.Setting.ServerSettingsKeys.EXTERNAL_AWACS_MODE))
                                        // {
                                        //     DisconnectExternalAWACSMode();
                                        // }
                                        //
                                        // CallUpdateUIOnMain();

                                        break;

                                    case NetworkMessage.MessageType.SERVER_SETTINGS:

                                        _serverSettings.Decode(serverMessage.ServerSettings);
                                        ServerVersion = serverMessage.Version;

                                        //TODO
                                        // if (_clientStateSingleton.ExternalAWACSModelSelected &&
                                        //     !_serverSettings.GetSettingAsBool(Common.Setting.ServerSettingsKeys.EXTERNAL_AWACS_MODE))
                                        // {
                                        //     DisconnectExternalAWACSMode();
                                        // }

                                  //      CallUpdateUIOnMain();

                                        break;
                                    case NetworkMessage.MessageType.CLIENT_DISCONNECT:

                                        SRClient outClient;
                                        _clients.TryRemove(serverMessage.Client.ClientGuid, out outClient);

                                        if (outClient != null)
                                        EventBus.Instance.PublishOnUIThreadAsync(
                                            new SRClientUpdateMessage(outClient, false));

                                        break;
                                    case NetworkMessage.MessageType.VERSION_MISMATCH:
                                        Logger.Error($"Version Mismatch Between Client ({Constants.VERSION}) & Server ({serverMessage.Version}) - Disconnecting");

                                        ShowVersionMistmatchWarning(serverMessage.Version);

                                        Disconnect();
                                        break;
                                    case NetworkMessage.MessageType.EXTERNAL_AWACS_MODE_PASSWORD:
                                        
                                        //TODO fix all this
                                 //        if (serverMessage.Client.Coalition == 0)
                                 //        {
                                 //            Logger.Info("External AWACS mode authentication failed");
                                 //            
                                 //           //CallExternalAWACSModeOnMain(false, 0);
                                 //        }
                                 //        else if (_radioDCSSync != null && _radioDCSSync.IsListening)
                                 //        {
                                 //            Logger.Info("External AWACS mode authentication succeeded, coalition {0}", serverMessage.Client.Coalition == 1 ? "red" : "blue");
                                 //
                                 //            ///TODO
                                 // //           CallExternalAWACSModeOnMain(true, serverMessage.Client.Coalition);
                                 //            
                                 //            //TODO send AWASCS start
                                 //          //  EventBus.Instance.PublishOnUIThreadAsync(
                                 //            //    new SRClientUpdateMessage(outClient, false));
                                 //
                                 //           // _radioDCSSync.StartExternalAWACSModeLoop();
                                 //        }
                                        break;
                                    default:
                                        Logger.Error("Recevied unknown " + line);
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            decodeErrors++;
                            if (!_stop)
                            {
                                Logger.Error(ex, "Client exception reading from socket ");
                            }

                            if (decodeErrors > MAX_DECODE_ERRORS)
                            {
                                ShowVersionMistmatchWarning("unknown");
                                Disconnect();
                                break;
                            }
                        }

                        // do something with line
                    }
                }
                catch (Exception ex)
                {
                    if (!_stop)
                    {
                        Logger.Error(ex, "Client exception reading - Disconnecting ");
                    }
                }
            }

            //disconnected - reset DCS Info
            //TODO fix this
          //  ClientStateSingleton.Instance.DcsPlayerRadioInfo.LastUpdate = 0;

            //clear the clients list
            _clients.Clear();

            Disconnect();
        }

        private void ShowVersionMistmatchWarning(string serverVersion)
        {
            //TODO
            //SEND Message here
            // MessageBox.Show($"The SRS server you're connecting to is incompatible with this Client. " +
            //                 $"\n\nMake sure to always run the latest version of the SRS Server & Client" +
            //                 $"\n\nServer Version: {serverVersion}" +
            //                 $"\nClient Version: {UpdaterChecker.VERSION}",
            //                 "SRS Server Incompatible",
            //                 MessageBoxButton.OK,
            //                 MessageBoxImage.Error);
        }

        private void SendToServer(NetworkMessage message)
        {
            try
            {
                _lastSent = DateTime.Now.Ticks;
                message.Version = Constants.VERSION;

                var json = message.Encode();

                if (message.MsgType == NetworkMessage.MessageType.RADIO_UPDATE)
                {
                    Logger.Debug("Sending Radio Update To Server: "+ (json));
                }

                var bytes = Encoding.UTF8.GetBytes(json);
                _tcpClient.GetStream().Write(bytes, 0, bytes.Length);
                //Need to flush?
            }
            catch (Exception ex)
            {
                if (!_stop)
                {
                    Logger.Error(ex, "Client exception sending to server");
                }

                Disconnect();
            }
        }

        // //implement IDispose? To close stuff properly?
        // [MethodImpl(MethodImplOptions.Synchronized)]
        public void Disconnect()
        {
            EventBus.Instance.Unsubcribe(this);

            _stop = true;

            _lastSent = DateTime.Now.Ticks;
            _idleTimeoutTimer?.Stop();

            //TODO
            //DisconnectExternalAWACSMode();

            try
            {
                if (_tcpClient != null)
                {   _tcpClient.Close(); // this'll stop the socket blocking
                    _tcpClient = null;
                    
                    EventBus.Instance.PublishOnUIThreadAsync(new TCPClientStatusMessage(false));
                    
                }
            }
            catch (Exception)
            {
            }

            Logger.Error("Disconnecting from server");
            //TODO fix this
            //ClientStateSingleton.Instance.IsConnected = false;

            //CallOnMain(false);
        }
    }
}