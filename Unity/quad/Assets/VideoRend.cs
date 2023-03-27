
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

public class VideoRend : MonoBehaviour
{
    // Start is called before the first frame update
    public RawImage image;
    UdpClient client;
    IPEndPoint endPoint;
    float interval = 0.2f;
    float nextTime = 0;
    private int count;

    public int xp = 0;
    public int yp = 0;
    public int zp = 0;
    void Start()
    {
        client = new UdpClient(8000);
        endPoint = new IPEndPoint(IPAddress.Any, 0);

    }



    //Update is called once per frame
    void Update()
    {

        //if (Time.time >= nextTime)
        //{
        //    Texture2D texture = new Texture2D(2, 2);
        //    string path = "C:/Users/rushi/Desktop/Research/frames20/frame_" + count.ToString() + ".jpg";
        //    byte[] imageData = File.ReadAllBytes(path);
        //    string test = Convert.ToBase64String(imageData); // "HsIAAA=="
        //    byte[] result = Convert.FromBase64String(test);
        //    texture.LoadImage(result);
        //    image.texture = texture;
        //    Debug.Log(path);
        //    nextTime += interval;
        //    count += 1;
        //    //Camera.main.transform.position = new Vector3(xp, yp, zp);
        //    //xp += 5;
        //    //yp += 5;
        //    //zp += 5;
        //}

        if (client.Available > 0)
        {
           

            byte[] data = client.Receive(ref endPoint);
            string text = Encoding.UTF8.GetString(data);
            //Debug.Log(text);
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            //Debug.Log(values["image"]);
            if (values["image"] != null)
            {
                byte[] result = Convert.FromBase64String(values["image"]);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(result);
                image.texture = texture;
            }
            xp = Int32.Parse(values["lat"]);
            yp = Int32.Parse(values["alt"]);
            zp = Int32.Parse(values["lon"]);
            Camera.main.transform.position = new Vector3(xp, yp, zp);


        }

        if (Input.GetButtonDown("Fire1"))
        {
            Dictionary<string, string> payload = new Dictionary<string, string>();
            Vector3 mousePos = Input.mousePosition;
            {
               
                payload.Add("xpos", mousePos.x.ToString());
                payload.Add("ypos", mousePos.y.ToString());               
            }
            payload.Add("lat", mousePos.y.ToString());
            payload.Add("alt", yp.ToString());
            payload.Add("lon", zp.ToString());
            string result = string.Join(",", payload.Select(x => '"'+x.Key+'"' + ": " + '"' + x.Value + '"'));
            byte[] data = Encoding.UTF8.GetBytes(result);
            client.Send(data, data.Length, endPoint);
        }

        //if (client.Available > 0)
        //{
        //    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        //    byte[] data = client.Receive(ref endPoint);

        //    if (data != null)
        //    {
        //        Texture2D texture = new Texture2D(2, 2);
        //        texture.LoadImage(data);
        //        //File.WriteAllBytes("C:/Users/rushi/Desktop/Research/temp/1.jpg", data);
        //        image.texture = texture;
        //    }
        //}



    }
}
