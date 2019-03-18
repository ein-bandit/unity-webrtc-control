using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityToolbag;
using UnityWebRtCControl.Network.Data;

namespace UnityWebRtCControl.Network
{
    public class NetworkEventDispatcher
    {
        //Runs on the Network Thread and collects events from 
        //using Dispatcher.cs to wait for Unity Main Thread to execute Update. 
        //using a special unityevent inside manager.
        private class AsyncEvent : UnityEvent<InputDataHolder> { }

        private Dictionary<NetworkEventType, AsyncEvent> eventDictionary;

        private static NetworkEventDispatcher eventManager;

        public static NetworkEventDispatcher instance
        {
            get
            {
                if (eventManager == null)
                {
                    eventManager = new NetworkEventDispatcher();

                    eventManager.Init();
                }

                return eventManager;
            }
        }

        void Init()
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

        //trigger event is called from another thread when data is received.
        //with the dispatcher triggerEvent waits for unity to be ready and sends all events on update immediately.
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