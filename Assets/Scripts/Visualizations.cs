using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.FastLineRenderer;

public class Visualizations : MonoBehaviour {

    public enum TrailVizType
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

    public TrailVizType trailVisualizationType = TrailVizType.Generic;
    public Transform dataRoot;

    [Header("Elevation")]
    public bool ShowElevation = true;
    public FastLineRenderer elevationRenderer;

    [Header("Suspension Travel")]
    public bool ShowSuspensionTravel = true;
    public Gradient suspensionGradient;

    [Header("G Force")]
    public Gradient gForceGradient;

    [Header("Trail Visualization Prefabs")]
    public GameObject genericTrailPrefab;
    public GameObject suspensionVizTrailPrefab;
    public GameObject gforceVizTrailPrefab;
    
    [Header("Tire References")]
    public GameObject FLTire;
    public GameObject FRTire;
    public GameObject RLTire;
    public GameObject RRTire;

    [Header("Graph References")]
    public Graph FLGraph;
    public Graph FRGraph;
    public Graph RLGraph;
    public Graph RRGraph;
    
    private MainCamera mainCamera;
    private TrackInfo trackInfo;

    private GameObject lastGo;
    private Vector3 lastPoint;
    private UInt32 lastTimestamp = 0;

    private Dictionary<MeshTopVertex, int[]> meshTopVertices;

    void Start ()
    {
        mainCamera = Camera.main.GetComponent<MainCamera>();
        trackInfo = GetComponent<TrackInfo>();

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

        GameObject trailPrefab = genericTrailPrefab;

        switch (trailVisualizationType)
        {
            case TrailVizType.SuspensionViz:
                trailPrefab = suspensionVizTrailPrefab;
                break;
            case TrailVizType.GForce:
                trailPrefab = gforceVizTrailPrefab;
                break;
        }

        GameObject go = Instantiate(trailPrefab);
        go.transform.SetParent(dataRoot);
        go.transform.position = lastPoint;

        go.transform.eulerAngles = new Vector3(Mathf.Rad2Deg * packet.Pitch, Mathf.Rad2Deg * packet.Yaw, Mathf.Rad2Deg * packet.Roll);

        go.transform.Translate(
            packet.VelocityX * frameTick,
            packet.VelocityY * frameTick,
            packet.VelocityZ * frameTick,
            Space.Self
        );

        if (!ShowElevation)
            go.transform.position = new Vector3(go.transform.position.x, 0, go.transform.position.z);

        if (mainCamera.IsFollowing())
            mainCamera.FollowCurrentPoint(go);

        lastGo = go;
        lastPoint = go.transform.position;
        lastTimestamp = packet.TimestampMS;

        trackInfo.FindLap(go);

        ElevationViz(go);
        GForceViz(packet, go, frameTick);
        SuspensionTravelMeshColourViz(packet, go);

        NormalizedSuspensionGraph(packet);

        if (ShowSuspensionTravel)
            SuspensionTravelTireViz(packet);
    }

    void NormalizedSuspensionGraph (ForzaPacket packet)
    {
        FLGraph.AddPoint(1f - packet.NormalizedSuspensionTravelFrontLeft);
        FRGraph.AddPoint(1f - packet.NormalizedSuspensionTravelFrontRight);
        RLGraph.AddPoint(1f - packet.NormalizedSuspensionTravelRearLeft);
        RRGraph.AddPoint(1f - packet.NormalizedSuspensionTravelRearRight);
    }

    void ElevationViz (GameObject go)
    {
        FastLineRendererProperties elevationRendererProps = new FastLineRendererProperties
        {
            Start = go.transform.position,
            End = new Vector3(go.transform.position.x, 0, go.transform.position.z),
            Radius = 0.0075f,
            Color = new Color(0.165f, 0.173f, 0.2f)
        };

        elevationRenderer.AddLine(elevationRendererProps);
        elevationRenderer.Apply();
    }

    void GForceViz (ForzaPacket packet, GameObject go, float frameTick)
    {
        if (trailVisualizationType != TrailVizType.GForce)
            return;

        Transform arrow = go.transform.GetChild(0);

        Vector3 accelVector = new Vector3(packet.AccelerationX, packet.AccelerationY, packet.AccelerationZ);

        arrow.transform.forward = accelVector;

        float gforce = accelVector.magnitude / 9.80665f;

        arrow.GetComponent<MeshRenderer>().material.color = gForceGradient.Evaluate(gforce);

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
        if (trailVisualizationType != TrailVizType.SuspensionViz)
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

    public GameObject CurrentPoint ()
    {
        return lastGo;
    }
}
