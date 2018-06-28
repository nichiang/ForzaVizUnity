using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
    public FastLineRenderer lineTrailRenderer;

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

    [Header("Misc References")]
    public Transform dataRoot;
    public GameObject graphAnchor;
    public GameObject tractionCircleAnchor;

    private MainCamera mainCamera;
    private TrackInfo trackInfo;

    private GameObject lastGo;
    private Vector3 lastPoint;
    private UInt32 lastTimestamp = 0;

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
    }

    public void DrawTrail (ForzaPacket packet)
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
        node.transform.SetParent(dataRoot);
        node.transform.position = lastPoint;

        node.transform.eulerAngles = new Vector3(Mathf.Rad2Deg * packet.Pitch, Mathf.Rad2Deg * packet.Yaw, Mathf.Rad2Deg * packet.Roll);

        node.transform.Translate(
            packet.VelocityX * frameTick,
            packet.VelocityY * frameTick,
            packet.VelocityZ * frameTick,
            Space.Self
        );

        if (!ShowElevation)
            node.transform.position = new Vector3(node.transform.position.x, 0, node.transform.position.z);

        if (mainCamera.IsFollowing())
        {
            mainCamera.FollowCurrentPoint(node);

            DrawCarVisualizations(packet);
        }

        lastGo = node;
        lastPoint = node.transform.position;
        lastTimestamp = packet.TimestampMS;

        trackInfo.FindLap(node);

        //ElevationViz(node);

        if (trailVisualizationType == TrailVizType.Line)
        {
            LineTrailViz(packet, node);
        }
        else if (trailVisualizationType == TrailVizType.GForce)
        {
            GForceViz(packet, node, frameTick);
        }
        
    }

    void DrawCarVisualizations (ForzaPacket packet)
    {
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
    }

    public void DrawCarVisualizationsAtIndex (int packetIndex)
    {
        ForzaPacket packet;

        if (onCarVizType == OnCarVizType.SuspensionTravel)
        {
            List<float> FLSuspensionGraphPoints = new List<float>();
            List<float> FRSuspensionGraphPoints = new List<float>();
            List<float> RLSuspensionGraphPoints = new List<float>();
            List<float> RRSuspensionGraphPoints = new List<float>();

            for (int i = packetIndex; i > packetIndex - suspensionGraphVisiblePoints; i--)
            {
                packet = DataPoints.GetPoint(i);

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

            FLGraph.AddPoints(FLSuspensionGraphPoints);
            FRGraph.AddPoints(FRSuspensionGraphPoints);
            RLGraph.AddPoints(RLSuspensionGraphPoints);
            RRGraph.AddPoints(RRSuspensionGraphPoints);
        }
        else if (onCarVizType == OnCarVizType.TractionCircle)
        {
            packet = DataPoints.GetPoint(packetIndex);

            DrawTractionCircles(packet, packetIndex);
        }

        packet = DataPoints.GetPoint(packetIndex);

        DrawTireSuspensionTravel(packet);
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

        if (ShowTractionCircleHistory)
        {
            if (DataPoints.GetCurrentPacketIndex() % TractionCircleHistoryDensity == 0) // Run every n-th time
                DrawTractionCircleHistory(packetIndex);
        }
    }

    void DrawTractionCircleHistory (int packetIndex = -1)
    {
        int currentPacketIndex = packetIndex == -1 ? DataPoints.GetCurrentPacketIndex() : packetIndex;
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
                packet = DataPoints.GetPoint(i);

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
            LineRenderer FLTractionLine = FLDataPointsRoot.GetComponent<LineRenderer>();
            LineRenderer FRTractionLine = FRDataPointsRoot.GetComponent<LineRenderer>();
            LineRenderer RLTractionLine = RLDataPointsRoot.GetComponent<LineRenderer>();
            LineRenderer RRTractionLine = RRDataPointsRoot.GetComponent<LineRenderer>();

            FLTractionLine.positionCount = TractionCircleHistoryCount;
            FRTractionLine.positionCount = TractionCircleHistoryCount;
            RLTractionLine.positionCount = TractionCircleHistoryCount;
            RRTractionLine.positionCount = TractionCircleHistoryCount;

            for (int i = currentPacketIndex - TractionCircleHistoryCount * TractionCircleHistoryDensity + 1; i <= currentPacketIndex; i += TractionCircleHistoryDensity)
            {
                packet = DataPoints.GetPoint(i);

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

    void LineTrailViz (ForzaPacket packet, GameObject go)
    {
        float gforce = packet.AccelerationZ / 9.80665f;

        FastLineRendererProperties lineRendererProps = new FastLineRendererProperties
        {
            Start = go.transform.position,
            Radius = 0.1f,
            Color = gForceGradient.Evaluate(gforce  * 0.5f + 0.5f),
            LineJoin = FastLineRendererLineJoin.AttachToPrevious
        };

        lineTrailRenderer.AppendLine(lineRendererProps);
        lineTrailRenderer.Apply();
    }

    void GForceViz (ForzaPacket packet, GameObject go, float frameTick)
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

    public GameObject CurrentPoint ()
    {
        return lastGo;
    }
}
