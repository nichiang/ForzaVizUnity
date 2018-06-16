using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualizations : MonoBehaviour {

    public enum CarVizType
    {
        Generic,
        SuspensionViz,
        GForce
    };

    private enum MeshTopVertex
    {
        FrontLeft = 0,
        FrontRight = 1,
        RearLeft = 2,
        RearRight = 3,
        Center = 4,
        FrontCenter = 5,
        RearCenter = 6,
        LeftCenter = 7,
        RightCenter = 8
    };

    public CarVizType carVisualizationType = CarVizType.Generic;

    public bool ShowElevation = true;
    public bool ShowSuspensionTravel = true;

    public GameObject genericTrailPrefab;
    public GameObject suspensionVizCarPrefab;
    public GameObject gforceVizCarPrefab;

    public GameObject FLTire, FRTire, RLTire, RRTire;

    public Gradient suspensionGradient;

    public MainCamera mainCamera;

    private GameObject lastGo;
    private Vector3 lastPoint;
    private UInt32 lastTimestamp = 0;

    private Dictionary<MeshTopVertex, int[]> meshTopVertices;

    void Start ()
    {
        meshTopVertices = new Dictionary<MeshTopVertex, int[]>
        {
            { MeshTopVertex.FrontLeft, new int[] { 4, 15, 31 } },
            { MeshTopVertex.FrontRight, new int[] { 0, 9, 32 } },
            { MeshTopVertex.RearLeft, new int[] { 10, 19, 29 } },
            { MeshTopVertex.RearRight, new int[] { 5, 14, 30 } },
            { MeshTopVertex.Center, new int[] { 25 } },
            { MeshTopVertex.FrontCenter, new int[] { 1, 24 } },
            { MeshTopVertex.RearCenter, new int[] { 11, 28 } },
            { MeshTopVertex.LeftCenter, new int[] { 16, 26 } },
            { MeshTopVertex.RightCenter, new int[] { 6, 27 } }
        };

        /*
        Mesh vertices

        0   0.4999999, 0.5, 0.5			// Front Right
        1   -4.371139E-08, 0.5, 0.5		// Front Center
        2   0.5, -0.4999999, 0.5
        3   -0.5, -0.5, 0.5
        4   -0.5, 0.5, 0.4999999		// Front Left
        5   0.5, 0.5, -0.5				// Rear Right
        6   0.5, 0.5, 2.185569E-08		// Right Center
        7   0.5, -0.5, -0.4999999
        8   0.5, -0.4999999, 0.5
        9   0.4999999, 0.5, 0.5			// Front Right
        10  -0.5, 0.4999999, -0.5		// Rear Left
        11  -1.776357E-15, 0.5, -0.5	// Rear Center
        12  -0.4999999, -0.5, -0.5
        13  0.5, -0.5, -0.4999999
        14  0.5, 0.5, -0.5				// Rear Right
        15  -0.5, 0.5, 0.4999999		// Front Left
        16  -0.5, 0.5, -2.185569E-08	// Left Center
        17  -0.5, -0.5, 0.5
        18  -0.4999999, -0.5, -0.5
        19  -0.5, 0.4999999, -0.5		// Rear Left
        20  0.5, -0.4999999, 0.5
        21  -0.5, -0.5, 0.5
        22  -0.4999999, -0.5, -0.5
        23  0.5, -0.5, -0.4999999
        24  -4.371139E-08, 0.5, 0.5		// Front Center
        25  -2.18557E-08, 0.5, 0        // Center
        26  -0.5, 0.5, -2.185569E-08	// Left Center
        27  0.5, 0.5, 2.185569E-08		// Right Center
        28  -1.776357E-15, 0.5, -0.5	// Rear Center
        29  -0.5, 0.4999999, -0.5		// Rear Left
        30  0.5, 0.5, -0.5				// Rear Right
        31  -0.5, 0.5, 0.4999999		// Front Left
        32  0.4999999, 0.5, 0.5			// Front Right
        */
    }

    public void DrawCar (ForzaPacket packet)
    {
        if (lastTimestamp == 0)
        {
            lastPoint = new Vector3(0, 0, 0);
            lastTimestamp = packet.TimestampMS - 16; // 16 represents one frame at 60 fps
        }

        float frameTick = (packet.TimestampMS - lastTimestamp) / 1000f;


        // Setting car position

        GameObject carPrefab = genericTrailPrefab;

        switch (carVisualizationType)
        {
            case CarVizType.SuspensionViz:
                carPrefab = suspensionVizCarPrefab;
                break;
            case CarVizType.GForce:
                carPrefab = gforceVizCarPrefab;
                break;
        }

        GameObject go = Instantiate(carPrefab);
        go.transform.SetParent(this.transform);
        go.transform.position = lastPoint;

        /*
        Using yaw, pitch, roll below gives better results, less prone to floating point rounding issues

        go.transform.Rotate(
            Mathf.Rad2Deg * packet.AngularVelocityX * frameTick,
            Mathf.Rad2Deg * packet.AngularVelocityY * frameTick,
            Mathf.Rad2Deg * packet.AngularVelocityZ * frameTick,
            Space.Self
        );
        */

        go.transform.eulerAngles = new Vector3(Mathf.Rad2Deg * packet.Pitch, Mathf.Rad2Deg * packet.Yaw, Mathf.Rad2Deg * packet.Roll);

        go.transform.Translate(
            packet.VelocityX * frameTick,
            packet.VelocityY * frameTick,
            packet.VelocityZ * frameTick,
            Space.Self
        );

        if (!ShowElevation)
            go.transform.position = new Vector3(go.transform.position.x, 0, go.transform.position.z);

        mainCamera.FollowCurrentPoint(go);

        lastGo = go;
        lastPoint = go.transform.position;
        lastTimestamp = packet.TimestampMS;

        ElevationViz(go);
        GForceViz(packet, go, frameTick);
        SuspensionTravelMeshColourViz(packet, go);

        if (ShowSuspensionTravel)
            SuspensionTravelTireViz(packet);
    }

    void ElevationViz (GameObject go)
    {
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.useWorldSpace = true;
        line.startWidth = 0.005f;
        line.startColor = Color.grey;
        line.endWidth = 0.005f;
        line.endColor = Color.grey;
        line.material = (Material)Resources.Load("Elevation Line", typeof(Material));
        line.SetPosition(0, go.transform.position);
        line.SetPosition(1, new Vector3(go.transform.position.x, 0, go.transform.position.z));
    }

    void GForceViz (ForzaPacket packet, GameObject go, float frameTick)
    {
        if (carVisualizationType != CarVizType.GForce)
            return;

        Transform arrow = go.transform.GetChild(0);

        Vector3 accelVector = new Vector3(packet.AccelerationX, packet.AccelerationY, packet.AccelerationZ);

        arrow.transform.forward = accelVector;

        float gforce = accelVector.magnitude / 9.80665f;

        Vector3 scaleArrow = arrow.transform.localScale;
        scaleArrow.z *= gforce;
        arrow.transform.localScale = scaleArrow;
    }

    void SuspensionTravelTireViz (ForzaPacket packet)
    {
        if (mainCamera.IsFollowing())
        {
            FLTire.transform.localPosition = new Vector3(FLTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelFrontLeft * 0.4f, FLTire.transform.localPosition.z);
            FRTire.transform.localPosition = new Vector3(FRTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelFrontRight * 0.4f, FRTire.transform.localPosition.z);
            RLTire.transform.localPosition = new Vector3(RLTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelRearLeft * 0.4f, RLTire.transform.localPosition.z);
            RRTire.transform.localPosition = new Vector3(RRTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelRearRight * 0.4f, RRTire.transform.localPosition.z);
        }
        else
        {
            FLTire.transform.localPosition = new Vector3(FLTire.transform.localPosition.x, 3.2f, FLTire.transform.localPosition.z);
            FRTire.transform.localPosition = new Vector3(FRTire.transform.localPosition.x, 3.2f, FRTire.transform.localPosition.z);
            RLTire.transform.localPosition = new Vector3(RLTire.transform.localPosition.x, 3.2f, RLTire.transform.localPosition.z);
            RRTire.transform.localPosition = new Vector3(RRTire.transform.localPosition.x, 3.2f, RRTire.transform.localPosition.z);
        }
    }

    void SuspensionTravelMeshColourViz (ForzaPacket packet, GameObject go)
    {
        if (carVisualizationType != CarVizType.SuspensionViz)
            return;
        
        // Setting mesh colours

        Mesh carMesh = go.GetComponent<MeshFilter>().mesh;
        Color32[] colours = new Color32[carMesh.vertices.Length];

        for (int i = 0; i < colours.Length; i++)
        {
            colours[i] = Color.white;
        }

        colours = SetVertexColour(colours, MeshTopVertex.FrontLeft, suspensionGradient.Evaluate(packet.NormalizedSuspensionTravelFrontLeft));
        colours = SetVertexColour(colours, MeshTopVertex.FrontRight, suspensionGradient.Evaluate(packet.NormalizedSuspensionTravelFrontRight));
        colours = SetVertexColour(colours, MeshTopVertex.RearLeft, suspensionGradient.Evaluate(packet.NormalizedSuspensionTravelRearLeft));
        colours = SetVertexColour(colours, MeshTopVertex.RearRight, suspensionGradient.Evaluate(packet.NormalizedSuspensionTravelRearRight));

        carMesh.colors32 = colours;


        // Setting suspension tilt

        Vector3[] vertices = (Vector3[])carMesh.vertices.Clone();

        vertices = SetVertexHeight(vertices, MeshTopVertex.FrontLeft, 1f - packet.NormalizedSuspensionTravelFrontLeft * 0.75f);
        vertices = SetVertexHeight(vertices, MeshTopVertex.FrontRight, 1f - packet.NormalizedSuspensionTravelFrontRight * 0.75f);
        vertices = SetVertexHeight(vertices, MeshTopVertex.RearLeft, 1f - packet.NormalizedSuspensionTravelRearLeft * 0.75f);
        vertices = SetVertexHeight(vertices, MeshTopVertex.RearRight, 1f - packet.NormalizedSuspensionTravelRearRight * 0.75f);

        vertices = BalanceVertexHeights(vertices);

        carMesh.vertices = vertices;
    }

    Color32[] SetVertexColour (Color32[] colours, MeshTopVertex vertex, Color32 colour)
    {
        int[] topVertices = meshTopVertices[vertex];

        foreach (int v in topVertices)
        {
            colours[v] = colour;
        }

        return colours;
    }

    Vector3[] SetVertexHeight (Vector3[] vertices, MeshTopVertex vertex, float height)
    {
        int[] topVertices = meshTopVertices[vertex];

        foreach (int v in topVertices)
        {
            vertices[v].y = height;
        }

        return vertices;
    }

    Vector3[] BalanceVertexHeights (Vector3[] vertices)
    {
        float frontCenterHeight = (vertices[meshTopVertices[MeshTopVertex.FrontLeft][0]].y + vertices[meshTopVertices[MeshTopVertex.FrontRight][0]].y) / 2f;
        float rearCenterHeight = (vertices[meshTopVertices[MeshTopVertex.RearLeft][0]].y + vertices[meshTopVertices[MeshTopVertex.RearRight][0]].y) / 2f;
        float leftCenterHeight = (vertices[meshTopVertices[MeshTopVertex.FrontLeft][0]].y + vertices[meshTopVertices[MeshTopVertex.RearLeft][0]].y) / 2f;
        float rightCenterHeight = (vertices[meshTopVertices[MeshTopVertex.FrontRight][0]].y + vertices[meshTopVertices[MeshTopVertex.RearRight][0]].y) / 2f;
        float centerHeight = (vertices[meshTopVertices[MeshTopVertex.FrontLeft][0]].y + vertices[meshTopVertices[MeshTopVertex.FrontRight][0]].y + vertices[meshTopVertices[MeshTopVertex.RearLeft][0]].y + vertices[meshTopVertices[MeshTopVertex.RearRight][0]].y) / 4f;

        vertices = SetVertexHeight(vertices, MeshTopVertex.FrontCenter, frontCenterHeight);
        vertices = SetVertexHeight(vertices, MeshTopVertex.RearCenter, rearCenterHeight);
        vertices = SetVertexHeight(vertices, MeshTopVertex.LeftCenter, leftCenterHeight);
        vertices = SetVertexHeight(vertices, MeshTopVertex.RightCenter, rightCenterHeight);
        vertices = SetVertexHeight(vertices, MeshTopVertex.Center, centerHeight);

        return vertices;
    }
}
