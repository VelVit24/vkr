using KR2.Bounds;
using KR2.Parameters;
using KR2.Points;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using KR2.Sensor;

namespace KR2
{
    /// <summary>
    /// Логика взаимодействия для SimulationWindow.xaml
    /// </summary>
    public partial class SimulationWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime lastUpdateTime;
        private bool isPaused = false;
        private SimulationParameters parameters;
        private IBound currentBound;
        private IPoint currentPoints;
        private ISensor sensor;
        public SimulationResults simResults;

        public Action<SimulationResults> OnSimulationFinished; // 🔄 Callback в MainWindow
        private Stopwatch simulationTimer = new Stopwatch();

        public SimulationWindow(SimulationParameters inputParameters)
        {
            InitializeComponent();
            parameters = inputParameters.Clone(); // Копируем параметры
            simResults = new SimulationResults();
            Loaded += (s, e) => InitializeSimulation();
        }

        private void InitializeSimulation()
        {
            // То же, что и в MainWindow -> StartSimulationButton_Click, только без UI
            // Инициализируй currentBound, currentPoints, sensor, и т.п.
            timer?.Stop();
            SimulationCanvas.Children.Clear();
            currentBound = default;
            bool PointIsRandomWalk = parameters.PointType == "Динамическое";
            Debug.WriteLine(SimulationCanvas.ActualWidth + " " + SimulationCanvas.ActualHeight);
            switch (parameters.BoundType)
            {
                case "Круглая": // Circle
                    currentBound = new CircleBound(new Point(parameters.CenterX, parameters.CenterY),
                        parameters.Radius, SimulationCanvas, parameters.Vmax, parameters.Tmax);
                    currentPoints = new CircleBoundPoints(currentBound, SimulationCanvas, PointIsRandomWalk, parameters);
                    break;
                case "Прямоугольная": // Rectangle
                    currentBound = new RectBound(new Point(parameters.CenterX, parameters.CenterY),
                        parameters.Width, parameters.Height, SimulationCanvas, parameters.Vmax, parameters.Tmax, parameters.Omax);
                    currentPoints = new RectangleBoundPoints(currentBound, SimulationCanvas, PointIsRandomWalk, parameters);
                    break;
                case "Многоугольная": // Polygon
                    currentBound = new PolyBound(parameters.Points, SimulationCanvas, parameters.Vmax, parameters.Tmax);
                    currentPoints = new PolyBoundPoints(currentBound, SimulationCanvas, PointIsRandomWalk, parameters);
                    break;
            }
            if (parameters.SensorType)
            {
                switch (parameters.SensorBoundType)
                {
                    case "Выпуклая":
                        sensor = new Sensor.Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist);
                        break;
                    case "Невыпуклая":
                        sensor = new Sensor.Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist, Sensor.Sensor.HullType.Concave);
                        sensor.SetKoef(parameters.SensorKoef);
                        break;
                    case "Неск. кластеров":
                        sensor = new Sensor.Sensor(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist, Sensor.Sensor.HullType.Clusters);
                        sensor.SetClusterParam(parameters.SensorEps, parameters.SensorMinPts);
                        break;
                }
            }
            else
            {
                switch (parameters.SensorBoundType)
                {
                    case "Выпуклая":
                        sensor = new Sensor.SensorSlim(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist);
                        break;
                    case "Невыпуклая":
                        sensor = new Sensor.SensorSlim(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist,0);
                        break;
                    case "Неск. кластеров":
                        sensor = new Sensor.SensorSlim(simResults, currentPoints, SimulationCanvas, currentBound.GetCenter(), currentBound.GetRadius(), parameters.SensorMaxDetectDist,0);
                        break;
                }
            }

            simResults = sensor.GetSimulationResults();

            simulationTimer.Restart();
            InitializeTimer();
            currentBound.StartNewRandomWalk();
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16);
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
                OnSimulationFinished?.Invoke(simResults); // Возврат результатов
                Close(); // Закрываем окно
                return;
            }

            double deltaTime = (DateTime.Now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = DateTime.Now;
            parameters.SimulationTime -= deltaTime;

            currentBound.Update(deltaTime);
            currentPoints.UpdatePoints(deltaTime);
            try
            {
                sensor.Update();
            }
            catch 
            {
                simResults.SecondsSinceStart = simulationTimer.Elapsed.TotalSeconds;
                timer.Stop();
                OnSimulationFinished?.Invoke(simResults); // Возврат результатов
                Close(); // Закрываем окно
                return;
            }

            if (currentBound.RemainingTime <= 0)
                currentBound.StartNewRandomWalk();

            currentBound.UpdateAreaPosition();
            double t = simResults.IntersectionArea;
        }
    }
}
