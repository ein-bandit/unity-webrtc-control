using System;

namespace UnityWebRTCCOntrol.Network.Data
{
    public class InputDataHolder
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