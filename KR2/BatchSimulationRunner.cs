using KR2.Bounds;
using KR2.Parameters;
using KR2.Points;
using KR2.Sensor;

namespace KR2
{
    internal static class BatchSimulationRunner
    {
        private const double StepSeconds = 0.016;

        public static SimulationResults Run(SimulationParameters input)
        {
            var parameters = input.Clone();
            var simResults = new SimulationResults();

            bool pointIsRandomWalk = parameters.PointType == "Динамическое";
            IBound currentBound = CreateBound(parameters);
            IPoint currentPoints = CreatePoints(parameters, currentBound, pointIsRandomWalk);
            ISensor sensor = CreateSensor(parameters, simResults, currentPoints, currentBound);

            simResults = sensor.GetSimulationResults();
            currentBound.StartNewRandomWalk();

            double elapsed = 0;
            while (parameters.SimulationTime > 0)
            {
                double deltaTime = parameters.SimulationTime > StepSeconds ? StepSeconds : parameters.SimulationTime;
                parameters.SimulationTime -= deltaTime;
                elapsed += deltaTime;

                currentBound.Update(deltaTime);
                currentPoints.UpdatePoints(deltaTime);

                try
                {
                    sensor.Update();
                    _ = simResults.IntersectionArea;
                }
                catch
                {
                    simResults.SecondsSinceStart = elapsed;
                    return simResults;
                }

                if (currentBound.RemainingTime <= 0)
                {
                    currentBound.StartNewRandomWalk();
                }

                currentBound.UpdateAreaPosition();
            }

            return simResults;
        }

        private static IBound CreateBound(SimulationParameters parameters)
        {
            return parameters.BoundType switch
            {
                "Круглая" => new CircleBound(
                    new System.Windows.Point(parameters.CenterX, parameters.CenterY),
                    parameters.Radius,
                    null,
                    parameters.Vmax,
                    parameters.Tmax,
                    parameters.AreaWidth,
                    parameters.AreaHeight),
                "Прямоугольная" => new RectBound(
                    new System.Windows.Point(parameters.CenterX, parameters.CenterY),
                    parameters.Width,
                    parameters.Height,
                    null,
                    parameters.Vmax,
                    parameters.Tmax,
                    parameters.Omax,
                    parameters.AreaWidth,
                    parameters.AreaHeight),
                "Многоугольная" => new PolyBound(
                    parameters.Points,
                    null,
                    parameters.Vmax,
                    parameters.Tmax,
                    parameters.AreaWidth,
                    parameters.AreaHeight),
                _ => throw new InvalidOperationException($"Неизвестный тип области: {parameters.BoundType}")
            };
        }

        private static IPoint CreatePoints(SimulationParameters parameters, IBound currentBound, bool pointIsRandomWalk)
        {
            return parameters.BoundType switch
            {
                "Круглая" => new CircleBoundPoints(currentBound, null, pointIsRandomWalk, parameters),
                "Прямоугольная" => new RectangleBoundPoints(currentBound, null, pointIsRandomWalk, parameters),
                "Многоугольная" => new PolyBoundPoints(currentBound, null, pointIsRandomWalk, parameters),
                _ => throw new InvalidOperationException($"Неизвестный тип области: {parameters.BoundType}")
            };
        }

        private static ISensor CreateSensor(SimulationParameters parameters, SimulationResults simResults, IPoint currentPoints, IBound currentBound)
        {
            if (parameters.SensorType)
            {
                return parameters.SensorBoundType switch
                {
                    "Выпуклая" => new Sensor.Sensor(
                        simResults,
                        currentPoints,
                        null,
                        currentBound.GetCenter(),
                        currentBound.GetRadius(),
                        parameters.SensorMaxDetectDist),
                    "Невыпуклая" => CreateConcaveSensor(parameters, simResults, currentPoints, currentBound),
                    "Неск. кластеров" => CreateClusterSensor(parameters, simResults, currentPoints, currentBound),
                    _ => throw new InvalidOperationException($"Неизвестный тип сенсора: {parameters.SensorBoundType}")
                };
            }

            return new SensorSlim(
                simResults,
                currentPoints,
                null,
                currentBound.GetCenter(),
                currentBound.GetRadius(),
                parameters.SensorMaxDetectDist);
        }

        private static ISensor CreateConcaveSensor(SimulationParameters parameters, SimulationResults simResults, IPoint currentPoints, IBound currentBound)
        {
            var sensor = new Sensor.Sensor(
                simResults,
                currentPoints,
                null,
                currentBound.GetCenter(),
                currentBound.GetRadius(),
                parameters.SensorMaxDetectDist,
                Sensor.Sensor.HullType.Concave);
            sensor.SetKoef(parameters.SensorKoef);
            return sensor;
        }

        private static ISensor CreateClusterSensor(SimulationParameters parameters, SimulationResults simResults, IPoint currentPoints, IBound currentBound)
        {
            var sensor = new Sensor.Sensor(
                simResults,
                currentPoints,
                null,
                currentBound.GetCenter(),
                currentBound.GetRadius(),
                parameters.SensorMaxDetectDist,
                Sensor.Sensor.HullType.Clusters);
            sensor.SetClusterParam(parameters.SensorEps, parameters.SensorMinPts);
            return sensor;
        }
    }
}
