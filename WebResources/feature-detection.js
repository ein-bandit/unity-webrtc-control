var featureDisplayArea = document.getElementById("feature-area");

//consider using gyronorm.js

var supportedFeatures = [];
var enabledFeatures = [];

var features = {
  tapDetection: {
    available: true,
    message: "on screen actions enabled",
    registration: function(isRegister) {
      for (var prop in config.features.tapDetection.areas) {
        if (config.features.tapDetection.areas.hasOwnProperty(prop)) {
          var id = config.features.tapDetection.areas[prop];
          if ((tapArea = document.getElementById(id)) !== null) {
            tapArea[
              isRegister === true ? "addEventListener" : "removeEventListener"
            ]("click", features.tapDetection.listenerFunction);
          }
        }
      }
    },
    listenerFunction: function(evt) {
      evt.target.classList.add("tapped");
      setTimeout(() => {
        evt.target.classList.remove("tapped");
      }, 250);

      if (!evt.target.classList.contains("disabled")) {
        uwc.sendFunction({ type: "tap", data: evt.target.id });
      }
    }
  },
  vibrate: {
    available: navigator.vibrate,
    message: "vibration support",
    registration: function() {}, //no setup needed: just call navigator.vibrate(duration);
    listenerFunction: function() {}
  },
  deviceOrientation: {
    available: window.DeviceOrientationEvent,
    message: "device orientation available",
    registration: function(isRegister) {
      window[isRegister === true ? "addEventListener" : "removeEventListener"](
        "deviceorientation",
        features.deviceOrientation.listenerFunction
      );
    },
    listenerFunction: function(evt) {
      var data = {
        a: Math.floor(evt.alpha),
        b: Math.floor(evt.beta),
        c: Math.floor(evt.gamma)
      };
      if (data != features.deviceOrientation.lastData) {
        uwc.sendFunction({
          type: "orientation",
          data: data
        });
      }
    },
    lastData: null
  },
  deviceProximity: {
    available: "ondeviceproximity" in window,
    message: "device proximity available",
    registration: function(isRegister) {
      window[isRegister === true ? "addEventListener" : "removeEventListener"](
        "deviceproximity",
        features.deviceProximity.listenerFunction
      );
    },
    listenerFunction: function(evt) {
      uwc.sendFunction({ type: "proximity", data: evt.value > 0 });
    }
  },
  deviceMotion: {
    available: window.DeviceMotionEvent,
    message: "device motion available",
    registration: function(isRegister) {
      window[isRegister === true ? "addEventListener" : "removeEventListener"](
        "devicemotion",
        features.deviceMotion.listenerFunction
      );
    },
    listenerFunction: function(evt) {
      //TODO: add check if phone was shaked and send data once.

      //if (evt.acceleration.x > 0.5 || evt.acceleration.y > 0.5 || evt.acceleration.z > 0.5) {
      if (evt.rotationRate.alpha > 30) {
        dataElement.innerHTML =
          "motion:" +
          JSON.stringify(evt) +
          ", " +
          JSON.stringify({ x: evt.rotationRate.alpha });
      }
      //}
      //uwc.sendFunction({type:'motion', data: evt.value > 0}));
    }
  },
  deviceLight: {
    available: "ondevicelight" in window,
    message: "ambient light available",
    registration: function(isRegister) {
      window[isRegister === true ? "addEventListener" : "removeEventListener"](
        "devicelight",
        features.deviceLight.listenerFunction
      );
    },
    listenerFunction: function(evt) {
      uwc.sendFunction({ type: "lightsensor", data: evt.value });
    }
  }
};

for (var property in features) {
  if (features.hasOwnProperty(property) && features[property].available) {
    if (config.features[property].enabled) {
      addAvailableFeatureMessage(features[property].message);
      //feature is supported by browser and enabled in config.
      enabledFeatures.push(property);
    }
    supportedFeatures.push(property);
  }
}

if (debug) {
  console.log("available features: " + supportedFeatures.join(","));
}

function addAvailableFeatureMessage(message) {
  if (!featureDisplayArea || !message) {
    return;
  }
  var feature = document.createElement("div");
  feature.classList.add("detected-feature");
  var featureMessage = document.createElement("span");
  featureMessage.innerHTML = message;
  feature.appendChild(featureMessage);
  featureDisplayArea.appendChild(feature);
}

//TODO: consider using HTML5 API WakeLock.
var noSleep = new NoSleep();

function activateNoSleep() {
  noSleep.enable();
}
function deactivateNoSleep() {
  noSleep.disable();
}
