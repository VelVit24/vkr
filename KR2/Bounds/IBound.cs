using KR2.Parameters;
using System.Windows;

namespace KR2.Bounds
{
    public interface IBound
    {
        public CurrentParameters CurrentParameters { get; }
        public double RemainingTime { get; }
        public void StartNewRandomWalk() { }
        public void Update(double deltaTime) { }
        public void UpdateAreaPosition() { }
        public Point GetCenter() { return new Point(); }
        public double GetRadius() { return 0; }
    }
}
