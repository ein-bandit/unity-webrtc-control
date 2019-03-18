//define which browser apis should be enabled and used to stream data.

const config = {
  serverPort: 8880,
  features: {
    tapDetection: {
      enabled: true,
      areas: {
        test: "test-area"
      }
    },
    vibrate: { enabled: true }, //just referenced to show message as detected feature.
    deviceOrientation: { enabled: false },
    deviceProximity: { enabled: false },
    deviceMotion: { enabled: false },
    deviceLight: { enabled: false }
  }
};

const debug = false;
