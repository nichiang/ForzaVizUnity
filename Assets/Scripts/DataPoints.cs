using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPoints : MonoBehaviour {

    static List<ForzaPacket> allDataPoints = new List<ForzaPacket>();

	// Use this for initialization
	void Start () {
        
	}

    public static void AddPoint (ForzaPacket packet)
    {
        allDataPoints.Add(packet);
    }

    public static ForzaPacket GetPoint (int i)
    {
        if (i >= 0 && i < allDataPoints.Count)
            return allDataPoints[i];
        else
            return null;
    }
}
