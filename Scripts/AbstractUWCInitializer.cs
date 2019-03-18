using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityWebRtCControl.Network;
using UnityWebRtCControl.Network.Data;
using UnityWebRtCControl.Network.WebRTC;
using UnityWebRtCControl.Network.WebServer;

namespace UnityWebRtCControl
{
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
        void Awake()
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

            UWCController.Instance.Initialize(webServer, webRTCServer, networkDataInterpreter);
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