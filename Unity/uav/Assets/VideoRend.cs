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
using TMPro;
using System.Collections;
using UnityEngine.Networking;

[Serializable]
public class OSLocation 
{
    // Start is called before the first frame update
   public double lat {get;set;}
   public double lon {get;set;}
  
}

public class VideoRend : MonoBehaviour//,IPointerEnterHandler
{
  // Start is called before the first frame update
  public Canvas canv;
  public RawImage image;

  public GameObject OSPerson;
  public GameObject drone;
  public GameObject obj1;
  public GameObject obj2;
  public GameObject obj3;
  private GameObject tp;
  public List<GameObject> placed_objects = new List<GameObject>();
  public GameObject circle;

  public RawImage grid;
  public Texture2D grid2;
  public Texture2D grid3;
  public Texture2D grid4;
  public Texture2D grid5;
  private float checkalt = 0.0f;
  private float curscl = 1.0f;
  UdpClient client;
  UdpClient client2;
  IPEndPoint endPoint;
  public GPSEncoder GE;
  private int count;

  private int curgrid;

  private int selectedGrid;

  TextMeshProUGUI dg;
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
  public GameObject dialogue;

  void Start()
  {
    client = new UdpClient(8080);
    client2 = new UdpClient(8000);
    endPoint = new IPEndPoint(IPAddress.Any, 0);
    green.onClick.AddListener(GreenButton);
    blue.onClick.AddListener(BlueButton);
    red.onClick.AddListener(RedButton);

    curgrid = 1;
    SetGrid();


    selectedButton = "green";
    green.interactable = false;
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
        // new() {language = SystemLanguage.English, path = "LanguageModels/en-IND"},
        new() {language = SystemLanguage.English, path = "LanguageModels/en-US"},
        };
    // Setup microphone speech source. The default settings can be left unchanged, but we will do it as an example.
    speechSource.DeviceName = null;
    speechSource.TimeSensitivity = 0.25f;
    // Bind speech processor dependencies.
    speechRecognizer.LanguageModelProvider = languageModelProvider;
    speechRecognizer.SpeechSource = speechSource;
    // Handle events.
    // speechRecognizer.PartialResultReady.AddListener(OnPartialResult);
    speechRecognizer.ResultReady.AddListener(OnResult);
    speechRecognizer.Vocabulary = new List<string>
        {
            "one", "two", "three", "four", "five",
            "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen",
            "sixteen", "seventeen", "eighteen", "nineteen", "twenty",
            "twenty one", "twenty two", "twenty three", "twenty four", "twenty five",
            "green", "blue", "red",
        };
    startRecord();
    StartCoroutine(GetOSLocation());
     
  }



  IEnumerator GetOSLocation()
    {   
        while(true){

          if(!Po)
          {
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://uavlab-98a0c-default-rtdb.firebaseio.com/OSlocation.json"))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log(": Error: " + webRequest.error);
                }
                else
                {
                    // Print out the received data.
                    // Debug.Log("Received: " + webRequest.downloadHandler.text);
                    OSLocation osl = JsonConvert.DeserializeObject<OSLocation>(webRequest.downloadHandler.text);
                    // Debug.Log($"Location{osl.lat}, {osl.lon}");
                   
                    var os_pos = GPSEncoder.GPSToUCS((float)osl.lat, (float)osl.lon);
                    os_pos.y = 0f;
                    OSPerson.transform.position = os_pos;
                    // GPSEncoder.SetLocalOrigin(new Vector2((float)osl.lat,(float)osl.lon));
                }
            }
          }
          yield return new WaitForSeconds(1);
        }
        
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
      lat = (float)Convert.ToDouble(values["lat"]);
      lon = (float)Convert.ToDouble(values["lon"]);
      alt = (float)Convert.ToDouble(values["alt"]);

      if (alt - checkalt > 10.0f)
      {
        checkalt = alt;
        curscl += 0.5f;
        curgrid += 1;
        SetGrid();
        ScaleObjects(curscl);
      }
      if (checkalt - alt > 10.0f)
      {
        checkalt = alt;
        curscl -= 0.5f;
        curgrid -= 1;
        SetGrid();
        ScaleObjects(curscl);
      }
      w = (float)Convert.ToDouble(values["w"]);
      x = (float)Convert.ToDouble(values["x"]);
      y = (float)Convert.ToDouble(values["y"]);
      z = (float)Convert.ToDouble(values["z"]);

      var world_pos = GPSEncoder.GPSToUCS(lat, lon);
      world_pos.y = alt;

      pitch = (float)Convert.ToDouble(values["pitch"]);
      roll = (float)Convert.ToDouble(values["roll"]);
      yaw = (float)Convert.ToDouble(values["yaw"]);
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
        Vector3 ostemp = new Vector3(0f,-100f,0f);
        Vector3 osang = new Vector3();
        osang.x = 90;
        OSPerson = Instantiate(OSPerson,ostemp, Quaternion.Euler(osang));
      }
     
      if (!float.IsNaN(pitch) && !float.IsNaN(roll) && !float.IsNaN(yaw))
      {
        ang.x = -1.0f * pitch;
        ang.y = yaw;
        ang.z = roll;

        // Debug.Log(ang);
        float tempv = (float)Math.PI / 180;
        if (ang.x > 90.0f)
        {
          ang.x = ang.x % 90.0f;
        }
        if (ang.x < 0.0f)
        {
          ang.x = ang.x * -1.0f;
        }
        // Debug.Log(ang.x);
        float c = (90.0f - ang.x) * tempv;
        float tempang = (float)Math.Cos(c);
        float ht = alt / tempang;
        // Debug.Log(alt);
        // Debug.Log(ht);
        if (ht < 0.0f)
        {
          ht = -1.0f * ht;
        }
        canv.planeDistance = ht + 1;
        // Debug.Log(canv.planeDistance);
      }


      rotat = new Quaternion(-y, z, -x, w);
      if (!float.IsNaN(rotat.x) && !float.IsNaN(rotat.y) && !float.IsNaN(rotat.z) && !float.IsNaN(rotat.w))
      {
        drone.transform.rotation = rotat;
      }


      if (!float.IsNaN(world_pos.x) && !float.IsNaN(world_pos.y) && !float.IsNaN(world_pos.z))
      {
        drone.transform.position = world_pos;
      }



    }

    // Udp connection to get GeoLocation 
    if (client2.Available > 0)
    {


      byte[] data2 = client2.Receive(ref endPoint);
      string text2 = Encoding.UTF8.GetString(data2);
      // Debug.Log(text2);
      var values2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(text2);
      lat2 = (float)Convert.ToDouble(values2["lat"]);
      lon2 = (float)Convert.ToDouble(values2["lon"]);
      alt2 = (float)Convert.ToDouble(values2["alt"]);
      var world_pos2 = GPSEncoder.GPSToUCS(lat2, lon2);
      world_pos2.y = 1.0f;
      // var  temp = world_pos2.x;
      world_pos2.x = 1.0f * world_pos2.x;
      world_pos2.z = 1.0f * world_pos2.z;
      Debug.Log(world_pos2);
      InstObj(world_pos2);

    }

    //Placing object when user clicks on the screen
    if (Input.GetMouseButtonDown(0))
    {


      // To prevent placing object when user clicks on a button
      PointerEventData pe = new PointerEventData(EventSystem.current);
      pe.position = Input.mousePosition;
      List<RaycastResult> resList = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pe, resList);
      // for(int i = 0;i<resList.Count;i++){
      //     Debug.Log(resList[i]);
      // }


      Vector3 mousePos = Input.mousePosition;
      if (resList.Count == 1)
      {
        // sendUserInput(mousePos.x,mousePos.y);

        Dictionary<string, string> payload = new Dictionary<string, string>();
        var mouse_y = canv.GetComponent<RectTransform>().rect.height - mousePos.y;
        payload.Add("xpos", mousePos.x.ToString());
        payload.Add("ypos", mouse_y.ToString());
        payload.Add("lat", lat.ToString());
        payload.Add("alt", alt.ToString());
        payload.Add("lon", lon.ToString());


        // payload.Add("w", rotat.w.ToString());
        // payload.Add("x", rotat.x.ToString());
        // payload.Add("y", rotat.y.ToString());
        // payload.Add("z", rotat.z.ToString());

        if(selectedButton == "red"){
          payload.Add("obj","0");
        }
        if(selectedButton == "green"){
          payload.Add("obj","1");
        }
        if(selectedButton == "blue"){
          payload.Add("obj","2");
        }

        payload.Add("resh", canv.GetComponent<RectTransform>().rect.height.ToString());
        payload.Add("resw", canv.GetComponent<RectTransform>().rect.width.ToString());

        string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
        Debug.Log(result);
        byte[] data = Encoding.UTF8.GetBytes(result);
        client.Send(data, data.Length, endPoint);
      }

    }

  }



  private void ScaleObjects(float scale)
  {
    Debug.Log("Scaling");
    Debug.Log(scale);
    Vector3 scaleChange = new Vector3(scale, scale, scale);
    foreach (GameObject g in placed_objects)
    {
      g.transform.localScale = scaleChange;
    }
  }



  private void GreenButton()
  {
    Debug.Log("You have clicked Green Button!");
    selectedButton = "green";
    resetButtons();
    green.interactable = false;


  }

  private void RedButton()
  {
    Debug.Log("You have clicked Red Button!");
    selectedButton = "red";
    resetButtons();
    red.interactable = false;


  }

  private void BlueButton()
  {
    Debug.Log("You have clicked Blue Button!");
    selectedButton = "blue";
    resetButtons();
    blue.interactable = false;
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

  void sendUserInput(float x, float y, string clr, string grd)
  {
    var dobj = Instantiate(dialogue);
    dg = dobj.GetComponentInChildren<TextMeshProUGUI>();
    dg.text = clr+" marker is placed in grid "+grd;
    Destroy(dobj,3);
    Dictionary<string, string> payload = new Dictionary<string, string>();
    payload.Add("xpos", x.ToString());
    payload.Add("ypos", y.ToString());
    payload.Add("lat", lat.ToString());
    payload.Add("alt", alt.ToString());
    payload.Add("lon", lon.ToString());

    // payload.Add("w", w.ToString());
    // payload.Add("x", x.ToString());
    // payload.Add("y", y.ToString());
    // payload.Add("z", z.ToString());
    payload.Add("resh", canv.GetComponent<RectTransform>().rect.height.ToString());
    payload.Add("resw", canv.GetComponent<RectTransform>().rect.width.ToString());

    if(selectedButton == "red"){
      payload.Add("obj","0");
    }
    if(selectedButton == "green"){
      payload.Add("obj","1");
    }
    if(selectedButton == "blue"){
      payload.Add("obj","2");
    }

    string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
    byte[] data = Encoding.UTF8.GetBytes(result);
    client.Send(data, data.Length, endPoint);
  }

  public void OnPartialResult(PartialResult partialResult)
  {
    Debug.Log(partialResult.partial);
    string[] keywords = partialResult.partial.Split(' ');

  }

  private void OnResult(Result result)
  {
    Debug.Log(result.text);
    
    string[] keywords = result.text.Split(' ');

    Processtext(keywords);

  }


  private void Processtext(string[] keywords){

    int width = (int)canv.GetComponent<RectTransform>().rect.width;
    int height = (int)canv.GetComponent<RectTransform>().rect.height;
    string[] colors = { "green", "red", "blue" };
    string finalColor = "";
    string finalGrid = "";
    int finalGridNum = 0;
    string[] selectedColor = keywords.Intersect(colors).ToArray();
    if (selectedColor.Length != 1)
    {
      Debug.Log("Marker color error");
      return;
    }
    finalColor = selectedColor[0];
    string[] numbers =  {
            "one", "two", "three", "four", "five",
            "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen",
            "sixteen", "seventeen", "eighteen", "nineteen", "twenty",
        };

    string[] selectedNumber = keywords.Intersect(numbers).ToArray();
    Debug.Log(selectedNumber.Length);
    if (selectedNumber.Length < 1 || selectedNumber.Length > 2)
    {
      Debug.Log("Grid Number error");
      return;
    }
    if (selectedNumber.Contains("twenty"))
    {
      if (selectedNumber.Length == 1)
      {
        finalGrid = "twenty";
      }
      if (selectedNumber.Length == 2)
      {
        if (selectedNumber[0] == "twenty")
        {
          if (selectedNumber[1] == "one")
          {
            finalGrid = "twenty one";
          }
          else if (selectedNumber[1] == "two")
          {
            finalGrid = "twenty two";
          }
          else if (selectedNumber[1] == "three")
          {
            finalGrid = "twenty three";
          }
          else if (selectedNumber[1] == "four")
          {
            finalGrid = "twenty four";
          }
          else if (selectedNumber[1] == "five")
          {
            finalGrid = "twenty five";
          }
          else
          {
            Debug.Log("Grid Number error");
          }
        }
        else
        {
          Debug.Log("Grid Number error");
          return;
        }
      }
    }
    else
    {
      if (selectedNumber.Length != 1)
      {
        Debug.Log("Grid Number error");
        return;
      }
      else
      {
        finalGrid = selectedNumber[0];
      }
    }
    Debug.Log(finalGrid);
    Debug.Log(finalColor);

    Dictionary<string, int> numberDictionary = new Dictionary<string, int>
    {
        { "one", 1 },{ "two", 2 },{ "three", 3 },{ "four", 4 },{ "five", 5 },
        { "six", 6 },{ "seven", 7 },{ "eight", 8 },{ "nine", 9 },{ "ten", 10 },
        { "eleven", 11 },{ "twelve", 12 },{ "thirteen", 13 },{ "fourteen", 14 },{ "fifteen", 15 },
        { "sixteen", 16 },{ "seventeen", 17 },{ "eighteen", 18 },{ "nineteen", 19 },{ "twenty", 20 },
        { "twenty one", 21 },{ "twenty two", 22 },{ "twenty three", 23 },{ "twenty four", 24 },{ "twenty five", 25 }
    };
    if (numberDictionary.ContainsKey(finalGrid))
    {
      finalGridNum = numberDictionary[finalGrid];
    }
    else
    {
      Debug.Log("Grid Number error");
      return;
    }
    Debug.Log(finalGridNum);
    int tempSN = curgrid + 1;
    if (tempSN>5){
        tempSN = 5;
    }
    var midpoints = CalculateMidpoints(width, height, tempSN);

    if (midpoints.ContainsKey(finalGridNum - 1))
    {
      Debug.Log(midpoints[finalGridNum - 1].Item1);
      Debug.Log(midpoints[finalGridNum - 1].Item2);
      selectedButton = finalColor;
      sendUserInput(midpoints[finalGridNum - 1].Item1,midpoints[finalGridNum - 1].Item2,finalColor,finalGrid);
      if (selectedButton == "green")
        {
            GreenButton();
        }
        else if (selectedButton == "red")
        {
            RedButton();
        }
        else if (selectedButton == "blue")
        {
            BlueButton();
        }
    }
    else
    {
      Debug.Log("Grid Number error");
      return;
    }
  }


  private Dictionary<int, Tuple<int, int>> CalculateMidpoints(int width, int height, int gridsize)
  {
    var midpoints = new Dictionary<int, Tuple<int, int>>();

    int cellWidth = width / gridsize;
    int cellHeight = height / gridsize;

    for (int i = 0; i < gridsize; i++)
    {
      for (int j = 0; j < gridsize; j++)
      {
        // Calculate midpoint of each grid cell
        int midpointX = (j * cellWidth) + (cellWidth / 2);
        int midpointY = (i * cellHeight) + (cellHeight / 2);

        midpoints.Add(i * gridsize + j, new Tuple<int, int>(midpointX, midpointY));
      }
    }

    return midpoints;
  }

  private void InstObj(Vector3 wp)
  {
    ang.x = 90;

    if (selectedButton == "green")
    {
      tp = obj1;
    }
    else if (selectedButton == "red")
    {
      tp = obj2;
    }
    else if (selectedButton == "blue")
    {
      tp = obj3;
    }
    Instantiate(tp, wp, Quaternion.Euler(ang));
    //GameObject c = Instantiate(circle,wp,Quaternion.Euler(ang));
    //placed_objects.Add(c);
    //Vector3 scaleChange = new Vector3(curscl, curscl, curscl);
    //c.transform.localScale = scaleChange;

  }

  private void resetButtons()
  {
    red.interactable = true;
    green.interactable = true;
    blue.interactable = true;
  }

  private void SetGrid()
  {
    switch (curgrid)
    {
      case 1:
        grid.texture = grid2;
        break;
      case 2:
        grid.texture = grid3;
        break;
      case 3:
        grid.texture = grid4;
        break;
      default:
        if (curgrid >= 4)
        {
          grid.texture = grid5;
        }
        break;
    }
  }


}