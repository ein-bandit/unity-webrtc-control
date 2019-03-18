using System;

namespace UnityWebRTCCOntrol.Network.Data
{
    //identifier (usually guid, but can be any comparable type)
    public interface INetworkDataInterpreter
    {
        InputDataHolder InterpretInputDataFromJson(IComparable identifier, string message);

        InputDataHolder RegisterClient(IComparable identifier);
        InputDataHolder UnregisterClient(IComparable identifier);

        string ConvertOutputDataToJson(Enum outputDataType, object outputData);
    }
}