using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using TMPro;

public class OSOupdates : MonoBehaviour
{

  private VideoRend videoRend;
  public TextProcessor textProcessor;

  Dictionary<string, Location> locations;
  void Start()
  {
    videoRend = FindObjectOfType<VideoRend>();
    textProcessor = FindObjectOfType<TextProcessor>();
  }
  // Get Onsite operator and update it every second.
  public IEnumerator GetOSLocation()
  {
    while (true)
    {

      if (!videoRend.Po)
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
            videoRend.OSPerson.transform.position = os_pos;

          }
        }
      }
      yield return new WaitForSeconds(1);
    }

  }
  // Get Onsite Operator Next Move.
  public IEnumerator GetNextMove()
  {
    while (true)
    {

      if (!videoRend.Po)
      {
        if (videoRend.fetchedLocations)
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
              foreach (var key in videoRend.placed_markers.Keys)
              {
                // Debug.Log($"----------{key}");
              }
              // Debug.Log($"{webRequest.downloadHandler.text}------");
              var values = JsonConvert.DeserializeObject<Dictionary<string, int>>(webRequest.downloadHandler.text);
              var tempNextMove = values["color"].ToString() + values["num"].ToString();
              if (videoRend.placed_markers.ContainsKey(tempNextMove))
              {
                var tempGobj = videoRend.placed_markers[tempNextMove];

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
                if (tempNextMove != videoRend.OSnextMove)
                {
                  Debug.Log($"NextMove changed");
                  var dobj = Instantiate(videoRend.nextMvNotif);
                  videoRend.dg = dobj.GetComponentInChildren<TextMeshProUGUI>();
                  videoRend.dg.text = "OnSite Operator next move changed to " + color + " " + values["num"].ToString();
                  videoRend.circleMarker.transform.position = tempGobj.transform.position;
                  videoRend.circleMarker.transform.rotation = tempGobj.transform.rotation;
                  Destroy(dobj, 3);
                  videoRend.OSnextMove = tempNextMove;
                }
                float distance = 0f;
                distance = Vector3.Distance(tempGobj.transform.position, videoRend.OSPerson.transform.position);
                // dist.text = "Onsite Operator is " + distance + " from target";
                videoRend.nextMv.text = "Onsite Operator next Move: " + color + " " + values["num"].ToString() + " and is " + distance + "m from target";
              }
            }
          }
        }
      }
      yield return new WaitForSeconds(1);
    }
  }

  // Get already placed locations from Firebase database.
  public IEnumerator GetLocations()
  {

    if (!videoRend.Po)
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
          GameObject tempG = videoRend.obj1;
          // Creating respective markers for fetched locations and updating marker(Example, Green 1 to Green 4) count.
          if (locations != null)
          {
            foreach (var key in locations.Keys)
            {
              num = locations[key].ctr.ToString();
              if (locations[key].obj == 0)
              {
                tempG = videoRend.obj2;
                color = "Gun";
                if (locations[key].ctr > videoRend.RedCount)
                {
                  videoRend.RedCount = locations[key].ctr;
                  // videoRend.RedCount += 1;
                }
              }
              if (locations[key].obj == 1)
              {
                tempG = videoRend.obj1;
                color = "Victim";
                if (locations[key].ctr > videoRend.GreenCount)
                {
                  videoRend.GreenCount = locations[key].ctr;
                  // videoRend.GreenCount += 1;
                }
              }
              if (locations[key].obj == 2)
              {
                tempG = videoRend.obj3;
                color = "Blue";
                if (locations[key].ctr > videoRend.BlueCount)
                {
                  videoRend.BlueCount = locations[key].ctr;
                  // videoRend.BlueCount += 1;
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
                videoRend.placed_markers[tempKey] = gob;

              }
              if (locations[key].obj == 5)
              {
                pos.y = 0f;
                Instantiate(videoRend.pathMrkr, pos, Quaternion.Euler(new Vector3(90, 0, 0)));
              }
              else if (locations[key].obj == 6)
              {
                pos.y = 0f;
                Instantiate(videoRend.pathStart, pos, Quaternion.Euler(new Vector3(90, 0, 0)));
              }
              else if (locations[key].obj == 7)
              {
                pos.y = 0f;
                Instantiate(videoRend.pathEnd, pos, Quaternion.Euler(new Vector3(90, 0, 0)));
              }
            }
          }
          videoRend.fetchedLocations = true;
        }
      }
      Debug.Log($"{videoRend.RedCount}----,{videoRend.GreenCount}");
      videoRend.RedCount += 1;
      videoRend.BlueCount += 1;
      videoRend.GreenCount += 1;
      yield return null;
    }
  }

}
