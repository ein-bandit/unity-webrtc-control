using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityWebRTCControl.Network.Data;
using UnityWebRTCControl.Network.WebServer;
using UnityWebRTCControl.Network.WebRTC;

namespace UnityWebRTCControl.Network
{
    /// <summary>
    /// UWCController is a Singelton functioning as Unitys interface to actions on other threads <see cref="IWebServer"/> <see cref="IWebRTCServer"/>.
    /// Uses an async event system <see cref="NetworkDataDispatcher"/> to send received events to Unitys main thread.
    /// Offers methods for handling retrieved WebRTC messages and distributes application messages to <see cref="IWebRTCServer"/> implementation via the <c>Instance</c>.
    /// Discards retrieved messages if clean up process was started.
    /// </summary>
    public class UWCController
    {
        /// <summary>
        /// Holds the public connection string of <see cref="IWebServer"/>.
        /// </summary>
        /// <value>Webserver address as string.</value>
        public string webServerAddress
        {
            get
            {
                return webServer.GetPublicConnectionString();
            }
        }

        private INetworkDataInterpreter interpreter;

        private IWebRTCServer webRTCServer;

        private IWebServer webServer;


        private static UWCController instance;
        public static UWCController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UWCController();
                }
                return instance;
            }
        }

        private bool isAlive = false;

        /// <summary>
        /// Sets local references to the used servers and network data interpreter.
        /// </summary>
        /// <param name="webServer">Used <see cref="IWebserver"/> instance.</param>
        /// <param name="webRTCServer">Used <see cref="IWebRTCServer"/> instance.</param>
        /// <param name="networkDataInterpreter">Used <see cref="InetworkDataInterpreter"/> instance.</param>
        public void Initialize(IWebServer webServer,
                                IWebRTCServer webRTCServer,
                                INetworkDataInterpreter networkDataInterpreter)
        {

            if (isAlive)
            {
                Debug.LogError("UnityWebRTCControl already set up. Aborting initialization.");
                return;
            };

            this.webServer = webServer;
            this.webRTCServer = webRTCServer;
            this.interpreter = networkDataInterpreter;

            isAlive = true;
        }

        public void OnReceiveData(IComparable identifier, string message)
        {
            PassReceivedMessage(
                NetworkEventType.Network_Input_Event,
                interpreter.InterpretInputDataFromJson(identifier, message)
            );
        }

        public void OnRegisterClient(IComparable identifier)
        {
            PassReceivedMessage(
                NetworkEventType.Register_Player,
                interpreter.RegisterClient(identifier)
            );
        }

        public void OnUnregisterClient(IComparable identifier)
        {
            PassReceivedMessage(
                NetworkEventType.Unregister_Player,
                interpreter.UnregisterClient(identifier)
            );
        }

        private void PassReceivedMessage(NetworkEventType eventType, InputDataHolder data)
        {
            if (!isAlive) { return; }
            NetworkEventDispatcher.TriggerEvent(eventType, data);
        }

        /// <summary>
        /// Converts application messages via <see cref="INetworkDataInterpreter"/> instance and 
        /// passes converted message to <see cref="IWebRTCServer"/>.
        /// </summary>
        /// <param name="identifier">The identifier of the client</param>
        /// <param name="type">Type of the application data to send.</param>
        /// <param name="data">Data to be sent.</param>
        public void SendMessageToClient(IComparable identifier, System.Enum type, object data)
        {
            webRTCServer.SendWebRTCMessage(identifier, convertDataForSending(type, data));
        }

        private string convertDataForSending(System.Enum type, object data)
        {
            return interpreter.ConvertOutputDataToJson(type, data);
        }

        /// <summary>
        /// Triggers clean up of used server implementations and clears the <see cref="NetworkEventDispatcher"/> event dictionary.
        /// </summary>
        public void Cleanup()
        {
            isAlive = false;
            NetworkEventDispatcher.ClearEventDictionary();

            webRTCServer.CloseConnection();
            webServer.CloseConnection();
        }
    }
}