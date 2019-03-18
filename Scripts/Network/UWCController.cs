using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityWebRTCCOntrol.Network.Data;
using UnityWebRTCCOntrol.Network.WebServer;
using UnityWebRTCCOntrol.Network.WebRTC;

namespace UnityWebRTCCOntrol.Network
{

    //is unitys interface to actions on other threads (webserver, webrtcserver).
    //uses an async event system to wait for unitys thread to be ready and dispatch the retrieved network events.

    //put this script on a gameobject that is a singleton ie. has a class as singleton already.
    //starts the server (which is on another thread) and registers callbacks.
    //therefore events are collected and distributed in next update.
    public class UWCController
    {
        public string webServerAddress
        {
            get
            {
                return webServer.GetPublicIPAddress();
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

        public void Initialize(IWebServer httpServer,
                                IWebRTCServer webRTCServer,
                                INetworkDataInterpreter networkDataInterpreter)
        {

            if (isAlive)
            {
                Debug.LogError("UnityWebRtCControl was already set up. Aborting initialization.");
                return;
            };

            this.webServer = httpServer;
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
            if (!isAlive) return;
            NetworkEventDispatcher.TriggerEvent(eventType, data);
        }

        public void SendMessageToClient(IComparable identifier, System.Enum type, object data)
        {
            webRTCServer.SendWebRTCMessage(identifier, convertDataForSending(type, data));
        }

        private string convertDataForSending(System.Enum type, object data)
        {
            return interpreter.ConvertOutputDataToJson(type, data);
        }

        public void Cleanup()
        {
            isAlive = false;
            NetworkEventDispatcher.ClearEventDictionary();

            webRTCServer.CloseConnection();
            webServer.CloseConnection();
        }
    }
}