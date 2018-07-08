using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPoints : MonoBehaviour {

    static List<DataPoint> allPoints = new List<DataPoint>();

    static Vector3 lastPosition;
    static float lastTimestamp = 0;
    static float frameTick;

	// Use this for initialization
	void Start () {
        
	}

    public static DataPoint AddPoint (ForzaPacket packet)
    {
        if (lastTimestamp == 0)
        {
            lastPosition = new Vector3(0, 0, 0);
            lastTimestamp = packet.TimestampMS - 16; // 16 represents one frame at 60 fps, which is the rate FM7 feeds the datastream at
        }

        frameTick = (packet.TimestampMS - lastTimestamp) / 1000f;

        Quaternion newRotation = Quaternion.Euler(Mathf.Rad2Deg * packet.Pitch, Mathf.Rad2Deg * packet.Yaw, Mathf.Rad2Deg * packet.Roll);
        Vector3 newPosition = lastPosition;
        newPosition += newRotation * new Vector3(packet.VelocityX * frameTick, packet.VelocityX * frameTick, packet.VelocityZ * frameTick);

        DataPoint newPoint = new DataPoint(packet, newPosition, newRotation);
        
        allPoints.Add(newPoint);

        lastPosition = newPosition;
        lastTimestamp = packet.TimestampMS;

        return newPoint;
    }

    public static DataPoint GetPoint (int i)
    {
        if (i >= 0 && i < allPoints.Count)
        {
            return allPoints[i];
        }
        else
        {
            return null;
        }
    }

    public static DataPoint GetCurrentPoint()
    {
        return allPoints.Count > 1 ? allPoints[allPoints.Count - 1] : null;
    }

    public static DataPoint GetPrevPoint()
    {
        return allPoints.Count > 2 ? allPoints[allPoints.Count - 2] : null;
    }

    public static int GetLatestPacketIndex ()
    {
        return allPoints.Count - 1;
    }

    public static float GetFrameTick()
    {
        return frameTick;
    }
}
