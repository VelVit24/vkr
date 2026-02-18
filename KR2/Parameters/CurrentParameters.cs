using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KR2.Parameters
{
    public class CurrentParameters
    {
        public Point center;
        public List<Point> points;
        public double width, height, radius, angularVelocity, angle;
        public Vector velocity;
        public CurrentParameters()
        {
            center = new Point();
            points = new List<Point>();
            width = 0; height = 0; radius = 0; angularVelocity = 0; angle = 0;
            velocity = new Vector();
        }
    }
}
