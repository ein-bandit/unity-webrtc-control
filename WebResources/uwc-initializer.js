/*! uwc-initializer.js
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

import UWC from "./uwc/unity-webrtc-control.js";
import StaticEventDispatcher from "./uwc/static-event-dispatcher";

const uwc = new UWC();
const eventDispatcher = new StaticEventDispatcher();

const connectBtn = document.getElementById("connect-btn");
const receiver = document.getElementById("receiver");
const testarea = document.getElementById("test-area");

eventDispatcher.on("uwc.initialized", () => {
  testarea.classList.remove("hidden");
  //vibrate 100ms when connected.
  eventDispatcher.emit("features.vibrate", 100);
});

eventDispatcher.on("uwc.message", data => {
  receiver.innerHTML = JSON.stringify(message);
});

connectBtn.onclick = function() {
  eventDispatcher.emit("uwc.connect");
  connectBtn.classList.add("tapped");
  setTimeout(() => {
    connectBtn.classList.remove("tapped");
  }, 250);
};
