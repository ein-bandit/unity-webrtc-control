namespace UnityWebRTCCOntrol.Network.WebServer
{
    public interface IWebServer
    {
        string GetPublicIPAddress();
        void CloseConnection();
    }
}
