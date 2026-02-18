using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KR2.Parameters
{
    public class SimulationParameters
    {
        public double SimulationTime;
        public double Vmax, Tmax, Omax; // для области
        public string BoundType;

        public double CenterX, CenterY, Radius, Width, Height;
        public List<Point> Points;

        public int PointCount;
        public string PointType;
        public double vmax, tmax; // для точек

        public string PointVisibleType;
        public double TmaxVisible;
        public double TmaxInvisible;

        public string SensorBoundType;
        public int SensorKoef;
        public double SensorEps;
        public int SensorMinPts;
        public double SensorMaxDetectDist;
        public bool SensorType;
        public double AreaWidth;
        public double AreaHeight;
        public int ParallelCount;
        public bool ParallelHidden;

        public SimulationParameters()
        {
            Reset();
        }

        public SimulationParameters Reset()
        {
            SimulationTime = 0;
            Vmax = 0;
            Tmax = 0;
            Omax = 0;
            BoundType = "Круглая";
            CenterX = 0;
            CenterY = 0;
            Radius = 0;
            Width = 0;
            Height = 0;
            Points = new List<Point>();
            PointCount = 0;
            PointType = "Статическое";
            vmax = 0;
            tmax = 0;
            PointVisibleType = "Равномерное";
            TmaxVisible = 0;
            TmaxInvisible = 0;
            SensorBoundType = "Выпуклая";
            SensorKoef = 1;
            SensorEps = 0;
            SensorMinPts = 0;
            SensorMaxDetectDist = 2;
            SensorType = true;
            AreaWidth = 2500;
            AreaHeight = 1700;
            ParallelCount = 50;
            ParallelHidden = false;
            return this;
        }
        public SimulationParameters Clone()
        {
            var clone = (SimulationParameters)MemberwiseClone();
            clone.Points = new List<Point>(Points);
            return clone;
        }
    }
}
