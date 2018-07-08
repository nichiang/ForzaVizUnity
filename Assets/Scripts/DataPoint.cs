using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPoint {

    Vector3 position;
    Quaternion rotation;
    ForzaPacket packet;

	public DataPoint (ForzaPacket packet, Vector3 position, Quaternion rotation)
    {
        this.packet = packet;
        this.position = position;
        this.rotation = rotation;
    }

    public ForzaPacket GetPacket()
    {
        return packet;
    }

    public Vector3 GetPosition()
    {
        return position;
    }

    public Quaternion GetRotation()
    {
        return rotation;
    }
}
