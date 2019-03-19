using System;

namespace UnityWebRTCControl.Network.WebRTC
{
    /// <summary>
    /// Negotiates (signaling) and establishs a WebRTC connection with a client.
    /// </summary>
    public interface IWebRTCServer
    {
        /// <summary>
        /// Handles sending of a message to a client using WebRTC protocol.
        /// </summary>
        /// <param name="identifier">identifier for the client</param>
        /// <param name="message">message as string</param>
        void SendWebRTCMessage(IComparable identifier, string message);
        /// <summary>
        /// Cleans up local resources and closes open connections.
        /// </summary>
        void CloseConnection();
    }
}
