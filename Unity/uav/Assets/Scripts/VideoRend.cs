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

  public Canvas canvas2;

  public GameObject OSPerson;
  public GameObject drone;
  public GameObject obj1;
  public GameObject obj2;
  public GameObject obj3;
  public GameObject pathMrkr;
  public GameObject pathStart;
  public GameObject pathEnd;
  public GameObject arrow;
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
  UdpClient client3;
  IPEndPoint endPoint;
  IPEndPoint endPoint2;

  public IndependentFun IndF;
  public TextProcessor textProcessor;
  public int curgrid;

  private Dictionary<string, GameObject> placed_markers = new Dictionary<string, GameObject>();

  private Dictionary<GameObject, string> tracker_boxes = new Dictionary<GameObject, string>();


  private Dictionary<GameObject, GameObject> objectToArrowMap = new Dictionary<GameObject, GameObject>();

  private Dictionary<GameObject, bool> animationRunningMap = new Dictionary<GameObject, bool>();



  Dictionary<string, Location> locations;

  TextMeshProUGUI dg;

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
  public string selectedButton;

  public Button green;
  public Button blue;
  public Button red;

  public Button hover;
  public Button resume;
  public SpeechRecognizer speechRecognizer;

  public GameObject tracker;

  public GameObject detect;

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


  public List<GameObject> trackerList = new List<GameObject>();

  public List<GameObject> trackingList = new List<GameObject>();

  private float pathMrkTrgr = 0f;

  private float pathMrkIntr = 1f;

  public float animationDuration = 0.5f;

  public float waitDuration = 0.2f;

  void Start()
  {
    client = new UdpClient(8080);
    client2 = new UdpClient(8000);
    client3 = new UdpClient(8005);
    endPoint = new IPEndPoint(IPAddress.Any, 8001);
    endPoint2 = new IPEndPoint(IPAddress.Any, 8002);
    green.onClick.AddListener(GreenButton);
    blue.onClick.AddListener(BlueButton);
    red.onClick.AddListener(RedButton);
    hover.onClick.AddListener(HoverButton);
    resume.onClick.AddListener(ResumeButton);
    blue.gameObject.SetActive(false);
    resume.gameObject.SetActive(false);
    curgrid = 1;
    BlueCount = 0;
    GreenCount = 0;
    RedCount = 0;
    SetGrid();
    OSnextMove = "";
    grid.gameObject.SetActive(false);
    circleMarker = Instantiate(circle, new Vector3(0, -200, 0), Quaternion.identity);

    selectedButton = "";
    // green.interactable = false;
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
            // "green", "blue", "red",
            "victim", "gun"
        };
    // startRecord();
    StartCoroutine(GetOSLocation());
    // StartCoroutine(GetNextMove());
    IndF = FindObjectOfType<IndependentFun>();
    textProcessor = FindObjectOfType<TextProcessor>();


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
                // Debug.Log($"----------{key}");
              }
              // Debug.Log($"{webRequest.downloadHandler.text}------");
              var values = JsonConvert.DeserializeObject<Dictionary<string, int>>(webRequest.downloadHandler.text);
              var tempNextMove = values["color"].ToString() + values["num"].ToString();
              if (placed_markers.ContainsKey(tempNextMove))
              {
                var tempGobj = placed_markers[tempNextMove];

                string color = "";
                if (values["color"] == 0)
                {
                  color = "Gun";
                }
                if (values["color"] == 1)
                {
                  color = "Victim";
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
          if (locations != null)
          {
            foreach (var key in locations.Keys)
            {
              num = locations[key].ctr.ToString();
              if (locations[key].obj == 0)
              {
                tempG = obj2;
                color = "Gun";
                if (locations[key].ctr > RedCount)
                {
                  RedCount = locations[key].ctr;
                  // RedCount += 1;
                }
              }
              if (locations[key].obj == 1)
              {
                tempG = obj1;
                color = "Victim";
                if (locations[key].ctr > GreenCount)
                {
                  GreenCount = locations[key].ctr;
                  // GreenCount += 1;
                }
              }
              if (locations[key].obj == 2)
              {
                tempG = obj3;
                color = "Blue";
                if (locations[key].ctr > BlueCount)
                {
                  BlueCount = locations[key].ctr;
                  // BlueCount += 1;
                }
              }
              var pos = GPSEncoder.GPSToUCS((float)locations[key].lat, (float)locations[key].lon);
              if (locations[key].obj == 0 || locations[key].obj == 1)
              {

                var gob = Instantiate(tempG, pos, Quaternion.Euler(0, 0, 0));
                GameObject ttxt = gob.transform.GetChild(0).gameObject;
                TextMeshPro mText = ttxt.GetComponent<TextMeshPro>();
                mText.text = color + " " + num;
                string tempKey = locations[key].obj.ToString() + locations[key].ctr.ToString();
                placed_markers[tempKey] = gob;

              }
              if (locations[key].obj == 5)
              {
                pos.y = 0f;
                Instantiate(pathMrkr, pos, Quaternion.Euler(new Vector3(90, 0, 0)));
              }
              else if (locations[key].obj == 6)
              {
                pos.y = 0f;
                Instantiate(pathStart, pos, Quaternion.Euler(new Vector3(90, 0, 0)));
              }
              else if (locations[key].obj == 7)
              {
                pos.y = 0f;
                Instantiate(pathEnd, pos, Quaternion.Euler(new Vector3(90, 0, 0)));
              }

            }

          }

          fetchedLocations = true;
        }
      }
      Debug.Log($"{RedCount}----,{GreenCount}");
      RedCount += 1;
      BlueCount += 1;
      GreenCount += 1;
      yield return null;
    }
  }


  private void GenerateDetectionBoxes(string bbox)
  {
    
    foreach (GameObject obj in trackerList)
    {
      Destroy(obj);
    }
    trackerList.Clear();

    if (bbox != "")
    {
      string[] boxes = bbox.Split('n');
      foreach (string box in boxes)
      {
        string[] crds = box.Split('.');
        var trkpos = IndF.ConvertBboxToUnityUI(crds, 720f, 1280f, 1080f, 1920f);
        var trcpos = new Vector3(trkpos.y, trkpos.x);
        GameObject t = Instantiate(detect);
        t.transform.position = trcpos;
        t.transform.SetParent(canvas2.transform);
        trackerList.Add(t);
        tracker_boxes[t] = box;
      }
    }
  }

  private void GenerateTrackerBoxes(string bbox)
  {


    foreach (GameObject obj in trackingList)
    {
      Destroy(obj);
    }
    trackingList.Clear();

    if (bbox != "")
    {
      string[] boxes = bbox.Split('n');
      foreach (string box in boxes)
      {
        string[] crds = box.Split('.');

        var trkpos = IndF.ConvertBboxToUnityUI(crds, 720f, 1280f, 1080f, 1920f);
        // Debug.Log(trkpos.width);
        var trcpos = new Vector3(trkpos.y, trkpos.x);
        // Debug.Log($"Tracker box at{trcpos}");
        GameObject t = Instantiate(tracker);
        t.transform.position = trcpos;
        t.transform.SetParent(canvas2.transform);
        trackingList.Add(t);
        // tracker_boxes[t] = box;
        // Debug.Log($"{x1},{y1},{x2},{y2}");

      }
    }
  }

  private void SendData(string res)
  {
    // string tr = "ooooooooooooooooooooo";
    // byte[] bdata = Encoding.UTF8.GetBytes(tr);
    // client.Send(bdata, bdata.Length, endPoint2);
    Debug.Log($"{res} iiiiiiiiiiiiiiiiiiiii");
    byte[] data = Encoding.UTF8.GetBytes(res);
    client.Send(data, data.Length, endPoint2);
  }

  private void PathMarker(string activity, string tracker)
  {
    var ctr = 0;
    string[] boxes = tracker.Split('n');
    if (activity != "")
    {
      string[] trkr_act = activity.Split('.');
      foreach (string act in trkr_act)
      {
        if (act == "1")
        {
          if (tracker != "")
          {
            var tmp = boxes[ctr];
            string[] crds = boxes[ctr].Split('.');
            var trkpos = IndF.ConvertBboxToUnityUI(crds, 720f, 1280f, 1080f, 1920f);
            var pathXpos = trkpos.y + trkpos.width / 2.0f;
            var pathYpos = canv.GetComponent<RectTransform>().rect.height - trkpos.x + trkpos.height / 2.0f;
            string result = IndF.PayloadPrep(pathXpos.ToString(), pathYpos.ToString(), lat.ToString(), lon.ToString(), alt.ToString(), "5", "0", "");
            // string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
            Debug.Log(result);
            SendData(result);
            ctr += 1;
          }
          // Debug.Log("Tracker active");
        }
        if (act == "0")
        {

          Debug.Log("Tracker inactivee");
        }
      }
    }
    Debug.Log("Path marking triggered");
  }

  // Animate object indicators 
  IEnumerator AnimateArrow(Transform arrowTransform, float moveDuration, float waitTime)
  {
    Vector3 forwardDirection = arrowTransform.up;
    Vector3 startPos = arrowTransform.localPosition;
    Vector3 forwardPos = startPos + (forwardDirection * 10);
    Vector3 backwardPos = startPos - (forwardDirection * 10);

    while (true)
    {

      float elapsedTime = 0;
      while (elapsedTime < moveDuration)
      {
        arrowTransform.localPosition = Vector3.Lerp(startPos, forwardPos, elapsedTime / moveDuration);
        elapsedTime += Time.deltaTime;
        yield return null;
      }

      arrowTransform.localPosition = forwardPos;

      yield return new WaitForSeconds(waitTime);


      elapsedTime = 0;
      while (elapsedTime < moveDuration)
      {
        arrowTransform.localPosition = Vector3.Lerp(forwardPos, backwardPos, elapsedTime / moveDuration);
        elapsedTime += Time.deltaTime;
        yield return null;
      }

      arrowTransform.localPosition = backwardPos;

      yield return new WaitForSeconds(waitTime);
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

      GenerateDetectionBoxes(values["bbox_data"]);
      GenerateTrackerBoxes(values["tracker"]);
      pathMrkTrgr += Time.deltaTime;
      Debug.Log("ooooooooooooooooooooooooooooooooooooooo");
      // Check if it's time to call the function.
      if (pathMrkTrgr >= pathMrkIntr)
      {
        PathMarker(values["tracker_status"], values["tracker"]);

        // Reset the time elapsed.
        pathMrkTrgr = 0f;
      }


    }

    foreach (var key in placed_markers.Keys)
    {
      Vector3 directionToCamera = placed_markers[key].transform.position - Camera.main.transform.position;
      Quaternion lookRotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
      placed_markers[key].transform.rotation = lookRotation;
    }

    foreach (var key in placed_markers.Keys)
    {
      Vector3 screenPos = Camera.main.WorldToScreenPoint(placed_markers[key].transform.position);
      // Debug.Log(screenPos);
      // Debug.Log(placed_markers[key].transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text);
      GameObject arrowObject;

      if (!objectToArrowMap.ContainsKey(placed_markers[key]))
      {
        arrowObject = Instantiate(arrow);
        // GameObject ttxt = arrowObject.transform.GetChild(0).gameObject;
        TextMeshProUGUI mText = arrowObject.GetComponentInChildren<TextMeshProUGUI>(); ;
        mText.text = placed_markers[key].transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text;
        // arrowObject.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = placed_markers[key].transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text;
        arrowObject.transform.SetParent(canvas2.transform);
        objectToArrowMap[placed_markers[key]] = arrowObject;
      }
      else
      {
        arrowObject = objectToArrowMap[placed_markers[key]];
      }
      // Show indicators for objects that are not visible in the screen
      if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
      {
        arrowObject.SetActive(true);

        Vector2 canvasSize = canvas2.GetComponent<RectTransform>().sizeDelta;
        Vector2 canvasPosition = new Vector2(screenPos.x / Screen.width * canvasSize.x, screenPos.y / Screen.height * canvasSize.y) - canvasSize * 0.5f;

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, screenPos.z);
        Vector3 direction = screenPos - screenCenter;
        direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        float clampBorder = 100.0f; // Adjust this value to set how far inside the screen's edge the marker appears
        canvasPosition = new Vector2(
            Mathf.Clamp(canvasPosition.x, -canvasSize.x * 0.5f + clampBorder, canvasSize.x * 0.5f - clampBorder),
            Mathf.Clamp(canvasPosition.y, -canvasSize.y * 0.5f + clampBorder, canvasSize.y * 0.5f - clampBorder)
        );

        arrowObject.transform.localPosition = canvasPosition;
        if (!animationRunningMap.ContainsKey(arrowObject) || !animationRunningMap[arrowObject])
        {
          StartCoroutine(AnimateArrow(arrowObject.transform, animationDuration, waitDuration));
          animationRunningMap[arrowObject] = true;
        }
      }
      else
      {
        arrowObject.SetActive(false);
        if (animationRunningMap.ContainsKey(arrowObject) && animationRunningMap[arrowObject])
        {
          StopCoroutine(AnimateArrow(arrowObject.transform, animationDuration, waitDuration));
          animationRunningMap[arrowObject] = false;
        }
        Debug.Log("Visibleee");
      }
    }

    Vector3 tempCamDirec = OSPerson.transform.position - Camera.main.transform.position;
    Quaternion tempRotn = Quaternion.LookRotation(tempCamDirec, Vector3.up);
    OSPerson.transform.rotation = tempRotn;

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
      InstObj(world_pos2, values2["obj"]);

      // if (values2["obj"] == "0" || values2["obj"] == "1" || values2["obj"] == "2"){
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
      // }


    }

    if (client3.Available > 0)
    {
      byte[] data = client3.Receive(ref endPoint);
      string text = Encoding.UTF8.GetString(data);
      var values3 = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
      // Debug.Log(values3["point"]);
      var obj = "";
      if (values3["point"] == "start")
      {
        obj = "6";
      }
      else if (values3["point"] == "last")
      {
        obj = "7";
      }
      string[] crds = values3["box"].Split('.');
      var trkpos = IndF.ConvertBboxToUnityUI(crds, 720f, 1280f, 1080f, 1920f);
      var pathXpos = trkpos.y + trkpos.width / 2.0f;
      var pathYpos = canv.GetComponent<RectTransform>().rect.height - trkpos.x + trkpos.height / 2.0f;
      Debug.Log($"{pathXpos},{pathYpos} are midPointsssssssssssssssssssssssssssssssssssss");
      string result = IndF.PayloadPrep(pathXpos.ToString(), pathYpos.ToString(), lat.ToString(), lon.ToString(), alt.ToString(), obj, "0", "");
      // Debug.Log(result);
      SendData(result);

    }


    //Placing object when user clicks on the screen
    if (Input.GetMouseButtonDown(0))
    {
      // To prevent placing object when user clicks on a button
      PointerEventData pe = new PointerEventData(EventSystem.current);
      pe.position = Input.mousePosition;
      List<RaycastResult> resList = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pe, resList);

      foreach (RaycastResult obj in resList)
      {
        if (tracker_boxes.ContainsKey(obj.gameObject))
        {
          // Debug.Log(tracker_boxes[obj.gameObject]);
          Dictionary<string, string> payload = new Dictionary<string, string>();
          payload.Add("track", tracker_boxes[obj.gameObject]);
          string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
          Debug.Log(result);
          byte[] data = Encoding.UTF8.GetBytes(result);
          client2.Send(data, data.Length, endPoint);
        }
      }
      Vector3 mousePos = Input.mousePosition;
      if (selectedButton != "")
      {


        if (resList.Count == 1)
        {

          var mouse_y = canv.GetComponent<RectTransform>().rect.height - mousePos.y;
          var obj = "";
          var ctr = "";
          if (selectedButton == "red")
          {
            obj = "0";
            ctr = RedCount.ToString();
          }
          if (selectedButton == "green")
          {
            obj = "1";
            ctr = GreenCount.ToString();
          }
          if (selectedButton == "blue")
          {
            obj = "2";
            ctr = BlueCount.ToString();
          }

          string result = IndF.PayloadPrep(mousePos.x.ToString(), mouse_y.ToString(), lat.ToString(), lon.ToString(), alt.ToString(), obj, ctr, "");

          Debug.Log(result);
          byte[] data = Encoding.UTF8.GetBytes(result);
          client.Send(data, data.Length, endPoint);

        }
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

  public void GreenButton()
  {
    Debug.Log("You have clicked Green Button!");
    selectedButton = "green";
    resetButtons();
    green.interactable = false;
  }

  public void RedButton()
  {
    Debug.Log("You have clicked Red Button!");
    selectedButton = "red";
    resetButtons();
    red.interactable = false;
  }

  public void BlueButton()
  {
    Debug.Log("You have clicked Blue Button!");
    selectedButton = "blue";
    resetButtons();
    blue.interactable = false;
  }

  public void HoverButton()
  {
    Debug.Log("You have clicked Hover Button!");
    hover.gameObject.SetActive(false);
    resume.gameObject.SetActive(true);

    Dictionary<string, string> payload = new Dictionary<string, string>();
    payload.Add("hover", true.ToString());
    string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
    byte[] data = Encoding.UTF8.GetBytes(result);
    client.Send(data, data.Length, endPoint);
  }

  public void ResumeButton()
  {
    Debug.Log("You have clicked Resume Button!");
    hover.gameObject.SetActive(true);
    resume.gameObject.SetActive(false);

    Dictionary<string, string> payload = new Dictionary<string, string>();
    payload.Add("hover", false.ToString());
    string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
    byte[] data = Encoding.UTF8.GetBytes(result);
    client.Send(data, data.Length, endPoint);
  }

  // Placing object based on user voice input.
  public void sendUserInput(float x, float y, string clr, string grd)
  {
    var dobj = Instantiate(dialogue);
    dg = dobj.GetComponentInChildren<TextMeshProUGUI>();
    dg.text = clr + " marker is placed in grid " + grd;
    Destroy(dobj, 3);
    var obj = "";
    var ctr = "";
    if (selectedButton == "red")
    {
      obj = "0";
      ctr = RedCount.ToString();
    }
    if (selectedButton == "green")
    {
      obj = "1";
      ctr = GreenCount.ToString();
    }
    if (selectedButton == "blue")
    {
      obj = "2";
      ctr = BlueCount.ToString();
    }
    string result = IndF.PayloadPrep(x.ToString(), y.ToString(), lat.ToString(), lon.ToString(), alt.ToString(), obj, ctr, "");
    // string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
    byte[] data = Encoding.UTF8.GetBytes(result);
    client.Send(data, data.Length, endPoint);
  }


  // Record user voice inputs and place objects in respective grid.
  private void OnResult(Result result)
  {
    Debug.Log(result.text);
    string[] keywords = result.text.Split(' ');
    textProcessor.Processtext(keywords);
  }



  // Creating markers.
  private void InstObj(Vector3 wp, string oj)
  {
    ang.x = 90;
    string mclr = "";
    var ctr = "";
    string tempKey2 = "";
    if (oj == "1")
    {
      tp = obj1;
      mclr = "Victim";
      ctr = GreenCount.ToString();
      GreenCount += 1;
      tempKey2 = "1";
    }
    else if (oj == "0")
    {
      tp = obj2;
      mclr = "Gun";
      ctr = RedCount.ToString();
      RedCount += 1;
      tempKey2 = "0";
    }
    else if (oj == "2")
    {
      tp = obj3;
      mclr = "Blue";
      ctr = BlueCount.ToString();
      BlueCount += 1;
      tempKey2 = "2";
    }
    if (oj == "1" || oj == "0")
    {
      var gob = Instantiate(tp, wp, Quaternion.Euler(ang));
      GameObject ttxt = gob.transform.GetChild(0).gameObject;
      TextMeshPro mText = ttxt.GetComponent<TextMeshPro>();
      mText.text = mclr + " " + ctr;
      string tempKey = tempKey2 + ctr;
      placed_markers[tempKey] = gob;
    }
    else if (oj == "5")
    {
      wp.y = 0f;
      Instantiate(pathMrkr, wp, Quaternion.Euler(new Vector3(90, 0, 0)));
    }

    else if (oj == "6")
    {
      wp.y = 0f;
      Instantiate(pathStart, wp, Quaternion.Euler(new Vector3(90, 0, 0)));
    }

    else if (oj == "7")
    {
      wp.y = 0f;
      Instantiate(pathEnd, wp, Quaternion.Euler(new Vector3(90, 0, 0)));
    }


    resetButtons();
    selectedButton = "";
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