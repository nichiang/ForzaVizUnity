using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.FastLineRenderer;

public class Visualizations : MonoBehaviour {

    public enum TrailVizType
    {
        Line,
        SuspensionViz,
        GForce
    };

    public enum OnCarVizType
    {
        None,
        SuspensionTravel,
        TractionCircle
    };

    public enum TractionCircleVizType
    {
        Dots,
        Line
    }

    public TrailVizType trailVisualizationType = TrailVizType.Line;
    public OnCarVizType onCarVizType = OnCarVizType.TractionCircle;

    [Header("Elevation")]
    public bool ShowElevation = true;
    public FastLineRenderer elevationRenderer;

    [Header("Suspension Travel")]
    public Gradient suspensionGradient;
    public int suspensionGraphVisiblePoints = 20;

    [Header("Line Trail")]
    public float lineTrailWidth = 1f;
    public Material lineMaterial;
    public Material lineMaterialInactive;

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
    public Graph FLUiGraph;
    public Graph FRUiGraph;
    public Graph RLUiGraph;
    public Graph RRUiGraph;

    [Header("Traction Circle References")]
    public bool ShowTractionCircleHistory = true;
    public int TractionCircleHistoryCount = 50;
    public int TractionCircleHistoryDensity = 4;
    public TractionCircleVizType tractionCircleVizType = TractionCircleVizType.Line;
    public Gradient TractionCircleHistoryGradient;
    public Ellipse FLCircle;
    public Ellipse FRCircle;
    public Ellipse RLCircle;
    public Ellipse RRCircle;
    public GameObject CircleHistoryPrefab;
    private LineRenderer FLPointer;
    private LineRenderer FRPointer;
    private LineRenderer RLPointer;
    private LineRenderer RRPointer;
    private Transform FLDataPointsRoot;
    private Transform FRDataPointsRoot;
    private Transform RLDataPointsRoot;
    private Transform RRDataPointsRoot;
    private LineRenderer FLTractionLine;
    private LineRenderer FRTractionLine;
    private LineRenderer RLTractionLine;
    private LineRenderer RRTractionLine;

    [Header("Ghost References")]
    public GameObject ghostMarkerPrefab;
    public Transform ghostMarkersRoot;
    private List<GhostMarker> ghostMarkers = new List<GhostMarker>();

    [Header("Misc References")]
    public Transform dataRoot;
    public GameObject graphAnchor;
    public GameObject tractionCircleAnchor;

    private MainCamera mainCamera;
    private TrackInfo trackInfo;
    private int currentLapNum = 1;
    private int currentTrackingLapNum = 1;

    private List<Mesh> oldLapMeshes = new List<Mesh>();
    private List<Vector3> lineMeshVertices = new List<Vector3>();
    private List<int> lineMeshTriangles = new List<int>();
    private List<Color> lineMeshColours = new List<Color>();

    void Start ()
    {
        mainCamera = Camera.main.GetComponent<MainCamera>();
        trackInfo = GetComponent<TrackInfo>();

        if (onCarVizType == OnCarVizType.SuspensionTravel)
        {
            graphAnchor.SetActive(true);
            tractionCircleAnchor.SetActive(false);
        }
        else if (onCarVizType == OnCarVizType.TractionCircle)
        {
            graphAnchor.SetActive(false);
            tractionCircleAnchor.SetActive(true);
        }
        else
        {
            graphAnchor.SetActive(false);
            tractionCircleAnchor.SetActive(false);
        }

        FLPointer = FLCircle.transform.GetChild(0).GetComponent<LineRenderer>();
        FRPointer = FRCircle.transform.GetChild(0).GetComponent<LineRenderer>();
        RLPointer = RLCircle.transform.GetChild(0).GetComponent<LineRenderer>();
        RRPointer = RRCircle.transform.GetChild(0).GetComponent<LineRenderer>();

        FLDataPointsRoot = FLCircle.transform.parent.GetChild(1);
        FRDataPointsRoot = FRCircle.transform.parent.GetChild(1);
        RLDataPointsRoot = RLCircle.transform.parent.GetChild(1);
        RRDataPointsRoot = RRCircle.transform.parent.GetChild(1);

        FLTractionLine = FLDataPointsRoot.GetComponent<LineRenderer>();
        FRTractionLine = FRDataPointsRoot.GetComponent<LineRenderer>();
        RLTractionLine = RLDataPointsRoot.GetComponent<LineRenderer>();
        RRTractionLine = RRDataPointsRoot.GetComponent<LineRenderer>();
    }

    void Update ()
    {
        if (lineMeshVertices.Count > 0)
        {
            Material activeMat;

            for (int i = 0; i < oldLapMeshes.Count; i++)
            {
                activeMat = i + 1 == currentTrackingLapNum ? lineMaterial : lineMaterialInactive;
                Graphics.DrawMesh(oldLapMeshes[i], Matrix4x4.identity, activeMat, 0);
            }

            Mesh lineMesh = new Mesh();
            lineMesh.SetVertices(lineMeshVertices);
            lineMesh.SetTriangles(lineMeshTriangles, 0);
            lineMesh.SetColors(lineMeshColours);

            activeMat = currentLapNum == currentTrackingLapNum ? lineMaterial : lineMaterialInactive;
            Graphics.DrawMesh(lineMesh, Matrix4x4.identity, activeMat, 0);
        }
    }

    public void DrawTrail (DataPoint p, int lapNum, bool loadFromFile = false)
    {
        GameObject trailPrefab = genericTrailPrefab;

        switch (trailVisualizationType)
        {
            case TrailVizType.Line:
                trailPrefab = genericTrailPrefab;
                break;
            case TrailVizType.SuspensionViz:
                trailPrefab = suspensionVizTrailPrefab;
                break;
            case TrailVizType.GForce:
                trailPrefab = gforceVizTrailPrefab;
                break;
        }
        
        GameObject node = Instantiate(trailPrefab);
        node.transform.position = p.GetPosition();
        node.transform.rotation = p.GetRotation();
        node.transform.SetParent(dataRoot);
        
        if (!ShowElevation)
            node.transform.position = new Vector3(node.transform.position.x, 0, node.transform.position.z);

        if (mainCamera.IsFollowing() && !loadFromFile)
        {
            mainCamera.FollowCurrentPoint(DataPoints.GetLatestPacketIndex());

            DrawVisualizations(p.GetPacket());
        }

        //ElevationViz(node);

        if (trailVisualizationType == TrailVizType.Line)
        {
            LineTrailViz(lapNum);
        }
        else if (trailVisualizationType == TrailVizType.GForce)
        {
            GForceViz(p.GetPacket(), node);
        }
    }

    void DrawVisualizations (ForzaPacket packet)
    {
        currentTrackingLapNum = (int)packet.LapNum;

        if (onCarVizType == OnCarVizType.SuspensionTravel)
        {
            FLGraph.AddPoint(1f - packet.NormalizedSuspensionTravelFrontLeft);
            FRGraph.AddPoint(1f - packet.NormalizedSuspensionTravelFrontRight);
            RLGraph.AddPoint(1f - packet.NormalizedSuspensionTravelRearLeft);
            RRGraph.AddPoint(1f - packet.NormalizedSuspensionTravelRearRight);
        }
        else if (onCarVizType == OnCarVizType.TractionCircle)
        {
            DrawTractionCircles(packet);
        }

        DrawTireSuspensionTravel(packet);

        FLUiGraph.AddPoint(1f - packet.NormalizedSuspensionTravelFrontLeft);
        FRUiGraph.AddPoint(1f - packet.NormalizedSuspensionTravelFrontRight);
        RLUiGraph.AddPoint(1f - packet.NormalizedSuspensionTravelRearLeft);
        RRUiGraph.AddPoint(1f - packet.NormalizedSuspensionTravelRearRight);

        GhostPositionViz();
    }

    public void DrawVisualizationsAtIndex (int packetIndex)
    {
        ForzaPacket packet = DataPoints.GetPoint(packetIndex).GetPacket();
        currentTrackingLapNum = (int)packet.LapNum;

        List<float> FLSuspensionGraphPoints = new List<float>();
        List<float> FRSuspensionGraphPoints = new List<float>();
        List<float> RLSuspensionGraphPoints = new List<float>();
        List<float> RRSuspensionGraphPoints = new List<float>();

        /*
        if (packetIndex > 0)
            Debug.Log("\nLine trail mesh vertices: " + lineMeshes[packetIndex].vertices[0] + ", " + lineMeshes[packetIndex].vertices[1] + ", " + lineMeshes[packetIndex].vertices[2] + ", " + lineMeshes[packetIndex].vertices[3]
                    + " | Distance between points: " + Vector3.Distance(dataRoot.GetChild(packetIndex).position, dataRoot.GetChild(packetIndex - 1).position)
                    + " | Difference between packet timestamps: " + (DataPoints.GetPoint(packetIndex).TimestampMS - DataPoints.GetPoint(packetIndex - 1).TimestampMS));
*/
        for (int i = packetIndex; i > packetIndex - suspensionGraphVisiblePoints; i--)
        {
            if (!DataPoints.IsValidIndex(i))
                continue;

            packet = DataPoints.GetPoint(i).GetPacket();

            if (i >= 0)
            {
                FLSuspensionGraphPoints.Add(1f - packet.NormalizedSuspensionTravelFrontLeft);
                FRSuspensionGraphPoints.Add(1f - packet.NormalizedSuspensionTravelFrontRight);
                RLSuspensionGraphPoints.Add(1f - packet.NormalizedSuspensionTravelRearLeft);
                RRSuspensionGraphPoints.Add(1f - packet.NormalizedSuspensionTravelRearRight);
            }
            else
            {
                FLSuspensionGraphPoints.Add(0);
                FRSuspensionGraphPoints.Add(0);
                RLSuspensionGraphPoints.Add(0);
                RRSuspensionGraphPoints.Add(0);
            }
        }

        if (onCarVizType == OnCarVizType.SuspensionTravel)
        {
            FLGraph.AddPoints(FLSuspensionGraphPoints);
            FRGraph.AddPoints(FRSuspensionGraphPoints);
            RLGraph.AddPoints(RLSuspensionGraphPoints);
            RRGraph.AddPoints(RRSuspensionGraphPoints);
        }
        else if (onCarVizType == OnCarVizType.TractionCircle)
        {
            packet = DataPoints.GetPoint(packetIndex).GetPacket();

            DrawTractionCircles(packet, packetIndex);
        }

        packet = DataPoints.GetPoint(packetIndex).GetPacket();

        DrawTireSuspensionTravel(packet);

        FLUiGraph.AddPoints(FLSuspensionGraphPoints);
        FRUiGraph.AddPoints(FRSuspensionGraphPoints);
        RLUiGraph.AddPoints(RLSuspensionGraphPoints);
        RRUiGraph.AddPoints(RRSuspensionGraphPoints);
    }

    void DrawTractionCircles (ForzaPacket packet, int packetIndex = -1)
    {
        FLCircle.radius = new Vector2(packet.TireCombinedSlipFrontLeft, packet.TireCombinedSlipFrontLeft) / 2f;
        FRCircle.radius = new Vector2(packet.TireCombinedSlipFrontRight, packet.TireCombinedSlipFrontRight) / 2f;
        RLCircle.radius = new Vector2(packet.TireCombinedSlipRearLeft, packet.TireCombinedSlipRearLeft) / 2f;
        RRCircle.radius = new Vector2(packet.TireCombinedSlipRearRight, packet.TireCombinedSlipRearRight) / 2f;

        FLCircle.UpdateEllipse();
        FRCircle.UpdateEllipse();
        RLCircle.UpdateEllipse();
        RRCircle.UpdateEllipse();

        FLPointer.SetPosition(1, new Vector3(packet.TireSlipAngleFrontLeft, packet.TireSlipRatioFrontLeft, 0) / 2f);
        FRPointer.SetPosition(1, new Vector3(packet.TireSlipAngleFrontRight, packet.TireSlipRatioFrontRight, 0) / 2f);
        RLPointer.SetPosition(1, new Vector3(packet.TireSlipAngleRearLeft, packet.TireSlipRatioRearLeft, 0) / 2f);
        RRPointer.SetPosition(1, new Vector3(packet.TireSlipAngleRearRight, packet.TireSlipRatioRearRight, 0) / 2f);

        if (ShowTractionCircleHistory && packetIndex > -1)
        {
            if (packetIndex % TractionCircleHistoryDensity == 0) // Run every n-th time
                DrawTractionCircleHistory(packetIndex);
        }
    }

    void DrawTractionCircleHistory (int packetIndex = -1)
    {
        int currentPacketIndex = packetIndex == -1 ? DataPoints.GetLatestPacketIndex() : packetIndex;
        ForzaPacket packet;
        int childIndex = 0;

        if (tractionCircleVizType == TractionCircleVizType.Dots)
        {
            if (FLDataPointsRoot.childCount == 0)
            {
                for (int i = 0; i < TractionCircleHistoryCount; i++)
                {
                    Instantiate(CircleHistoryPrefab, Vector3.zero, Quaternion.identity, FLDataPointsRoot);
                    Instantiate(CircleHistoryPrefab, Vector3.zero, Quaternion.identity, FRDataPointsRoot);
                    Instantiate(CircleHistoryPrefab, Vector3.zero, Quaternion.identity, RLDataPointsRoot);
                    Instantiate(CircleHistoryPrefab, Vector3.zero, Quaternion.identity, RRDataPointsRoot);
                }
            }

            for (int i = currentPacketIndex - TractionCircleHistoryCount * TractionCircleHistoryDensity + 1; i <= currentPacketIndex; i += TractionCircleHistoryDensity)
            {
                if (!DataPoints.IsValidIndex(i))
                    continue;

                packet = DataPoints.GetPoint(i).GetPacket();

                Transform FLDot = FLDataPointsRoot.GetChild(childIndex);
                Transform FRDot = FRDataPointsRoot.GetChild(childIndex);
                Transform RLDot = RLDataPointsRoot.GetChild(childIndex);
                Transform RRDot = RRDataPointsRoot.GetChild(childIndex);

                Renderer FLRenderer = FLDot.GetComponent<MeshRenderer>();
                Renderer FRRenderer = FRDot.GetComponent<MeshRenderer>();
                Renderer RLRenderer = RLDot.GetComponent<MeshRenderer>();
                Renderer RRRenderer = RRDot.GetComponent<MeshRenderer>();

                if (packet != null)
                {
                    FLDot.localPosition = new Vector3(packet.TireSlipAngleFrontLeft, packet.TireSlipRatioFrontLeft, 0) / 2f;
                    FRDot.localPosition = new Vector3(packet.TireSlipAngleFrontRight, packet.TireSlipRatioFrontRight, 0) / 2f;
                    RLDot.localPosition = new Vector3(packet.TireSlipAngleRearLeft, packet.TireSlipRatioRearLeft, 0) / 2f;
                    RRDot.localPosition = new Vector3(packet.TireSlipAngleRearRight, packet.TireSlipRatioRearRight, 0) / 2f;

                    FLRenderer.material.color = TractionCircleHistoryGradient.Evaluate(childIndex / 50f);
                    FRRenderer.material.color = TractionCircleHistoryGradient.Evaluate(childIndex / 50f);
                    RLRenderer.material.color = TractionCircleHistoryGradient.Evaluate(childIndex / 50f);
                    RRRenderer.material.color = TractionCircleHistoryGradient.Evaluate(childIndex / 50f);

                    FLRenderer.sortingOrder = childIndex;
                    FRRenderer.sortingOrder = childIndex;
                    RLRenderer.sortingOrder = childIndex;
                    RRRenderer.sortingOrder = childIndex;

                    FLDot.gameObject.SetActive(true);
                    FRDot.gameObject.SetActive(true);
                    RLDot.gameObject.SetActive(true);
                    RRDot.gameObject.SetActive(true);
                }
                else
                {
                    FLDot.gameObject.SetActive(false);
                    FRDot.gameObject.SetActive(false);
                    RLDot.gameObject.SetActive(false);
                    RRDot.gameObject.SetActive(false);
                }

                childIndex++;
            }
        }
        else
        {
            FLTractionLine.positionCount = TractionCircleHistoryCount;
            FRTractionLine.positionCount = TractionCircleHistoryCount;
            RLTractionLine.positionCount = TractionCircleHistoryCount;
            RRTractionLine.positionCount = TractionCircleHistoryCount;

            for (int i = currentPacketIndex - TractionCircleHistoryCount * TractionCircleHistoryDensity + 1; i <= currentPacketIndex; i += TractionCircleHistoryDensity)
            {
                packet = DataPoints.GetPoint(i).GetPacket();

                if (packet != null)
                {
                    FLTractionLine.SetPosition(childIndex, new Vector3(packet.TireSlipAngleFrontLeft, packet.TireSlipRatioFrontLeft, 0) / 2f);
                    FRTractionLine.SetPosition(childIndex, new Vector3(packet.TireSlipAngleFrontRight, packet.TireSlipRatioFrontRight, 0) / 2f);
                    RLTractionLine.SetPosition(childIndex, new Vector3(packet.TireSlipAngleRearLeft, packet.TireSlipRatioRearLeft, 0) / 2f);
                    RRTractionLine.SetPosition(childIndex, new Vector3(packet.TireSlipAngleRearRight, packet.TireSlipRatioRearRight, 0) / 2f);
                }

                childIndex++;
            }
        }
    }

    void GhostPositionViz ()
    {
        int currentLap = trackInfo.CurrentLap();

        if (ghostMarkers.Count < currentLap)
        {
            GameObject go = Instantiate(ghostMarkerPrefab, Vector3.zero, Quaternion.identity, ghostMarkersRoot);
            GhostMarker marker = go.GetComponent<GhostMarker>();
            marker.markerText.text = "LAP " + currentLap;
            ghostMarkers.Add(marker);
        }

        for (int i = 0; i < ghostMarkers.Count; i++)
        {
            int positionIndex = trackInfo.GetGhostPosition(i);

            if (DataPoints.IsValidIndex(positionIndex))
            {
                Vector3 pos = DataPoints.GetPoint(positionIndex).GetPosition();
                ghostMarkers[i].transform.position = pos;
            }

            ghostMarkers[i].gameObject.SetActive(i + 1 != currentLap);
        }
    }

    void GhostPositionVizAtIndex (int packetIndex)
    {
        List<int> ghostIndices = trackInfo.GetGhostPositionsAtIndex(packetIndex);

        for (int i = 0; i < ghostMarkers.Count; i++)
        {
            if (DataPoints.IsValidIndex(ghostIndices[i]))
            {
                DataPoint indexPoint = DataPoints.GetPoint(ghostIndices[i]);

                Vector3 pos = indexPoint.GetPosition();
                ghostMarkers[i].transform.position = pos;

                ghostMarkers[i].gameObject.SetActive(i + 1 != indexPoint.GetPacket().LapNum);
            }
            else
            {
                ghostMarkers[i].gameObject.SetActive(false);
            }
        }
    }

    void DrawTireSuspensionTravel (ForzaPacket packet)
    {
        FLTire.transform.localPosition = new Vector3(FLTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelFrontLeft * 0.4f, FLTire.transform.localPosition.z);
        FRTire.transform.localPosition = new Vector3(FRTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelFrontRight * 0.4f, FRTire.transform.localPosition.z);
        RLTire.transform.localPosition = new Vector3(RLTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelRearLeft * 0.4f, RLTire.transform.localPosition.z);
        RRTire.transform.localPosition = new Vector3(RRTire.transform.localPosition.x, 3.2f + packet.NormalizedSuspensionTravelRearRight * 0.4f, RRTire.transform.localPosition.z);
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

    public void ShowElevationLines (bool show)
    {
        if (show)
        {
            elevationRenderer.ScreenRadiusMultiplier = 0;
        }
        else
        {
            elevationRenderer.ScreenRadiusMultiplier = 0.001f;
        }
    }

    void LineTrailViz (int lapNum)
    {
        if (DataPoints.GetLatestPacketIndex() <= 1)
            return;

        if (currentLapNum != lapNum)
        {
            Mesh lineMesh = new Mesh();
            lineMesh.SetVertices(lineMeshVertices);
            lineMesh.SetTriangles(lineMeshTriangles, 0);
            lineMesh.SetColors(lineMeshColours);

            oldLapMeshes.Add(lineMesh);

            lineMeshVertices.Clear();
            lineMeshTriangles.Clear();
            lineMeshColours.Clear();

            currentLapNum = lapNum;
        }

        float gforce = DataPoints.GetCurrentPoint().GetPacket().AccelerationZ / 9.80665f;
        Vector3 currPoint = DataPoints.GetCurrentPoint().GetPosition();
        Vector3 lastPoint = DataPoints.GetPrevPoint().GetPosition();
        Color gforceColour;
        Vector3 offset = new Vector3(lastPoint.z - currPoint.z, 0, currPoint.x - lastPoint.x).normalized * lineTrailWidth / 2f;

        if (lineMeshVertices.Count == 0)
        {
            lineMeshVertices.Add(lastPoint - offset);
            lineMeshVertices.Add(lastPoint + offset);

            gforceColour = gForceGradient.Evaluate(0.5f);

            lineMeshColours.Add(gforceColour);
            lineMeshColours.Add(gforceColour);
        }

        lineMeshVertices.Add(currPoint - offset);
        lineMeshVertices.Add(currPoint + offset);

        int vertexCount = lineMeshVertices.Count;

        lineMeshTriangles.Add(vertexCount - 4);
        lineMeshTriangles.Add(vertexCount - 3);
        lineMeshTriangles.Add(vertexCount - 2);
        lineMeshTriangles.Add(vertexCount - 1);
        lineMeshTriangles.Add(vertexCount - 2);
        lineMeshTriangles.Add(vertexCount - 3);

        gforceColour = gForceGradient.Evaluate(gforce * 0.5f + 0.5f);

        lineMeshColours.Add(gforceColour);
        lineMeshColours.Add(gforceColour);
    }

    void GForceViz (ForzaPacket packet, GameObject go)
    {
        Transform arrow = go.transform.GetChild(0);

        Vector3 accelVector = new Vector3(packet.AccelerationX, packet.AccelerationY, packet.AccelerationZ);

        arrow.transform.forward = accelVector;

        float gforce = accelVector.magnitude / 9.80665f;

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", gForceGradient.Evaluate(gforce));
        arrow.GetComponent<MeshRenderer>().SetPropertyBlock(props);

        Vector3 scaleArrow = arrow.transform.localScale;
        scaleArrow.z *= gforce;
        arrow.transform.localScale = scaleArrow;
    }

    public void ResetVisualizations ()
    {
        FLGraph.ResetGraph();
        FRGraph.ResetGraph();
        RLGraph.ResetGraph();
        RRGraph.ResetGraph();

        FLUiGraph.ResetGraph();
        FRUiGraph.ResetGraph();
        RLUiGraph.ResetGraph();
        RRUiGraph.ResetGraph();

        Vector2 resetTractionCircle = new Vector2(0.5f, 0.5f);
        Vector3 resetTractionPointer = new Vector3(0.5f, 0, 0);

        FLCircle.radius = resetTractionCircle;
        FRCircle.radius = resetTractionCircle;
        RLCircle.radius = resetTractionCircle;
        RRCircle.radius = resetTractionCircle;

        FLCircle.UpdateEllipse();
        FRCircle.UpdateEllipse();
        RLCircle.UpdateEllipse();
        RRCircle.UpdateEllipse();

        FLPointer.SetPosition(1, resetTractionPointer);
        FRPointer.SetPosition(1, resetTractionPointer);
        RLPointer.SetPosition(1, resetTractionPointer);
        RRPointer.SetPosition(1, resetTractionPointer);

        if (FLDataPointsRoot.childCount > 0)
        {
            for (int i = 0; i < TractionCircleHistoryCount; i++)
            {
                FLDataPointsRoot.GetChild(i).localPosition = Vector3.zero;
                FRDataPointsRoot.GetChild(i).localPosition = Vector3.zero;
                RLDataPointsRoot.GetChild(i).localPosition = Vector3.zero;
                RRDataPointsRoot.GetChild(i).localPosition = Vector3.zero;
            }
        }

        FLTractionLine.positionCount = 0;
        FRTractionLine.positionCount = 0;
        RLTractionLine.positionCount = 0;
        RRTractionLine.positionCount = 0;

        oldLapMeshes.Clear();
        lineMeshVertices.Clear();
        lineMeshTriangles.Clear();
        lineMeshColours.Clear();

        mainCamera.ResetCamera();

        for (int i = dataRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(dataRoot.GetChild(i).gameObject);
        }
    }
}
