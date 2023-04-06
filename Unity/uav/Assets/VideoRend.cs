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
    public Canvas canv;
    public RawImage image;
    public GameObject drone;
    public GameObject cube;
    UdpClient client;
    UdpClient client2;
    IPEndPoint endPoint;
    public GPSEncoder GE;
    float interval = 0.2f;
    float nextTime = 0;
    private int count;

    public float lat = 0.0f;
    public float lon = 0.0f;
    public float alt = 0.0f;
    public float lat2 = 0.0f;
    public float lon2 = 0.0f;
    public float alt2 = 0.0f;
    public float w = 0.0f;
    public float x = 0.0f;
    public float y = 0.0f;
    public float z = 0.0f;
    


    void Start()
    {
        client = new UdpClient(8080);
        client2 = new UdpClient(8000);
        endPoint = new IPEndPoint(IPAddress.Any, 0);
        
    }



    //Update is called once per frame
    void Update()
    {


        if (client.Available > 0)
        {


            byte[] data = client.Receive(ref endPoint);
            string text = Encoding.UTF8.GetString(data);
            //Debug.Log(text);
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            lat = (float) Convert.ToDouble(values["lat"]);
            lon = (float) Convert.ToDouble(values["lon"]);
            alt =  (float) Convert.ToDouble(values["alt"]);
            w =  (float) Convert.ToDouble(values["w"]);
            x =  (float) Convert.ToDouble(values["x"]);
            y =  (float) Convert.ToDouble(values["y"]);
            z =  (float) Convert.ToDouble(values["z"]);
            if (values["image"] != null)
            {
                byte[] result = Convert.FromBase64String(values["image"]);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(result);
                image.texture = texture;
            }
            var world_pos = GPSEncoder.GPSToUCS(lat,lon);
            world_pos.y =alt;
            
            Debug.Log(world_pos);

            
            var quat = new Quaternion(x,y,z,w);
            
            // Debug.Log(quat.eulerAngles);
            var ang = GPSEncoder.QuatToEuler(quat);
            var adj_ang = new Vector3();
            adj_ang.x = -1.0f*ang.y;
            Debug.Log(adj_ang.x);
            adj_ang.z = ang.x;
            adj_ang.y = ang.z;
            float tempv = (float)Math.PI/180;
            float c = (90.0f-adj_ang.x) * tempv;
            float tempang = (float)Math.Cos(c);
            float ht = alt/tempang;
            canv.planeDistance = ht+1;
            Debug.Log(ht);

            drone.transform.rotation = Quaternion.Euler(adj_ang);
            Debug.Log(ang);
            drone.transform.position = world_pos; 


        }

        if (client2.Available > 0)
        {


            byte[] data2 = client2.Receive(ref endPoint);
            string text2 = Encoding.UTF8.GetString(data2);
            Debug.Log(text2);
            var values2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(text2);
            lat2 = (float) Convert.ToDouble(values2["lat"]);
            lon2 = (float) Convert.ToDouble(values2["lon"]);
            alt2 =  (float) Convert.ToDouble(values2["alt"]);
            var world_pos2 = GPSEncoder.GPSToUCS(lat2,lon2);
            world_pos2.y =1.0f;
            Instantiate(cube,world_pos2, Quaternion.identity);

        }

        if (Input.GetButtonDown("Fire1"))
        {
           Dictionary<string, string> payload = new Dictionary<string, string>();
           Vector3 mousePos = Input.mousePosition;
           {

               payload.Add("xpos", mousePos.x.ToString());
               payload.Add("ypos", mousePos.y.ToString());
           }
           payload.Add("lat", lat.ToString());
           payload.Add("alt", alt.ToString());
           payload.Add("lon", lon.ToString());
           payload.Add("w", w.ToString());
           payload.Add("x", x.ToString());
           payload.Add("y", y.ToString());
           payload.Add("z", z.ToString());
           
           string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
           byte[] data = Encoding.UTF8.GetBytes(result);
           client.Send(data, data.Length, endPoint);
        }

      


    }
}