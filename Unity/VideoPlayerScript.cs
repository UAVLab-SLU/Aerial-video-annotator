
using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.Video;

public class VideoPlayerScript : MonoBehaviour
{
    public UnityEngine.Video.VideoPlayer videoPlayer;
    UdpClient client;
    // Start is called before the first frame update
    void Start()
    {
        client = new UdpClient(8000);
        this.videoPlayer.targetCameraAlpha = 1F;
        //this.videoPlayer.url = "C:/Users/rushi/Desktop/Research/test-3-angle.mp4";
        this.videoPlayer.frame = 0;

        this.videoPlayer.skipOnDrop = true;

        // Restart from beginning when done.
        this.videoPlayer.isLooping = false;
        this.videoPlayer.Play();
        StartCoroutine(ReceiveData());
    }

    private IEnumerator ReceiveData()
    {
        while (true)
        {
            // print("CC");
            if (client.Available > 0)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref endPoint);
                // do something with the received data
                string text = Encoding.UTF8.GetString(data);
                print("iii");
                print(text);

                Texture2D texture = new Texture2D(360, 360, TextureFormat.RGB24, false);
                //Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(data);
                RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 0);
                Graphics.Blit(texture, renderTexture);
                // Add the render texture to the video player
                this.videoPlayer.targetTexture = renderTexture;
                //this.videoPlayer.Play();

                //  
                //texture.LoadRawTextureData(data);
                //texture.Apply();
                //RenderTexture.active = renderTexture;
                //Graphics.Blit(texture, renderTexture);
                //RenderTexture.active = null;
            }
            yield return null;
        }
    }


}
