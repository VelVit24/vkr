using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KR2.Parameters
{
    public class SimulationResults
    {
        private int pointCount = 0;
        public int PointCount { get { return pointCount; } set { pointCount=value; PointCountList.Add(value); } }
        private double hullArea = 0;
        public double HullArea { get { return hullArea; } set { hullArea=value; HullAreaList.Add(value); } }
        private double trueHullArea = 0;
        public double TrueHullArea { get { return trueHullArea; } set { trueHullArea=value; TrueHullAreaList.Add(value); } }
        private double centerDelay = 0;
        public double CenterDelay { get { return centerDelay; } set { centerDelay=value; CenterDelayList.Add(value); } }
        private double intersectionArea = 0;
        public double IntersectionArea 
        { 
            get 
            {
                if (trueHullArea <= 0 || double.IsNaN(trueHullArea) || double.IsInfinity(trueHullArea))
                {
                    IntersectionAreaList.Add(0);
                    return 0;
                }
                double value = hullArea / trueHullArea * 100;
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    value = 0;
                }
                IntersectionAreaList.Add(value);
                return value;
            } 
            set 
            { 
                intersectionArea=value; 
            } 
        }
        public List<int> PointCountList;
        public List<double> HullAreaList;
        public List<double> TrueHullAreaList;
        public List<double> CenterDelayList;
        public List<double> IntersectionAreaList;

        public double SecondsSinceStart = 0;

        public SimulationResults()
        {
            PointCountList = new List<int>();
            HullAreaList = new List<double>();
            TrueHullAreaList = new List<double>();
            CenterDelayList = new List<double>();
            IntersectionAreaList = new List<double>();
        }

        public double AvgPointCount()
        {
            int k = 0;
            double sum = 0;
            for (int i = 0; i < PointCountList.Count; i++)
            {
                if (!double.IsNaN(PointCountList[i]))
                {
                    sum += PointCountList[i];
                    k++;
                }
            }
            return sum / k;
        }
        public double AvgHullArea()
        {
            int k = 0;
            double sum = 0;
            for (int i = 0; i < HullAreaList.Count; i++)
            {
                if (!double.IsNaN(HullAreaList[i]))
                {
                    sum += HullAreaList[i];
                    k++;
                }
            }
            return sum / k;
        }
        public double AvgTrueHullArea()
        {
            int k = 0;
            double sum = 0;
            for (int i = 0; i < TrueHullAreaList.Count; i++)
            {
                if (!double.IsNaN(TrueHullAreaList[i]))
                {
                    sum += TrueHullAreaList[i];
                    k++;
                }
            }
            return sum / k;
        }
        public double AvgCenterDelay()
        {
            int k = 0;
            double sum = 0;
            for (int i = 0; i < CenterDelayList.Count; i++)
            {
                if (!double.IsNaN(CenterDelayList[i]))
                {
                    sum += CenterDelayList[i];
                    k++;
                }
            }
            return sum / k;
        }
        public double AvgIntersectionArea()
        {
            int k = 0;
            double sum = 0;
            for (int i = 0; i < IntersectionAreaList.Count; i++)
            {
                if (!double.IsNaN(IntersectionAreaList[i]) && !double.IsInfinity(IntersectionAreaList[i]))
                {
                    sum += IntersectionAreaList[i];
                    k++;
                }
            }
            return sum / k;
        }
        public void Clear()
        {
            PointCount = 0;
            HullArea = 0;
            TrueHullArea = 0;
            CenterDelay = 0;
            IntersectionArea = 0;
            PointCountList.Clear();
            IntersectionAreaList.Clear();
            HullAreaList.Clear();
            TrueHullAreaList.Clear();
            CenterDelayList.Clear();
            SecondsSinceStart = 0;
        }
    }
}
