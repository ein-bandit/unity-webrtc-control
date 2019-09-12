using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using LitJson;
using System.Text;

using Spitfire.Net;

namespace UnityWebRTCControl.Network.WebRTC
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
    private SendMode sendMode = SendMode.text;

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
      UnityToolbag.Dispatcher.InvokeAsync(() =>
      {
        UnityEngine.Debug.Log("Unregister from websocket disconnect");
      });
      UWCController.Instance.OnUnregisterClient(context.ConnectionInfo.Id);

      UserList.TryRemove(context.ConnectionInfo.Id, out IWebSocketConnection ctx);

      WebRtcManager.RemoveSession(context.ConnectionInfo.Id);
    }

    private void OnReceive(IWebSocketConnection context, string message)
    {
      if (!message.Contains("command"))
      {
        UnityEngine.Debug.Log("DEBUG: received message that is not a command");
        return;
      }

      if (!UserList.ContainsKey(context.ConnectionInfo.Id))
      {
        UnityEngine.Debug.Log("DEBUG: got message from unknown client:" + context.ConnectionInfo.Id);
        UnityEngine.Debug.Log("DEBUG: message from unknown: " + message);
        return;
      }

      JsonData jsonMessage = JsonMapper.ToObject(message);
      string command = jsonMessage["command"].ToString();

      UnityEngine.Debug.Log("received message: " + command);
      UnityEngine.Debug.Log("received message: " + jsonMessage.ToJson());

      switch (command)
      {
        case offer:
          {
            CreateNewPeerIfNotExisting(context.ConnectionInfo.Id, jsonMessage);
          }
          break;

        case onicecandidate:
          {
            JsonData c = jsonMessage["candidate"];

            int sdpMLineIndex = (int)c["sdpMLineIndex"];
            string sdpMid = c["sdpMid"].ToString();
            string candidate = c["candidate"].ToString();
            //TODO: what to do here?
          }
          break;
      }
    }

    public void SendWebRTCMessage(IComparable identifier, string message)
    {
      // if (sendMode == SendMode.text)
      // {
      WebRtcManager.SendMessage((Guid)identifier, message);

      //}
      // else if (sendMode == SendMode.bytes)
      // {
      //     byte[] messageAsBytes = ConvertStringToByteArray(message);
      //     Streams[(Guid)identifier].WebRtc.DataChannelSendData(messageAsBytes, messageAsBytes.Length);
      // }
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
    /// Cancels all connections to clients (WebRTC and WebSocket) and cleans up resources.
    /// </summary>
    public void CloseConnection()
    {
      UnityEngine.Debug.Log("close everything");

      //remove all session when unity closes.
      WebRtcManager.RemoveAllSession();

      foreach (IWebSocketConnection i in UserList.Values)
      {
        i.Close();
      }

      webSocketServer.Dispose();
      UserList.Clear();
    }

    public void CreateNewPeerIfNotExisting(Guid id, JsonData jsonMessage)
    {
      var session = WebRtcManager.AddSession(id, jsonMessage["desc"]["sdp"].ToString());

      if (session == null)
      {
        return;
      }

      //ManualResetEvent offerEvt = new ManualResetEvent(false);
      string offer = string.Empty;

      session.OnIceCandidateFound += (s, ex) =>
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
              {
                UnityEngine.Debug.Log("ice candidate found");
              });
        if (UserList[id].IsAvailable)
        {
          //TODO: make this prettier.
          JsonData j = new JsonData();
          j["command"] = "OnIceCandidate";
          j["sdp_mid"] = ex.IceCandidate.SdpMid;
          j["sdp_mline_index"] = ex.IceCandidate.SdpIndex;
          j["sdp"] = ex.IceCandidate.Sdp;
          UserList[id].Send(j.ToJson());
        }
      };

      session.OnSuccessAnswerInternal += (s, ex) =>
      {
        if (UserList[id].IsAvailable)
        {
          //TODO: make this prettier.
          JsonData j = new JsonData();
          j["command"] = "OnSuccessAnswer";
          j["sdp"] = ex.Sdp;
          UserList[id].Send(j.ToJson());
        }
      };
      session.Spitfire.OnSuccessOffer += (ex) =>
      {
        session.Spitfire.SetOfferReply(SdpTypes.Answer.ToString().ToLower(), ex.Sdp);


      };
      session.DataChannelOpened += (s, ex) =>
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
              {
                UnityEngine.Debug.Log("Datachannel opened spitfire");
              });

        UWCController.Instance.OnRegisterClient(id);
      };
      session.Spitfire.CreateOffer();
      //offerEvt.WaitOne();
      //session.Spitfire.SetOfferReply()
      //offerEvt.Reset();
      //offerEvt.WaitOne();
    }


    public class WebRtcSession
    {
      public WebRtcSession(Guid id)
      {
        Id = id;
        Spitfire = new SpitfireRtc(44110, 44113);
        Token = new CancellationTokenSource();
      }

      public Guid Id { get; set; }
      public bool IsConnected { get; set; }
      public SpitfireRtc Spitfire { get; set; }
      public readonly CancellationTokenSource Token;
      public event EventHandler<SpitfireIceCandidateEventArgs> OnIceCandidateFound;
      public event EventHandler<SpitfireSdp> OnSuccessAnswerInternal;

      public event EventHandler DataChannelOpened;


      private StunServer[] stunServers = {
         //new StunServer("stun:stun.anyfirewall.com", 3478)
    		 //new StunServer("stun:stun.stunprotocol.org", 3478)
         };

      private struct StunServer
      {
        public StunServer(string address, int port)
        {
          this.Address = address;
          this.Port = port;
        }
        public string Address;
        public int Port;
      }

      public class SpitfireIceCandidateEventArgs : EventArgs
      {
        public SpitfireIceCandidate IceCandidate;
        public SpitfireIceCandidateEventArgs(SpitfireIceCandidate iceCandidate)
        {
          this.IceCandidate = iceCandidate;
        }
      }

      public void BeginLoop(ManualResetEvent go)
      {
        SpitfireRtc.InitializeSSL();

        foreach (StunServer s in stunServers)
        {
          Spitfire.AddServerConfig(
            new ServerConfig
            {
              Host = s.Address,
              Port = Convert.ToUInt16(s.Port),
              Type = ServerType.Stun,
            });
        }

        if (Spitfire.InitializePeerConnection())
        {
          go.Set();
          //Keeps the RTC thread alive and active
          while (!Token.Token.IsCancellationRequested && Spitfire.ProcessMessages(1000))
          {
            Spitfire.ProcessMessages(1000);
          }
          //TODO: Do cleanup here
          UnityEngine.Debug.Log("WebRTC message loop is dead.");
        }
      }

      public void Setup(string sdp)
      {
        Spitfire.OnFailure += Spitfire_OnFailure;
        Spitfire.OnDataChannelOpen += DataChannelOpen;
        Spitfire.OnDataChannelClose += SpitfireOnOnDataChannelClose;
        Spitfire.OnDataChannelConnecting += Spitfire_OnDataChannelConnecting;
        Spitfire.OnDataChannelClosing += Spitfire_OnDataChannelClosing;
        Spitfire.OnDataMessage += Spitfire_OnDataMessage;
        Spitfire.OnDataBinaryMessage += Spitfire_OnDataBinaryMessage;
        Spitfire.OnSuccessAnswer += OnSuccessAnswer;
        Spitfire.OnIceCandidate += SpitfireOnOnIceCandidate;
        //Spitfire.OnIceStateChange += IceStateChange;
        //Spitfire.OnBufferAmountChange += SpitfireOnOnBufferAmountChange;

        //Gives your newly created peer connection the remote clients SDP
        Spitfire.SetOfferRequest(sdp);
      }

      private void Spitfire_OnDataBinaryMessage(string label, byte[] data)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("binary data : label is : " + label);
        });
        throw new NotSupportedException("Binary messages not supported");
      }

      private void Spitfire_OnDataMessage(string label, string message)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("label is : " + label);
        });

        UWCController.Instance.OnReceiveData(Id, message);
      }


      private void Spitfire_OnDataChannelClosing(string label)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("Data channel closing");
        });
      }

      private void Spitfire_OnDataChannelConnecting(string label)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("Data channel creation");
        });
      }

      private void Spitfire_OnError()
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("Spitfire error");
        });
      }

      private void Spitfire_OnFailure(string error)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("spitfire failure");
        });
      }

      private void Spitfire_OnSuccessOffer(SpitfireSdp sdp)
      {
        this.OfferStp = sdp;
      }

      private void SpitfireOnOnIceCandidate(SpitfireIceCandidate iceCandidate)
      {
        //var parsed = IceParser.Parse(iceCandidate.Sdp);
        OnIceCandidateFound?.Invoke(this, new SpitfireIceCandidateEventArgs(iceCandidate));
      }

      private void OnSuccessAnswer(SpitfireSdp sdp)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("success answer:" + sdp.Sdp);
        });
        AnswerSdp = sdp;
        OnSuccessAnswerInternal?.Invoke(this, sdp);
      }

      public SpitfireSdp OfferStp;
      public SpitfireSdp AnswerSdp;

      private void SpitfireOnOnDataChannelClose(string label)
      {
        IsConnected = false;
        UnityToolbag.Dispatcher.InvokeAsync(() =>
       {
         UnityEngine.Debug.Log("data channel closed: " + label);
       });
      }

      private void DataChannelOpen(string label)
      {
        UnityToolbag.Dispatcher.InvokeAsync(() =>
        {
          UnityEngine.Debug.Log("data channel open: " + label);
          //Spitfire.GetDataChannelInfo(label).Reliable
        });
        DataChannelOpened?.Invoke(this, EventArgs.Empty);
        IsConnected = true;
      }
    }

    public class Message
    {
      public Guid Source;
      public string Msg;
      public SpitfireSdp Sdp;
      public SpitfireIceCandidate IceCandidate;
      public Message(Guid id, string message, SpitfireSdp sdp = null, SpitfireIceCandidate iceCandidate = null)
      {
        this.Source = id;
        this.Msg = message;
        this.Sdp = sdp;
        this.IceCandidate = iceCandidate;
      }
    }

    private enum SendMode
    {
      text,
      bytes
    }

    public static class WebRtcManager
    {
      private static readonly ConcurrentDictionary<Guid, WebRtcSession> Sessions =
          new ConcurrentDictionary<Guid, WebRtcSession>();


      /// <summary>
      /// A remote session has sent over its ice candidate, add it to your local session.
      /// </summary>
      /// <param name="id"></param>
      /// <param name="sdpMid"></param>
      /// <param name="sdpMLineIndex"></param>
      /// <param name="candidate"></param>
      /// <returns></returns>
      public static bool AddIceCandidate(Guid id, string sdpMid, int sdpMLineIndex, string candidate)
      {
        if (!Sessions.ContainsKey(id))
        {
          UnityEngine.Debug.Log("Attempted to add candidate to invalid session.");
          return false;
        }
        Sessions[id].Spitfire.AddIceCandidate(sdpMid, sdpMLineIndex, candidate);
        return true;
      }

      /// <summary>
      /// A remote session has sent over its SDP, create a local session based on it.
      /// </summary>
      /// <param name="id"></param>
      /// <param name="sdp"></param>
      public static WebRtcSession AddSession(Guid id, string sdp)
      {
        if (Sessions.ContainsKey(id))
        {
          return null;
        }

        var session = Sessions[id] = new WebRtcSession(id);
        using (var go = new ManualResetEvent(false))
        {

          Task.Factory.StartNew(() =>
          {
            try
            {
              using (session.Spitfire)
              {
                Console.WriteLine($"Starting WebRTC Loop for {id}");
                session.BeginLoop(go);
              }
            }
            catch (Exception e)
            {
              UnityToolbag.Dispatcher.InvokeAsync(() =>
              {
                UnityEngine.Debug.Log("xception in spitfire dispose");
              });

            }
          }, session.Token.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);


          if (go.WaitOne(9999))
          {
            session.Setup(sdp);
          }

        }
        return session;
      }

      public static void SendMessage(Guid id, string message)
      {
        UnityEngine.Debug.Log("sending message now");
        Sessions[id].Spitfire.DataChannelSendText("uwc-datachannel", message);
      }

      public static void RemoveSession(Guid id)
      {
        WebRtcSession s;
        if (Sessions.TryRemove(id, out s))
        {
          s.Token.Cancel();
          //s.Spitfire.Dispose();
        }
      }

      public static void RemoveAllSession()
      {
        foreach (Guid id in Sessions.Keys)
        {
          WebRtcManager.RemoveSession(id);
        }
      }
    }
  }
}