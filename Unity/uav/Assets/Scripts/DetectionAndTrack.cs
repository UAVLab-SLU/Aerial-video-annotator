using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionAndTracking : MonoBehaviour
{
  // Start is called before the first frame update
  private VideoRend videoRend;
  private IndependentFun IndF;
  void Start()
  {
    videoRend = FindObjectOfType<VideoRend>();
    IndF = FindObjectOfType<IndependentFun>();
  }

  public void GenerateDetectionBoxes(string bbox)
  {

    foreach (GameObject obj in videoRend.trackerList)
    {
      Destroy(obj);
    }
    videoRend.trackerList.Clear();

    if (bbox != "")
    {
      string[] boxes = bbox.Split('n');
      foreach (string box in boxes)
      {
        string[] crds = box.Split('.');
        var trkpos = IndF.ConvertBboxToUnityUI(crds, 720f, 1280f, 1080f, 1920f);
        var trcpos = new Vector3(trkpos.y, trkpos.x);
        GameObject t = Instantiate(videoRend.detect);
        t.transform.position = trcpos;
        t.transform.SetParent(videoRend.canvas2.transform);
        videoRend.trackerList.Add(t);
        videoRend.tracker_boxes[t] = box;
      }
    }
  }

  public void GenerateTrackerBoxes(string bbox)
  {


    foreach (GameObject obj in videoRend.trackingList)
    {
      Destroy(obj);
    }
    videoRend.trackingList.Clear();

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
        GameObject t = Instantiate(videoRend.tracker);
        t.transform.position = trcpos;
        t.transform.SetParent(videoRend.canvas2.transform);
        videoRend.trackingList.Add(t);
        // tracker_boxes[t] = box;
        // Debug.Log($"{x1},{y1},{x2},{y2}");

      }
    }
  }

}
