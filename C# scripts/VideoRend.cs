
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;


public class VideoRend : MonoBehaviour
{
    // Start is called before the first frame update
    public RawImage image;
    UdpClient client;
    float interval = 0.05f;
    float nextTime = 0;
    private int count;
    void Start()
    {
        count = 0;
        client = new UdpClient(8000);

    }



    //Update is called once per frame
    void Update()
    {

        //if (Time.time >= nextTime)
        //{
        //    Texture2D texture = new Texture2D(2, 2);
        //    string path = "C:/Users/rushi/Desktop/Research/frames20/frame_" + count.ToString() + ".jpg";
        //    byte[] imageData = File.ReadAllBytes(path);
        //    texture.LoadImage(imageData);
        //    image.texture = texture;
        //    Debug.Log(path);
        //    nextTime += interval;
        //    count += 1;
        //}

        if (client.Available > 0)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] data = client.Receive(ref endPoint);
            string text = Encoding.UTF8.GetString(data);
            File.WriteAllBytes("C:/Users/rushi/Desktop/Research/temp/1.jpg", data);
            Debug.Log(text);
            //MemoryStream ms = new MemoryStream();
            //ms.Write(data, 0, data.Length);

            if (data != null)
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(data);
                //File.WriteAllBytes("C:/Users/rushi/Desktop/Research/temp/1.jpg", data);
                image.texture = texture;
            }
        }

    }
}
