using KR2.Bounds;
using KR2.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static KR2.Points.SimPoint;

namespace KR2.Points
{
    public class RectangleBoundPoints : IPoint
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

        SimulationParameters simParameters;
        Distribution distribution;

        public RectangleBoundPoints(IBound boundArea, Canvas? area, bool isRandomWalk, SimulationParameters parameters)
        {
            if (parameters.PointVisibleType == "Равномерное") distribution = Distribution.Uniform;
            else distribution = Distribution.Exponential;
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
            CurrentParameters parameters = boundArea.CurrentParameters;

            for (int i = 0; i < count; i++)
            {
                double halfWidth = parameters.width / 2 - 5;
                double halfHeight = parameters.height / 2 - 5;

                Point relativePoint = new Point(
                    (random.NextDouble() * 2 - 1) * halfWidth,
                    (random.NextDouble() * 2 - 1) * halfHeight);

                relativePoints.Add(relativePoint);

                Point rotatedPoint = RotatePoint(relativePoint, parameters.angle);
                absolutePoints.Add(new SimPoint(
                    parameters.center.X + rotatedPoint.X,
                    parameters.center.Y + rotatedPoint.Y,
                    area, simParameters, distribution));

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
                if (isRandomWalk)
                {
                    UpdateRandomWalk(i, deltaTime, parameters);
                }
                else
                {
                    Point rotatedPoint = RotatePoint(relativePoints[i], parameters.angle);
                    absolutePoints[i].X = parameters.center.X + rotatedPoint.X;
                    absolutePoints[i].Y = parameters.center.Y + rotatedPoint.Y;
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

            double halfWidth = parameters.width / 2 - 5;
            double halfHeight = parameters.height / 2 - 5;
            double angle = parameters.angle;

            Vector displacement = pointVelocities[pointIndex] * deltaTime;
            Point newRelativePos = Point.Add(relativePoints[pointIndex], displacement);

            bool outOfBounds = false;

            if (newRelativePos.X < -halfWidth)
            {
                newRelativePos.X = -halfWidth;
                pointVelocities[pointIndex] = new Vector(-pointVelocities[pointIndex].X, pointVelocities[pointIndex].Y);
                outOfBounds = true;
            }
            else if (newRelativePos.X > halfWidth)
            {
                newRelativePos.X = halfWidth;
                pointVelocities[pointIndex] = new Vector(-pointVelocities[pointIndex].X, pointVelocities[pointIndex].Y);
                outOfBounds = true;
            }

            if (newRelativePos.Y < -halfHeight)
            {
                newRelativePos.Y = -halfHeight;
                pointVelocities[pointIndex] = new Vector(pointVelocities[pointIndex].X, -pointVelocities[pointIndex].Y);
                outOfBounds = true;
            }
            else if (newRelativePos.Y > halfHeight)
            {
                newRelativePos.Y = halfHeight;
                pointVelocities[pointIndex] = new Vector(pointVelocities[pointIndex].X, -pointVelocities[pointIndex].Y);
                outOfBounds = true;
            }

            if (outOfBounds)
            {
                double randomAngle = random.NextDouble() * Math.PI * 0.1 - Math.PI * 0.05;
                pointVelocities[pointIndex] = RotateVector(pointVelocities[pointIndex], randomAngle);
            }

            relativePoints[pointIndex] = newRelativePos;

            Point rotatedPoint = RotatePoint(newRelativePos, angle);
            absolutePoints[pointIndex].X = parameters.center.X + rotatedPoint.X;
            absolutePoints[pointIndex].Y = parameters.center.Y + rotatedPoint.Y;
            absolutePoints[pointIndex].Update(deltaTime);
        }

        private Point RotatePoint(Point point, double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            return new Point(
                point.X * cos - point.Y * sin,
                point.X * sin + point.Y * cos);
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
