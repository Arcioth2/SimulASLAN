using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace WpfApp1
{
    public class DronePhysics
    {
        public Vector3D Position { get; set; } = new Vector3D(0, 0, 0.5);
        public Vector3D Velocity { get; private set; } = new Vector3D(0, 0, 0);

        public double Pitch { get; set; }
        public double Roll { get; set; }
        public double Yaw { get; set; }

        private const double Gravity = -9.81;
        private const double DragFactor = 0.99;
        private const double LiftPower = 80.0;
        private const double GlideRatio = 15.0;

        public double ThrottleInput { get; set; }
        public bool IsBoosting { get; set; }

        public void OverrideVelocity(Vector3D newVelocity)
        {
            Velocity = newVelocity;
        }

        public void ApplyForwardImpulse(double strength)
        {
            double yRad = Yaw * Math.PI / 180.0;
            Vector3D forward = new Vector3D(-Math.Sin(yRad), Math.Cos(yRad), 0);
            forward.Normalize();
            Velocity += forward * strength;
        }

        public void Update(double deltaTime)
        {
            Vector3D forces = new Vector3D(0, 0, Gravity);

            double yRad = Yaw * Math.PI / 180.0;
            Vector3D forward = new Vector3D(-Math.Sin(yRad), Math.Cos(yRad), 0);
            Vector3D right = new Vector3D(Math.Cos(yRad), Math.Sin(yRad), 0);
            Vector3D up = new Vector3D(0, 0, 1);

            double horizontalMultiplier = IsBoosting ? 6.0 : 2.0;
            double pitchFactor = -Pitch / 45.0;
            double rollFactor = -Roll / 45.0;

            Vector3D engineDir = up +
                                 (forward * pitchFactor * horizontalMultiplier) +
                                 (right * rollFactor * horizontalMultiplier);
            engineDir.Normalize();

            forces += engineDir * (ThrottleInput * LiftPower);

            double speed = Velocity.Length;
            if (speed > 1.0)
            {
                Vector3D glideForce = (forward * pitchFactor * GlideRatio) +
                                      (right * rollFactor * GlideRatio);
                forces += glideForce;
            }

            Velocity += forces * deltaTime;
            Velocity *= DragFactor;
            Position += Velocity * deltaTime;

            if (Position.Z < 0)
            {
                Position = new Vector3D(Position.X, Position.Y, 0);
                Velocity = new Vector3D(Velocity.X, Velocity.Y, 0);
            }
        }
    }

    // --- MISSION DATA STRUCTURES ---
    public class MissionWaypoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Heading { get; set; }
    }

    public class MissionPlan
    {
        public string Name { get; set; }
        public List<MissionWaypoint> Waypoints { get; set; } = new List<MissionWaypoint>();
    }
}