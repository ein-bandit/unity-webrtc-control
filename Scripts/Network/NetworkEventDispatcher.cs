using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityToolbag;
using UnityWebRTCCOntrol.Network.Data;

namespace UnityWebRTCCOntrol.Network
{
    /// <summary>
    /// Collects received messages as events in format <see cref="InputDataHolder"/>.
    /// Uses <see cref="UnityToolbag.Dispatcher"> for passing events to Unitys main thread.
    /// Offers static methods for registering and unregistering event handlers.
    /// </summary>
    public class NetworkEventDispatcher
    {
        private class AsyncEvent : UnityEvent<InputDataHolder> { }

        private Dictionary<NetworkEventType, AsyncEvent> eventDictionary;

        private static NetworkEventDispatcher networkEventDispatcher;

        //Implements singleton pattern for internal usage, 
        //public access is granted through static methods only.
        private static NetworkEventDispatcher instance
        {
            get
            {
                if (networkEventDispatcher == null)
                {
                    networkEventDispatcher = new NetworkEventDispatcher();

                    networkEventDispatcher.InitializeEventDictionary();
                }

                return networkEventDispatcher;
            }
        }

        private void InitializeEventDictionary()
        {
            if (eventDictionary == null)
            {
                eventDictionary = new Dictionary<NetworkEventType, AsyncEvent>();
            }
        }

        public static void StartListening(NetworkEventType eventType, UnityAction<InputDataHolder> listener)
        {
            AsyncEvent thisEvent = null;
            if (instance.eventDictionary.TryGetValue(eventType, out thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new AsyncEvent();
                thisEvent.AddListener(listener);
                instance.eventDictionary.Add(eventType, thisEvent);
            }
        }

        public static void StopListening(NetworkEventType eventType, UnityAction<InputDataHolder> listener)
        {
            AsyncEvent thisEvent = null;
            if (instance.eventDictionary.TryGetValue(eventType, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        /// <summary>
        /// This method is called from a WebRTC thread from <see cref="UWCController">. 
        /// Uses <see cref="UnityToolbag.Dispatcher.InvokeAsync()"/> 
        /// to wait for Unitys main thread to be ready before posting events.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="data">Converted input data from client.</param>
        public static void TriggerEvent(NetworkEventType eventType, InputDataHolder data)
        {
            AsyncEvent thisEvent = null;
            if (instance.eventDictionary.TryGetValue(eventType, out thisEvent))
            {
                Dispatcher.InvokeAsync(() =>
                {
                    thisEvent.Invoke(data);
                });
            }
        }

        public static void ClearEventDictionary()
        {
            instance.eventDictionary.Clear();
        }
    }
}