using System.Windows.Controls;

namespace KR2.Points
{
    public interface IPoint
    {
        public IReadOnlyList<SimPoint> Points { get; }
        public void UpdatePoints(double deltaTime) { }
        public void Draw(Canvas canvas) { }
    }
}
