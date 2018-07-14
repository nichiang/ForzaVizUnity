using System;

public class ForzaPacket {

    public Int32 IsRaceOn { get; set; } // = 1 when race is on. = 0 when in menus/race stopped

    public UInt32 TimestampMS { get; set; } //Can overflow to 0 eventually

    public Single EngineMaxRpm { get; set; }
    public Single EngineIdleRpm { get; set; }
    public Single CurrentEngineRpm { get; set; }

    public Single AccelerationX { get; set; } //In the car's local space; X = right, Y = up, Z = forward
    public Single AccelerationY { get; set; }
    public Single AccelerationZ { get; set; }

    public Single VelocityX { get; set; } //In the car's local space; X = right, Y = up, Z = forward
    public Single VelocityY { get; set; }
    public Single VelocityZ { get; set; }

    public Single AngularVelocityX { get; set; } //In the car's local space; X = pitch, Y = yaw, Z = roll
    public Single AngularVelocityY { get; set; }
    public Single AngularVelocityZ { get; set; }

    public Single Yaw { get; set; }
    public Single Pitch { get; set; }
    public Single Roll { get; set; }

    public Single NormalizedSuspensionTravelFrontLeft { get; set; } // Suspension travel normalized: 0.0f = max stretch; 1.0 = max compression
    public Single NormalizedSuspensionTravelFrontRight { get; set; }
    public Single NormalizedSuspensionTravelRearLeft { get; set; }
    public Single NormalizedSuspensionTravelRearRight { get; set; }

    public Single TireSlipRatioFrontLeft { get; set; } // Tire normalized slip ratio, = 0 means 100% grip and |ratio| > 1.0 means loss of grip.
    public Single TireSlipRatioFrontRight { get; set; }
    public Single TireSlipRatioRearLeft { get; set; }
    public Single TireSlipRatioRearRight { get; set; }

    public Single WheelRotationSpeedFrontLeft { get; set; } // Wheel rotation speed radians/sec. 
    public Single WheelRotationSpeedFrontRight { get; set; }
    public Single WheelRotationSpeedRearLeft { get; set; }
    public Single WheelRotationSpeedRearRight { get; set; }

    public Int32 WheelOnRumbleStripFrontLeft { get; set; } // = 1 when wheel is on rumble strip, = 0 when off.
    public Int32 WheelOnRumbleStripFrontRight { get; set; }
    public Int32 WheelOnRumbleStripRearLeft { get; set; }
    public Int32 WheelOnRumbleStripRearRight { get; set; }

    public Single WheelInPuddleDepthFrontLeft { get; set; } // = from 0 to 1, where 1 is the deepest puddle
    public Single WheelInPuddleDepthFrontRight { get; set; }
    public Single WheelInPuddleDepthRearLeft { get; set; }
    public Single WheelInPuddleDepthRearRight { get; set; }

    public Single SurfaceRumbleFrontLeft { get; set; } // Non-dimensional surface rumble values passed to controller force feedback
    public Single SurfaceRumbleFrontRight { get; set; }
    public Single SurfaceRumbleRearLeft { get; set; }
    public Single SurfaceRumbleRearRight { get; set; }

    public Single TireSlipAngleFrontLeft { get; set; } // Tire normalized slip angle, = 0 means 100% grip and |angle| > 1.0 means loss of grip.
    public Single TireSlipAngleFrontRight { get; set; }
    public Single TireSlipAngleRearLeft { get; set; }
    public Single TireSlipAngleRearRight { get; set; }

    public Single TireCombinedSlipFrontLeft { get; set; } // Tire normalized combined slip, = 0 means 100% grip and |slip| > 1.0 means loss of grip.
    public Single TireCombinedSlipFrontRight { get; set; }
    public Single TireCombinedSlipRearLeft { get; set; }
    public Single TireCombinedSlipRearRight { get; set; }

    public Single SuspensionTravelMetersFrontLeft { get; set; } // Actual suspension travel in meters
    public Single SuspensionTravelMetersFrontRight { get; set; }
    public Single SuspensionTravelMetersRearLeft { get; set; }
    public Single SuspensionTravelMetersRearRight { get; set; }

    public Int32 CarOrdinal { get; set; } //Unique ID of the car make/model
    public Int32 CarClass { get; set; } //Between 0 (D -- worst cars) and 7 (X class -- best cars) inclusive 
    public Int32 CarPerformanceIndex { get; set; } //Between 100 (slowest car) and 999 (fastest car) inclusive
    public Int32 DrivetrainType { get; set; } //Corresponds to EDrivetrainType { get; set; } 0 = FWD, 1 = RWD, 2 = AWD
    public Int32 NumCylinders { get; set; } //Number of cylinders in the engine

    public UInt32 LapNum = 0; //Custom value

    public ForzaPacket()
    {

    }
}
