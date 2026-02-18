using System.Windows;
using KR2.Parameters;

namespace KR2
{
    public partial class ParralResults : Window
    {
        public ParralResults(List<SimulationResults> results, int allCount, bool showRes)
        {
            InitializeComponent();
            if (results == null || results.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            List<double> errors = new List<double>();
            foreach (SimulationResults res in results)
            {
                if (res.SecondsSinceStart > 0)
                {
                    errors.Add(res.SecondsSinceStart);
                }
            }

            ErrorRate.Text = $"Вероятность потери области: {((float)errors.Count / allCount * 100)}%";
            var errorItems = new List<ErrorItem>();
            for (int i = 0; i < errors.Count; i++)
            {
                errorItems.Add(new ErrorItem
                {
                    Number = i + 1,
                    Time = errors[i]
                });
            }
            ErrorDataGrid.ItemsSource = errorItems;

            if (showRes)
            {
                SimulationResults avgResults = new SimulationResults();
                foreach (SimulationResults result in results)
                {
                    avgResults.PointCount = (int)result.AvgPointCount();
                    avgResults.HullArea = result.AvgHullArea();
                    avgResults.TrueHullArea = result.AvgTrueHullArea();
                    if (!double.IsNaN(result.AvgCenterDelay()))
                    {
                        avgResults.CenterDelay = result.AvgCenterDelay();
                    }
                    double t = avgResults.IntersectionArea;
                }

                PointCountText.Text = avgResults?.AvgPointCount().ToString();
                HullAreaText.Text = Math.Round(avgResults.AvgHullArea(), 2).ToString();
                TrueHullAreaText.Text = Math.Round(avgResults.AvgTrueHullArea(), 2).ToString();
                CenterDelayText.Text = Math.Round(avgResults.AvgCenterDelay(), 2).ToString();
                IntersectionText.Text = Math.Round(avgResults.AvgIntersectionArea(), 2).ToString();
            }
        }
    }

    public class ErrorItem
    {
        public int Number { get; set; }
        public double Time { get; set; }
    }
}
