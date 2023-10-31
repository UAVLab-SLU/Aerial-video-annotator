using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections;


public class TextProcessor : MonoBehaviour
{
    // Process user voice transcript to determine grid and color of object to be placed.

    private VideoRend videoRend;
    private IndependentFun IndF;
    public void Processtext(string[] keywords)
    {
        // Resolution of screen 
        int width = (int)videoRend.canv.GetComponent<RectTransform>().rect.width;
        int height = (int)videoRend.canv.GetComponent<RectTransform>().rect.height;
        string[] colors = { "victim", "gun" };
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
        int tempSN = videoRend.curgrid + 1;
        if (tempSN > 5)
        {
            tempSN = 5;
        }
        var midpoints = IndF.CalculateMidpoints(width, height, tempSN);

        if (midpoints.ContainsKey(finalGridNum - 1))
        {
            
            if (finalColor == "victim")
            {
                videoRend.selectedButton = "green";
            }
            if (finalColor == "gun")
            {
                videoRend.selectedButton = "red";
            }
            videoRend.sendUserInput(midpoints[finalGridNum - 1].Item1, midpoints[finalGridNum - 1].Item2, finalColor, finalGrid);
            if (videoRend.selectedButton == "green")
            {
                videoRend.GreenButton();
            }
            else if (videoRend.selectedButton == "red")
            {
                videoRend.RedButton();
            }
            else if (videoRend.selectedButton == "blue")
            {
                videoRend.BlueButton();
            }
        }
        else
        {
            Debug.Log("Grid Number error");
            return;
        }
    }
}

// Original code for future reference

//  // Placing object based on user voice input.
//   public void sendUserInput(float x, float y, string clr, string grd)
//   {
//     var dobj = Instantiate(dialogue);
//     dg = dobj.GetComponentInChildren<TextMeshProUGUI>();
//     dg.text = clr + " marker is placed in grid " + grd;
//     Destroy(dobj, 3);
//     var obj = "";
//     var ctr = "";
//     if (selectedButton == "red")
//     { 
//       obj = "0";
//       ctr = RedCount.ToString();
//     }
//     if (selectedButton == "green")
//     {
//       obj = "1";
//       ctr = GreenCount.ToString();
//     }
//     if (selectedButton == "blue")
//     {
//       obj = "2";
//       ctr = BlueCount.ToString();
//     }
//     string result = IndF.PayloadPrep(x.ToString(),y.ToString(),lat.ToString(),lon.ToString(),alt.ToString(),obj,ctr,"");
//     // string result = string.Join(",", payload.Select(x => '"' + x.Key + '"' + ": " + '"' + x.Value + '"'));
//     byte[] data = Encoding.UTF8.GetBytes(result);
//     client.Send(data, data.Length, endPoint);
//   }


//   // Record user voice inputs and place objects in respective grid.
//   private void OnResult(Result result)
//   {
//     Debug.Log(result.text);

//     string[] keywords = result.text.Split(' ');

//     Processtext(keywords);

//   }

//   // Process user voice transcript to determine grid and color of object to be placed.
//   private void Processtext(string[] keywords)
//   {
//     // Resolution of screen 
//     int width = (int)canv.GetComponent<RectTransform>().rect.width;
//     int height = (int)canv.GetComponent<RectTransform>().rect.height;
//     string[] colors = { "victim", "gun" };
//     string finalColor = "";
//     string finalGrid = "";
//     int finalGridNum = 0;
//     string[] selectedColor = keywords.Intersect(colors).ToArray();
//     if (selectedColor.Length != 1)
//     {
//       Debug.Log("Marker color error");
//       return;
//     }
//     finalColor = selectedColor[0];
//     string[] numbers =  {
//             "one", "two", "three", "four", "five",
//             "six", "seven", "eight", "nine", "ten",
//             "eleven", "twelve", "thirteen", "fourteen", "fifteen",
//             "sixteen", "seventeen", "eighteen", "nineteen", "twenty",
//         };

//     string[] selectedNumber = keywords.Intersect(numbers).ToArray();
//     // Condition to check if there are valid numbers in user voice input. (1 and 2 are valid inputs)
//     //(2 is valid because twenty three is combination of twenty and three)
//     if (selectedNumber.Length < 1 || selectedNumber.Length > 2)
//     {
//       Debug.Log("Grid Number error");
//       return;
//     }
//     // Handling case of numbers greater than twenty.
//     if (selectedNumber.Contains("twenty"))
//     {
//       if (selectedNumber.Length == 1)
//       {
//         finalGrid = "twenty";
//       }
//       if (selectedNumber.Length == 2)
//       {
//         if (selectedNumber[0] == "twenty")
//         {
//           if (selectedNumber[1] == "one")
//           {
//             finalGrid = "twenty one";
//           }
//           else if (selectedNumber[1] == "two")
//           {
//             finalGrid = "twenty two";
//           }
//           else if (selectedNumber[1] == "three")
//           {
//             finalGrid = "twenty three";
//           }
//           else if (selectedNumber[1] == "four")
//           {
//             finalGrid = "twenty four";
//           }
//           else if (selectedNumber[1] == "five")
//           {
//             finalGrid = "twenty five";
//           }
//           else
//           {
//             Debug.Log("Grid Number error");
//           }
//         }
//         else
//         {
//           Debug.Log("Grid Number error");
//           return;
//         }
//       }
//     }
//     else
//     {
//       if (selectedNumber.Length != 1)
//       {
//         Debug.Log("Grid Number error");
//         return;
//       }
//       else
//       {
//         finalGrid = selectedNumber[0];
//       }
//     }
//     Debug.Log(finalGrid);
//     Debug.Log(finalColor);
//     Dictionary<string, int> numberDictionary = new Dictionary<string, int>
//     {
//         { "one", 1 },{ "two", 2 },{ "three", 3 },{ "four", 4 },{ "five", 5 },
//         { "six", 6 },{ "seven", 7 },{ "eight", 8 },{ "nine", 9 },{ "ten", 10 },
//         { "eleven", 11 },{ "twelve", 12 },{ "thirteen", 13 },{ "fourteen", 14 },{ "fifteen", 15 },
//         { "sixteen", 16 },{ "seventeen", 17 },{ "eighteen", 18 },{ "nineteen", 19 },{ "twenty", 20 },
//         { "twenty one", 21 },{ "twenty two", 22 },{ "twenty three", 23 },{ "twenty four", 24 },{ "twenty five", 25 }
//     };
//     if (numberDictionary.ContainsKey(finalGrid))
//     {
//       finalGridNum = numberDictionary[finalGrid];
//     }
//     else
//     {
//       Debug.Log("Grid Number error");
//       return;
//     }
//     Debug.Log(finalGridNum);
//     int tempSN = curgrid + 1;
//     if (tempSN > 5)
//     {
//       tempSN = 5;
//     }
//     var midpoints = IndF.CalculateMidpoints(width, height, tempSN);

//     if (midpoints.ContainsKey(finalGridNum - 1))
//     {
//       Debug.Log(midpoints[finalGridNum - 1].Item1);
//       Debug.Log(midpoints[finalGridNum - 1].Item2);
//       if (finalColor == "victim")
//       {
//         selectedButton = "green";
//       }
//       if (finalColor == "gun")
//       {
//         selectedButton = "red";
//       }
//       sendUserInput(midpoints[finalGridNum - 1].Item1, midpoints[finalGridNum - 1].Item2, finalColor, finalGrid);
//       if (selectedButton == "green")
//       {
//         GreenButton();
//       }
//       else if (selectedButton == "red")
//       {
//         RedButton();
//       }
//       else if (selectedButton == "blue")
//       {
//         BlueButton();
//       }
//     }
//     else
//     {
//       Debug.Log("Grid Number error");
//       return;
//     }
//   }
