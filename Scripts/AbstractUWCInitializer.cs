using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityWebRTCControl.Network;
using UnityWebRTCControl.Network.Data;
using UnityWebRTCControl.Network.WebRTC;
using UnityWebRTCControl.Network.WebServer;

namespace UnityWebRTCControl
{
    /// <summary>
    /// Abstract Initializer as Unity singleton holding adjustable parameters for setting up default servers.
    /// Offering a <see cref="InitializeUWC()"/> method for setup and initiates application cleanup 
    /// on Unity application quit.
    /// </summary>
    public abstract class AbstractUWCInitializer : MonoBehaviour
    {
        [Header("Default Server Configuration")]
        public int httpServerPort = 8880;
        public string webResourcesFolder;
        public int webRTCServerPort = 7770;

        private readonly string standardResourcesFolder = "Unity-WebRTC-Control/WebResources";

        private IWebServer webServer;
        private IWebRTCServer webRTCServer;

        private static AbstractUWCInitializer instance = null;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// Initializes <see cref="UWCController"/>  with passed references.
        /// If server implementations are omitted, default servers will be set up.
        /// </summary>
        /// <param name="networkDataInterpreter">Network data interpreter as implementation of <see cref="INetworkDataInterpreter"/>.</param>
        /// <param name="webserver">Web server as implementation of <see cref="IWebServer"/>.</param>
        /// <param name="webRTCServer">WebRTC server as implementation of <see cref="IWebRTCServer"/>.</param>
        protected void InitializeUWC(
            INetworkDataInterpreter networkDataInterpreter,
            IWebServer webserver = null,
            IWebRTCServer webRTCServer = null)
        {
            if (webserver == null)
            {
                webServer = new SimpleHTTPServer(
                    GetFullPath(webResourcesFolder),
                    GetFullPath(standardResourcesFolder),
                    httpServerPort);
            }
            if (webRTCServer == null)
            {
                webRTCServer = new WebRTCServer(webRTCServerPort);
            }

            UWCController.Instance.Initialize(this.webServer, this.webRTCServer, networkDataInterpreter);
        }

        private string GetFullPath(string relativePath)
        {
            return Path.Combine(Application.dataPath, relativePath);
        }

        private void OnApplicationQuit()
        {
            UWCController.Instance.Cleanup();
        }
    }
}