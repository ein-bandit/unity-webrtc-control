# Unity WebRTC Control

Unity WebRTC Control is a library offering an easy way to establish a WebRTC communication between web browsers and Unity.
It's focus lies on using modern web technology, especially HTML5 device APis, to create low latency and easy accessible game controllers.
However, you can use this library to stream any kind of data between your Unity application and a modern web browser.

Included in this pacakage is a HTTP weberver, a WebRTC server and a threadsafe event system implementation for receiving and sending e.g. streaming fast and reliable low-latency data messages.

Want a .unitypackage version of the plugin to integrate hasslefree to your project? [Find it here](unity asset store / unity-webrtc-control)
Learn more about WebRTC and it's capabilities? [Try this one](webrtc guide)

## How to use it

Include this package in your Unity application. (Either use the provieded unitypackage version or clone this repository / download the source files and paste them to your project)
\_Implement a message interpreter (using the INetworkDataInterpreter interface) and extend the abstract UWCInitializer, which you put together with a (Event-)Dispatcher to your scene(s).
\_Create a HTML Controller interface and a JavaScript configuration file to be able to use the HTML5 device APIs and communicate with your application.
\_Register handlers on UWCController.Instance to receive messages in your application and use UWCController.Instance.SendMessage to distribute data to the web clients.
More details can be found in the [Setup](#Setup) and [Design your frontend](#Design-your-frontend) sections.
A demo project which is showing the integration of this plugin is available [here](link to demo project). (Have a look at Assets/Scripts/Network/ for the Unity application code and Assets/WebResources for the frontend implementation)

## Setup

Firstly, if you want to use the default server implementations, you need to create a script extending the abstract UWCInitializer, which will set up the network communication with your network data interpreter.
Internally your implementation will be converted to a Unity singleton (DontDestroyOnLoad) and will call the clean up methods of used server implementations automatically when unity your application is shutting down.
Preferably you can use Unitys Start method to initialize the UnityWebRTCController but you can also call the UWCController instance at any other point of time which fits for your applications needs.
Keep in mind that internally a webserver and a websocket along with the necessary threads are fired up, so this may consume a little performance.

For your Initializer you will also need to pass a network data interpreter implementation which will parse the message data sent from the frontend and prepare messages to be sent to the browser(s).

If no custom server implementatons are provided the default servers will be fired up.

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

You can get some inspiration from [this](add link to interpreater) implementation inside the demo project.
You are free to use whatever code you need from there, but coming up with your very own approach is perfectly fine as well.

Secondly create a GameObject on your entry scene and add YourUWCInitializer as well as the Dispatcher script from the Unity-WebRTC-Control/Network folder to it.
Your Initializer will expose some Properties from the abstract base class to Unitys inspector allowing you to specify webserver and webRTC ports, as well as the directory you placed your web resources.
The default webserver implementation will serve your custom files first, but if not present a file with the same name from the plugins web resources may be served.
You can use your Initializer in all scenes, since it is designed as (Unity) singleton the initialization will be executed only once. Moreover the UWCController is aware of its internal state and aborts further calls to Initialize if setup was already executed previously.
When the application is shut down, the OnApplicationQuit hook of the abstract base class gets executed and calls the CleanUp methods of the server implementations.

a screenshot of how it is added to unity.

Instructions on how to prepare your frontend are stated in the section [Design your interface](#Design-your-interface)

## Design your interface

On clientside you are free to use your very own frontend design and implementation as web frontend for interacting with your application.
From the base index.html file (located in Unity-WebRTC-Control/WebResources) or from [demo project](https://github.com/ein-bandit/triangler-mwc)s index.html you will need to add the referenced scripts your HTML file.
Specifically you will need the webrtc.js and unity-webrtc-control.js files from the WebResources folder.
As mentioned above a javascript object 'config' needs to be present in the global scope, holding an array of activated features and a port configuration for the webrtc connection.
For testing you can use the debug mode by setting a variable of type bool named debug to true.
If a <div id="data" /> is present in your html file, data which will be sent to your Unity application will be printed onto it.
Furthermore you can use console-to-div.js logging extender to your HTML file. This handy script passes all console.log calls to a <div id="logger" /> if present. This is especially useful if you are using a native mobile device web browser.

unity-webrtc-control.js initializeConnection function requires an object to be passed to it which handles the messages from the webrtc library.
You can pass in an object with the same signature as the following one, exchanging the functions with your needed implementations.

```JavaScript
var clientActions = {
  initialize: function() {},
  onMessage: function(message) {},
  onError: function(error) {},
  onClose: function() {}
};
```

... and you are ready to go.

## Further customization

If you are willing to use your own Webserver and/or WebRTC implementation there is no need to inherit from AbstractUWCInitializer. You can call the static UWCController instance and it's Initialize method from anywhere else in your application and pass in your custom implementations.

To use a custom servers you need to:

- implement the IWebServer interface
- implement the IWebRTC interface
- Pass instances together with your data interpreter to UWCController

If you decide to use your own WebRTC implementation bear in mind that you may also need to adapt or extend parts of the frontend webrtc code.

## Implementation details

This section mainly belongs to the default servers.
At the bottom you can find more information on the UWCController class, which can also be used with your custom server implementations.

### SimpleHTTPServer

As the name mentions is a simple http server which serves the web resource files from the library and your frontend to the webrtc clients.
Its implementation is based on [this approach](https://answers.unity.com/questions/1245582/create-a-simple-http-server-on-the-streaming-asset.html) and implements the IWerbServer interface.
The SimpleHTTPServer constructor takes 3 arguments, the location of your web resources (relative to Unitys application code directory), the location of the plugins web resources (relative to Unitys application code directory) and a port for opening a websocket connection.
The HTTPListener implementation is used from the .NET library System.Net.
Internally a thread for seving the requests is fired up and the class internally determines the IP address which can be accessed via UWCController.instance.webServerAddress.

### WebRTCServer

Based on radiomans [WebRTC.net](https://github.com/radioman/WebRtc.NET) implementation this class handles the negotiation for the webrtc connection between the Unity application and a web browser.
By default the JavaScript side makes the offer and requests opening of necessary channels.
Internally a WebSocket, using the Fleck WebSockets for C# library, starts another thread and is responsible for the signaling process, determining the suitable methods for establishing a connection to the web browser.
Received messages are passed from the WebRTC callbacks, each WebRTC session (for a distinct client) runs on a seperate thread as well, are passed via the static UWCController instance the this plugin.
For debugging and troubleshooting tips also see section [Troubleshooting](#Troubleshooting)

From the above mentioned radioman webrtc solution also the javascript implementation acts as the base for the web frontend communication mechanism.

### UnityWebRTCController

Besides handling webrtc messages (sending and receiving) the static UWCController provides functionality for starting the message interpretation and conversion process, from browser to Unity and vice versa.
After preparation of the message a simple event system (based on Unitys default event system implementation) collects the registered events and uses a Dispatcher from [UnityToolbag](https://github.com/nickgravelyn/UnityToolbag) to pass events, received from secondary threads, to Unity main thread. This happens inside Unitys Update cycle.
Sending messages is directly executed on Unitys main thread and guided through from the using application to WebRTCServer.

When the Unity application is shut down, the UWCController requests a CleanUp (e.g. closing all threads) on the registered server instances and rejects further retrieved messages.

### QRCodeGenerator

This MonoBehaviour can be used to generate an easy accesible QR Code to connect your (mobile devices).
To use it you have to place a GameObject to your scene which includes an Image and Text component.
Make sure to call the QRCodeGenerator from a place where it is also accessible i.e. the correct scene it is placed on.
Often it makes sense to generate the QRCode just after you initialized your UWCController.
Base implementation comes from [@adrian.n's approach](https://medium.com/@adrian.n/reading-and-generating-qr-codes-with-c-in-unity-3d-the-easy-way-a25e1d85ba51)

## Troubleshooting

Working with Unity and multiple threads often leads to problems hard to track down.
You can help yourself out by using UnityEngine.Debug.Log for logging inside your multithreaded classes. In some cases it is useful to call the Dispatcher so you can rely on your logs being collected by Unitys main thread.

As mention in the [Design your interface](#Design-your-interface) section also the frontend comes with a few handy options to help you debug and troubleshoot the application, specifically the frontend part.

Setting a debug object with value true on the globa scope offers printing to-be-sent data a div with id 'data'.
Adding the console-to-div.js file offers functionalty to log console.log messages to the screen. (useful for mobile browsers)
A div with id logger (<div id="logger"></div>) needs to be present on the DOM.
Bear in mind that the logging to the div does not include extra objects sent to console.log. Add your data you want to visualize as string and append it to the message.

## Included Libraries

- [Flex](https://github.com/statianzo/Fleck) - C# WebSocket implementation for the WebRTC signaling process (MIT License)
- [LitJson](https://github.com/LitJSON/litjson) - JSON intepreter and converter for C# (UniLicense)
- [WebRTC.NET](https://github.com/radioman/WebRtc.NET) - WebRTC implementation for C# (MIT License)

Missing some .dll files? Check your global git ignore file if it blacklists dll libraries.

# Support

You can always open an issue including a detailed description and steps to reproduce.
If you like this lib consider buying me a coffee :)
[crypto address tba]

# License

MIT

**_Free Software, Hell Yeah!_**
