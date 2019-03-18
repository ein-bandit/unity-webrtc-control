namespace UnityWebRTCCOntrol.Network.WebServer
{
    /// <summary>
    /// Interface for using a WebServer with UWCController.
    /// </summary>
    public interface IWebServer
    {
        /// <summary>
        /// Provides a publicly accessible connection string where files are served from.
        /// </summary>
        /// <returns>Connection string as string.</returns>
        string GetPublicConnectionString();

        /// <summary>
        /// Cleans up resources and closes the connection.
        /// </summary>
        void CloseConnection();
    }
}
