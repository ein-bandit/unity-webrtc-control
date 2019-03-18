using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityWebRTCCOntrol.Network;
using ZXing;
using ZXing.QrCode;

namespace UnityWebRTCCOntrol.QRCode
{
    /// <summary>
    /// Generates a QRCode of the connection string available from <see cref="UWCController">.
    /// Relies on presence of a specially provided GameObject where the connection string as text
    /// and the generated image can be set to.
    /// </summary>
    public class QRCodeGenerator : MonoBehaviour
    {
        public int qRCodeWidth = 256;
        public int qRCodeHeight = 256;

        public GameObject qRCodeArea;

        /// <summary>
        /// Generates the QRCode and sets the connection string as text to <c>qRCodeArea</c> Gameobject.
        /// Collects the connection from <see cref="UWCController">.
        /// </summary>
        public void GenerateQRCode()
        {
            if (!qRCodeArea)
            {
                Debug.LogError("A placeholder GameObject for printing the QR Code is not available.");
                return;
            }

            string webServerAddress = UWCController.Instance.webServerAddress;
            if (qRCodeArea.GetComponentInChildren<Image>())
            {
                qRCodeArea.GetComponentInChildren<Image>().material.mainTexture = GenerateQRCode(webServerAddress);
            }
            if (qRCodeArea.GetComponentInChildren<Text>())
            {
                qRCodeArea.GetComponentInChildren<Text>().text = webServerAddress;
            }
        }

        private Texture2D GenerateQRCode(string address)
        {
            var encoded = new Texture2D(qRCodeWidth, qRCodeHeight);
            var color32 = Encode(address, encoded.width, encoded.height);
            encoded.SetPixels32(color32);
            encoded.Apply();
            return encoded;
        }

        private Color32[] Encode(string textForEncoding, int width, int height)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };
            return writer.Write(textForEncoding);
        }
    }
}