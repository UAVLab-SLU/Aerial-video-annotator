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
using Proyecto26;


public class VideoRend : MonoBehaviour
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
  private List<GameObject> placed_objects = new List<GameObject>();
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

  private Dictionary<string, GameObject> placed_markers = new Dictionary<string, GameObject>();

  private int selectedGrid;

  Dictionary<string, Location> locations;

  TextMeshProUGUI dg;

  public TextMeshProUGUI dist;

  public TextMeshProUGUI nextMv;
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

  private int GreenCount;

  private int BlueCount;

  private int RedCount;

  private string OSnextMove;

  bool Po;

  bool fetchedLocations;

  // string microPhn = Microphone.devices[0];
  public GameObject dialogue;

  public GameObject nextMvNotif;

  private GameObject circleMarker;

  void Start()
  {
    client = new UdpClient(8080);
    client2 = new UdpClient(8000);
    endPoint = new IPEndPoint(IPAddress.Any, 0);
    green.onClick.AddListener(GreenButton);
    blue.onClick.AddListener(BlueButton);
    red.onClick.AddListener(RedButton);

    curgrid = 1;
    BlueCount = 1;
    GreenCount = 1;
    RedCount = 1;
    SetGrid();
    OSnextMove = "";

    circleMarker = Instantiate(circle, new Vector3(0, -200, 0), Quaternion.identity);

    selectedButton = "green";
    green.interactable = false;
    Po = true;
    fetchedLocations = false;

		//---------Speech Recognition setup.
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
		// List of dictionary words which Speech Recognizer looks for from user commands.
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
    // StartCoroutine(GetNextMove());

  }

	// Get Onsite Operator Next Move.
  IEnumerator GetNextMove()
  {
    while (true)
    {

      if (!Po)
      {
        if (fetchedLocations)
        {
          using (UnityWebRequest webRequest = UnityWebRequest.Get("https://uavlab-98a0c-default-rtdb.firebaseio.com/nextMove.json"))
          {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
              Debug.Log(": Error: " + webRequest.error);
            }
            else
            {

              foreach (var key in placed_markers.Keys)
              {
                Debug.Log($"----------{key}");
              }
              Debug.Log($"{webRequest.downloadHandler.text}------");
              var values = JsonConvert.DeserializeObject<Dictionary<string, int>>(webRequest.downloadHandler.text);
              var tempNextMove = values["color"].ToString() + values["num"].ToString();
              if (placed_markers.ContainsKey(tempNextMove))
              {
                var tempGobj = placed_markers[tempNextMove];

                string color = "";
                if (values["color"] == 0)
                {
                  color = "Red";
                }
                if (values["color"] == 1)
                {
                  color = "Green";
                }
                if (values["color"] == 2)
                {
                  color = "Blue";
                }
                if (tempNextMove != OSnextMove)
                {
                  Debug.Log($"NextMove changed");
                  var dobj = Instantiate(nextMvNotif);
                  dg = dobj.GetComponentInChildren<TextMeshProUGUI>();
                  dg.text = "OnSite Operator next move changed to " + color + " " + values["num"].ToString();
                  circleMarker.transform.position = tempGobj.transform.position;
                  circleMarker.transform.rotation = tempGobj.transform.rotation;
                  Destroy(dobj, 3);
                  OSnextMove = tempNextMove;
                }
                float distance = 0f;
                distance = Vector3.Distance(tempGobj.transform.position, OSPerson.transform.position);
                // dist.text = "Onsite Operator is " + distance + " from target";
                nextMv.text = "Onsite Operator next Move: " + color + " " + values["num"].ToString() + " and is " + distance + "m from target";
              }
            }
          }
        }
      }
      yield return new WaitForSeconds(1);
    }

  }

	// Get Onsite operator and update it every second.
  IEnumerator GetOSLocation()
  {
    while (true)
    {

      if (!Po)
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
            
            OSLocation osl = JsonConvert.DeserializeObject<OSLocation>(webRequest.downloadHandler.text);
            var os_pos = GPSEncoder.GPSToUCS((float)osl.lat, (float)osl.lon);
            os_pos.y = 0f;
            OSPerson.transform.position = os_pos;
            
          }
        }
      }
      yield return new WaitForSeconds(1);
    }

  }

	// Get already placed locations from Firebase database.
  IEnumerator GetLocations()
  {

    if (!Po)
    {
      using (UnityWebRequest webRequest = UnityWebRequest.Get("https://uavlab-98a0c-default-rtdb.firebaseio.com/location.json"))
      {
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
          // Debug.Log(": Error: " + webRequest.error);
        }
        else
        {
          locations = JsonConvert.DeserializeObject<Dictionary<string, Location>>(webRequest.downloadHandler.text);
          string color = "";
          string num = "";
          GameObject tempG = obj1;
					// Creating respective markers for fetched locations and updating marker(Example, Green 1 to Green 4) count.
          foreach (var key in locations.Keys)
          {
            num = locations[key].ctr.ToString();
            if (locations[key].obj == 0)
            {
              tempG = obj2;
              color = "Red";
              if (locations[key].ctr > RedCount)
              {
                RedCount = locations[key].ctr;
                RedCount += 1;
              }
            }
            if (locations[key].obj == 1)
            {
              tempG = obj1;
              color = "Green";
              if (locations[key].ctr > GreenCount)
              {
                GreenCount = locations[key].ctr;
                GreenCount += 1;
              }
            }
            if (locations[key].obj == 2)
            {
              tempG = obj3;
              color = "Blue";
              if (locations[key].ctr > BlueCount)
              {
                BlueCount = locations[key].ctr;
                BlueCount += 1;
              }
            }
            var pos = GPSEncoder.GPSToUCS((float)locations[key].lat, (float)locations[key].lon);
            var gob = Instantiate(tempG, pos, Quaternion.Euler(ang));
            GameObject ttxt = gob.transform.GetChild(0).gameObject;
            TextMeshPro mText = ttxt.GetComponent<TextMeshPro>();
            mText.text = color + " " + num;
            string tempKey = locations[key].obj.ToString() + locations[key].ctr.ToString();
            placed_markers[tempKey] = gob;

          }
          fetchedLocations = true;
        }
      }
      yield return null;
    }
  }


  //Update is called once per frame
  void Update()
  {
		// Setting up UDP connection for receiving frames and metadata from drone.
    if (client.Available > 0)
    {
      byte[] data = client.Receive(ref endPoint);
      string text = Encoding.UTF8.GetString(data);
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
			// Quaternion(w,x,y,z) of drone rotation.
      w = (float)Convert.ToDouble(values["w"]);
      x = (float)Convert.ToDouble(values["x"]);
      y = (float)Convert.ToDouble(values["y"]);
      z = (float)Convert.ToDouble(values["z"]);

			// converting drone postion in GPS to unity coordinates.
      var world_pos = GPSEncoder.GPSToUCS(lat, lon);
      world_pos.y = alt;

      pitch = (float)Convert.ToDouble(values["pitch"]);
      roll = (float)Convert.ToDouble(values["roll"]);
      yaw = (float)Convert.ToDouble(values["yaw"]);
			// Setting up rawImage with the frame received from drone.(Simuating ground in unity by projecting this in clipping plane of main camera)
      if (values["image"] != null)
      {
        byte[] result = Convert.FromBase64String(values["image"]);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(result);
        image.texture = texture;
      }
			//Boolean check to set if the stream from drone is received.
      if (Po)
      {
        GPSEncoder.SetLocalOrigin(new Vector2(lat, lon));
        var world_p = GPSEncoder.GPSToUCS(lat, lon);
        // InstObj(world_p); // Mark drones takeoff location.
        Debug.Log("Object placed");
        Po = false;
        Vector3 ostemp = new Vector3(0f, -100f, 0f);
        Vector3 osang = new Vector3();
        osang.x = 90;
        OSPerson = Instantiate(OSPerson, ostemp, Quaternion.Euler(osang));
        StartCoroutine(GetLocations());
        StartCoroutine(GetNextMove());
      }

			// Checks to validate data before manipulating gameobjects in Unity.
      if (!float.IsNaN(pitch) && !float.IsNaN(roll) && !float.IsNaN(yaw))
      {
        ang.x = -1.0f * pitch;
        ang.y = yaw;
        ang.z = roll;
        float tempv = (float)Math.PI / 180;
        if (ang.x > 90.0f)
        {
          ang.x = ang.x % 90.0f;
        }
        if (ang.x < 0.0f)
        {
          ang.x = ang.x * -1.0f;
        }
				// Projecting ground at hypotenuse of Drone height from ground and pitch of drone to simulate real world movement.
        float c = (90.0f - ang.x) * tempv;
        float tempang = (float)Math.Cos(c);
        float ht = alt / tempang;
        if (ht < 0.0f)
        {
          ht = -1.0f * ht;
        }
        canv.planeDistance = ht + 1;
      }
			//Converting Quaternion from NED to ENU.
      rotat = new Quaternion(-y, z, -x, w);
			// Validating data
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
      var values2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(text2);
      lat2 = (float)Convert.ToDouble(values2["lat"]);
      lon2 = (float)Convert.ToDouble(values2["lon"]);
      alt2 = (float)Convert.ToDouble(values2["alt"]);
      var world_pos2 = GPSEncoder.GPSToUCS(lat2, lon2);
      world_pos2.y = 1.0f;
      world_pos2.x = 1.0f * world_pos2.x;
      world_pos2.z = 1.0f * world_pos2.z;
      Debug.Log(world_pos2);
      InstObj(world_pos2);


      Location lpl = new Location();
      lpl.ctr = int.Parse(values2["ctr"]);
      lpl.obj = int.Parse(values2["obj"]);
      lpl.lat = Convert.ToDouble(values2["lat"]);
      lpl.lon = Convert.ToDouble(values2["lon"]);

      

      Dictionary<string, double> pl = new Dictionary<string, double>();
      var tempVal = JsonConvert.SerializeObject(lpl);
			// Creating a new entry for location in Firebase database.
      RestClient.Post<Location>("https://uavlab-98a0c-default-rtdb.firebaseio.com/location.json", tempVal).Then(response =>
      {
        Debug.Log(response);
      });

    }

    //Placing object when user clicks on the screen
    if (Input.GetMouseButtonDown(0))
    {
      // To prevent placing object when user clicks on a button
      PointerEventData pe = new PointerEventData(EventSystem.current);
      pe.position = Input.mousePosition;
      List<RaycastResult> resList = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pe, resList);
      

      Vector3 mousePos = Input.mousePosition;
      if (resList.Count == 1)
      {
       
        Dictionary<string, string> payload = new Dictionary<string, string>();
        var mouse_y = canv.GetComponent<RectTransform>().rect.height - mousePos.y;
        payload.Add("xpos", mousePos.x.ToString());
        payload.Add("ypos", mouse_y.ToString());
        payload.Add("lat", lat.ToString());
        payload.Add("alt", alt.ToString());
        payload.Add("lon", lon.ToString());

        if (selectedButton == "red")
        {
          payload.Add("obj", "0");
          payload.Add("ctr", RedCount.ToString());
        }
        if (selectedButton == "green")
        {
          payload.Add("obj", "1");
          payload.Add("ctr", GreenCount.ToString());
        }
        if (selectedButton == "blue")
        {
          payload.Add("obj", "2");
          payload.Add("ctr", BlueCount.ToString());
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
	// Placing object based on user voice input.
  void sendUserInput(float x, float y, string clr, string grd)
  {
    var dobj = Instantiate(dialogue);
    dg = dobj.GetComponentInChildren<TextMeshProUGUI>();
    dg.text = clr + " marker is placed in grid " + grd;
    Destroy(dobj, 3);
    Dictionary<string, string> payload = new Dictionary<string, string>();
    payload.Add("xpos", x.ToString());
    payload.Add("ypos", y.ToString());
    payload.Add("lat", lat.ToString());
    payload.Add("alt", alt.ToString());
    payload.Add("lon", lon.ToString());

    payload.Add("resh", canv.GetComponent<RectTransform>().rect.height.ToString());
    payload.Add("resw", canv.GetComponent<RectTransform>().rect.width.ToString());

    if (selectedButton == "red")
    {
      payload.Add("obj", "0");
      payload.Add("ctr", RedCount.ToString());
    }
    if (selectedButton == "green")
    {
      payload.Add("obj", "1");
      payload.Add("ctr", GreenCount.ToString());
    }
    if (selectedButton == "blue")
    {
      payload.Add("obj", "2");
      payload.Add("ctr", BlueCount.ToString());
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

	// Record user voice inputs and place objects in respective grid.
  private void OnResult(Result result)
  {
    Debug.Log(result.text);

    string[] keywords = result.text.Split(' ');

    Processtext(keywords);

  }

	// Process user voice transcript to determine grid and color of object to be placed.
  private void Processtext(string[] keywords)
  {
		// Resolution of screen 
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
		// Condition to check if there are valid numbers in user voice input. (1 and 2 are valid inputs)
		//(2 is valid because twenty three is combination of twenty and three)
    if (selectedNumber.Length < 1 || selectedNumber.Length > 2)
    {
      Debug.Log("Grid Number error");
      return;
    }
		// Handling case of numbers greater than twenty.
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
    if (tempSN > 5)
    {
      tempSN = 5;
    }
    var midpoints = CalculateMidpoints(width, height, tempSN);

    if (midpoints.ContainsKey(finalGridNum - 1))
    {
      Debug.Log(midpoints[finalGridNum - 1].Item1);
      Debug.Log(midpoints[finalGridNum - 1].Item2);
      selectedButton = finalColor;
      sendUserInput(midpoints[finalGridNum - 1].Item1, midpoints[finalGridNum - 1].Item2, finalColor, finalGrid);
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

	// Calculating midpoints of each cell in NxN grid.
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

	// Creating markers.
  private void InstObj(Vector3 wp)
  {
    ang.x = 90;
    string mclr = "";
    var ctr = "";
    string tempKey2 = "";
    if (selectedButton == "green")
    {
      tp = obj1;
      mclr = "Green";
      ctr = GreenCount.ToString();
      GreenCount += 1;
      tempKey2 = "1";
    }
    else if (selectedButton == "red")
    {
      tp = obj2;
      mclr = "Red";
      ctr = RedCount.ToString();
      RedCount += 1;
      tempKey2 = "0";
    }
    else if (selectedButton == "blue")
    {
      tp = obj3;
      mclr = "Blue";
      ctr = BlueCount.ToString();
      BlueCount += 1;
      tempKey2 = "2";
    }
    var gob = Instantiate(tp, wp, Quaternion.Euler(ang));
    GameObject ttxt = gob.transform.GetChild(0).gameObject;
    TextMeshPro mText = ttxt.GetComponent<TextMeshPro>();
    mText.text = mclr + " " + ctr;
    string tempKey = tempKey2 + ctr;
    placed_markers[tempKey] = gob;


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

	// Changing grid based on altitude(plane distance) of drone from ground.
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