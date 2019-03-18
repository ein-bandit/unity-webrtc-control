using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using WebRtc.NET;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using LitJson;
using System.Text;
using UnityEngine;

namespace UnityWebRTCCOntrol.Network.WebRTC
{
    /// <summary>
    /// Handles signaling (using <see cref="Fleck.WebSocketServer"/>), connection establishment, sending and retrieving messages via WebRTC protocol.
    /// <para>Retrieves string or byte[] messages from clients and passes them to <see cref="UWCController"/>.
    /// Retrieves data to be distributed to a client from <see cref="UWCController"/>.
    /// Implements the <see cref="IWebRTCServer"/> interface.</para>
    /// </summary>
    [Serializable]
    public class WebRTCServer : IWebRTCServer
    {
        private WebSocketServer webSocketServer;

        public readonly ConcurrentDictionary<Guid, IWebSocketConnection> UserList =
            new ConcurrentDictionary<Guid, IWebSocketConnection>();
        public readonly ConcurrentDictionary<Guid, WebRtcSession> Streams =
            new ConcurrentDictionary<Guid, WebRtcSession>();

        private SendMode sendMode = SendMode.text;

        private readonly string[] stunServers = {
            "stun:stun.anyfirewall.com:3478",
            "stun:stun.stunprotocol.org:3478"
            };

        private int clientLimit = 4;
        public int ClientLimit
        {
            get
            {
                lock (this)
                {
                    return clientLimit;
                }
            }
            set
            {
                lock (this)
                {
                    clientLimit = value;
                }
            }
        }

        public const string offer = "offer";
        public const string onicecandidate = "onicecandidate";


        public WebRTCServer(int port) : this("ws://0.0.0.0:" + port)
        {
        }

        public WebRTCServer(string URL)
        {
            webSocketServer = new WebSocketServer(URL);
            webSocketServer.Start(socket =>
            {
                socket.OnOpen = () => { OnConnected(socket); };
                socket.OnMessage = message => { OnReceive(socket, message); };
                socket.OnClose = () => { OnDisconnect(socket); };
                socket.OnError = (error) =>
                {
                    OnDisconnect(socket);
                    socket.Close();
                };
            });
        }

        private void OnConnected(IWebSocketConnection context)
        {
            if (UserList.Count < ClientLimit)
            {
                UserList[context.ConnectionInfo.Id] = context;
            }
            else
            {
                context.Close();
            }
        }

        private void OnDisconnect(IWebSocketConnection context)
        {
            UWCController.Instance.OnUnregisterClient(context.ConnectionInfo.Id);

            UserList.TryRemove(context.ConnectionInfo.Id, out IWebSocketConnection ctx);

            WebRtcSession s;
            if (Streams.TryRemove(context.ConnectionInfo.Id, out s))
            {
                s.Cancel.Cancel();
            }
        }

        private void OnReceive(IWebSocketConnection context, string message)
        {
            if (!message.Contains("command")) return;

            if (!UserList.ContainsKey(context.ConnectionInfo.Id)) return;

            JsonData jsonMessage = JsonMapper.ToObject(message);
            string command = jsonMessage["command"].ToString();

            switch (command)
            {
                case offer:
                    {
                        if (!Streams.ContainsKey(context.ConnectionInfo.Id))
                        {
                            UWCController.Instance.OnRegisterClient(context.ConnectionInfo.Id);

                            WebRtcSession session = Streams[context.ConnectionInfo.Id] = new WebRtcSession();

                            SetupWebRTCConnection(session, context, jsonMessage);
                        }
                    }
                    break;

                case onicecandidate:
                    {
                        JsonData c = jsonMessage["candidate"];

                        int sdpMLineIndex = (int)c["sdpMLineIndex"];
                        string sdpMid = c["sdpMid"].ToString();
                        string candidate = c["candidate"].ToString();

                        Streams[context.ConnectionInfo.Id].WebRtc.AddIceCandidate(sdpMid, sdpMLineIndex, candidate);
                    }
                    break;
            }
        }
        /// <summary>
        /// Sets up the WebRTC connection by starting a new <see cref="System.Threading.Task"/>.
        /// </summary>
        /// <param name="session">Created session for the client.</param>
        /// <param name="context">Context of the user connection.</param>
        /// <param name="jsonMessage">Retrieved WebSocket message as Json.</param>
        private void SetupWebRTCConnection(WebRtcSession session, IWebSocketConnection context, JsonData jsonMessage)
        {
            using (ManualResetEvent manualResetEvent = new ManualResetEvent(false))
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    WebRtcNative.InitializeSSL();

                    using (session.WebRtc)
                    {
                        foreach (string stunServer in stunServers)
                        {
                            session.WebRtc.AddServerConfig(stunServer, string.Empty, string.Empty);
                        }

                        bool success = session.WebRtc.InitializePeerConnection();
                        if (success)
                        {
                            manualResetEvent.Set();

                            while (!session.Cancel.Token.IsCancellationRequested &&
                               session.WebRtc.ProcessMessages(1000))
                            {
                                //UnityEngine.Debug.Log(".");
                            }
                            session.WebRtc.ProcessMessages(1000);
                        }
                        else
                        {
                            context.Close();
                        }
                    }

                }, session.Cancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                if (manualResetEvent.WaitOne(9999))
                {
                    InitWebRTCCallbacks(session, context, jsonMessage);
                }
            }
        }

        private void InitWebRTCCallbacks(WebRtcSession session, IWebSocketConnection context, JsonData jsonMessage)
        {
            session.WebRtc.OnIceCandidate += delegate (string sdp_mid, int sdp_mline_index, string sdp)
            {
                if (context.IsAvailable)
                {
                    JsonData j = new JsonData();
                    j["command"] = "OnIceCandidate";
                    j["sdp_mid"] = sdp_mid;
                    j["sdp_mline_index"] = sdp_mline_index;
                    j["sdp"] = sdp;
                    context.Send(j.ToJson());
                }
            };

            session.WebRtc.OnSuccessAnswer += delegate (string sdp)
            {
                if (context.IsAvailable)
                {
                    JsonData j = new JsonData();
                    j["command"] = "OnSuccessAnswer";
                    j["sdp"] = sdp;
                    context.Send(j.ToJson());
                }
            };

            session.WebRtc.OnFailure += delegate (string error)
            {
                UnityEngine.Debug.Log($"WebRTC Callback OnFailure: {context.ConnectionInfo.Id}, {error}");
            };

            session.WebRtc.OnError += delegate (string error)
            {
                UnityEngine.Debug.Log($"OnError: {context.ConnectionInfo.Id}, {error}");
            };

            session.WebRtc.OnDataMessage += delegate (string dmsg)
            {
                UWCController.Instance.OnReceiveData(context.ConnectionInfo.Id, dmsg);
            };

            session.WebRtc.OnDataBinaryMessage += delegate (byte[] dmsg)
            {
                UWCController.Instance.OnReceiveData(
                    context.ConnectionInfo.Id,
                    ConvertByteArrayToString(dmsg)
                    );
            };

            //send offer request after registering handlers.
            string desc_sdp = jsonMessage["desc"]["sdp"].ToString();
            session.WebRtc.OnOfferRequest(desc_sdp);
        }

        public void SendWebRTCMessage(IComparable identifier, string message)
        {
            if (sendMode == SendMode.text)
            {
                Streams[(Guid)identifier].WebRtc.DataChannelSendText(message);
            }
            else if (sendMode == SendMode.bytes)
            {
                byte[] messageAsBytes = ConvertStringToByteArray(message);
                Streams[(Guid)identifier].WebRtc.DataChannelSendData(messageAsBytes, messageAsBytes.Length);
            }
        }

        private string ConvertByteArrayToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        private byte[] ConvertStringToByteArray(string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        /// <summary>
        /// Cancles all connections to clients (WebRTC and WebSocket) and cleans up resources.
        /// </summary>
        public void CloseConnection()
        {
            foreach (var s in Streams)
            {
                if (!s.Value.Cancel.IsCancellationRequested)
                {
                    s.Value.Cancel.Cancel();
                }
            }

            foreach (IWebSocketConnection i in UserList.Values)
            {
                i.Close();
            }

            webSocketServer.Dispose();
            UserList.Clear();
            Streams.Clear();
        }

        public class WebRtcSession
        {
            public readonly WebRtcNative WebRtc;
            public readonly CancellationTokenSource Cancel;

            public WebRtcSession()
            {
                WebRtc = new WebRtcNative();
                Cancel = new CancellationTokenSource();
            }
        }

        private enum SendMode
        {
            text,
            bytes
        }
    }
}