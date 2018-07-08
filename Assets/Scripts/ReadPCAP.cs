using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ReadPCAP : MonoBehaviour {

    public int listenPort = 51384;
    public Visualizations visualizations;
    public UIVisualizations uiVisualizations;

    private UdpClient receivingUdpClient;
    private ConcurrentQueue<ForzaPacket> packetQueue;
    private int packetCount = 0;

    private Thread listener;
    private UInt32 lastTimestamp = 0;

    // Use this for initialization
	void Start () {
        packetQueue = new ConcurrentQueue<ForzaPacket>();

        listener = new Thread(new ThreadStart(ListenPackets));
        listener.IsBackground = true;
        listener.Start();
	}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            receivingUdpClient.Close();
            Debug.Log("Packet queue watcher stopped by user");
        }

        if (listener.IsAlive || packetQueue.Count > 0)
        {
            ForzaPacket packet;

            if (packetQueue.TryDequeue(out packet))
            {
                DataPoint newPoint = DataPoints.AddPoint(packet);

                visualizations.DrawTrail(newPoint);
                uiVisualizations.DrawUI(packet);
            }
        }
    }

    void ListenPackets()
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
}
