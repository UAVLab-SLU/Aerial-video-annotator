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
using Recognissimo.Components;

public class VideoRend : MonoBehaviour//,IPointerEnterHandler
{
    // Start is called before the first frame update
    public Canvas canv;
    public RawImage image;
    public GameObject drone;
    public GameObject obj1;
    public GameObject obj2;
    public GameObject obj3;
    private GameObject tp ;
    public List<GameObject> placed_objects = new List<GameObject>();
    public GameObject circle;
    private float checkalt = 0.0f;
    private float curscl = 1.0f;
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
    float pitch = 0;
    float roll = 0;
    float yaw = 0;

    private Vector3 ang;
    private Quaternion rotat;
    string selectedButton;
    
    public Button green;
    public Button blue;
    public Button red;
    AudioClip clip;
    public SpeechRecognizer speechRecognizer;

    bool Po;
    
    // string microPhn = Microphone.devices[0];
    

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
        Po = true;

        speechRecognizer = gameObject.AddComponent<SpeechRecognizer>();
        var languageModelProvider = gameObject.AddComponent<StreamingAssetsLanguageModelProvider>();
        var speechSource = gameObject.AddComponent<MicrophoneSpeechSource>();
        // Setup StreamingAssets language model provider.
        // Set the language used for recognition.
        languageModelProvider.language = SystemLanguage.English;
        // Set paths to language models.
        languageModelProvider.languageModels = new List<StreamingAssetsLanguageModel>
        {
        new() {language = SystemLanguage.English, path = "LanguageModels/en-US"},
        new() {language = SystemLanguage.French, path = "LanguageModels/fr-FR"}
        };
        // Setup microphone speech source. The default settings can be left unchanged, but we will do it as an example.
        speechSource.DeviceName = null;
        speechSource.TimeSensitivity = 0.25f;
        // Bind speech processor dependencies.
        speechRecognizer.LanguageModelProvider = languageModelProvider;
        speechRecognizer.SpeechSource = speechSource;
        // Handle events.
        speechRecognizer.ResultReady.AddListener(OnResult);


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
            lat = (float) Convert.ToDouble(values["lat"]);
            lon = (float) Convert.ToDouble(values["lon"]);
            alt = (float) Convert.ToDouble(values["alt"]);

            if(alt - checkalt >10.0f){
                checkalt = alt;
                curscl+=0.5f;
                ScaleObjects(curscl);
            }
            if(checkalt - alt > 10.0f){
                checkalt = alt;
                curscl-=0.5f;
                ScaleObjects(curscl);
            }
            w = (float) Convert.ToDouble(values["w"]);
            x = (float) Convert.ToDouble(values["x"]);
            y = (float) Convert.ToDouble(values["y"]);
            z = (float) Convert.ToDouble(values["z"]);

            var drone_x = (float) Convert.ToDouble(values["drone_x"]);
            var drone_y = (float) Convert.ToDouble(values["drone_y"]);
            var drone_z = (float) Convert.ToDouble(values["drone_z"]);

            var world_pos = GPSEncoder.GPSToUCS(lat,lon);
            world_pos.y = -1.0f*drone_z;
            // world_pos.x = 1.0f*drone_x;
            // world_pos.z = 1.0f*drone_y;

            pitch = (float) Convert.ToDouble(values["pitch"]);
            roll = (float) Convert.ToDouble(values["roll"]);
            yaw = (float) Convert.ToDouble(values["yaw"]);
            if (values["image"] != null)
            {
                byte[] result = Convert.FromBase64String(values["image"]);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(result);
                image.texture = texture;
            }
            if (Po)
            {
                GPSEncoder.SetLocalOrigin(new Vector2(lat, lon));
                var world_p = GPSEncoder.GPSToUCS(lat, lon);
                InstObj(world_p);
                Debug.Log("Object placed");
                Po = false;
            }
            
            
           
            

            ang.x = pitch;
            ang.y = yaw;
            ang.z = roll;

            // Debug.Log(ang);
            float tempv = (float)Math.PI/180;
            float c = (90.0f-ang.x) * tempv;
            float tempang = (float)Math.Cos(c);
            float ht = alt/tempang;
            canv.planeDistance = ht+4;

            
            rotat = Quaternion.Euler(ang);
            // Debug.Log(rotat);
            drone.transform.rotation = rotat;
            // Debug.Log(ang);
            drone.transform.position = world_pos; 


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
                // sendUserInput(mousePos.x,mousePos.y);
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

    private void ScaleObjects(float scale){
        Debug.Log("Scaling");
        Debug.Log(scale);
        Vector3 scaleChange = new Vector3(scale, scale, scale);
        foreach(GameObject g in placed_objects){
            g.transform.localScale = scaleChange;
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



    public void startRecord()
    {
        Debug.Log("Recording started");

        //clip = Microphone.Start(Microphone.devices[0], true, 10, AudioSettings.outputSampleRate);
        //Debug.Log(clip.length);
        speechRecognizer.StartProcessing();
    }
    public void stopRecord()
    {
        Debug.Log("Recording stopped");
        speechRecognizer.StopProcessing();
        
        
    }

    void sendUserInput(float x,float y){
        Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("xpos", x.ToString());
            payload.Add("ypos", y.ToString());
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
    private void OnResult(Result result)
    {
        Debug.Log(result.text);
        int width = (int)canv.GetComponent<RectTransform>().rect.width;  
        int height = (int)canv.GetComponent<RectTransform>().rect.height; 
        int rectWidth = width / 2;
        int rectHeight = height / 2;
        string[] keywords = result.text.Split(' ');

        if (keywords.Length < 2)
        {
            Debug.Log("Invalid input. Please provide two directions.");
            return;
        }

        bool isValidDirection = true;
        foreach (string keyword in keywords)
        {
            if (keyword != "top" && keyword != "bottom" && keyword != "left" && keyword != "right")
            {
                isValidDirection = false;
                break;
            }
        }

        if (!isValidDirection)
        {
            Debug.Log("Invalid directions in user input.");
            return;
        }

        // Check directions based on keywords
        int midpointX = 0, midpointY = 0;

        foreach (string keyword in keywords)
        {
            switch (keyword)
            {
                case "top":
                    midpointY += height - (rectHeight / 2);
                    break;
                case "bottom":
                    midpointY += rectHeight / 2;
                    break;
                case "left":
                    midpointX += rectWidth / 2;
                    break;
                case "right":
                    midpointX += rectWidth + (rectWidth / 2);
                    break;
            }
        }

        Debug.Log(midpointX);
        Debug.Log(midpointY);
        sendUserInput(midpointX,midpointY);
        
    }

    private void InstObj(Vector3 wp){
        ang.x = 90;
        
        if(selectedButton == "green"){
            tp = obj1;
        }
        else if(selectedButton == "red")
        {
            tp = obj2;
        }
        else if(selectedButton == "blue")
        {
            tp = obj3;
        }
        Instantiate(tp,wp, Quaternion.Euler(ang));
        //GameObject c = Instantiate(circle,wp,Quaternion.Euler(ang));
        //placed_objects.Add(c);
        //Vector3 scaleChange = new Vector3(curscl, curscl, curscl);
        //c.transform.localScale = scaleChange;
        
    }

    private void resetButtons(){
        red.interactable=true;
        green.interactable=true;
        blue.interactable=true;
    }
}