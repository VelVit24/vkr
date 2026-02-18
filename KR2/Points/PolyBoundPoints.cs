using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using KR2.Bounds;
using KR2.Parameters;

namespace KR2.Points
{
    public class PolyBoundPoints : IPoint
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
        SimPoint.Distribution distribution;

        public PolyBoundPoints(IBound boundArea, Canvas? area, bool isRandomWalk, SimulationParameters parameters)
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
            CurrentParameters parameters = boundArea.CurrentParameters;
            List<Point> polygonPoints = parameters.points;
            Point center = parameters.center;

            Rect bounds = CalculateBoundingBox(polygonPoints);

            for (int i = 0; i < count; i++)
            {
                Point relativePoint;
                bool pointInside;
                int attempts = 0;

                do
                {
                    relativePoint = new Point(
                        bounds.Left + random.NextDouble() * bounds.Width,
                        bounds.Top + random.NextDouble() * bounds.Height);

                    pointInside = IsPointInPolygon(relativePoint, polygonPoints);
                    attempts++;

                } while (!pointInside && attempts < 100);

                relativePoints.Add(new Point(relativePoint.X-center.X, relativePoint.Y-center.Y));
                absolutePoints.Add(new SimPoint(relativePoint.X, relativePoint.Y, area, simParameters, distribution));

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
            List<Point> polygonPoints = parameters.points;
            Point center = parameters.center;

            for (int i = 0; i < relativePoints.Count; i++)
            {
                if (isRandomWalk)
                {
                    UpdateRandomWalk(i, deltaTime, polygonPoints, center);
                }
                else
                {
                    Point globalPos = new Point(center.X + relativePoints[i].X, center.Y + relativePoints[i].Y);
                    absolutePoints[i].X = globalPos.X;
                    absolutePoints[i].Y = globalPos.Y;
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

        private void UpdateRandomWalk(int pointIndex, double deltaTime, List<Point> polygonPoints, Point center)
        {
            movementTimes[pointIndex] -= deltaTime;

            if (movementTimes[pointIndex] <= 0)
            {
                StartNewRandomWalk(pointIndex);
            }

            Vector displacement = pointVelocities[pointIndex] * deltaTime;
            Point newRelativePos = Point.Add(relativePoints[pointIndex], displacement);
            Point newGlobalPos = new Point(center.X + newRelativePos.X, center.Y + newRelativePos.Y);

            if (!IsPointInPolygon(newGlobalPos, polygonPoints))
            {
                Point closestBoundary = FindClosestBoundaryPoint(
                    new Point(center.X + relativePoints[pointIndex].X, center.Y + relativePoints[pointIndex].Y),
                    newGlobalPos,
                    polygonPoints);

                newRelativePos = new Point(closestBoundary.X - center.X, closestBoundary.Y - center.Y);

                Vector normal = CalculateEdgeNormal(closestBoundary, polygonPoints);
                normal.Normalize();

                double dotProduct = pointVelocities[pointIndex].X * normal.X +
                                  pointVelocities[pointIndex].Y * normal.Y;

                pointVelocities[pointIndex] = new Vector(
                    pointVelocities[pointIndex].X - 2 * dotProduct * normal.X,
                    pointVelocities[pointIndex].Y - 2 * dotProduct * normal.Y);

                double randomAngle = random.NextDouble() * Math.PI * 0.1 - Math.PI * 0.05;
                pointVelocities[pointIndex] = RotateVector(pointVelocities[pointIndex], randomAngle);
            }

            relativePoints[pointIndex] = newRelativePos;
            absolutePoints[pointIndex].X = center.X + newRelativePos.X;
            absolutePoints[pointIndex].Y = center.Y + newRelativePos.Y;
            absolutePoints[pointIndex].Update(deltaTime);
        }

        private bool IsPointInPolygon(Point point, List<Point> polygon)
        {
            int intersections = 0;
            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % count];

                if (point.Y > Math.Min(p1.Y, p2.Y))
                {
                    if (point.Y <= Math.Max(p1.Y, p2.Y))
                    {
                        if (point.X <= Math.Max(p1.X, p2.X))
                        {
                            double xIntersection = (point.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;

                            if (p1.X == p2.X || point.X <= xIntersection)
                            {
                                intersections++;
                            }
                        }
                    }
                }
            }

            return intersections % 2 != 0;
        }

        private Rect CalculateBoundingBox(List<Point> points)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (Point p in points)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        private Point FindClosestBoundaryPoint(Point start, Point end, List<Point> polygon)
        {
            Point closest = start;
            double minDistance = double.MaxValue;
            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % count];

                Point intersection = FindLineIntersection(start, end, p1, p2);
                if (!double.IsNaN(intersection.X))
                {
                    double dist = Point.Subtract(intersection, start).Length;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closest = intersection;
                    }
                }
            }

            return closest;
        }

        private Point FindLineIntersection(Point a1, Point a2, Point b1, Point b2)
        {
            double denominator = (a1.X - a2.X) * (b1.Y - b2.Y) - (a1.Y - a2.Y) * (b1.X - b2.X);

            if (denominator == 0)
                return new Point(double.NaN, double.NaN);

            double x = ((a1.X * a2.Y - a1.Y * a2.X) * (b1.X - b2.X) - (a1.X - a2.X) * (b1.X * b2.Y - b1.Y * b2.X)) / denominator;
            double y = ((a1.X * a2.Y - a1.Y * a2.X) * (b1.Y - b2.Y) - (a1.Y - a2.Y) * (b1.X * b2.Y - b1.Y * b2.X)) / denominator;

            if (x < Math.Min(a1.X, a2.X) || x > Math.Max(a1.X, a2.X) ||
                x < Math.Min(b1.X, b2.X) || x > Math.Max(b1.X, b2.X) ||
                y < Math.Min(a1.Y, a2.Y) || y > Math.Max(a1.Y, a2.Y) ||
                y < Math.Min(b1.Y, b2.Y) || y > Math.Max(b1.Y, b2.Y))
            {
                return new Point(double.NaN, double.NaN);
            }

            return new Point(x, y);
        }

        private Vector CalculateEdgeNormal(Point point, List<Point> polygon)
        {
            int count = polygon.Count;
            double minDistance = double.MaxValue;
            Vector normal = new Vector(0, 0);

            for (int i = 0; i < count; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % count];

                double distance = PointToLineDistance(point, p1, p2);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    Vector edge = Point.Subtract(p2, p1);
                    normal = new Vector(-edge.Y, edge.X);
                }
            }

            normal.Normalize();
            return normal;
        }

        private double PointToLineDistance(Point point, Point lineStart, Point lineEnd)
        {
            double l2 = Math.Pow(lineStart.X - lineEnd.X, 2) + Math.Pow(lineStart.Y - lineEnd.Y, 2);
            if (l2 == 0) return Point.Subtract(point, lineStart).Length;

            double t = Math.Max(0, Math.Min(1, ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                                              (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / l2));

            Point projection = new Point(
                lineStart.X + t * (lineEnd.X - lineStart.X),
                lineStart.Y + t * (lineEnd.Y - lineStart.Y));

            return Point.Subtract(point, projection).Length;
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
