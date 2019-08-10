/*! feature-detection.js
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

import config from "./../config.js";
import StaticEventDispatcher from "./static-event-dispatcher.js";

const eventDispatcher = new StaticEventDispatcher();

//handles vibration event.
eventDispatcher.on("features.vibrate", duration => {
    featureHandlers["vibration"]({ duration: duration });
});

const featureAvailability = {
    tapDetection: true,
    vibration: typeof navigator.vibrate !== "undefined",
    deviceOrientation: "ondeviceorientation" in window, //consider using gyronorm
    deviceProximity: "ondeviceproximity" in window, //only supported in FF
    deviceMotion: typeof window.DeviceMotionEvent !== "undefined",
    deviceLight: "ondevicelight" in window //no chrome.
};

const featureHandlers = {
    tapDetection: evt => {
        evt.target.classList.add("tapped");
        setTimeout(() => {
            evt.target.classList.remove("tapped");
        }, 250);

        if (!evt.target.classList.contains("disabled")) {
            eventDispatcher.emit("uwc.send-data", {
                type: "tap",
                data: evt.target.id
            });
        }
    },
    vibration: evt => {
        navigator.vibrate(evt.duration);
    },
    deviceOrientation: evt => {
        var data = {
            a: Math.floor(evt.alpha),
            b: Math.floor(evt.beta),
            c: Math.floor(evt.gamma)
        };
        //find a solution to not resend same data as before
        eventDispatcher.emit("uwc.send-data", {
            type: "orientation",
            data: data
        });
    },
    deviceProximity: evt => {
        eventDispatcher.emit("uwc.send-data", {
            type: "proximity",
            data: evt.value > 0
        });
    },
    deviceMotion: evt => {
        //TODO: add check if phone was shaked and send data once.

        //if (evt.acceleration.x > 0.5 || evt.acceleration.y > 0.5 || evt.acceleration.z > 0.5) {
        if (evt.rotationRate.alpha > 30) {
            // prettier-ignore
            dataElement.innerHTML =
                `motion ${JSON.stringify(evt)}, ${JSON.stringify({ x: evt.rotationRate.alpha })}`;
        }
        //}
        //eventDispatcher.emit("uwc.send-data",{type:'motion', data: evt.value > 0}));
    },
    deviceLight: evt => {
        eventDispatcher.emit("uwc.send-data", {
            type: "lightsensor",
            data: evt.value
        });
    }
};

var handlersRegistered = false;
const registerFeatures = () => {
    if (handlersRegistered === true) {
        console.error(
            "Handlers were already intialized. Change handler before calling uwc.initialize()"
        );
        return;
    }

    //runs over config customhandlers and overrides featureHandlers[feature] with new handler.
    for (var prop in config.customHandlers) {
        if (config.customHandlers.hasOwnProperty(prop)) {
            featureHandlers[prop] = config.customHandlers[prop];
        }
    }
    handleFeatureRegistering(true);
    handlersRegistered = true;
};

const unregisterFeatures = () => {
    handleFeatureRegistering(false);
    handlersRegistered = false;
};

const registrationFunction = (featureName, isRegister) => {
    var element = featureName.includes("click") ? getTapId(featureName) : window;
    element[isRegister === true ? "addEventListener" : "removeEventListener"](
        featureName.split("#")[0].toLowerCase(), //first part up to # sign (click#click-id) or all of string.
        featureName.includes("click") ?
        featureHandlers["tapDetection"] :
        featureHandlers[featureName]
    );
};

const handleFeatureRegistering = register => {
    for (var prop in config.features) {
        if (
            config.features.hasOwnProperty(prop) &&
            config.features[prop] !== false &&
            featureAvailability[prop] === true
        ) {
            //if taps shall be registered, iterate over the given ids, else register directly.
            if (prop === "tapDetection") {
                for (var index in config.features[prop]) {
                    registrationFunction(
                        "click#" + config.features[prop][index],
                        register
                    );
                }
            } else {
                registrationFunction(prop, register);
            }
        }
    }
};

const getTapId = featureName => {
    return document.getElementById(featureName.split("#")[1]);
};

const availableFeatures = (() => {
    return Object.keys(featureAvailability)
        .filter(key => featureAvailability[key] === true)
        .reduce((obj, key) => {
            obj[key] = featureAvailability[key];
            return obj;
        }, {});
})();

if (debug) {
    console.log("available features", availableFeatures);
}

export { availableFeatures, registerFeatures, unregisterFeatures };