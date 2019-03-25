/*! webrtc.js
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

import * as adapter from "./../lib//adapter.min.js";
import StaticEventDispatcher from "./static-event-dispatcher.js";

const iceServers = [
  { url: "stun:stun.stunprotocol.org:3478" },
  { url: "stun:stun.anyfirewall.com:3478" }
];

const iceCandidatePropertyNames = [
  "foundation",
  "component_id",
  "transport",
  "priority",
  "localIP",
  "localPort",
  "type",
  "remoteIP",
  "remotePort",
  "generation",
  "ufrag"
];

const dataChannelName = "uwc-datachannel";

const dataChannelOptions = {
  ordered: false
};

const peerConnectionOptions = {
  optional: [{ DtlsSrtpKeyAgreement: true }]
};

const rtcOfferOptions = {
  offerToReceiveAudio: 0,
  offerToReceiveVideo: 0,
  voiceActivityDetection: false,
  iceRestart: true
};

const _createWebSocket = Symbol("createWebsocket");
const _initializeWebRTC = Symbol("initializeWebRTC");
const _handleIceCandidates = Symbol("handleIceCandidates");
const _parseIceCandidate = Symbol("parseIceCandidate");
const _createRTCOffer = Symbol("createRTCOffer");
const _handleRTCMessage = Symbol("handleRTCMessage");
const _cleanUpWebRTC = Symbol("cleanUpWebRTC");

class WebRTC {
  constructor() {
    this.rTCPeerConnection = null;
    this.dataChannel = null;

    this.socket = null;
    this.remoteIce = [];
    this.localIce = [];

    this.eventDispatcher = new StaticEventDispatcher();
  }

  connect(serverAddress) {
    this[_createWebSocket](serverAddress);
  }

  sendMessage(message) {
    if (this.dataChannel.readyState === "open") {
      this.dataChannel.send(message);
    }
  }

  [_createWebSocket](serverAddress) {
    var websocket = new WebSocket("ws://" + serverAddress);
    this.socket = Object.assign(websocket, {
      onopen: () => {
        this[_initializeWebRTC]();
      },
      onclose: () => {
        this[_cleanUpWebRTC]();
      },
      onmessage: message => {
        this[_handleRTCMessage](message);
      },
      onerror: error => {
        console.log("websocket error", error);
      }
    });
  }

  [_initializeWebRTC]() {
    this.rTCPeerConnection = new RTCPeerConnection(
      { iceServers: iceServers },
      peerConnectionOptions
    );

    this.rTCPeerConnection = Object.assign(this.rTCPeerConnection, {
      onicecandidate: event => {
        this[_handleIceCandidates](event);
      }
    });

    this.dataChannel = this.rTCPeerConnection.createDataChannel(
      dataChannelName,
      dataChannelOptions
    );

    this.dataChannel = Object.assign(this.dataChannel, {
      onopen: () => {
        this.eventDispatcher.emit("webrtc.open");
      },
      onclose: () => {
        this.eventDispatcher.emit("webrtc.close");
      },
      onmessage: message => {
        this.eventDispatcher.emit(
          "webrtc.message",
          JSON.parse(JSON.parse(message.data))
        );
      }, //convert string message to object.
      onerror: error => {
        this.eventDispatcher.emit("webrtc.error", error);
      }
    });

    //create offer after initialization of resources finished.
    this.rTCPeerConnection.createOffer(
      description => {
        this[_createRTCOffer](description);
      },
      error => {
        this[_cleanUpWebRTC]();
      },
      rtcOfferOptions
    );
  }

  [_cleanUpWebRTC]() {
    if (this.dataChannel) {
      this.dataChannel.close();
      this.dataChannel = null;
    }
    if (this.rTCPeerConnection) {
      this.rTCPeerConnection.close();
      this.rTCPeerConnection = null;
    }
    if (this.socket) {
      this.socket.close();
      this.socket = null;
    }

    this.remoteIce = [];
    this.localIce = [];
  }

  [_handleIceCandidates](event) {
    if (event.candidate) {
      var ice = this[_parseIceCandidate](event.candidate.candidate);
      if (ice && ice.component_id == 1 && ice.localIP.indexOf(":") < 0) {
        this.socket.send(
          JSON.stringify({
            command: "onicecandidate",
            candidate: event.candidate
          })
        );
        this.localIce.push(ice);
      }
    } else {
      //nothing to do here: ice cadidate complete.
    }
  }

  [_createRTCOffer](description) {
    this.rTCPeerConnection.setLocalDescription(
      description,
      () => {
        this.socket.send(
          JSON.stringify({
            command: "offer",
            desc: description
          })
        );
      },
      error => {
        console.log("webrtc error setting local description", error);
        this[_cleanUpWebRTC]();
      }
    );
  }
  [_handleRTCMessage](message) {
    var obj = JSON.parse(message.data);
    var command = obj.command;

    switch (command) {
      case "OnSuccessAnswer":
        {
          if (this.rTCPeerConnection) {
            this.rTCPeerConnection.setRemoteDescription(
              new RTCSessionDescription({ type: "answer", sdp: obj.sdp }),
              () => {},
              error => {
                console.log("webrtc error setting remote description", error);
                this[_cleanUpWebRTC];
              }
            );
          }
        }
        break;

      case "OnIceCandidate":
        {
          if (this.rTCPeerConnection) {
            var iceCandidate = new RTCIceCandidate({
              sdpMLineIndex: obj.sdp_mline_index,
              candidate: obj.sdp
            });
            this.remoteIce.push(iceCandidate);

            this.rTCPeerConnection.addIceCandidate(iceCandidate);
          }
        }
        break;

      default: {
        console.log("webrtc received unknown message", message.data);
      }
    }
  }
  [_parseIceCandidate](candidate) {
    // Check if the string was successfully parsed
    var parsedIceCandidate = candidate.match(iceCandidateRegex);
    if (!parsedIceCandidate) {
      console.warn("Error while parsing ice candidate.");
      return null;
    }

    var candObj = {};
    for (var i = 0; i < iceCandidatePropertyNames.length; i++) {
      candObj[iceCandidatePropertyNames[i]] = parsedIceCandidate[i + 1];
    }
    return candObj;
  }
}

const iceCandidateRegex = () => {
  // token                  =  1*(alphanum / "-" / "." / "!" / "%" / "*"
  //                              / "_" / "+" / "`" / "'" / "~" )
  var token_re = "[0-9a-zA-Z\\-\\.!\\%\\*_\\+\\`\\'\\~]+";

  // ice-char               = ALPHA / DIGIT / "+" / "/"
  var ice_char_re = "[a-zA-Z0-9\\+\\/]+";

  // foundation             = 1*32ice-char
  var foundation_re = ice_char_re;

  // component-id           = 1*5DIGIT
  var component_id_re = "[0-9]{1,5}";

  // transport             = "UDP" / transport-extension
  // transport-extension   = token      ; from RFC 3261
  var transport_re = token_re;

  // priority              = 1*10DIGIT
  var priority_re = "[0-9]{1,10}";

  // connection-address SP      ; from RFC 4566
  var connection_address_v4_re =
    "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}";
  var connection_address_v6_re = "\\:?(?:[0-9a-fA-F]{0,4}\\:?)+"; // fde8:cd2d:634c:6b00:6deb:9894:734:f75f

  var connection_address_re =
    "(?:" + connection_address_v4_re + ")|(?:" + connection_address_v6_re + ")";

  // port                      ; port from RFC 4566
  var port_re = "[0-9]{1,5}";

  //  cand-type             = "typ" SP candidate-types
  //  candidate-types       = "host" / "srflx" / "prflx" / "relay" / token
  var cand_type_re = token_re;

  // prettier-ignore
  var ICE_RE = '(?:a=)?candidate:(' + foundation_re + ')' + // candidate:599991555 // 'a=' not passed for Firefox (and now for Chrome too)
      '\\s' + '(' + component_id_re + ')' +                 // 2
      '\\s' + '(' + transport_re + ')' +                 // udp
      '\\s' + '(' + priority_re + ')' +                 // 2122260222
      '\\s' + '(' + connection_address_re + ')' +                 // 192.168.1.32 || fde8:cd2d:634c:6b00:6deb:9894:734:f75f
      '\\s' + '(' + port_re + ')' +                 // 49827
      '\\s' + 'typ' +                       // typ
      '\\s' + '(' + cand_type_re + ')' +                 // host
      '(?:' +
      '\\s' + 'raddr' +
      '\\s' + '(' + connection_address_re + ')' +
      '\\s' + 'rport' +
      '\\s' + '(' + port_re + ')' +
      ')?' +
      '(?:' +
      '\\s' + 'generation' +                       // generation
      '\\s' + '(' + '\\d+' + ')' +                 // 0
      ')?' +
      '(?:' +
      '\\s' + 'ufrag' +                       // ufrag
      '\\s' + '(' + ice_char_re + ')' +      // WreAYwhmkiw6SPvs
      ')?';

  return new RegExp(ICE_RE);
};

export default WebRTC;
