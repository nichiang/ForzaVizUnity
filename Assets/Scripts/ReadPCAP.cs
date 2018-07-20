using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Crosstales.FB;

public class ReadPCAP : MonoBehaviour {

    public int listenPort = 51384;
    public Visualizations visualizations;
    public UIVisualizations uiVisualizations;
    public TrackInfo trackInfo;

    private UdpClient receivingUdpClient;
    private ConcurrentQueue<ForzaPacket> packetQueue;
    private int packetCount = 0;

    private Thread listener;
    private UInt32 lastTimestamp = 0;

    private string lastSavePath;

    // Use this for initialization
	void Start () {
        lastSavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        StartNewPacketListener();
	}

    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            receivingUdpClient.Close();
            Debug.Log("Packet queue watcher stopped by user");
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            NewCapture();
        }

        if (listener.IsAlive || packetQueue.Count > 0)
        {
            ForzaPacket packet;

            if (packetQueue.TryDequeue(out packet))
            {
                DataPoint newPoint = DataPoints.AddPoint(packet);

                int lapNum = trackInfo.CheckNewLap(DataPoints.GetLatestPacketIndex());
                newPoint.GetPacket().LapNum = (uint)lapNum;

                visualizations.DrawTrail(newPoint, lapNum);
                uiVisualizations.DrawUI(packet);
            }
        }
    }

    void StartNewPacketListener ()
    {
        packetQueue = new ConcurrentQueue<ForzaPacket>();

        if (listener != null)
        {
            listener.Abort();
        }

        listener = new Thread(new ThreadStart(ListenPackets));
        listener.IsBackground = true;
        listener.Start();
    }

    void ListenPackets ()
    {
        receivingUdpClient = new UdpClient(listenPort);
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        bool recordRace = true;
        bool raceStarted = false;

        Debug.Log("Starting UDP listener on port " + listenPort);

        try
        {
            while (recordRace)
            {
                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);

                int index = 0;

                ForzaPacket packet = new ForzaPacket
                {
                    IsRaceOn = BitConverter.ToInt32(receiveBytes, index), // = 1 when race is on. = 0 when in menus/race stopped …

                    TimestampMS = BitConverter.ToUInt32(receiveBytes, index += 4), //Can overflow to 0 eventually

                    EngineMaxRpm = BitConverter.ToSingle(receiveBytes, index += 4),
                    EngineIdleRpm = BitConverter.ToSingle(receiveBytes, index += 4),
                    CurrentEngineRpm = BitConverter.ToSingle(receiveBytes, index += 4),

                    AccelerationX = BitConverter.ToSingle(receiveBytes, index += 4), //In the car's local space; X = right, Y = up, Z = forward
                    AccelerationY = BitConverter.ToSingle(receiveBytes, index += 4),
                    AccelerationZ = BitConverter.ToSingle(receiveBytes, index += 4),

                    VelocityX = BitConverter.ToSingle(receiveBytes, index += 4), //In the car's local space; X = right, Y = up, Z = forward
                    VelocityY = BitConverter.ToSingle(receiveBytes, index += 4),
                    VelocityZ = BitConverter.ToSingle(receiveBytes, index += 4),

                    AngularVelocityX = BitConverter.ToSingle(receiveBytes, index += 4), //In the car's local space; X = pitch, Y = yaw, Z = roll
                    AngularVelocityY = BitConverter.ToSingle(receiveBytes, index += 4),
                    AngularVelocityZ = BitConverter.ToSingle(receiveBytes, index += 4),

                    Yaw = BitConverter.ToSingle(receiveBytes, index += 4),
                    Pitch = BitConverter.ToSingle(receiveBytes, index += 4),
                    Roll = BitConverter.ToSingle(receiveBytes, index += 4),

                    NormalizedSuspensionTravelFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Suspension travel normalized: 0.0f = max stretch; 1.0 = max compression
                    NormalizedSuspensionTravelFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    NormalizedSuspensionTravelRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    NormalizedSuspensionTravelRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    TireSlipRatioFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Tire normalized slip ratio, = 0 means 100% grip and |ratio| > 1.0 means loss of grip.
                    TireSlipRatioFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    TireSlipRatioRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    TireSlipRatioRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    WheelRotationSpeedFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Wheel rotation speed radians/sec. 
                    WheelRotationSpeedFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    WheelRotationSpeedRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    WheelRotationSpeedRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    WheelOnRumbleStripFrontLeft = BitConverter.ToInt32(receiveBytes, index += 4), // = 1 when wheel is on rumble strip, = 0 when off.
                    WheelOnRumbleStripFrontRight = BitConverter.ToInt32(receiveBytes, index += 4),
                    WheelOnRumbleStripRearLeft = BitConverter.ToInt32(receiveBytes, index += 4),
                    WheelOnRumbleStripRearRight = BitConverter.ToInt32(receiveBytes, index += 4),

                    WheelInPuddleDepthFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // = from 0 to 1, where 1 is the deepest puddle
                    WheelInPuddleDepthFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    WheelInPuddleDepthRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    WheelInPuddleDepthRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    SurfaceRumbleFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Non-dimensional surface rumble values passed to controller force feedback
                    SurfaceRumbleFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    SurfaceRumbleRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    SurfaceRumbleRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    TireSlipAngleFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Tire normalized slip angle, = 0 means 100% grip and |angle| > 1.0 means loss of grip.
                    TireSlipAngleFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    TireSlipAngleRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    TireSlipAngleRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    TireCombinedSlipFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Tire normalized combined slip, = 0 means 100% grip and |slip| > 1.0 means loss of grip.
                    TireCombinedSlipFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    TireCombinedSlipRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    TireCombinedSlipRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    SuspensionTravelMetersFrontLeft = BitConverter.ToSingle(receiveBytes, index += 4), // Actual suspension travel in meters
                    SuspensionTravelMetersFrontRight = BitConverter.ToSingle(receiveBytes, index += 4),
                    SuspensionTravelMetersRearLeft = BitConverter.ToSingle(receiveBytes, index += 4),
                    SuspensionTravelMetersRearRight = BitConverter.ToSingle(receiveBytes, index += 4),

                    CarOrdinal = BitConverter.ToInt32(receiveBytes, index += 4), //Unique ID of the car make/model
                    CarClass = BitConverter.ToInt32(receiveBytes, index += 4), //Between 0 (D -- worst cars) and 7 (X class -- best cars) inclusive 
                    CarPerformanceIndex = BitConverter.ToInt32(receiveBytes, index += 4), //Between 100 (slowest car) and 999 (fastest car) inclusive
                    DrivetrainType = BitConverter.ToInt32(receiveBytes, index += 4), //Corresponds to EDrivetrainType; 0 = FWD, 1 = RWD, 2 = AWD
                    NumCylinders = BitConverter.ToInt32(receiveBytes, index += 4) //Number of cylinders in the engine
                };

                if (packet.TimestampMS == lastTimestamp)
                {
                    Debug.Log("Same packet received, dropping.");
                    continue;
                }
                else
                {
                    lastTimestamp = packet.TimestampMS;
                }

                packetCount++;

                if (packet.IsRaceOn == 1)
                {
                    raceStarted = true;

                    packetQueue.Enqueue(packet);
                }

                if (raceStarted && packet.IsRaceOn == 0)
                {
                    recordRace = false; // Stop recording after one race
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        receivingUdpClient.Close();
        Debug.Log("UDP listener stopped. Final packet count: " + packetCount);
    }

    void OnDestroy ()
    {
        receivingUdpClient.Close();

        Debug.Log("Packet queue watcher stopped");
    }

    public void NewCapture ()
    {
        receivingUdpClient.Close();
        Debug.Log("Clearing. Starting new packet listener");

        DataPoints.Reset();
        visualizations.ResetVisualizations();
        uiVisualizations.ResetUIVisualizations();

        StartNewPacketListener();
    }

    public void LoadCSV ()
    {
        receivingUdpClient.Close();

        string path = FileBrowser.OpenSingleFile("Open CSV", lastSavePath, "csv");

        if (path != "")
        {
            Debug.Log("Reading packet info from " + path);

            visualizations.ResetVisualizations();
            uiVisualizations.ResetUIVisualizations();

            using (StreamReader reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] values = line.Split(',');

                    ForzaPacket p = new ForzaPacket
                    {
                        IsRaceOn = int.Parse(values[0]),
                        TimestampMS = uint.Parse(values[1]),
                        EngineMaxRpm = float.Parse(values[2]),
                        EngineIdleRpm = float.Parse(values[3]),
                        CurrentEngineRpm = float.Parse(values[4]),
                        AccelerationX = float.Parse(values[5]),
                        AccelerationY = float.Parse(values[6]),
                        AccelerationZ = float.Parse(values[7]),
                        VelocityX = float.Parse(values[8]),
                        VelocityY = float.Parse(values[9]),
                        VelocityZ = float.Parse(values[10]),
                        AngularVelocityX = float.Parse(values[11]),
                        AngularVelocityY = float.Parse(values[12]),
                        AngularVelocityZ = float.Parse(values[13]),
                        Yaw = float.Parse(values[14]),
                        Pitch = float.Parse(values[15]),
                        Roll = float.Parse(values[16]),
                        NormalizedSuspensionTravelFrontLeft = float.Parse(values[17]),
                        NormalizedSuspensionTravelFrontRight = float.Parse(values[18]),
                        NormalizedSuspensionTravelRearLeft = float.Parse(values[19]),
                        NormalizedSuspensionTravelRearRight = float.Parse(values[20]),
                        TireSlipRatioFrontLeft = float.Parse(values[21]),
                        TireSlipRatioFrontRight = float.Parse(values[22]),
                        TireSlipRatioRearLeft = float.Parse(values[23]),
                        TireSlipRatioRearRight = float.Parse(values[24]),
                        WheelRotationSpeedFrontLeft = float.Parse(values[25]),
                        WheelRotationSpeedFrontRight = float.Parse(values[26]),
                        WheelRotationSpeedRearLeft = float.Parse(values[27]),
                        WheelRotationSpeedRearRight = float.Parse(values[28]),
                        WheelOnRumbleStripFrontLeft = int.Parse(values[29]),
                        WheelOnRumbleStripFrontRight = int.Parse(values[30]),
                        WheelOnRumbleStripRearLeft = int.Parse(values[31]),
                        WheelOnRumbleStripRearRight = int.Parse(values[32]),
                        WheelInPuddleDepthFrontLeft = float.Parse(values[33]),
                        WheelInPuddleDepthFrontRight = float.Parse(values[34]),
                        WheelInPuddleDepthRearLeft = float.Parse(values[35]),
                        WheelInPuddleDepthRearRight = float.Parse(values[36]),
                        SurfaceRumbleFrontLeft = float.Parse(values[37]),
                        SurfaceRumbleFrontRight = float.Parse(values[38]),
                        SurfaceRumbleRearLeft = float.Parse(values[39]),
                        SurfaceRumbleRearRight = float.Parse(values[40]),
                        TireSlipAngleFrontLeft = float.Parse(values[41]),
                        TireSlipAngleFrontRight = float.Parse(values[42]),
                        TireSlipAngleRearLeft = float.Parse(values[43]),
                        TireSlipAngleRearRight = float.Parse(values[44]),
                        TireCombinedSlipFrontLeft = float.Parse(values[45]),
                        TireCombinedSlipFrontRight = float.Parse(values[46]),
                        TireCombinedSlipRearLeft = float.Parse(values[47]),
                        TireCombinedSlipRearRight = float.Parse(values[48]),
                        SuspensionTravelMetersFrontLeft = float.Parse(values[49]),
                        SuspensionTravelMetersFrontRight = float.Parse(values[50]),
                        SuspensionTravelMetersRearLeft = float.Parse(values[51]),
                        SuspensionTravelMetersRearRight = float.Parse(values[52]),
                        CarOrdinal = int.Parse(values[53]),
                        CarClass = int.Parse(values[54]),
                        CarPerformanceIndex = int.Parse(values[55]),
                        DrivetrainType = int.Parse(values[56]),
                        NumCylinders = int.Parse(values[57])
                    };

                    if (p.TimestampMS == lastTimestamp)
                    {
                        Debug.Log("Same packet received, dropping.");
                        continue;
                    }
                    else
                    {
                        lastTimestamp = p.TimestampMS;
                    }

                    DataPoint datapoint = DataPoints.AddPoint(p);

                    int lapNum = trackInfo.CheckNewLap(DataPoints.GetLatestPacketIndex());
                    p.LapNum = (uint)lapNum;

                    visualizations.DrawTrail(datapoint, lapNum, true);
                }

                reader.Close();

                Debug.Log("Read complete");
            }

            MainCamera mainCamera = Camera.main.GetComponent<MainCamera>();
            mainCamera.GoToPoint(0);
        }
    }

    public void SaveCSV ()
    {
        receivingUdpClient.Close();

        string path = FileBrowser.SaveFile("Save CSV", lastSavePath, "output", "csv");

        if (path != "")
        {
            Debug.Log("Saving packet info to " + path);

            DataPoints.SaveCSV(path);
            lastSavePath = Path.GetDirectoryName(path);

            Debug.Log("Save complete");
        }
    }
}
