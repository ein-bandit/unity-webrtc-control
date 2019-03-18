var socket = null;
var remoteIce = [];
var remoteAnswer = null;
var localIce = [];

var dataChannel = null;
var deviceDataChannel = null;
var testmsgcount = 0;
var feedbackmsg = "";
var feedbackmsgrecv = "";
var feedbackmsgsend = "";

var pcOptions = {
  optional: [{ DtlsSrtpKeyAgreement: true }]
};

var servers = {
  //iceTransportPolicy: 'relay', // force turn
  iceServers: [
    { url: "stun:stun.l.google.com:19302" },
    { url: "stun:stun.stunprotocol.org:3478" },
    { url: "stun:stun.anyfirewall.com:3478" },
    { url: "turn:192.168.0.100:3478", username: "test", credential: "test" }
  ]
};

var offerOptions = {
  offerToReceiveAudio: 0,
  offerToReceiveVideo: 0,
  voiceActivityDetection: false,
  iceRestart: true
};

var vgaConstraints = {
  //not using video/audio by now.
  video: false,
  audio: false
};

var dataChannelOptions = {
  ordered: false, // do not guarantee order

  maxRetransmits: 1, // The maximum number of times to try and retransmit
  // a failed message (forces unreliable mode)

  negotiated: false // If set to true, it removes the automatic
  // setting up of a data channel on the other peer,
  // meaning that you are provided your own way to
  // create a data channel with the same id on the other side
  // aka: session.WebRtc.CreateDataChannel("msgDataChannel");
};

var channels = {};
var rTCPeerConnection = null;

function sendData(data) {
  try {
    socket.send(data);
  } catch (ex) {
    console.log("Message sending failed!");
  }
}

function connect(serverAddress, dataChannelSetupCallback) {
  socket = new WebSocket("ws://" + serverAddress);

  socket.onopen = function() {
    setupWebRTC(dataChannelSetupCallback);
  };

  socket.onclose = function() {
    connecting = false;

    console.log("Socket connection has been disconnected!");

    for (channel in channels) {
      console.log(
        "socket closed. removing channel",
        channel,
        channels[channel]
      );
      channels[channel].close();
      delete channels[channel];
    }

    if (rTCPeerConnection) {
      rTCPeerConnection.close();
      rTCPeerConnection = null;
    }

    remoteAnswer = null;
    remoteIce = [];
    localIce = [];
  };

  socket.onmessage = function(Message) {
    var obj = JSON.parse(Message.data);
    var command = obj.command;
    console.log("received message", command, rTCPeerConnection);
    switch (command) {
      case "OnSuccessAnswer":
        {
          if (rTCPeerConnection) {
            console.log("OnSuccessAnswer[remote]: " + obj.sdp);

            remoteAnswer = obj.sdp;

            rTCPeerConnection.setRemoteDescription(
              new RTCSessionDescription({ type: "answer", sdp: remoteAnswer }),
              function() {},
              function(errorInformation) {
                console.log("setRemoteDescription error: " + errorInformation);
                socket.close();
              }
            );
          }
        }
        break;

      case "OnIceCandidate":
        {
          if (rTCPeerConnection) {
            console.log("OnIceCandidate[remote]: " + obj.sdp);

            var c = new RTCIceCandidate({
              sdpMLineIndex: obj.sdp_mline_index,
              candidate: obj.sdp
            });
            remoteIce.push(c);

            rTCPeerConnection.addIceCandidate(c);
          }
        }
        break;

      default: {
        console.log(Message.data);
      }
    }
  };
}

function setupWebRTC(dataChannelSetupCallback) {
  rTCPeerConnection = new RTCPeerConnection(servers, pcOptions);

  dataChannelSetupCallback();

  rTCPeerConnection.onicecandidate = function(event) {
    if (event.candidate) {
      var ice = parseIce(event.candidate.candidate);
      if (
        ice &&
        ice.component_id == 1 && // skip RTCP
        //&& ice.type == 'relay'           // force turn
        ice.localIP.indexOf(":") < 0
      ) {
        // skip IP6

        console.log("onicecandidate[local]: " + event.candidate.candidate);
        var obj = JSON.stringify({
          command: "onicecandidate",
          candidate: event.candidate
        });
        sendData(obj);
        localIce.push(ice);
      } else {
        console.log("onicecandidate[local skip]: " + event.candidate.candidate);
      }
    } else {
      console.log("onicecandidate: complete." + JSON.stringify(event));

      if (remoteAnswer) {
        // fill empty pairs using last remote ice
        //for (var i = 0, lenl = localIce.length; i < lenl; i++) {
        //    if (i >= remoteIce.length) {
        //        var c = remoteIce[remoteIce.length - 1];
        //        var ice = parseIce(c.candidate);
        //        ice.foundation += i;
        //        c.candidate = stringifyIce(ice);
        //        rTCPeerConnection.addIceCandidate(c);
        //    }
        //}
      }
    }
  };

  // can't manage to get trigger from other side ;/ wtf?
  // rTCPeerConnection.ondatachannel = function(event) {
  //   dataChannel = event.channel;
  //   console.log("special", dataChannel);
  //   console.log("new channels", channels);
  //   setDataChannel(dataChannel);
  //   channels[dataChannel.label] = dataChannel;
  // };

  rTCPeerConnection.createOffer(
    function(desc) {
      console.log("createOffer: " + desc.sdp);

      rTCPeerConnection.setLocalDescription(
        desc,
        function() {
          var obj = JSON.stringify({
            command: "offer",
            desc: desc
          });
          sendData(obj);
        },
        function(errorInformation) {
          console.log("setLocalDescription error: " + errorInformation);

          socket.close();
        }
      );
    },
    function(error) {
      console.log(error);
      socket.close();
    },
    offerOptions
  );
}

function createLocalDataChannel(name, callbacks) {
  console.log("creating data channel", name);
  dataChannel = rTCPeerConnection.createDataChannel(name, {});
  channels[name] = dataChannel;
  setDataChannel(dataChannel, callbacks);
}

function setDataChannel(dc, callbacks) {
  console.log("setDataChannel[" + dc.id + "]: " + dc.label);

  dc.onerror = function(error) {
    console.log("DataChannel Error:", error);
    callbacks.error(error);
  };
  dc.onmessage = function(event) {
    console.log("DataChannel Message:", event.data);

    callbacks.message(prepareMessage(event.data));
  };
  dc.onopen = function() {
    console.log("datachannel connection successful.");
    callbacks.initialize();
  };
  dc.onclose = function() {
    console.log(dc.id, "closed");
    callbacks.close();
  };

  function prepareMessage(message) {
    return JSON.parse(JSON.parse(message));
  }
}
