using KR2.Parameters;

namespace KR2
{
    public class VariableRunStats
    {
        private readonly object sync = new();

        public int CompletedRuns { get; private set; }
        public int LossCount { get; private set; }
        public double SumPointCount { get; private set; }
        public double SumHullArea { get; private set; }
        public double SumTrueHullArea { get; private set; }
        public double SumCenterDelay { get; private set; }
        public double SumLossTime { get; private set; }

        public void Add(SimulationResults result)
        {
            double pointCount = Safe(result.AvgPointCount());
            double hullArea = Safe(result.AvgHullArea());
            double trueHullArea = Safe(result.AvgTrueHullArea());
            double centerDelay = Safe(result.AvgCenterDelay());
            bool hasLoss = result.SecondsSinceStart > 0;
            double lossTime = hasLoss ? result.SecondsSinceStart : 0;

            lock (sync)
            {
                CompletedRuns++;
                SumPointCount += pointCount;
                SumHullArea += hullArea;
                SumTrueHullArea += trueHullArea;
                SumCenterDelay += centerDelay;
                if (hasLoss)
                {
                    LossCount++;
                    SumLossTime += lossTime;
                }
            }
        }

        public double AverageIntersectionPercent()
        {
            if (SumTrueHullArea <= 0)
            {
                return 0;
            }
            double value = SumHullArea / SumTrueHullArea * 100.0;
            return Safe(value);
        }

        private static double Safe(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }
    }
}
