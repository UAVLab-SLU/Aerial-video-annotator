using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Location 
{
    // Start is called before the first frame update
   public double lat {get;set;}
   public double lon {get;set;}
   public int obj {get;set;}
    
   public int ctr {get;set;}

//    public Location(double lat,double lon,int obj){
//     Debug.Log("88888888888");
//     this.lat = lat;
//     this.lon = lon;
//     this.obj = obj;
//    }
}

[Serializable]
public class OSLocation 
{
    // Start is called before the first frame update
   public double lat {get;set;}
   public double lon {get;set;}

}


