using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LiveCharts;
using KR2;
using KR2.Bounds;
using KR2.Points;
using KR2.Parameters;
using KR2.Sensor;

namespace FlowSensor
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime lastUpdateTime;
        private bool isPaused = false;
        private SimulationParameters parameters;
        private IBound currentBound;
        private IPoint currentPoints;
        private FileManager fileManager;
        private ISensor sensor;
        private SimulationResults simResults;
        private const int VariableModeRunsPerValue = 10000;

        public MainWindow()
        {
            InitializeComponent();
            parameters = new SimulationParameters();
            simResults = new SimulationResults();
            fileManager = new FileManager(parameters, simResults);
            fileManager.LoadFromStart();
            UpdateParametersInputs();
        }
        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16); // ~30 FPS
            lastUpdateTime = DateTime.Now;
            timer.Tick += OnTimerTick;
            timer.Start();
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (isPaused) return;
            if (parameters.SimulationTime <= 0)
            {
                timer.Stop();
                ShowSimulationEndDialog(); 
                if (SensorType.IsChecked==true) FinalResults();
                return;
            }
            DateTime currentTime = DateTime.Now;
            double deltaTime = (currentTime - lastUpdateTime).TotalSeconds;
            lastUpdateTime = currentTime;
            parameters.SimulationTime -= deltaTime;
            

            currentBound.Update(deltaTime);
            currentPoints.UpdatePoints(deltaTime);
            try
            {
                sensor.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
                timer?.Stop();
                SimulationCanvas.Children.Clear();
                currentBound = default;
                if (SensorType.IsChecked==true) FinalResults();
                return ;
            }

            if (currentBound.RemainingTime <= 0)
            {
                currentBound.StartNewRandomWalk();
            }

            currentBound.UpdateAreaPosition();

            UpdateSimResults();
        }

        private void ShowSimulationEndDialog()
        {
            MessageBox.Show("Симуляция завершена!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StartSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            timer?.Stop();
            SimulationCanvas.Children.Clear();
            currentBound = default;
            if (!UpdateParameters()) return;
            bool PointIsRandomWalk = PointTypeComboBox.SelectedIndex == 1;
            switch (BoundTypeComboBox.SelectedIndex)
            {
                case 0: // Circle
                    currentBound = new CircleBound(new Point(parameters.CenterX, parameters.CenterY), 
                        parameters.Radius, SimulationCanvas, parameters.Vmax, parameters.Tmax);
                    currentPoints = new CircleBoundPoints(currentBound, SimulationCanvas, PointIsRandomWalk, parameters);
                    break;
                case 1: // Rectangle
                    currentBound = new RectBound(new Point(parameters.CenterX, parameters.CenterY),
                        parameters.Width, parameters.Height, SimulationCanvas, parameters.Vmax, parameters.Tmax, parameters.Omax);
                    currentPoints = new RectangleBoundPoints(currentBound, SimulationCanvas, PointIsRandomWalk, parameters);
                    break;
                case 2: // Polygon
                    currentBound = new PolyBound(parameters.Points, SimulationCanvas, parameters.Vmax, parameters.Tmax);
                    currentPoints = new PolyBoundPoints(currentBound, SimulationCanvas, PointIsRandomWalk, parameters);
                    break;
            }
            if (SensorType.IsChecked == true)
            {
                switch (SensorBoundTypeComboBox.SelectedIndex)
                {
                    case 0:
                        sensor = new Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist);
                        break;
                    case 1:
                        sensor = new Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist, Sensor.HullType.Concave);
                        sensor.SetKoef(parameters.SensorKoef);
                        break;
                    case 2:
                        sensor = new Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist, Sensor.HullType.Clusters);
                        sensor.SetClusterParam(parameters.SensorEps, parameters.SensorMinPts);
                        break;
                }
            }
            else
            {
                switch (SensorBoundTypeComboBox.SelectedIndex)
                {
                    case 0:
                        sensor = new SensorSlim(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist);
                        break;
                    case 1:
                        sensor = new Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist, 0);
                        break;
                    case 2:
                        sensor = new Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist, 0);
                        break;
                }
            }

            simResults = sensor.GetSimulationResults();

            InitializeTimer();
            currentBound.StartNewRandomWalk();
        }

        private bool UpdateParameters()
        {
            if (!double.TryParse(SimulationTimeInput.Text, out parameters.SimulationTime))
            {
                MessageBox.Show("Введите корректное время моделирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!double.TryParse(circleCenterX.Text, out parameters.CenterX))
            {
                MessageBox.Show("Некорректная координата X", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!double.TryParse(circleCenterY.Text, out parameters.CenterY))
            {
                MessageBox.Show("Некорректная координата Y", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            switch (parameters.BoundType)
            {
                case "Круглая":
                    if (!double.TryParse(circleRadius.Text, out parameters.Radius) || parameters.Radius <= 0)
                    {
                        MessageBox.Show("Некорректный радиус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                case "Прямоугольная":
                    if (!double.TryParse(rectWidth.Text, out parameters.Width) || parameters.Width <= 0)
                    {
                        MessageBox.Show("Некорректное значение ширины", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    if (!double.TryParse(rectHeight.Text, out parameters.Height) || parameters.Height <= 0)
                    {
                        MessageBox.Show("Некорректное значение высоты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    if (!double.TryParse(OmaxInput.Text, out parameters.Omax) || parameters.Omax <= 0)
                    {
                        MessageBox.Show("Некорректное значение Omax", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                case "Многоугольная":
                    if (pointsContainer.Children.Count < 3)
                    {
                        MessageBox.Show("Многоугольник должен иметь хотя бы 3 точки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    var points = new List<Point>();

                    foreach (StackPanel pointPanel in pointsContainer.Children)
                    {
                        if (pointPanel.Children[1] is TextBox xBox && pointPanel.Children[3] is TextBox yBox)
                        {
                            if (double.TryParse(xBox.Text, out double x) && double.TryParse(yBox.Text, out double y))
                            {
                                points.Add(new Point(x, y));
                            }
                            else
                            {
                                MessageBox.Show("Некорректное значение координат точек", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return false;
                            }
                        }
                    }
                    parameters.Points = new List<Point>(points);
                    //MessageBox.Show($"Polygon with {points.Count} points: " + string.Join(", ", points));
                    break;
            }

            if (!double.TryParse(VmaxInput.Text, out parameters.Vmax) || parameters.Vmax <= 0)
            {
                MessageBox.Show("Некорректное значение Vmax", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!double.TryParse(TmaxInput.Text, out parameters.Tmax) || parameters.Tmax <= 0)
            {
                MessageBox.Show("Некорректное значение Tmax", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!int.TryParse(PointsCountInput.Text, out parameters.PointCount) || parameters.PointCount <= 0)
            {
                MessageBox.Show("Некорректное значение количества точек", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            switch (PointTypeComboBox.SelectedIndex)
            {
                case 0:
                    parameters.PointType = "Статическое";
                    break;
                case 1:
                    parameters.PointType = "Динамическое";
                    break;

            }
            if (!double.TryParse(vmaxInput.Text, out parameters.vmax) || parameters.vmax <= 0)
            {
                MessageBox.Show("Некорректное значение vmax", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!double.TryParse(tmaxInput.Text, out parameters.tmax) || parameters.tmax <= 0)
            {
                MessageBox.Show("Некорректное значение tmax", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            switch (PointVisibleType.SelectedIndex)
            {
                case 0:
                    parameters.PointVisibleType = "Равномерное";
                    break;
                case 1:
                    parameters.PointVisibleType = "Экспоненциальное";
                    break;

            }
            if (!double.TryParse(TmaxVisibleInput.Text, out parameters.TmaxVisible) || parameters.TmaxVisible <= 0)
            {
                MessageBox.Show("Некорректное значение TmaxVisible", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!double.TryParse(TmaxInvisibleInput.Text, out parameters.TmaxInvisible) || parameters.TmaxInvisible <= 0)
            {
                MessageBox.Show("Некорректное значение TmaxInvisible", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            switch (SensorBoundTypeComboBox.SelectedIndex)
            {
                case 0:
                    parameters.SensorBoundType = "Выпуклая";
                    break;
                case 1:
                    parameters.SensorBoundType = "Невыпуклая";
                    if (!int.TryParse(SensorKoef.Text, out parameters.SensorKoef) || parameters.SensorKoef <= 0)
                    {
                        MessageBox.Show("Некорректное значение коефициента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
                case 2:
                    parameters.SensorBoundType = "Неск. кластеров";
                    if (!double.TryParse(SensorClusterEps.Text, out parameters.SensorEps) || parameters.SensorEps <= 0)
                    {
                        MessageBox.Show("Некорректное значение максимального расстояния между соседями", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    if (!int.TryParse(SensorClusterMinPts.Text, out parameters.SensorMinPts) || parameters.SensorMinPts <= 0)
                    {
                        MessageBox.Show("Некорректное значение минимального количества соседей", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;

            }
            if (!double.TryParse(SensorMaxDetectDist.Text, out parameters.SensorMaxDetectDist) || parameters.SensorMaxDetectDist <= 0)
            {
                MessageBox.Show("Некорректное значение поля зрения сенсора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            parameters.SensorType = SensorType.IsChecked==true;
            if (!int.TryParse(ParalCount.Text, out parameters.ParallelCount) || parameters.ParallelCount <= 0)
            {
                parameters.ParallelCount = 50;
            }
            parameters.ParallelHidden = ParalVisible.IsChecked == true;
            return true;
        }

        private void PauseSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                if (isPaused)
                {
                    timer.Start();
                    PauseSimulationButton.Content = "Пауза";
                    lastUpdateTime = DateTime.Now;
                }
                else
                {
                    timer.Stop();
                    PauseSimulationButton.Content = "Продолжить";
                }
                isPaused = !isPaused;
            }
        }

        private void ResetSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            timer?.Stop();
            SimulationCanvas.Children.Clear();
            currentBound = default;
            if (SensorType.IsChecked==true) FinalResults();
        }

        private void SaveParamsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateParameters();
            fileManager.SaveParameters(parameters);
        }

        private void LoadParamsButton_Click(object sender, RoutedEventArgs e)
        {
            parameters = fileManager.LoadParameters();
            UpdateParametersInputs();
        }

        private void ResetParamsButton_Click(object sender, RoutedEventArgs e)
        {
            parameters.Reset();
            UpdateParametersInputs();
        }

        private void SaveResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (simResults != null)
                fileManager.SaveResults(simResults);
            else
                MessageBox.Show("Нет результатов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LoadResultsButton_Click(object sender, RoutedEventArgs e)
        {
            simResults = fileManager.LoadResults();
            UpdateLoadedResults();
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            simResults.Clear();
            UpdateLoadedResults();
        }

        private void ShapeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (circlePanel == null) return;
            circlePanel.Visibility = Visibility.Collapsed;
            rectanglePanel.Visibility = Visibility.Collapsed;
            polygonPanel.Visibility = Visibility.Collapsed;
            OmaxText.Visibility = Visibility.Collapsed;
            OmaxInput.Visibility = Visibility.Collapsed;
            parameters.BoundType = ((ComboBoxItem)BoundTypeComboBox.SelectedItem)?.Content.ToString() ?? "Круглая";
            switch (BoundTypeComboBox.SelectedIndex)
            {
                case 0: // Circle
                    circlePanel.Visibility = Visibility.Visible;
                    break;
                case 1: // Rectangle
                    rectanglePanel.Visibility = Visibility.Visible;
                    OmaxText.Visibility = Visibility.Visible;
                    OmaxInput.Visibility = Visibility.Visible;
                    break;
                case 2: // Polygon
                    polygonPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void AddPointButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем новую панель для ввода точки
            var pointPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

            // Добавляем поля для X и Y
            pointPanel.Children.Add(new TextBlock { Text = "X:", Margin = new Thickness(0, 0, 5, 0) });
            pointPanel.Children.Add(new TextBox { Width = 60, Margin = new Thickness(0, 0, 10, 0) });
            pointPanel.Children.Add(new TextBlock { Text = "Y:", Margin = new Thickness(0, 0, 5, 0) });
            pointPanel.Children.Add(new TextBox { Width = 60 });

            // Добавляем кнопку удаления
            var removeButton = new Button { Content = "Удалить", Margin = new Thickness(10, 0, 0, 0) };
            removeButton.Click += (s, args) => pointsContainer.Children.Remove(pointPanel);
            pointPanel.Children.Add(removeButton);

            pointsContainer.Children.Add(pointPanel);
        }

        private void UpdateParametersInputs()
        {
            SimulationTimeInput.Text = parameters.SimulationTime.ToString();
            switch (parameters.BoundType)
            {
                case "Круглая":
                    BoundTypeComboBox.SelectedIndex = 0; break;
                case "Прямоугольная":
                    BoundTypeComboBox.SelectedIndex = 1; break;
                case "Многоугольная":
                    BoundTypeComboBox.SelectedIndex = 2; break;
            }
            circleCenterX.Text = parameters.CenterX.ToString();
            circleCenterY.Text = parameters.CenterY.ToString();
            circleRadius.Text = parameters.Radius.ToString();
            rectCenterX.Text = parameters.CenterX.ToString();
            rectCenterY.Text = parameters.CenterY.ToString();
            rectWidth.Text = parameters.Width.ToString();
            rectHeight.Text = parameters.Height.ToString();
            VmaxInput.Text = parameters.Vmax.ToString();
            TmaxInput.Text = parameters.Tmax.ToString();
            OmaxInput.Text = parameters.Omax.ToString();

            pointsContainer.Children.Clear();
            foreach (Point point in parameters.Points)
            {
                var pointPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

                pointPanel.Children.Add(new TextBlock { Text = "X:", Margin = new Thickness(0, 0, 5, 0) });
                pointPanel.Children.Add(new TextBox { Text = point.X.ToString(), Width = 60, Margin = new Thickness(0, 0, 10, 0) });
                pointPanel.Children.Add(new TextBlock { Text = "Y:", Margin = new Thickness(0, 0, 5, 0) });
                pointPanel.Children.Add(new TextBox { Text = point.Y.ToString(), Width = 60 });

                var removeButton = new Button { Content = "Удалить", Margin = new Thickness(10, 0, 0, 0) };
                removeButton.Click += (s, args) => pointsContainer.Children.Remove(pointPanel);
                pointPanel.Children.Add(removeButton);

                pointsContainer.Children.Add(pointPanel);
            }

            PointsCountInput.Text = parameters.PointCount.ToString();
            switch (parameters.PointType)
            {
                case "Статическое":
                    PointTypeComboBox.SelectedIndex = 0; break;
                case "Динамическое":
                    PointTypeComboBox.SelectedIndex = 1; break;
            }
            vmaxInput.Text = parameters.vmax.ToString();
            tmaxInput.Text = parameters.tmax.ToString();
            switch (parameters.PointVisibleType)
            {
                case "Равномерное":
                    PointVisibleType.SelectedIndex = 0; break;
                case "Экспоненциальное":
                    PointVisibleType.SelectedIndex = 1; break;
            }
            TmaxVisibleInput.Text = parameters.TmaxVisible.ToString();
            TmaxInvisibleInput.Text = parameters.TmaxInvisible.ToString();
            switch (parameters.SensorBoundType)
            {
                case "Выпуклая":
                    SensorBoundTypeComboBox.SelectedIndex = 0; break;
                case "Невыпуклая":
                    SensorBoundTypeComboBox.SelectedIndex = 1; break;
                case "Неск. кластеров":
                    SensorBoundTypeComboBox.SelectedIndex = 2; break;
            }
            SensorKoef.Text = parameters.SensorKoef.ToString();
            SensorClusterEps.Text = parameters.SensorEps.ToString();
            SensorClusterMinPts.Text = parameters.SensorMinPts.ToString();
            SensorMaxDetectDist.Text = parameters.SensorMaxDetectDist.ToString();
            ParalCount.Text = parameters.ParallelCount.ToString();
            ParalVisible.IsChecked = parameters.ParallelHidden;
        }

        private void SensorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConcavePanel == null) return;
            ConcavePanel.Visibility = Visibility.Collapsed;
            ClusterPanel.Visibility = Visibility.Collapsed;
            parameters.SensorBoundType = ((ComboBoxItem)SensorBoundTypeComboBox.SelectedItem)?.Content.ToString() ?? "Выпуклая";
            switch (SensorBoundTypeComboBox.SelectedIndex)
            {
                case 1:
                    ConcavePanel.Visibility= Visibility.Visible;
                    break;
                case 2:
                    ClusterPanel.Visibility= Visibility.Visible;
                    break;
            }
        }
        private void UpdateSimResults()
        {
            PointCountText.Text = Math.Round(simResults.AvgPointCount(), 1).ToString();
            HullAreaText.Text = Math.Round(simResults.HullArea, 2).ToString();
            TrueHullAreaText.Text = Math.Round(simResults.TrueHullArea, 2).ToString();
            CenterDelayText.Text = Math.Round(simResults.CenterDelay, 2).ToString();
            IntersectionText.Text = Math.Round(simResults.IntersectionArea, 2).ToString();
        }
        private void UpdateLoadedResults()
        {
            PointCountText.Text = Math.Round(simResults.AvgPointCount(), 1).ToString();
            HullAreaText.Text = Math.Round(simResults.HullArea, 2).ToString();
            TrueHullAreaText.Text = Math.Round(simResults.TrueHullArea, 2).ToString();
            CenterDelayText.Text = Math.Round(simResults.CenterDelay, 2).ToString();
            IntersectionText.Text = Math.Round(simResults.IntersectionArea, 2).ToString();
            AreaChart.Series[0].Values = new ChartValues<double>(simResults.HullAreaList.Where((point, index) => index % 5 == 0).ToList());
            AreaChart.Series[1].Values = new ChartValues<double>(simResults.TrueHullAreaList.Where((point, index) => index % 5 == 0).ToList());
            CenterChart.Series[0].Values = new ChartValues<double>(simResults.CenterDelayList.Where((point, index) => index % 5 == 0).ToList());
        }
        private void FinalResults()
        {
            PointCountText.Text = Math.Round(simResults.AvgPointCount(), 1).ToString();
            HullAreaText.Text = Math.Round(simResults.AvgHullArea(), 2).ToString();
            TrueHullAreaText.Text = Math.Round(simResults.AvgTrueHullArea(), 2).ToString();
            CenterDelayText.Text = Math.Round(simResults.AvgCenterDelay(), 2).ToString();
            IntersectionText.Text = Math.Round(simResults.AvgIntersectionArea(), 2).ToString();
            AreaChart.Series[0].Values = new ChartValues<double>(simResults.HullAreaList.Where((point, index) => index % 5 == 0).ToList());
            AreaChart.Series[1].Values = new ChartValues<double>(simResults.TrueHullAreaList.Where((point, index) => index % 5 == 0).ToList());
            CenterChart.Series[0].Values = new ChartValues<double>(simResults.CenterDelayList.Where((point, index) => index % 5 == 0).ToList());
        }

        private int totalSimulations = 0;
        private List<SimulationResults> allResults;
        private bool isParallelRunning = false;

        private void StartParalSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (isParallelRunning)
            {
                return;
            }
            if (!UpdateParameters())
            {
                return;
            }
            parameters.AreaWidth = SimulationCanvas.ActualWidth > 0 ? SimulationCanvas.ActualWidth : 2500;
            parameters.AreaHeight = SimulationCanvas.ActualHeight > 0 ? SimulationCanvas.ActualHeight : 1700;
            totalSimulations = parameters.ParallelCount;
            if (totalSimulations <= 0)
            {
                MessageBox.Show("Количество запусков должно быть больше 0", "Ошибка");
                return;
            }
            RunMultipleSimulationsAsync(totalSimulations);
        }
        private void ShowFinalSummary()
        {
            ParralResults res = new ParralResults(allResults, totalSimulations, SensorType.IsChecked==true);
            res.Show();
        }

        private async void RunMultipleSimulationsAsync(int count)
        {
            isParallelRunning = true;
            StartParalSimulationButton.IsEnabled = false;
            StartVariableParalSimulationButton.IsEnabled = false;
            ParallelProgressBar.Minimum = 0;
            ParallelProgressBar.Maximum = count;
            ParallelProgressBar.Value = 0;
            ParallelProgressText.Text = $"Выполнено 0 / {count} (0%)";

            try
            {
                var results = new SimulationResults[count];
                int maxDegree = Math.Max(1, Environment.ProcessorCount - 1);
                int completed = 0;
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegree
                };

                await Parallel.ForEachAsync(Enumerable.Range(0, count), options, (index, _) =>
                {
                    var parametersCopy = parameters.Clone();
                    results[index] = BatchSimulationRunner.Run(parametersCopy);

                    int done = Interlocked.Increment(ref completed);
                    if (done == count || done % 10 == 0)
                    {
                        Dispatcher.Invoke(() => UpdateParallelProgress(done, count));
                    }
                    return ValueTask.CompletedTask;
                });

                allResults = results.ToList();
                UpdateParallelProgress(count, count);
                ShowFinalSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
            finally
            {
                isParallelRunning = false;
                StartParalSimulationButton.IsEnabled = true;
                StartVariableParalSimulationButton.IsEnabled = true;
            }
        }

        private void UpdateParallelProgress(int completed, int total)
        {
            ParallelProgressBar.Value = completed;
            double percent = total == 0 ? 0 : (double)completed / total * 100;
            ParallelProgressText.Text = $"Выполнено {completed} / {total} ({Math.Round(percent, 1)}%)";
        }

        private void StartVariableParalSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (isParallelRunning)
            {
                return;
            }
            if (!UpdateParameters())
            {
                return;
            }

            parameters.AreaWidth = SimulationCanvas.ActualWidth > 0 ? SimulationCanvas.ActualWidth : 2500;
            parameters.AreaHeight = SimulationCanvas.ActualHeight > 0 ? SimulationCanvas.ActualHeight : 1700;

            var configWindow = new VariableParallelConfigWindow(parameters) { Owner = this };
            bool? configResult = configWindow.ShowDialog();
            if (configResult != true)
            {
                return;
            }

            var values = BuildRangeValues(configWindow.RangeFrom, configWindow.RangeTo, configWindow.RangeStep);
            if (values.Count == 0)
            {
                MessageBox.Show("Диапазон не содержит значений.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (values.Count > 5000)
            {
                MessageBox.Show("Слишком много значений диапазона. Увеличьте шаг или уменьшите диапазон.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RunVariableSimulationsAsync(configWindow.SelectedVariable, values);
        }

        private async void RunVariableSimulationsAsync(string variableName, List<double> values)
        {
            isParallelRunning = true;
            StartParalSimulationButton.IsEnabled = false;
            StartVariableParalSimulationButton.IsEnabled = false;

            int totalRuns = values.Count * VariableModeRunsPerValue;
            ParallelProgressBar.Minimum = 0;
            ParallelProgressBar.Maximum = totalRuns;
            ParallelProgressBar.Value = 0;
            ParallelProgressText.Text = $"Выполнено 0 / {totalRuns} (0%)";

            var rows = new List<VariableParallelResultRow>();
            int globalCompleted = 0;
            int maxDegree = Math.Max(1, Environment.ProcessorCount - 1);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegree
            };

            try
            {
                for (int valueIndex = 0; valueIndex < values.Count; valueIndex++)
                {
                    double value = values[valueIndex];
                    var stats = new VariableRunStats();

                    await Parallel.ForEachAsync(Enumerable.Range(0, VariableModeRunsPerValue), options, (index, _) =>
                    {
                        var parametersCopy = parameters.Clone();
                        parametersCopy.SensorType = true;
                        ApplyVariableValue(parametersCopy, variableName, value);

                        var result = BatchSimulationRunner.Run(parametersCopy);
                        stats.Add(result);

                        int done = Interlocked.Increment(ref globalCompleted);
                        if (done == totalRuns || done % 20 == 0)
                        {
                            Dispatcher.Invoke(() => UpdateVariableParallelProgress(done, totalRuns, variableName, valueIndex + 1, values.Count, value));
                        }

                        return ValueTask.CompletedTask;
                    });

                    rows.Add(VariableParallelResultRow.FromStats(value, stats, VariableModeRunsPerValue));
                    UpdateVariableParallelProgress(globalCompleted, totalRuns, variableName, valueIndex + 1, values.Count, value);
                }

                var resultsWindow = new VariableParallelResultsWindow(variableName, VariableModeRunsPerValue, rows) { Owner = this };
                resultsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isParallelRunning = false;
                StartParalSimulationButton.IsEnabled = true;
                StartVariableParalSimulationButton.IsEnabled = true;
            }
        }

        private void UpdateVariableParallelProgress(int completed, int total, string variableName, int currentValueIndex, int totalValues, double currentValue)
        {
            ParallelProgressBar.Value = completed;
            double percent = total == 0 ? 0 : completed * 100.0 / total;
            ParallelProgressText.Text = $"[{variableName}={Math.Round(currentValue, 6)}] {currentValueIndex}/{totalValues}. Выполнено {completed}/{total} ({Math.Round(percent, 1)}%)";
        }

        private static List<double> BuildRangeValues(double from, double to, double step)
        {
            var values = new List<double>();
            const double epsilon = 1e-9;
            for (double value = from; value <= to + epsilon; value += step)
            {
                values.Add(Math.Round(value, 12));
            }
            return values;
        }

        private static void ApplyVariableValue(SimulationParameters target, string variableName, double value)
        {
            switch (variableName)
            {
                case "radius":
                    target.Radius = value;
                    break;
                case "Omax":
                    target.Omax = value;
                    break;
                case "Vmax":
                    target.Vmax = value;
                    break;
                case "Tmax":
                    target.Tmax = value;
                    break;
                case "vmax":
                    target.vmax = value;
                    break;
                case "tmax":
                    target.tmax = value;
                    break;
                case "TmaxVisible":
                    target.TmaxVisible = value;
                    break;
                case "TmaxInvisible":
                    target.TmaxInvisible = value;
                    break;
                default:
                    throw new InvalidOperationException($"Неизвестная переменная: {variableName}");
            }
        }
    }
}
