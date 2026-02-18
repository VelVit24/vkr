using KR2.Parameters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KR2.Bounds
{
    public class CircleBound : IBound
    {
        Canvas? area;
        private double Vmax;
        private double Tmax;
        private Ellipse? circle;
        double areaWidth, areaHeight;
        Random random = new Random();
        private double remainingTime;
        public double RemainingTime { get { return remainingTime; } }
        public CurrentParameters CurrentParameters { get; }

        public CircleBound(Point center, double radius, Canvas? area, double Vmax, double Tmax, double fallbackAreaWidth = 2500, double fallbackAreaHeight = 1700)
        {
            CurrentParameters = new CurrentParameters();
            this.area = area;
            CurrentParameters.center = center;
            CurrentParameters.radius = radius;
            this.Vmax = Vmax;
            this.Tmax = Tmax;
            areaWidth = area?.ActualWidth > 0 ? area.ActualWidth : fallbackAreaWidth;
            areaHeight = area?.ActualHeight > 0 ? area.ActualHeight : fallbackAreaHeight;
            if (area != null)
            {
                circle = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };
                area.Children.Add(circle);
            }
            UpdateAreaPosition();
        }

        public void StartNewRandomWalk()
        {
            double speed = random.NextDouble() * Vmax;
            double angle = random.NextDouble() * 2 * Math.PI;

            CurrentParameters.velocity = new Vector(
                speed * Math.Cos(angle),
                speed * Math.Sin(angle));

            remainingTime = random.NextDouble() * Tmax;
            Console.WriteLine(remainingTime);
        }

        public void Update(double deltaTime)
        {
            remainingTime -= deltaTime;
            CurrentParameters.center += CurrentParameters.velocity * deltaTime;
            CheckBoundaries();
        }

        private void CheckBoundaries()
        {
            bool bounced = false;

            if (CurrentParameters.center.X - CurrentParameters.radius < 0)
            {
                CurrentParameters.center = new Point(CurrentParameters.radius, CurrentParameters.center.Y);
                CurrentParameters.velocity.X = -CurrentParameters.velocity.X;
                bounced = true;
            }
            else if (CurrentParameters.center.X + CurrentParameters.radius > areaWidth)
            {
                CurrentParameters.center = new Point(areaWidth - CurrentParameters.radius, CurrentParameters.center.Y);
                CurrentParameters.velocity.X = -CurrentParameters.velocity.X;
                bounced = true;
            }

            if (CurrentParameters.center.Y - CurrentParameters.radius < 0)
            {
                CurrentParameters.center = new Point(CurrentParameters.center.X, CurrentParameters.radius);
                CurrentParameters.velocity.Y = -CurrentParameters.velocity.Y;
                bounced = true;
            }
            else if (CurrentParameters.center.Y + CurrentParameters.radius > areaHeight)
            {
                CurrentParameters.center = new Point(CurrentParameters.center.X, areaHeight - CurrentParameters.radius);
                CurrentParameters.velocity.Y = -CurrentParameters.velocity.Y;
                bounced = true;
            }

            if (bounced)
            {
                remainingTime = 0;
            }
        }

        public Point GetCenter()
        {
            return CurrentParameters.center;
        }

        public double GetRadius()
        {
            return CurrentParameters.radius;
        }

        public void UpdateAreaPosition()
        {
            if (circle == null)
            {
                return;
            }
            Canvas.SetLeft(circle, CurrentParameters.center.X-CurrentParameters.radius);
            Canvas.SetTop(circle, CurrentParameters.center.Y-CurrentParameters.radius);
        }
    }
}
