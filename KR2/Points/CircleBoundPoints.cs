using KR2.Bounds;
using KR2.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KR2.Points
{
    public class CircleBoundPoints : IPoint
    {
        Canvas? area;
        private IBound boundArea;
        private List<Point> relativePoints = new List<Point>();
        private List<SimPoint> absolutePoints = new List<SimPoint>();
        private List<Vector> pointVelocities = new List<Vector>();
        private List<double> movementTimes = new List<double>();
        private Random random = new Random();

        private double vmax = 50.0;
        private double tmax = 2.0;
        private bool isRandomWalk;
        public IReadOnlyList<SimPoint> Points => absolutePoints;

        SimPoint.Distribution distribution;

        SimulationParameters simParameters;

        public CircleBoundPoints(IBound boundArea, Canvas? area, bool isRandomWalk, SimulationParameters parameters)
        {
            if (parameters.PointVisibleType == "Равномерное") distribution = SimPoint.Distribution.Uniform;
            else distribution = SimPoint.Distribution.Exponential;
            this.area = area;
            this.boundArea = boundArea;
            this.isRandomWalk = isRandomWalk;
            simParameters = parameters;
            vmax = parameters.vmax;
            tmax = parameters.tmax;
            InitializePoints(parameters.PointCount);
        }

        private void InitializePoints(int count)
        {
            Debug.WriteLine(isRandomWalk);
            CurrentParameters parameters = boundArea.CurrentParameters;

            for (int i = 0; i < count; i++)
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                double distance = random.NextDouble() * (parameters.radius-5);

                Point relativePoint = new Point(
                    distance * Math.Cos(angle),
                    distance * Math.Sin(angle));

                relativePoints.Add(relativePoint);
                absolutePoints.Add(new SimPoint(
                    parameters.center.X + relativePoint.X,
                    parameters.center.Y + relativePoint.Y, area, simParameters, distribution));

                pointVelocities.Add(new Vector());
                movementTimes.Add(0);
                if (isRandomWalk)
                {
                    StartNewRandomWalk(i);
                }
            }
        }

        public void UpdatePoints(double deltaTime)
        {
            CurrentParameters parameters = boundArea.CurrentParameters;
            for (int i = 0; i < relativePoints.Count; i++)
            {
                if (isRandomWalk) UpdateRandomWalk(i, deltaTime, parameters);
                else
                {
                    absolutePoints[i].X = parameters.center.X + relativePoints[i].X;
                    absolutePoints[i].Y = parameters.center.Y + relativePoints[i].Y;
                    absolutePoints[i].Update(deltaTime);
                }
            }
        }

        private void StartNewRandomWalk(int pointIndex)
        {
            double speed = random.NextDouble() * vmax;
            double angle = random.NextDouble() * 2 * Math.PI;

            pointVelocities[pointIndex] = new Vector(
                speed * Math.Cos(angle),
                speed * Math.Sin(angle));

            movementTimes[pointIndex] = random.NextDouble() * tmax;
        }

        private void UpdateRandomWalk(int pointIndex, double deltaTime, CurrentParameters parameters)
        {
            movementTimes[pointIndex] -= deltaTime;

            if (movementTimes[pointIndex] <= 0)
            {
                StartNewRandomWalk(pointIndex);
            }

            Point center = parameters.center;
            double radius = parameters.radius - 5;

            Vector displacement = pointVelocities[pointIndex] * deltaTime;
            Point newRelativePos = Point.Add(relativePoints[pointIndex], displacement);

            Vector toPoint = new Vector(relativePoints[pointIndex].X, relativePoints[pointIndex].Y);
            double distance = toPoint.Length;

            if (distance > radius)
            {
                Vector normal = toPoint;
                normal.Normalize();

                newRelativePos.X = normal.X * radius;
                newRelativePos.Y = normal.Y * radius;

                double dotProduct = pointVelocities[pointIndex].X * normal.X +
                                  pointVelocities[pointIndex].Y * normal.Y;

                pointVelocities[pointIndex] = new Vector(
                    pointVelocities[pointIndex].X - 2 * dotProduct * normal.X,
                    pointVelocities[pointIndex].Y - 2 * dotProduct * normal.Y);

                double randomAngle = random.NextDouble() * Math.PI * 0.1; // ±18 градусов
                pointVelocities[pointIndex] = RotateVector(pointVelocities[pointIndex], randomAngle - Math.PI * 0.05);
            }
            relativePoints[pointIndex] = newRelativePos;
            absolutePoints[pointIndex].X = parameters.center.X + relativePoints[pointIndex].X;
            absolutePoints[pointIndex].Y = parameters.center.Y + relativePoints[pointIndex].Y;
            absolutePoints[pointIndex].Update(deltaTime);
        }

        private Vector RotateVector(Vector vector, double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Vector(
                vector.X * cos - vector.Y * sin,
                vector.X * sin + vector.Y * cos);
        }
    }
}
