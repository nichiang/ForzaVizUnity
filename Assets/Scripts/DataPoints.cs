using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPoints : MonoBehaviour {

    static List<DataPoint> allPoints = new List<DataPoint>();

    static Vector3 lastPosition;
    static float lastTimestamp = 0;
    static float frameTick = 0;

    static double timestampTotal = 0;

    public static DataPoint AddPoint (ForzaPacket packet)
    {
        if (lastTimestamp == 0)
        {
            lastPosition = new Vector3(0, 0, 0);
            lastTimestamp = packet.TimestampMS - 16; // 16 ms represents one frame at 60 fps, which is the rate FM7 feeds the datastream at
        }

        frameTick = (packet.TimestampMS - lastTimestamp) / 1000f;

        Quaternion newRotation = Quaternion.Euler(Mathf.Rad2Deg * packet.Pitch, Mathf.Rad2Deg * packet.Yaw, Mathf.Rad2Deg * packet.Roll);
        Vector3 newPosition = lastPosition;

        // Keeping as reference. Results in floating point rounding issues
        //newPosition += newRotation * new Vector3(packet.VelocityX * frameTick, packet.VelocityX * frameTick, packet.VelocityZ * frameTick); 

        newPosition += VectorRotation(newRotation, packet.VelocityX, packet.VelocityY, packet.VelocityZ) * frameTick;

        DataPoint newPoint = new DataPoint(packet, newPosition, newRotation);
        
        allPoints.Add(newPoint);

        timestampTotal += packet.TimestampMS - lastTimestamp;
        DebugConsole.Write("Packet frame time average: " + (timestampTotal / allPoints.Count));

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

    public static bool IsValidIndex (int i)
    {
        return i >= 0 && i < allPoints.Count;
    }

    public static void Reset ()
    {
        allPoints.Clear();
        lastPosition = Vector3.zero;
        lastTimestamp = 0;
    }

    // Double version of Quaternion * operator to avoid float rounding issue
    private static Vector3 VectorRotation (Quaternion quat, double x, double y, double z)
    {
        double num = quat.x * 2f;
        double num2 = quat.y * 2f;
        double num3 = quat.z * 2f;
        double num4 = quat.x * num;
        double num5 = quat.y * num2;
        double num6 = quat.z * num3;
        double num7 = quat.x * num2;
        double num8 = quat.x * num3;
        double num9 = quat.y * num3;
        double num10 = quat.w * num;
        double num11 = quat.w * num2;
        double num12 = quat.w * num3;
        Vector3 result;
        result.x = (float)((1f - (num5 + num6)) * x + (num7 - num12) * y + (num8 + num11) * z);
        result.y = (float)((num7 + num12) * x + (1f - (num4 + num6)) * y + (num9 - num10) * z);
        result.z = (float)((num8 - num11) * x + (num9 + num10) * y + (1f - (num4 + num5)) * z);
        return result;
    }
}
