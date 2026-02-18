using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Diagnostics;
using KR2.Points;
using KR2.Parameters;

namespace KR2.Sensor
{
    public class Sensor : ISensor
    {
        public enum HullType { Convex, Concave, Clusters }

        private IPoint pointsSource; // исходные точки
        private List<Point> hull = new List<Point>(); // оболочка
        private List<Point> trueHull = new List<Point>();
        private List<List<Point>> clusterHulls = new List<List<Point>>(); // кластерные оболочки
        private List<List<Point>> trueClusterHulls = new List<List<Point>>();
        private Polygon? visualHull; // отображение оболочки
        private List<Polygon>? visualHulls; // отображение кластеров
        private Canvas? canvas;
        private HullType hullType;
        private int k, minPts; // k - параметр гладкости, minPts - мин колво соседей в кластере
        private double eps; // макс расстояние между соседями

        public SimulationResults simResults;

        private Ellipse? visualCenter;

        Point viewCenter;
        double viewRadius;
        private List<Point> previousPoints;
        Vector lastMotion = new Vector(0, 0);
        double maxDetectDist;
        Point lastCenter = new Point(0, 0);
        List<Vector> SpeedList = new List<Vector>();
        double koef = 1;

        public Sensor(SimulationResults results, IPoint pointsSource, Canvas? canvas, Point boundCenter, double boundRadius, double maxDetect, HullType hullType = HullType.Convex)
        {
            simResults = results;
            this.pointsSource = pointsSource;
            this.canvas = canvas;
            this.hullType = hullType;

            if (canvas != null && hullType != HullType.Clusters)
            {
                visualHull = new Polygon
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };
                canvas.Children.Add(visualHull);
            }

            if (canvas != null)
            {
                visualCenter = new Ellipse
                {
                    Fill = Brushes.Blue,
                    Width = 5,
                    Height = 5
                };
                canvas.Children.Add(visualCenter);
            }

            viewCenter = boundCenter;
            viewRadius = boundRadius;
            maxDetectDist = viewRadius * maxDetect;
            if (canvas != null)
            {
                DrawCircle(viewCenter, viewRadius, canvas);
            }
        }

        public void SetKoef(int k) { this.k = k; }
        public void SetClusterParam(double eps, int minPts) { this.eps=eps; this.minPts=minPts; }

        public SimulationResults GetSimulationResults() { return simResults; }

        bool pointLost = false;
        public void Update()
        {

            var visiblePoints = pointsSource.Points.Where(p => p.CurrentState == SimPoint.State.Active).Select(p => new Point(p.X, p.Y)).ToList();
            var allPoints = pointsSource.Points.Select(p => new Point(p.X, p.Y)).ToList();
            simResults.PointCount = visiblePoints.Count;
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

            switch (hullType)
            {
                case HullType.Convex:
                    hull = ComputeConvexHull(visiblePoints);
                    trueHull = ComputeConvexHull(allPoints);
                    break;
                case HullType.Concave:
                    hull = ComputeConcaveHull(visiblePoints, k);
                    trueHull = ComputeConcaveHull(allPoints, k);
                    break;
                case HullType.Clusters:
                    clusterHulls = ComputeClusterHulls(visiblePoints, eps, minPts);
                    trueClusterHulls = ComputeClusterHulls(allPoints, eps, minPts);
                    break;

            }
            if (hullType != HullType.Clusters)
            {
                UpdateVisualHull();
                simResults.HullArea = ComputePolygonArea(hull);
                simResults.TrueHullArea = ComputePolygonArea(trueHull);
                if (hull.Count >= 3)
                {
                    var center = ComputeCentroid(hull);
                    var trueCenter = ComputeCentroid(trueHull);
                    Vector offset = center - trueCenter;
                    simResults.CenterDelay = offset.Length;
                    UpdateVisualCenter(center);
                    DrawTrajectory(center);
                }
            }
            else
            {
                UpdateVisualClusterHulls();
                double area = 0;
                foreach (var cluster in clusterHulls)
                    area += ComputePolygonArea(cluster);
                simResults.HullArea = area;
                area = 0;
                foreach (var cluster in trueClusterHulls)
                    area += ComputePolygonArea(cluster);
                if (clusterHulls.Count > 0)
                {
                    int count = 0;
                    Point center = new Point(0, 0);
                    foreach (var cluster in clusterHulls)
                    {
                        foreach (var point in cluster)
                        {
                            count++;
                            center.X += point.X;
                            center.Y += point.Y;
                        }
                    }
                    center.X/=count; center.Y/=count;
                    count = 0;
                    Point trueCenter = new Point(0, 0);
                    foreach (var cluster in trueClusterHulls)
                    {
                        foreach (var point in cluster)
                        {
                            count++;
                            trueCenter.X += point.X;
                            trueCenter.Y += point.Y;
                        }
                    }
                    trueCenter.X/=count; trueCenter.Y/=count;
                    Vector offset = center - trueCenter;
                    simResults.CenterDelay = offset.Length;
                    UpdateVisualCenter(center);
                    DrawTrajectory(center);
                }
                simResults.TrueHullArea = area;
            }
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
                koef *= 0.99;
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

        private List<Point> ComputeConvexHull(List<Point> points) // выпуклая
        {
            if (points.Count < 3)
                return new List<Point>(points);
            List<Point> hull = new List<Point>();
            Point p0 = points[0];
            foreach (Point p in points)
                if (p.X < p0.X || p.X == p0.X && p.Y < p0.Y)
                    p0 = p;
            Point current = p0;
            do
            {
                hull.Add(current);
                Point next = points[0];
                foreach (Point p in points)
                {
                    if (p == current)
                        continue;
                    double orientation = Orientation(current, next, p);
                    if (next == current || orientation < 0 || orientation == 0 && Distance(current, p) > Distance(current, next))
                    {
                        next = p;
                    }
                }
                current = next;
            } while (current != p0);

            return hull;
        }
        private double Orientation(Point a, Point b, Point c) // поворот
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private double Distance(Point a, Point b) // расстояние
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }

        private List<Point> ComputeConcaveHull(List<Point> points, int k) // невыпуклая оболочка
        {
            List<Point> convexHull = ComputeConvexHull(points);

            List<Point> G = points.Except(convexHull).ToList();
            for (int i = 0; i < k * convexHull.Count; i++)
            {
                int im = IndexMaxSide(convexHull);
                Point pb = convexHull[im];
                Point pe = convexHull[(im+1)%convexHull.Count];
                int jpt = -1;
                double qd0 = qDist(pb, pe);
                double Sqmax = 0;
                foreach (Point pt in G)
                {
                    double qd1 = qDist(pb, pt), qd2 = qDist(pe, pt);
                    if (qd0 > Math.Abs(qd1-qd2))
                    {
                        double Sqt = Sq(pb, pt, pe);
                        if (Sqt < Sqmax) continue;
                        if (NotEmptyTriangle(pb, pe, pt, G))
                            continue;
                        if (IsCrossHull(pb, pe, pt, convexHull))
                            continue;
                        jpt = G.IndexOf(pt);
                        Sqmax = Sqt;
                    }
                }
                if (jpt >=0)
                {
                    convexHull.Insert(im+1, G[jpt]);
                    G.RemoveAt(jpt);
                }
                else break;
            }

            return convexHull;
        }
        private int IndexMaxSide(List<Point> hull) // находим макс. сторону
        {
            double maxDist = -1;
            int maxIndex = 0;
            for (int i = 0; i < hull.Count; i++)
            {
                Point a = hull[i];
                Point b = hull[(i + 1) % hull.Count];

                double dist = Distance(a, b);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxIndex = i;
                }
            }

            return maxIndex;
        }
        private double qDist(Point pb, Point pe) // квадрат расстояния
        {
            return Math.Pow(pb.X- pe.X, 2) + Math.Pow(pb.Y-pe.Y, 2);
        }
        private double Sq(Point p1, Point p2, Point p3) // площадь треугольника
        {
            return 1/2*Math.Abs(p1.X*(p2.Y-p3.Y)+p2.X*(p3.Y-p1.Y)+p3.X*(p1.Y-p2.Y));
        }
        private bool NotEmptyTriangle(Point pb, Point pe, Point pt, List<Point> G) // проверяем нет ли точкек в треугольнике
        {
            foreach (var p in G)
            {
                if (p != pb && p != pe && p != pt && PointInTriangle(p, pb, pe, pt))
                    return true; // хотя бы одна точка внутри треугольника
            }
            return false;
        }
        private bool PointInTriangle(Point p, Point a, Point b, Point c)
        {
            double areaOrig = Math.Abs(OrientationArea(a, b, c));
            double area1 = Math.Abs(OrientationArea(p, b, c));
            double area2 = Math.Abs(OrientationArea(a, p, c));
            double area3 = Math.Abs(OrientationArea(a, b, p));

            double sum = area1 + area2 + area3;

            return Math.Abs(areaOrig - sum) < 1e-6;
        }
        private double OrientationArea(Point a, Point b, Point c)
        {
            return 0.5 * ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));
        }
        private bool IsCrossHull(Point pb, Point pe, Point pt, List<Point> convexHull) // проверяем пересекает ли строна pbpt или pept оболочку
        {
            var testEdges = new List<(Point, Point)>
            {
                (pb, pt),
                (pe, pt)
            };

            for (int i = 0; i < convexHull.Count; i++)
            {
                Point h1 = convexHull[i];
                Point h2 = convexHull[(i + 1) % convexHull.Count];

                foreach (var (a, b) in testEdges)
                {
                    if (SegmentsIntersect(a, b, h1, h2))
                        return true; // одна из сторон пересекает оболочку
                }
            }

            return false;
        }
        private bool SegmentsIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            return Orientation(p1, p2, q1) * Orientation(p1, p2, q2) < 0 &&
                   Orientation(q1, q2, p1) * Orientation(q1, q2, p2) < 0;
        }

        public List<List<Point>> DBSCAN(List<Point> points, double eps, int minPts) // вычисление кластеров
        {
            var clusters = new List<List<Point>>();
            var visited = new HashSet<Point>();
            var clustered = new HashSet<Point>();

            foreach (var point in points)
            {
                if (visited.Contains(point))
                    continue;

                visited.Add(point);
                var neighbors = RegionQuery(points, point, eps);

                if (neighbors.Count < minPts)
                {
                    continue;
                }

                var cluster = new List<Point>();
                var queue = new Queue<Point>(neighbors);

                cluster.Add(point);
                clustered.Add(point);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();

                    if (!visited.Contains(current))
                    {
                        visited.Add(current);
                        var currentNeighbors = RegionQuery(points, current, eps);
                        if (currentNeighbors.Count >= minPts)
                        {
                            foreach (var n in currentNeighbors)
                                if (!queue.Contains(n))
                                    queue.Enqueue(n);
                        }
                    }

                    if (!clustered.Contains(current))
                    {
                        cluster.Add(current);
                        clustered.Add(current);
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;

            List<Point> RegionQuery(List<Point> all, Point center, double radius) // поиск соседей
            {
                return all.Where(p =>
                {
                    double dx = p.X - center.X;
                    double dy = p.Y - center.Y;
                    return Math.Sqrt(dx * dx + dy * dy) <= radius;
                }).ToList();
            }
        }

        private List<List<Point>> ComputeClusterHulls(List<Point> points, double eps, int minPts) // для каждого кластера считаем оболочку
        {
            if (canvas != null && visualHulls != null)
                foreach (var hull in visualHulls)
                    canvas.Children.Remove(hull);
            if (canvas != null)
            {
                visualHulls = new List<Polygon>();
            }
            List<List<Point>> clusters = DBSCAN(points, eps, minPts);
            var hulls = new List<List<Point>>();
            foreach (var cluster in clusters)
            {
                var hull = ComputeConvexHull(cluster);
                hulls.Add(hull);
                if (canvas != null && visualHulls != null)
                {
                    var poly = new Polygon
                    {
                        Stroke = Brushes.Green,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent,
                        Points = new PointCollection(hull)
                    };
                    visualHulls.Add(poly);
                    canvas.Children.Add(poly);
                }
            }
            return hulls;
        }

        private void UpdateVisualClusterHulls()
        {
            if (visualHulls == null)
            {
                return;
            }
            for (int i = 0; i < Math.Min(clusterHulls.Count, visualHulls.Count); i++)
            {
                var poly = visualHulls[i];
                var hull = clusterHulls[i];

                if (hull.Count == 0)
                {
                    poly.Visibility = Visibility.Collapsed;
                }
                else
                {
                    poly.Visibility = Visibility.Visible;
                    poly.Points = new PointCollection(hull);
                }
            }
        }

        private void UpdateVisualHull()
        {
            if (visualHull == null)
            {
                return;
            }
            if (hull.Count == 0)
            {
                visualHull.Visibility = Visibility.Collapsed;
                return;
            }

            visualHull.Visibility = Visibility.Visible;
            visualHull.Points = new PointCollection(hull);
        }

        private double ComputePolygonArea(List<Point> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return 0;
            double area = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Point current = polygon[i];
                Point next = polygon[(i + 1) % polygon.Count]; // замкнутый контур
                area += current.X * next.Y - next.X * current.Y;
            }
            return Math.Abs(area) / 2.0;
        }

        private Point ComputeCentroid(List<Point> polygon)
        {
            double cx = 0, cy = 0;
            double area = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                Point current = polygon[i];
                Point next = polygon[(i + 1) % n];

                double cross = current.X * next.Y - next.X * current.Y;
                cx += (current.X + next.X) * cross;
                cy += (current.Y + next.Y) * cross;
                area += cross;
            }

            area *= 0.5;
            cx /= 6 * area;
            cy /= 6 * area;

            return new Point(cx, cy);
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
        private void UpdateVisualCenter(Point point)
        {
            if (visualCenter == null)
            {
                return;
            }
            Canvas.SetLeft(visualCenter, point.X);
            Canvas.SetTop(visualCenter, point.Y);
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

        private List<Point> trajectoryPoints = new();
        Point lastTrajPoint = new Point(-10000, -10000);
        //private void DrawTrajectory(Point point)
        //{
        //    trajectoryPoints.Add(point);
        //    if (trajectoryPoints.Count > 1)
        //    {
        //        var line = new Line
        //        {
        //            X1 = trajectoryPoints[^2].X,
        //            Y1 = trajectoryPoints[^2].Y,
        //            X2 = trajectoryPoints[^1].X,
        //            Y2 = trajectoryPoints[^1].Y,
        //            Stroke = Brushes.Red,
        //            StrokeThickness = 1
        //        };
        //        canvas.Children.Add(line);
        //    }
        //}
        private void DrawTrajectory(Point point)
        {
            if (canvas == null)
            {
                return;
            }
            int n = 10;
            trajectoryPoints.Add(point);
            if (trajectoryPoints.Count == n)
            {
                Point p = new Point(0, 0);
                foreach (Point p2 in trajectoryPoints)
                {
                    p.X += p2.X;
                    p.Y += p2.Y;
                }
                p.X /= n; p.Y /= n;
                if (lastTrajPoint.X != -10000)
                {
                    var line = new Line
                    {
                        X1 = lastTrajPoint.X,
                        Y1 = lastTrajPoint.Y,
                        X2 = p.X,
                        Y2 = p.Y,
                        Stroke = Brushes.Red,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(line);
                }
                lastTrajPoint = p;
                trajectoryPoints.Clear();
            }
            
        }
        private bool IsInsideCircle(Point p, Point center, double radius)
        {
            double dx = p.X - center.X;
            double dy = p.Y - center.Y;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
