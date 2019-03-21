/*! conifg.js (default configuration)
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

//define which browser apis should be enabled and used to stream data.
const config = {
  serverPort: 7770,
  features: {
    tapDetection: ["test-area"],
    vibration: true,
    deviceOrientation: false,
    deviceProximity: false,
    deviceMotion: false,
    deviceLight: false
  },
  customFeatureHandlers: {
    deviceLight: evt => {
      console.log("deviceLight event received", evt);
    }
  },
  exposeToWindow: true,
  debug: true
};

export default config;
