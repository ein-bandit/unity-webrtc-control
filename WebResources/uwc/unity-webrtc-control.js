/*! unity-webrtc-control.js
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

import config from "./../config.js";
import * as ConsoleLogger from "./console-to-div.js";
import * as featureDetection from "./feature-detection.js";
import StaticEventDispatcher from "./static-event-dispatcher.js";
import WebRTC from "./webrtc.js";

const _connect = Symbol("connect");
const _send = Symbol("send");
const _sendData = Symbol("sendData");
const _setSendingEnabled = Symbol("setSendingEnabled");
const _registerEventLisenters = Symbol("registerEventLisenters");
const _convertToBytes = Symbol("convertToBytes");

const debugElement = document.getElementById("debug");
if (config.debug) {
  debugElement.classList.remove("hidden");
  ConsoleLogger.logToDiv();
}

class UWC {
  constructor() {
    this.webRTC = new WebRTC();
    this.eventDispatcher = new StaticEventDispatcher();

    this.serverAddress = null;
    this.sendMode = null;

    this.connecting = false;
    this.connected = false;
    this.sendingEnabled = false;

    this.sendMode = config.sendMode === "string" ? "string" : "bytes";

    if (config.exposeToWindow === true) {
      window.uwc = {
        config: config,
        availableFeatures: featureDetection.availableFeatures,
        eventDispatcher: this.eventDispatcher
      };
    }

    this[_registerEventLisenters]();
  }

  [_registerEventLisenters]() {
    //webrtc event handlers
    this.eventDispatcher.on("webrtc.open", () => {
      this.connected = true;
      this.connecting = false;
      featureDetection.registerFeatures();
      this.eventDispatcher.emit("uwc.initialized");
    });
    this.eventDispatcher.on("webrtc.close", () => {
      this.connected = false;
      featureDetection.unregisterFeatures();
      this.eventDispatcher.emit("uwc.quit");
    });
    this.eventDispatcher.on("webrtc.message", message => {
      this.eventDispatcher.emit("uwc.message", message);
    });
    this.eventDispatcher.on("webrtc.error", error => {
      this.connected = false;
    });

    //uwc internal event handlers.
    this.eventDispatcher.on("uwc.connect", () => {
      this[_connect]();
    });
    this.eventDispatcher.on("uwc.sending-enabled", enabled => {
      this[_setSendingEnabled](enabled);
    });
    //    checks if sending is enabled - can be used for event handlers.
    this.eventDispatcher.on("uwc.send-data", data => {
      this[_sendData](data);
    });
    //    sends the data directly if channel is opened. no check of sending enabled - use for commands.
    this.eventDispatcher.on("uwc.send-message", data => {
      this[_send](data);
    });
  }

  [_connect]() {
    if (this.connecting || this.connected) {
      return;
    }
    this.connecting = true;

    this.webRTC.connect(`${window.location.hostname}:${config.serverPort}`);
  }

  //send function called by features. data sending can be deactivated with setSendingEnabled.
  [_sendData](data) {
    if (!this.sendingEnabled) {
      return;
    }

    if (config.debug) {
      console.log(`sending: ${data.type},${JSON.stringify(data.data)}`);
      if (debugElement) {
        debugElement.innerHTML = JSON.stringify(data);
      }
    }

    this[_send](data);
  }

  [_setSendingEnabled](sendingEnabled) {
    this.sendingEnabled = sendingEnabled;
  }

  [_send](data) {
    if (config.sendMode === "byte") {
      this.webRTC.sendMessage(this[_convertToBytes](data));
    } else {
      this.webRTC.sendMessage(JSON.stringify(data));
    }
  }

  [_convertToBytes](data) {
    var byteArray = [];
    for (var i = 0; i < data.length; i++) {
      var bytes = [];
      for (var j = 0; j < data[i].length; ++j) {
        bytes.push(data[i].charCodeAt(j));
      }
      byteArray.push(bytes);
    }
    return byteArray;
  }
}

export default UWC;
