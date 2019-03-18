using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UnityWebRTCCOntrol.Network.WebServer
{
    //inspired by: https://answers.unity.com/questions/1245582/create-a-simple-http-server-on-the-streaming-asset.html
    class SimpleHTTPServer : IWebServer
    {
        private readonly string _indexFile = "index.html";

        private static IDictionary<string, string> _mimeTypeMappings;
        private Thread _serverThread;
        private string _userDirectory;
        private string _rootDirectory;
        private HttpListener _listener;

        private int _port;
        private string _hostAddress;


        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="userPath">Directory path to serve.</param>
        /// <param name="rootPpath">Directory path to serve if not found in user path.</param>
        /// <param name="port">Port of the server.</param>
        public SimpleHTTPServer(string userPath, string rootPath, int port)
        {
            this.Initialize(userPath, rootPath, port);
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (ThreadAbortException exception)
                {
                    //nothing to do here. Thread.Abort throws exception. Initiated from Unitys OnApplicationQuit
                    UnityEngine.Debug.Log($"Successfully aborted webserver thread - catched {exception.GetType()}.");
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.Log(exception);
                }
            }
        }

        public string GetPublicIPAddress()
        {
            return "http://" + _hostAddress + ":" + _port;
        }

        private string GetPathToFile(string filename)
        {
            string userFilePath = Path.Combine(_userDirectory, filename);
            string rootFilePath = Path.Combine(_rootDirectory, filename);

            if (userFilePath.Length > filename.Length && File.Exists(userFilePath))
            {
                return userFilePath;
            }
            else if (File.Exists(rootFilePath))
            {
                return rootFilePath;
            }
            return null;
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                filename = _indexFile;
            }

            string pathToFile = GetPathToFile(filename);

            if (pathToFile != null)
            {
                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    Stream input = new FileStream(pathToFile, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(pathToFile), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(pathToFile).ToString("r"));

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.OutputStream.Flush();
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    UnityEngine.Debug.Log(exception);
                }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
        }

        private void Initialize(string userPath, string rootPath, int port)
        {
            InitMimeTypeMappingsDictionary();
            this._userDirectory = userPath;
            this._rootDirectory = rootPath;
            this._port = port;
            this._hostAddress = FindIPAddress();
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }

        private string FindIPAddress()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    return addr.ToString();
                }
            }
            //return localhost if no valid address found.
            return IPAddress.Loopback.ToString();
        }

        public void CloseConnection()
        {
            _listener.Stop();
            _serverThread.Abort();
        }

        private void InitMimeTypeMappingsDictionary()
        {
            _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                     {
                             { ".asf", "video/x-ms-asf" },
                             { ".asx", "video/x-ms-asf" },
                             { ".avi", "video/x-msvideo" },
                             { ".bin", "application/octet-stream" },
                             { ".cco", "application/x-cocoa" },
                             { ".crt", "application/x-x509-ca-cert" },
                             { ".css", "text/css" },
                             { ".deb", "application/octet-stream" },
                             { ".der", "application/x-x509-ca-cert" },
                             { ".dll", "application/octet-stream" },
                             { ".dmg", "application/octet-stream" },
                             { ".ear", "application/java-archive" },
                             { ".eot", "application/octet-stream" },
                             { ".exe", "application/octet-stream" },
                             { ".flv", "video/x-flv" },
                             { ".gif", "image/gif" },
                             { ".hqx", "application/mac-binhex40" },
                             { ".htc", "text/x-component" },
                             { ".htm", "text/html" },
                             { ".html", "text/html" },
                             { ".ico", "image/x-icon" },
                             { ".img", "application/octet-stream" },
                             { ".svg", "image/svg+xml" },
                             { ".iso", "application/octet-stream" },
                             { ".jar", "application/java-archive" },
                             { ".jardiff", "application/x-java-archive-diff" },
                             { ".jng", "image/x-jng" },
                             { ".jnlp", "application/x-java-jnlp-file" },
                             { ".jpeg", "image/jpeg" },
                             { ".jpg", "image/jpeg" },
                             { ".js", "application/x-javascript" },
                             { ".mml", "text/mathml" },
                             { ".mng", "video/x-mng" },
                             { ".mov", "video/quicktime" },
                             { ".mp3", "audio/mpeg" },
                             { ".mpeg", "video/mpeg" },
                             { ".mp4", "video/mp4" },
                             { ".mpg", "video/mpeg" },
                             { ".msi", "application/octet-stream" },
                             { ".msm", "application/octet-stream" },
                             { ".msp", "application/octet-stream" },
                             { ".pdb", "application/x-pilot" },
                             { ".pdf", "application/pdf" },
                             { ".pem", "application/x-x509-ca-cert" },
                             { ".pl", "application/x-perl" },
                             { ".pm", "application/x-perl" },
                             { ".png", "image/png" },
                             { ".prc", "application/x-pilot" },
                             { ".ra", "audio/x-realaudio" },
                             { ".rar", "application/x-rar-compressed" },
                             { ".rpm", "application/x-redhat-package-manager" },
                             { ".rss", "text/xml" },
                             { ".run", "application/x-makeself" },
                             { ".sea", "application/x-sea" },
                             { ".shtml", "text/html" },
                             { ".sit", "application/x-stuffit" },
                             { ".swf", "application/x-shockwave-flash" },
                             { ".tcl", "application/x-tcl" },
                             { ".tk", "application/x-tcl" },
                             { ".txt", "text/plain" },
                             { ".war", "application/java-archive" },
                             { ".wbmp", "image/vnd.wap.wbmp" },
                             { ".wmv", "video/x-ms-wmv" },
                             { ".xml", "text/xml" },
                             { ".xpi", "application/x-xpinstall" },
                             { ".zip", "application/zip" },
                     };
        }
    }
}