using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using Newtonsoft.Json;

public class VideoRend : MonoBehaviour//,IPointerEnterHandler
{
    // Start is called before the first frame update
    public Canvas canv;
    public RawImage image;
    public GameObject drone;
    public GameObject obj1;
    public GameObject obj2;
    public GameObject obj3;
    UdpClient client;
    UdpClient client2;
    IPEndPoint endPoint;
    public GPSEncoder GE;
    private int count;

    float lat = 0.0f;
    float lon = 0.0f;
    float alt = 0.0f;
    float lat2 = 0.0f;
    float lon2 = 0.0f;
    float alt2 = 0.0f;
    float w = 0.0f;
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;

    private Vector3 adj_ang;
    string selectedButton;
    
    public Button green;
    public Button blue;
    public Button red;



    

    void Start()
    {
        client = new UdpClient(8080);
        client2 = new UdpClient(8000);
        endPoint = new IPEndPoint(IPAddress.Any, 0);
        green.onClick.AddListener(GreenButton);
        blue.onClick.AddListener(BlueButton);
        red.onClick.AddListener(RedButton);
        
        selectedButton = "green";
        green.interactable=false;

       
    }



    //Update is called once per frame
    void Update()
    {


       
        // UdpConnection to get frame and metadata
        if (client.Available > 0)
        {
            byte[] data = client.Receive(ref endPoint);
            string text = Encoding.UTF8.GetString(data);
            //Debug.Log(text);
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            // lat = (float) Convert.ToDouble(values["lat"]);
            // lon = (float) Convert.ToDouble(values["lon"]);
            // alt =  (float) Convert.ToDouble(values["alt"]);
            // w =  (float) Convert.ToDouble(values["w"]);
            // x =  (float) Convert.ToDouble(values["x"]);
            // y =  (float) Convert.ToDouble(values["y"]);
            // z =  (float) Convert.ToDouble(values["z"]);
            if (values["image"] != null)
            {
                byte[] result = Convert.FromBase64String(values["image"]);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(result);
                image.texture = texture;
            }
            var world_pos = GPSEncoder.GPSToUCS(lat,lon);
            world_pos.y =alt;
            
            // Debug.Log(world_pos);

            
            // var quat = new Quaternion(x,y,z,w);
            
            // // Debug.Log(quat.eulerAngles);
            // var ang = GPSEncoder.QuatToEuler(quat);
            // adj_ang = new Vector3();
            // adj_ang.x = -1.0f*ang.y;
            // // Debug.Log(adj_ang.x);
            // adj_ang.z = ang.x;
            // adj_ang.y = ang.z;
            // float tempv = (float)Math.PI/180;
            // float c = (90.0f-adj_ang.x) * tempv;
            // float tempang = (float)Math.Cos(c);
            // float ht = alt/tempang;
            // canv.planeDistance = ht+1;
            // Quaternion rotat = Quaternion.Euler(adj_ang);
            // drone.transform.rotation = rotat;
            // Debug.Log(ang);
            // drone.transform.position = world_pos; 


        }

        // Udp connection to get GeoLocation 
        if (client2.Available > 0)
        {


            byte[] data2 = client2.Receive(ref endPoint);
            string text2 = Encoding.UTF8.GetString(data2);
            // Debug.Log(text2);
            var values2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(text2);
            lat2 = (float) Convert.ToDouble(values2["lat"]);
            lon2 = (float) Convert.ToDouble(values2["lon"]);
            alt2 =  (float) Convert.ToDouble(values2["alt"]);
            var world_pos2 = GPSEncoder.GPSToUCS(lat2,lon2);
            world_pos2.y =1.0f;
            InstObj(world_pos2);

        }

        //Placing object when user clicks on the screen
        if (Input.GetMouseButtonDown(0))
        {
            
        
            // To prevent placing object when user clicks on a button
            PointerEventData pe = new PointerEventData(EventSystem.current);
            pe.position = Input.mousePosition;
            List<RaycastResult> resList = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pe,resList);
            // for(int i = 0;i<resList.Count;i++){
            //     Debug.Log(resList[i]);
            // }


            Vector3 mousePos = Input.mousePosition;
            if(resList.Count == 1)
            {
            Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("xpos", mousePos.x.ToString());
            payload.Add("ypos", mousePos.y.ToString());
            payload.Add("lat", lat.ToString());
            payload.Add("alt", alt.ToString());
            payload.Add("lon", lon.ToString());
            payload.Add("w", w.ToString());
            payload.Add("x", x.ToString());
            payload.Add("y", y.ToString());
            payload.Add("z", z.ToString());
            payload.Add("resh",canv.GetComponent<RectTransform>().rect.height.ToString());
            payload.Add("resw",canv.GetComponent<RectTransform>().rect.width.ToString());

            string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
            byte[] data = Encoding.UTF8.GetBytes(result);
            client.Send(data, data.Length, endPoint);
            }
           
        }

    }

    private void GreenButton()
    {
        Debug.Log("You have clicked Green Button!");
        selectedButton="green";
        resetButtons();
        green.interactable=false;
        
        
    }

     private void RedButton()
    {
        Debug.Log("You have clicked Red Button!");
        selectedButton="red";
        resetButtons();
        red.interactable=false;
        
        
    }

    private void BlueButton()
    {
        Debug.Log("You have clicked Blue Button!");
        selectedButton="blue";
        resetButtons();
        blue.interactable=false;
    }

    private void InstObj(Vector3 wp){
        if(selectedButton == "green"){
            Instantiate(obj1,wp, Quaternion.Euler(adj_ang));
        }
        else if(selectedButton == "red")
        {
            Instantiate(obj2,wp, Quaternion.Euler(adj_ang));
        }
        else if(selectedButton == "blue")
        {
            Instantiate(obj3,wp, Quaternion.Euler(adj_ang));
        }
    }

    private void resetButtons(){
        red.interactable=true;
        green.interactable=true;
        blue.interactable=true;
    }
}