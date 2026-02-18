using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace KR2
{
    public partial class VariableParallelResultsWindow : Window
    {
        public VariableParallelResultsWindow(string variableName, int runsPerValue, List<VariableParallelResultRow> rows)
        {
            InitializeComponent();
            SummaryTextBlock.Text = $"Переменная: {variableName}. Запусков на значение: {runsPerValue}.";
            ResultsGrid.ItemsSource = rows;
        }
    }

    public class VariableParallelResultRow
    {
        public string VariableValue { get; set; } = string.Empty;
        public string LossProbabilityPercent { get; set; } = string.Empty;
        public int LossCount { get; set; }
        public string AvgPointCount { get; set; } = string.Empty;
        public string AvgHullArea { get; set; } = string.Empty;
        public string AvgTrueHullArea { get; set; } = string.Empty;
        public string AvgCenterDelay { get; set; } = string.Empty;
        public string AvgIntersectionArea { get; set; } = string.Empty;
        public string AvgLossTime { get; set; } = string.Empty;

        public static VariableParallelResultRow FromStats(double value, VariableRunStats stats, int runsPerValue)
        {
            var culture = CultureInfo.CurrentCulture;
            double probability = runsPerValue == 0 ? 0 : stats.LossCount * 100.0 / runsPerValue;
            return new VariableParallelResultRow
            {
                VariableValue = value.ToString("0.######", culture),
                LossProbabilityPercent = probability.ToString("0.###", culture),
                LossCount = stats.LossCount,
                AvgPointCount = SafeAverage(stats.SumPointCount, stats.CompletedRuns).ToString("0.###", culture),
                AvgHullArea = SafeAverage(stats.SumHullArea, stats.CompletedRuns).ToString("0.###", culture),
                AvgTrueHullArea = SafeAverage(stats.SumTrueHullArea, stats.CompletedRuns).ToString("0.###", culture),
                AvgCenterDelay = SafeAverage(stats.SumCenterDelay, stats.CompletedRuns).ToString("0.###", culture),
                AvgIntersectionArea = stats.AverageIntersectionPercent().ToString("0.###", culture),
                AvgLossTime = SafeAverage(stats.SumLossTime, stats.LossCount).ToString("0.###", culture)
            };
        }

        private static double SafeAverage(double sum, int count)
        {
            return count == 0 ? 0 : sum / count;
        }
    }
}
