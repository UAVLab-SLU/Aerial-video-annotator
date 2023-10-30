
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;


public class IndependentFun : MonoBehaviour
{

    private VideoRend videoRend;
    public string PayloadPrep(string xpos, string ypos, string lat, string lon, string alt, string obj, string ctr, string track)
    {
        Dictionary<string, string> payload = new Dictionary<string, string>();
        payload.Add("xpos", xpos);
        payload.Add("ypos", ypos);
        payload.Add("lat", lat);
        payload.Add("alt", alt);
        payload.Add("lon", lon);
        payload.Add("resh", videoRend.canv.GetComponent<RectTransform>().rect.height.ToString());
        payload.Add("resw", videoRend.canv.GetComponent<RectTransform>().rect.width.ToString());
        payload.Add("track", track);
        payload.Add("obj", obj);
        payload.Add("ctr", ctr);
        string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
        return result;
    }


    // Calculating midpoints of each cell in NxN grid.
    public Dictionary<int, Tuple<int, int>> CalculateMidpoints(int width, int height, int gridsize)
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

    // Convert Bounding boxes of objects from python to Unity bounding boxes
    public Rect ConvertBboxToUnityUI(string[] crds, float W1, float H1, float W2, float H2)
    {
        float y1 = (int)Convert.ToDouble(crds[0]);
        float x1 = (int)Convert.ToDouble(crds[1]);
        float y2 = (int)Convert.ToDouble(crds[2]);
        float x2 = (int)Convert.ToDouble(crds[3]);
        float w = x2 - x1;
        float h = y2 - y1;

        float x1Norm = x1 / W1;
        float y1Norm = y1 / H1;
        float wNorm = w / W1;
        float hNorm = h / H1;

        float x1UI = x1Norm * W2;
        float y1UI = y1Norm * H2;
        float wUI = wNorm * W2;
        float hUI = hNorm * H2;

        float offset = w / 2.0f;

        return new Rect(W2 - x1UI, y1UI + offset, wUI, hUI);
    }

}
