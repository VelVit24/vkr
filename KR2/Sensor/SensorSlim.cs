using KR2.Parameters;
using KR2.Points;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace KR2.Sensor
{
    internal class SensorSlim : ISensor
    {
        public enum HullType { Convex, Concave, Clusters }

        private IPoint pointsSource; // исходные точки
        private Canvas? canvas;

        public SimulationResults simResults;


        Point viewCenter;
        double viewRadius;
        private List<Point> previousPoints;
        Vector lastMotion = new Vector(0, 0);
        double maxDetectDist;
        Point lastCenter = new Point(0, 0);
        List<Vector> SpeedList = new List<Vector>();
        double koef = 1;

        public SensorSlim(SimulationResults results, IPoint pointsSource, Canvas? canvas, Point boundCenter, double boundRadius, double maxDetect, HullType hullType = HullType.Convex)
        {
            simResults = results;
            this.pointsSource = pointsSource;
            this.canvas = canvas;
            viewCenter = boundCenter;
            viewRadius = boundRadius;
            maxDetectDist = viewRadius * maxDetect;
            if (canvas != null)
            {
                DrawCircle(viewCenter, viewRadius, canvas);
            }
        }

        public void SetKoef(int k) { }
        public void SetClusterParam(double eps, int minPts) { }

        public SimulationResults GetSimulationResults() { return simResults; }

        bool pointLost = false;
        public void Update()
        {
            var visiblePoints = pointsSource.Points.Where(p => p.CurrentState == SimPoint.State.Active).Select(p => new Point(p.X, p.Y)).ToList();
            var allPoints = pointsSource.Points.Select(p => new Point(p.X, p.Y)).ToList();
            int inRange = 0;
            foreach (var point in allPoints)
            {
                double dist = Dist(viewCenter, point);
                if (dist < maxDetectDist)
                {
                    inRange++;
                }
            }
            if (inRange == 0)
            {
                throw new Exception("Область потеряна");
            }

            List<Point> temp = new List<Point>();
            foreach (var point in visiblePoints)
            {
                double dist = Dist(viewCenter, point);
                if (dist < maxDetectDist)
                {
                    temp.Add(point);
                }
            }
            visiblePoints = new List<Point>(temp);

            MoveViewField(visiblePoints);
        }

        private void MoveViewField(List<Point> visiblePoints)
        {
            if (visiblePoints.Count > 0)
            {
                Point farthestPoint = viewCenter;
                double maxDist = 0;

                foreach (var point in visiblePoints)
                {
                    double dist = Math.Sqrt(Distance(viewCenter, point));
                    if (dist > viewRadius && dist > maxDist)
                    {
                        maxDist = dist;
                        farthestPoint = point;
                    }
                }

                if (maxDist > viewRadius)
                {
                    Vector dir = farthestPoint - viewCenter;
                    dir.Normalize();
                    double moveDist = maxDist - viewRadius;
                    viewCenter.X += dir.X * moveDist;
                    viewCenter.Y += dir.Y * moveDist;
                }
            }



            Vector motionSum = new Vector(0, 0);
            int motionCount = 0;

            foreach (var point in visiblePoints)
            {
                double dist = Dist(viewCenter, point);
                if (dist > viewRadius)
                {
                    Vector motionVec = point - viewCenter;
                    motionVec.Normalize();
                    motionSum += motionVec;
                    motionCount++;
                }
            }

            Vector avgMotion;
            if (motionCount > 0)
            {
                avgMotion = motionSum / motionCount;
                koef = 1;
            }
            else
            {
                avgMotion = lastMotion;
                koef *= 0.95;
            }

            Point curCenter = new Point(0, 0);
            if (visiblePoints.Count > 0)
            {
                curCenter = CenterMass(visiblePoints);
                Vector centerMotion = curCenter - viewCenter;
                if (centerMotion.Length > 0.01)
                {
                    centerMotion.Normalize();
                    avgMotion += centerMotion;
                    avgMotion /= 2;
                }
            }

            if (visiblePoints.Count > 0 && lastCenter != new Point(0, 0))
            {
                Vector delta = curCenter - lastCenter;
                SpeedList.Add(delta);
            }
            if (SpeedList.Count > 10)
                SpeedList.RemoveAt(0);

            double avgSpeed = 0;
            foreach (var v in SpeedList)
                avgSpeed += v.Length;
            avgSpeed /= Math.Max(SpeedList.Count, 1);

            if (avgMotion.Length > 0 && avgSpeed > 0)
            {
                avgMotion.Normalize();
                avgSpeed *= koef;
                viewCenter.X += avgMotion.X * avgSpeed;
                viewCenter.Y += avgMotion.Y * avgSpeed;
            }

            lastMotion = avgMotion;
            lastCenter = curCenter;
            previousPoints = new List<Point>(visiblePoints);

            if (canvas != null)
            {
                DrawCircle(viewCenter, viewRadius, canvas);
            }
        }

        private double Dist(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X- b.X, 2) + Math.Pow(a.Y- b.Y, 2));
        }
        private double Distance(Point a, Point b) // расстояние
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }
        private Point CenterMass(List<Point> points)
        {
            double sumX = 0;
            double sumY = 0;
            int count = 0;

            foreach (var p in points)
            {
                sumX += p.X;
                sumY += p.Y;
                count++;
            }

            return new Point(sumX / count, sumY / count);
        }
        Ellipse? circle;
        public void DrawCircle(Point center, double radius, Canvas canvas)
        {
            canvas.Children.Remove(circle);
            circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            Canvas.SetLeft(circle, center.X - radius);
            Canvas.SetTop(circle, center.Y - radius);
            canvas.Children.Add(circle);
        }
    }
}
