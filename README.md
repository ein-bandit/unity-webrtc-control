# Unity WebRTC Control

Unity WebRTC Control (UWC) is a plugin for Unity3D which enables you to stream data from a modern web browser to your Unity application via the low-latency WebRTC protocol.

The plugin was created to improve accesibility for games by using web interfaces as game controllers.
Via WebRTC sensor data from the HTML5 API and any other custom data can be sent from the web client to Unity and vice versa in virtually no time.

# Setup

Clone this repository to your Assets folder or download the unitypackage version from Unity Asset Store.

You'll have setup a web interface und use the included event system to send messages to your application.

```HTML
...
<script type="module" src="uwc-initializer.js"></script>
<script type="module">
    import StaticEventDispatcher from './uwc/static-event-dispatcher.js';
    const eventDispatcher = new StaticEventDispatcher();
    //initiate connection via webrtc.
    eventDispatcher.emit("uwc.connect");
    //send a test message after successful intialization.
    eventDispatcher.on("uwc.initialized", message => {
        eventDispatcher.emit("uwc.send-data", {test: "data"});
    });
</script>
```

If you don't want to use ES6 modules and classes for your frontend logic have a look at the [documentation#legacy-frontend-code](documentation-frontend)

On server side you need to implement the `INetworkDataInterpreter` interface and pass an instance to UWCController.
(You can extend the `AbstractUWCInitializer`, though this is not a must.)

```c#
class YourUWCInitializer : AbstractUWCInitializer {
    void Start() { //Unitys Start Callback.
        Initialize();
    }
    void Initialize() {
        UWCController.Instance.Initialize(
            new YourNetworkDataInterpreter();
        );
    }
}
```

You can find more detailed information on how to use the library in the [documentation](documentation-link) or you can have a look at the [demo application](triangler-mwc) and see the plugin being used in a game project.

# Supported Platforms

Due to the currently used WebRTC implementation, Unity builds are only working with Windows build target. (This will change soon!)

ES6 frontend functionality is limited to Chrome and Opera browser currently.
(Extended cross-browser support in the works!)

# Troubleshooting

Have a look at the [documentation](documentation) for debugging tips and common issues.

# Support

You can always open an issue including a detailed description and steps to reproduce.

# License

GPL 3.0

**_Free Software, Hell Yeah!_**

[documentation-link]: https://github.com/ein-bandit/unity-webrtc-control/blob/master/DOCS.md
[documentation-frontend]: https://github.com/ein-bandit/unity-webrtc-control/blob/master/DOCS.md#legacy-frontend-code
[triangler-mwc]: https://github.com/ein-bandit/triangler-mwc
