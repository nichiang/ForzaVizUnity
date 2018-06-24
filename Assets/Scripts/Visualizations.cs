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

    public enum OnCarVizType
    {
        None,
        SuspensionTravel,
        TractionCircle
    };

    public TrailVizType trailVisualizationType = TrailVizType.Generic;
    public OnCarVizType onCarVizType = OnCarVizType.TractionCircle;

    [Header("Elevation")]
    public bool ShowElevation = true;
    public FastLineRenderer elevationRenderer;

    [Header("Suspension Travel")]
    public Gradient suspensionGradient;
    public int suspensionGraphVisiblePoints = 20;

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

    [Header("Circle References")]
    public Ellipse FLCircle;
    public Ellipse FRCircle;
    public Ellipse RLCircle;
    public Ellipse RRCircle;

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

        ElevationViz(node);
        GForceViz(packet, node, frameTick);
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
            FLCircle.radius = new Vector2(packet.TireSlipAngleFrontLeft, packet.TireSlipRatioFrontLeft) / 2f;
            FRCircle.radius = new Vector2(packet.TireSlipAngleFrontRight, packet.TireSlipRatioFrontRight) / 2f;
            RLCircle.radius = new Vector2(packet.TireSlipAngleRearLeft, packet.TireSlipRatioRearLeft) / 2f;
            RRCircle.radius = new Vector2(packet.TireSlipAngleRearRight, packet.TireSlipRatioRearRight) / 2f;

            FLCircle.UpdateEllipse();
            FRCircle.UpdateEllipse();
            RLCircle.UpdateEllipse();
            RRCircle.UpdateEllipse();
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

            FLCircle.radius = new Vector2(packet.TireSlipAngleFrontLeft, packet.TireSlipRatioFrontLeft) / 2f;
            FRCircle.radius = new Vector2(packet.TireSlipAngleFrontRight, packet.TireSlipRatioFrontRight) / 2f;
            RLCircle.radius = new Vector2(packet.TireSlipAngleRearLeft, packet.TireSlipRatioRearLeft) / 2f;
            RRCircle.radius = new Vector2(packet.TireSlipAngleRearRight, packet.TireSlipRatioRearRight) / 2f;

            FLCircle.UpdateEllipse();
            FRCircle.UpdateEllipse();
            RLCircle.UpdateEllipse();
            RRCircle.UpdateEllipse();
        }

        packet = DataPoints.GetPoint(packetIndex);

        DrawTireSuspensionTravel(packet);
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

    void GForceViz (ForzaPacket packet, GameObject go, float frameTick)
    {
        if (trailVisualizationType != TrailVizType.GForce)
            return;

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
