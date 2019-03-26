# Unity WebRTC Control

Unity WebRTC Control is a library offering an easy way to establish a WebRTC communication between web browsers and Unity.
It's focus lies on using modern web technology, especially HTML5 device APIs and sensor data, to create low latency communication and easy accessible game controllers from web interfaces.
However, you can use this library to stream any kind of data between your Unity application and a modern web browser.

Included in this pacakage is a HTTP weberver, a WebRTC server (including a Websocket implemenation for the connection setup) and a threadsafe event system implementation for Unity. JavaScript resources as ES6 modules are available for the frontend to enable a simple connection setup and super-fast data receiving and sending via the WebRTC protocol.

Webclients and the WebRTC implementation will negotiate about the connection parameters and, if this process finishes successfully, a peer to peer connection is set up.

Want to learn more about WebRTC and it's capabilities? Check the [HTML5Rocks][html5rocks-intro] article.

## How to use it

As mentioned in the [README][readme-link] include this package in your Unity applications Asset folder.

Furthermore you'll have to prepare the backend (= Unity side of WebRTC communication) as follows:

- Create a message interpreter by implementing the `INetworkDataInterpreter` interface and pass it to the plugins static UWCController instance.

You can get some inspiration from the demo projects [NetworkDataInterpreter][ndi-impl] implementation inside the demo project.

Extend the `AbstractUWCInitializer` which offers to use the included `SimpleHTTPServer` as well as the default `WebRTCServer` implementation.

```c#
class YourUWCInitializer : AbstractUWCInitializer {
    void Start() {
        Initialize();
    }
    void Initialize() {
        UWCController.Instance.Initialize(
            new YourNetworkDataInterpreter();
        );
    }
}
```

Add your custom implementation of the initializer together with the `Dispatcher` script (located in `Unity-WebRTC-Control/Scripts/Network`) to a GameObject and put it to your starting scene. Your Initializer will act as a Unity Singleton (DontDestroyOnLoad) inherited from `AbstractUWCInitializer`.

- In your game/application register handlers on UWCController.Instance to receive messages inside your application. Use UWCController.Instance.SendMessage to distribute data to the web clients. (You can have a look in the [demo project][triangler-mwc]s `PlayerManager` script on how to register the handlers.

For the frontend (web interface) you will need to create following resources:

- In Unitys `Assets` directory create a folder for your local WebResources and add a HTML controller interface named `index.html`, as well as a JavaScript configuration file `config.js` to it.
  Either copy the `uwc-initializer.js` file to your local resources and adapt it accordingly to handle WebRTC connection setup. Or use a HTML script tag with type="module" for initialization. (See [README][readme-link] for instructions)
  Further information on configuring the frontend can be found in the [Extend Frontend][extend-frontend] section.

## Advanced Usage

### Custom Server Implementations

If you are willing to use your own Webserver and/or WebRTC implementation there is no need to inherit from `AbstractUWCInitializer`, since it is designed for usage with the default servers.
But you can still extend it and pass in your implementations or call the used UWCController.instance directly:

```c#
class YourUWCInitializer : AbstractUWCInitializer {
    void Initialize() {
        UWCController.Instance.Initialize(
            new YourNetworkDataInterpreter(),
            new YourIWebServerImplementation(),
            new YourIWebRTCServerImplementation()
        );
    }
}

```

If you decide to use your own WebRTC implementation bear in mind that you may also need to adapt or extend parts of the frontend webrtc code located in `webrtc.js`.

### Extend Frontend

On clientside you are free to use your very own frontend design and implementation as interface for controlling with your application.
Either extend the plugins base index.html file or start over from scratch.
All you will need to add to your index.html is the script reference as starting point

```JavaScript
    ...
    <script type="module" src="uwc-initializer"></script>
```

or as inline code

```JavaScript
    <script type="module">
        //your initialization code
    </script>
```

Further resources are requested and loaded through ES6 `import` statements.

For interacting with the `unity-web-control.js` module you will have to get a reference to the `StaticEventDispatcher`.

```JavaScript
import StaticEventDispatcher from "./uwc/static-event-dispatcher.js";
const eventDispatcher = new StaticEventDispatcher();
```

Controlling the application is available through emitting/dispatching events.
Available events are the following:

```JavaScript
"uwc.connect",  //intiate connection
"uwc.sending-enabled", true|false, //enable sending of device data (handlers trigger all the time)
"uwc.send-data", //depends to "uwc.sending-enabled" is called with true - sends device data
"uwc.send-message" //send cusomt messages aside from device sensor data.
```

The configuaration file `config.js` enables you to acticate the neede HTML5 Sensor APIs.

```JavaScript
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
  customFeatureHandlers: { },
  exposeToWindow: true, //See "Using non-ES6 JavaScript code" section
  debug: true
};

export default config;

```

Activating device sensor features is straight forward.
Internally the `feature-detection.js` will check if the browser is capable of retrieving the chosen sensors data and will expose an object `availableFeatures` from the feature detection plugin.

```JavaScript
import {availableFeatures} from './uwc/feature-detection.js';
console.log(availableFeatures);
```

In the [demo project][triangler-mwc] this object is used in `controller-logic.js` to print available sensors to the UI.

A special case is the `vibration` property. To trigger a device vibration, emit a event with the duration in milliseconds.

```JavaScript
    eventDispatcher.emit("features.vibrate", 150);
```

For testing purposes the configurations `debug` flag offers a few advanced functionalitys.
When putitng div tags with specific ids (i.e. `data` and `logger`) to your index.html

```HTML
<!-- index.html -->
<div id="data"></div>
<div id="logger"></div>
```

data, which will be sent to your Unity application, is printed to the `data` div.
The presence of a div with id `logger` redirects `console.log` statements to this div tag.
This is useful for collecting log information on mobile device browsers.
If you are using an Android device and Chrome you can have a look at [Remote Debugging Android Device](https://developers.google.com/web/tools/chrome-devtools/remote-debugging/) to debug on your dekstop but using your mobile browser.

If you need to use your own handler implementation for a HTML5 API features you can specify the handlers with the corresponding property key

```JavaScript
{ //config.js
    ...
    customFeatureHandlers: {
        deviceLight: evt => {
            console.log("deviceLight event received", evt);
        },
        ...
    }
    ...
}
```

The property names are used in camelCase notation, but internally converted to lower case,
since the device API handlers use the lower case notation for (un-)registering the handlers.
This is also true for the `features` declaration in the configuration file.

For further debugging you can use the `StaticEventDispatcher`s registered events and add handlers for the following named events:

```JavaScript
"uwc.initialized",  "uwc.message", "uwc.quit",
("webrt.open", "webrtc.message", "webrtc.close", "webrtc.error")

eventDispatcher.on("uwc.message", message => {console.log("retrieved a message!"); });
```

`"webrtc.*` events are usally triggered from inside the `uwc` module, but can come in handy for debugging reasons.

#### Using non-ES6 JavaScript code

It is not necessary to code your frontend logic with ES6 modules and classes.
If you are willing to use plain old JavaScript code you can add your scripts after your initialization module (`uwc-initializer.js` by default) using this scheme:

```HTML
<!-- index.html -->
<script type="module" src="uwc-initializer.js"></script>
<script defer src="your-legacy-code.js"></script>
```

Important is the `defer` keyword which delays the execution of your script so the ES6 initialization code can run first.
If you are not using `import` statements inside your frontend logic, but you rely on other scripts you will have to load them the manually the same way.

Furthermore you can adapt the `config.js` to expose the necessary UWC features to the browsers window object.

```JavaScript
{ //config.js
    ...
    features: {...},
    exposeToWindow: true,
    ...
}
```

Finally you can access the static event dispatcher and your configuration via the browsers window object.

```JavaScript
var eventDispatcher = window.uwc.eventDispatcher;
var config = window.uwc.config;
```

## Implementation details

As mentioned above the frontend code relies on ES6 modules and classes.
This can cause issues in older browsers due to the lack of support for ES6 modules and classes.
You can have a look at the [browser support table](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Classes#Browser_compatibility) from MDN.

The following part of this section mainly belongs to the default servers and the Unity WebRTC Control implementation for Unity.

In general it is noteworthy that the application uses multiple threads to run the network communication operations.
There is a sperate thread used for the Webserver, serving the web resources.
WebRTC is using multiple threads itself, one for the WebSocket for negotiating the WebRTC connection and one more Thread for each established WebRTC connection.

Since Unity is a single-threaded game engine there may arise some difficulties bringing those different threads into states where they can pass data safely between each other.
This is why you need to use a `Dispatcher` script, which is in charge of collecting the received network data and passing it to Unitys main thread in appropriate situations (when Unitys Update cycle executes).

At the bottom you can find more information on the UWCController class, which can also be used with your custom server implementations.

### SimpleHTTPServer

As implied by the naming the default http server serves the web resource files from the libraries WebResources folder and your local WebResources folder to the webrtc clients.
Its implementation is based on [this approach](https://answers.unity.com/questions/1245582/create-a-simple-http-server-on-the-streaming-asset.html). In addition the IWerbServer interface is implemented.
The SimpleHTTPServer constructor takes 3 arguments, the location of your web resources (relative to Unitys Assets directory), the location of the plugins web resources (relative to Unitys Assets directory) and a port for opening an HTTPListener from the static files will be served.
The webserver will serve your custom files first, but if not present a file with the same name from the plugins web resources may be served.
(This means you can override the behaviour of the frontend code by exchanging the default implementation through your own implementations.)
Internally a thread for serving requests is fired up and the web server exposes the public IP address, which can be accessed via UWCController.Instance.webServerAddress.

### WebRTCServer

Based on radiomans [WebRTC.net](https://github.com/radioman/WebRtc.NET) implementation this class handles the negotiation for the webrtc connection between the Unity application and a web browser. Furthermore it is responsible for retrieving and sending messages via the WebRTC protocol.
By default the JavaScript side makes the offer and requests opening of necessary data channels.
Internally a WebSocket, using the Fleck WebSockets for C# library, starts a thread, which is responsible for the signaling process, determining the suitable methods and parameters for establishing a connection to the web browser.
Each established connection starts up a seperate thread.
Received messages are passed from the WebRTC callbacks to the static UWCController interface.

For debugging and troubleshooting tips see the [Troubleshooting][troubleshooting] section.

From the above mentioned radioman webrtc solution, the javascript frontend implementation acted as a base for the web frontend communication mechanism.

### AbstractUWCInitializer

Useful for intialization on Unity side. Offers the ability to specify server ports and the local web resources folder in Unitys inspector.
If you omit custom server implementatons the default servers (WebServer and WebRTCServer) will be fired up.
This class implements the `OnApplicationQuit` hook of Unity and triggers the servers to clean up their resources when the application is shut down.

You can use this script in every Unity scene (e.g make a GameObject prefab from it), since it is designed as a Unity singleton (DontDestroyOnLoad).
(Make sure to add a `Dispatcher` script to that prefab)

### Dispatcher

The Dispatcher is a useful tool from the [UnityToolbag](https://github.com/nickgravelyn/UnityToolbag) collection for working with mulitple threads inside Unity. You can find more handy scripts on their github page.

### UWCController - UnityWebRTCController

Besides handling messages (sent to and received from the WerbRTC implementation) the static UWCController provides functionality for interpreting the received or to-be-sent messages and initiates the data conversion process.
After preparation of the received messages a simple event system (based on Unitys default event system implementation) collects the registered events and uses the `Dispatcher` script to pass events to Unity.
Sending messages is directly executed on Unitys main thread and guided through from the using application to WebRTCServer.

### QRCodeGenerator

This MonoBehaviour can be used to enhance accessibility for the web clients. A QR Code can be generated from the WebServers public IP address to connect the clients easily.
To use it you have to place a GameObject to your scene which includes an Image and Text component.
Often it makes sense to generate the QRCode just after you initialized your UWCController.
Base implementation comes from [@adrian.n's approach](https://medium.com/@adrian.n/reading-and-generating-qr-codes-with-c-in-unity-3d-the-easy-way-a25e1d85ba51)

## Troubleshooting

Working with Unity and multiple threads often leads to problems hard to track down.
You can help yourself out by using UnityEngine.Debug.Log for logging inside your multithreaded classes.
Usually this call works just fine, but in some cases it is useful to call the Dispatcher first, so you can rely on your logs being collected by Unitys main thread.

```c#
Dispatcher.InvokeAsync(() => {
   UnityEngine.Debug.Log("I will surely retrieve this log executed on another thread.");
});
```

For debugging tips and help on finding problems in your web interface refer to the [Extend Frontend][extend-frontend] section.

If you are missing the .dll files in your remote repository, also check your global git ignore file, which may blacklist .dll libraries.

## Included Libraries

Unity:

- [Flex](https://github.com/statianzo/Fleck) - C# WebSocket implementation for the WebRTC signaling process (MIT License)
- [LitJson](https://github.com/LitJSON/litjson) - JSON intepreter and converter for C# (UniLicense)
- [WebRTC.NET](https://github.com/radioman/WebRtc.NET) - WebRTC implementation for C# (MIT License)

Webfrontend:

- [event-dispatcher](https://github.com/azasypkin/event-dispatcher) - ES6 event system internally used by `StaticEventDispatcher`.
- [adapter.js](https://github.com/webrtchacks/adapter) - shim for generalising WebRTC API across different browsers.

[html5rocks-intro]: https://www.html5rocks.com/en/tutorials/webrtc/basics/
[readme-link]: https://github.com/ein-bandit/unity-webrtc-control
[triangler-mwc]: https://github.com/ein-bandit/triangler-mwc
[ndi-impl]: https://github.com/ein-bandit/triangler-mwc/blob/master/Assets/Scripts/Network/NetworkDataInterpreter.cs
[extend-frontend]: https://github.com/ein-bandit/unity-webrtc-control/blob/master/DOCS.md#extend-frontend
[troubleshooting]: https://github.com/ein-bandit/unity-webrtc-control/blob/master/DOCS.md#troubleshooting
