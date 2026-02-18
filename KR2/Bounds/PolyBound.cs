using KR2.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KR2.Bounds
{
    public class PolyBound : IBound
    {
        Canvas? area;
        private double Vmax;
        private double Tmax;
        private Polygon? polygon;

        Point center;
        double areaWidth, areaHeight;
        Random random = new Random();
        private double remainingTime;
        public double RemainingTime { get { return remainingTime; } }

        private Vector velocity;

        public CurrentParameters CurrentParameters { get; }

        public PolyBound(List<Point> points, Canvas? area, double Vmax, double Tmax, double fallbackAreaWidth = 2500, double fallbackAreaHeight = 1700)
        {
            CurrentParameters = new CurrentParameters();
            this.area = area;
            CurrentParameters.points = new List<Point>(points);
            this.Vmax = Vmax;
            this.Tmax = Tmax;
            areaWidth = area?.ActualWidth > 0 ? area.ActualWidth : fallbackAreaWidth;
            areaHeight = area?.ActualHeight > 0 ? area.ActualHeight : fallbackAreaHeight;
            center = CalculateCentroid();
            CurrentParameters.center = center;

            if (area != null)
            {
                polygon = new Polygon
                {
                    Points = new PointCollection(points),
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };
                area.Children.Add(polygon);
            }
        }

        private Point CalculateCentroid()
        {
            double x = 0, y = 0;
            foreach (var point in CurrentParameters.points)
            {
                x += point.X;
                y += point.Y;
            }
            return new Point(x / CurrentParameters.points.Count, y / CurrentParameters.points.Count);
        }

        public void StartNewRandomWalk()
        {
            double speed = random.NextDouble() * Vmax;
            double angle = random.NextDouble() * 2 * Math.PI;

            velocity = new Vector(
                speed * Math.Cos(angle),
                speed * Math.Sin(angle));

            remainingTime = random.NextDouble() * Tmax;
        }

        public void Update(double deltaTime)
        {
            remainingTime -= deltaTime;
            Vector displacement = velocity * deltaTime;
            for (int i = 0; i < CurrentParameters.points.Count; i++)
            {
                CurrentParameters.points[i] += displacement;
            }
            center += displacement;
            CurrentParameters.center = center;
            CheckBoundaries();
            if (polygon != null)
            {
                polygon.Points = new PointCollection(CurrentParameters.points);
            }
        }

        private void CheckBoundaries()
        {
            bool bounced = false;
            double minX = CurrentParameters.points.Min(p => p.X);
            double maxX = CurrentParameters.points.Max(p => p.X);
            double minY = CurrentParameters.points.Min(p => p.Y);
            double maxY = CurrentParameters.points.Max(p => p.Y);

            if (minX < 0)
            {
                double offset = -minX;
                for (int i = 0; i < CurrentParameters.points.Count; i++)
                {
                    CurrentParameters.points[i] = new Point(CurrentParameters.points[i].X + offset, CurrentParameters.points[i].Y);
                }
                velocity.X = -velocity.X;
                bounced = true;
            }
            else if (maxX > areaWidth)
            {
                double offset = areaWidth - maxX;
                for (int i = 0; i < CurrentParameters.points.Count; i++)
                {
                    CurrentParameters.points[i] = new Point(CurrentParameters.points[i].X + offset, CurrentParameters.points[i].Y);
                }
                velocity.X = -velocity.X;
                bounced = true;
            }
            if (minY < 0)
            {
                double offset = -minY;
                for (int i = 0; i < CurrentParameters.points.Count; i++)
                {
                    CurrentParameters.points[i] = new Point(CurrentParameters.points[i].X, CurrentParameters.points[i].Y + offset);
                }
                velocity.Y = -velocity.Y;
                bounced = true;
            }
            else if (maxY > areaHeight)
            {
                double offset = areaHeight - maxY;
                for (int i = 0; i < CurrentParameters.points.Count; i++)
                {
                    CurrentParameters.points[i] = new Point(CurrentParameters.points[i].X, CurrentParameters.points[i].Y + offset);
                }
                velocity.Y = -velocity.Y;
                bounced = true;
            }
            if (bounced)
            {
                remainingTime = 0;
                center = CalculateCentroid();
            }
        }

        public Point GetCenter()
        {
            Point center = GetWelzlCircle(CurrentParameters.points).center;
            return center;
        }

        public double GetRadius()
        {
            double radius = GetWelzlCircle(CurrentParameters.points).radius;
            return radius;
        }

        public (Point center, double radius) GetWelzlCircle(List<Point> points)
        {
            var rand = new Random();
            var shuffled = new List<Point>(points);
            Shuffle(shuffled, rand);

            return WelzlRecursive(shuffled, new List<Point>(), shuffled.Count);
        }

        private (Point center, double radius) WelzlRecursive(List<Point> points, List<Point> boundary, int n)
        {
            if (n == 0 || boundary.Count == 3)
            {
                return GetCircleFromBoundary(boundary);
            }

            Point p = points[n - 1];
            var circle = WelzlRecursive(points, boundary, n - 1);

            if (IsInCircle(p, circle.center, circle.radius))
                return circle;

            var newBoundary = new List<Point>(boundary) { p };
            return WelzlRecursive(points, newBoundary, n - 1);
        }

        private (Point center, double radius) GetCircleFromBoundary(List<Point> boundary)
        {
            if (boundary.Count == 0)
                return (new Point(0, 0), 0);
            else if (boundary.Count == 1)
                return (boundary[0], 0);
            else if (boundary.Count == 2)
            {
                var mid = new Point((boundary[0].X + boundary[1].X) / 2, (boundary[0].Y + boundary[1].Y) / 2);
                double radius = Math.Sqrt(Distance(boundary[0], boundary[1])) / 2;
                return (mid, radius);
            }
            else
            {
                return CircleFromThreePoints(boundary[0], boundary[1], boundary[2]);
            }
        }

        private (Point center, double radius) CircleFromThreePoints(Point A, Point B, Point C)
        {
            double D = 2 * (A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y));
            if (Math.Abs(D) < 1e-10) return (new Point(0, 0), double.PositiveInfinity); // Вырожденный случай

            double Ux = ((A.X * A.X + A.Y * A.Y) * (B.Y - C.Y) + (B.X * B.X + B.Y * B.Y) * (C.Y - A.Y) + (C.X * C.X + C.Y * C.Y) * (A.Y - B.Y)) / D;
            double Uy = ((A.X * A.X + A.Y * A.Y) * (C.X - B.X) + (B.X * B.X + B.Y * B.Y) * (A.X - C.X) + (C.X * C.X + C.Y * C.Y) * (B.X - A.X)) / D;

            Point center = new Point(Ux, Uy);
            double radius = Math.Sqrt(Distance(center, A));
            return (center, radius);
        }

        private bool IsInCircle(Point p, Point center, double radius)
        {
            return Math.Sqrt(Distance(p, center)) <= radius + 1e-6;
        }

        private void Shuffle<T>(List<T> list, Random rand)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        private double Distance(Point a, Point b) // расстояние
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }
    }
}
