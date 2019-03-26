# Unity WebRTC Control

Unity WebRTC Control (UWC) is a plugin for Unity3D which enables low-latency data streaming between a modern web browser and a Unity application using the WebRTC protocol.

The plugin was created to improve accesibility for games by offering an opportunity to use web interfaces as game controllers.

Via WebRTC sensor data from the HTML5 API and any other custom data can be sent from the web client to Unity and vice versa in virtually no time.

# Setup

Clone this repository to your Assets folder or download the unitypackage version from Unity Asset Store.

For the frontend you will have to define a `index.html` as web interface, initialize the necessary modules (handled inside `uwc-initialized.js`) and use the integrated `StaticEventDispatcher` to interact with the `uwc` module.

```HTML
...
<script type="module" src="uwc-initializer.js"></script>
<script type="module">
    import StaticEventDispatcher from './uwc/static-event-dispatcher.js';
    const eventDispatcher = new StaticEventDispatcher();
    //initiate connection via webrtc.
    eventDispatcher.emit("uwc.connect");
    //send a test message after successful initialization.
    eventDispatcher.on("uwc.initialized", message => {
        eventDispatcher.emit("uwc.send-message", {test: "data"});
    });
</script>
```

On server side you need to implement the `INetworkDataInterpreter` interface and pass an instance to UWCController.
(You can extend the `AbstractUWCInitializer`, though this is not a must.)

```c#
class YourUWCInitializer : AbstractUWCInitializer {
    void Start() { //Unitys Start Callback
        Initialize();
    }
    void Initialize() {
        UWCController.Instance.Initialize(
            new YourNetworkDataInterpreter();
        );
    }
}
```

You can find more detailed information on how to use the library in the [documentation][documentation-link] or have a look at the [demo application][triangler-mwc] and see the plugin being used in an actual game project.

## Supported Platforms

Due to the currently used WebRTC implementation, Unity builds are only working with Windows build target. (This will change soon!)

ES6 frontend functionality is limited to Chrome and Opera browser currently.
(Extended cross-browser support in the works!)

## Documentation

Have a look at the [documentation][documentation-link] for indepth implementation details, debugging tips and information on how to handle common issues.

# Support

You can always open an issue including a detailed description and steps to reproduce.

# License

GPL 3.0

**_Free Software, Hell Yeah!_**

[documentation-link]: https://github.com/ein-bandit/unity-webrtc-control/blob/master/DOCS.md
[triangler-mwc]: https://github.com/ein-bandit/triangler-mwc
