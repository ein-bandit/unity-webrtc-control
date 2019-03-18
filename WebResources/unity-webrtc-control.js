const serverAddress = window.location.hostname + ":" + config.serverPort;

const sendMode = "string"; //"bytes"

//print data to screen if element with id is existing and config.debug = true.
const dataElement = document.getElementById("data");

var connecting = false;
var sendingEnabled = false;

var clientActions = {
  initialize: function() {},
  onMessage: function(message) {},
  onError: function(error) {},
  onClose: function() {}
};

var uwc = {
  initializeConnection: function(overridenClientActions) {
    clientActions = overridenClientActions;
    if (!connecting) {
      connecting = true;

      connect(
        serverAddress,
        setupDataChannelAndListeners
      );
    }
  },
  sendFunction: function(data, forceSend) {
    if (
      forceSend ||
      (sendingEnabled === true && dataChannel.readyState === "open")
    ) {
      if (debug) {
        console.log("sending: " + data.type + ", " + JSON.stringify(data.data));
        if (dataElement) {
          dataElement.innerHTML = JSON.stringify(data);
        }
      }

      //HINT: if your webrtc library supports multiple dataChannels you can use seperate channels for each data type.

      if (sendMode === "byte") {
        dataChannel.send(convertToBytes(data));
      } else {
        dataChannel.send(JSON.stringify(data));
      }
    }
  }
};

function setupDataChannelAndListeners() {
  createLocalDataChannel("message-data-channel", {
    initialize: function() {
      addListeners();
      clientActions.initialize();
    },
    message: clientActions.onMessage,
    error: clientActions.onError,
    close: function() {
      removeListeners();
      clientActions.onClose();
    }
  });
}

function convertToBytes(data) {
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

function addListeners() {
  enabledFeatures.forEach(featureName => {
    features[featureName].registration(true);
  });
}
function removeListeners() {
  enabledFeatures.forEach(featureName => {
    features[featureName].registration(false);
  });
}
