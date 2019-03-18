using System;

namespace UnityWebRTCCOntrol.Network.Data
{
    /// <summary>
    /// Holds received input message data in a usable format for the application.
    /// Property <c>type</c> usually refers to <see cref="InputDataType"/>.
    /// </summary>
    public struct InputDataHolder
    {
        public IComparable identifier;

        public System.Enum type;

        public object data;

        public InputDataHolder(IComparable identifier, System.Enum type, object data)
        {
            this.identifier = identifier;
            this.type = type;
            this.data = data;
        }
    }
}