using System;

namespace UnityWebRTCControl.Network.Data
{
    /// <summary>
    /// Interprets retrieved WebRTC message and converts it to <see cref="InputDataHolder"/> 
    /// for further processing in the application.
    /// Should internally use <see cref="InputDataType"/> to identify data type but must not be limited to that enumeration.
    /// </summary>
    public interface INetworkDataInterpreter
    {
        /// <summary>
        /// Converts input data from Json string to <see cref="InputDataHolder"/>.
        /// </summary>
        /// <param name="identifier">Identifier of the sending client.</param>
        /// <param name="message">Retrieved message as Json string.</param>
        /// <returns>Returns the converted data.</returns>
        InputDataHolder InterpretInputDataFromJson(IComparable identifier, string message);

        /// <summary>
        /// Converts client register message to <see cref="InputDataHolder"/>.
        /// </summary>
        /// <param name="identifier">Identifier of the sending client.</param>
        /// <returns>Returns the converted data.</returns>
        InputDataHolder RegisterClient(IComparable identifier);

        /// <summary>
        /// Converts client unregister message to <see cref="InputDataHolder"/>.
        /// </summary>
        /// <param name="identifier">Identifier of the sending client.</param>
        /// <returns>Returns converted data.</returns>
        InputDataHolder UnregisterClient(IComparable identifier);

        /// <summary>
        /// Converts an application message to Json string.
        /// </summary>
        /// <param name="outputDataType">Type of data to be sent.</param>
        /// <param name="outputData">Data to be sent.</param>
        /// <returns>Returns data converted to Json string.</returns>
        string ConvertOutputDataToJson(Enum outputDataType, object outputData);
    }
}