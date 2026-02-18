using KR2.Parameters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KR2.Bounds
{
    public class RectBound : IBound
    {
        Canvas? area;
        private double Vmax, Tmax, Omax;
        private Rectangle? rect;
        double diag;
        double areaWidth, areaHeight;
        Random random = new Random();
        private double remainingTime;
        public double RemainingTime { get { return remainingTime; } }
        private RotateTransform rotateTransform;

        private Vector velocity;

        public CurrentParameters CurrentParameters { get; }

        public RectBound(Point center, double width, double height, Canvas? area, double Vmax, double Tmax, double Omax, double fallbackAreaWidth = 2500, double fallbackAreaHeight = 1700)
        {
            CurrentParameters = new CurrentParameters();
            this.area = area;
            CurrentParameters.center = center;
            CurrentParameters.width = width;
            CurrentParameters.height = height;
            this.Vmax = Vmax;
            this.Tmax = Tmax;
            this.Omax = Omax;
            areaWidth = area?.ActualWidth > 0 ? area.ActualWidth : fallbackAreaWidth;
            areaHeight = area?.ActualHeight > 0 ? area.ActualHeight : fallbackAreaHeight;
            if (area != null)
            {
                rect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
            }

            diag = Math.Sqrt(Math.Pow(width/2, 2) + Math.Pow(height/2, 2));

            rotateTransform = new RotateTransform();
            if (rect != null)
            {
                rect.RenderTransform = rotateTransform;
            }

            if (area != null && rect != null)
            {
                area.Children.Add(rect);
            }
            UpdateAreaPosition();
        }

        public void StartNewRandomWalk()
        {
            double speed = random.NextDouble() * Vmax;
            double angle = random.NextDouble() * 2 * Math.PI;

            velocity = new Vector(
                speed * Math.Cos(angle),
                speed * Math.Sin(angle));

            CurrentParameters.angularVelocity = random.NextDouble() * Omax;
            if (random.Next(2) == 0)
            {
                CurrentParameters.angularVelocity = -CurrentParameters.angularVelocity;
            }

            remainingTime = random.NextDouble() * Tmax;
        }

        public void Update(double deltaTime)
        {
            remainingTime -= deltaTime;
            CurrentParameters.center += velocity * deltaTime;
            CurrentParameters.angle += CurrentParameters.angularVelocity * deltaTime;
            if (rect != null)
            {
                rotateTransform.Angle = CurrentParameters.angle * (180/Math.PI);
            }
            CheckBoundaries();
        }

        private void CheckBoundaries()
        {
            bool bounced = false;
            if (CurrentParameters.center.X - diag < 0)
            {
                CurrentParameters.center = new Point(diag, CurrentParameters.center.Y);
                velocity.X = -velocity.X;
                bounced = true;
            }
            else if (CurrentParameters.center.X + diag > areaWidth)
            {
                CurrentParameters.center = new Point(areaWidth - diag, CurrentParameters.center.Y);
                velocity.X = -velocity.X;
                bounced = true;
            }
            if (CurrentParameters.center.Y - diag < 0)
            {
                CurrentParameters.center = new Point(CurrentParameters.center.X, diag);
                velocity.Y = -velocity.Y;
                bounced = true;
            }
            else if (CurrentParameters.center.Y + diag > areaHeight)
            {
                CurrentParameters.center = new Point(CurrentParameters.center.X, areaHeight - diag);
                velocity.Y = -velocity.Y;
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
            return Math.Sqrt(Math.Pow(CurrentParameters.width/2, 2) + Math.Pow(CurrentParameters.height/2, 2));
        }

        public void UpdateAreaPosition()
        {
            if (rect == null)
            {
                return;
            }
            Canvas.SetLeft(rect, CurrentParameters.center.X - CurrentParameters.width/2);
            Canvas.SetTop(rect, CurrentParameters.center.Y - CurrentParameters.height/2);
        }
    }
}
