using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace KR2.Parameters
{
    public class FileManager
    {
        SimulationParameters parameters;
        SimulationResults results;
        public FileManager(SimulationParameters parameters, SimulationResults results)
        {
            this.parameters = parameters;
            this.results = results;
        }
        public void SaveParameters(SimulationParameters parameters)
        {
            this.parameters = parameters;
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "XML файл (*.xml)|*.xml|Текстовый файл (*.txt)|*.txt",
                Title = "Сохранить параметры моделирования"
            };
            if (saveDialog.ShowDialog() == true)
            {
                string filePath = saveDialog.FileName;
                if (filePath.EndsWith(".txt"))
                {
                    SaveToTxt(filePath);
                }
                else if (filePath.EndsWith(".xml"))
                {
                    SaveToXml(filePath);
                }
            }
        }
        public SimulationParameters LoadParameters()
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "XML файл (*.xml)|*.xml|Текстовый файл (*.txt)|*.txt",
                Title = "Загрузить параметры моделирования"
            };

            if (openDialog.ShowDialog() == true)
            {
                string filePath = openDialog.FileName;
                if (filePath.EndsWith(".txt"))
                {
                    LoadFromTxt(filePath);
                }
                else if (filePath.EndsWith(".xml"))
                {
                    LoadFromXml(filePath);
                }
            }
            return parameters;
        }
        public void SaveResults(SimulationResults results)
        {
            this.results = results;
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "XML файл (*.xml)|*.xml|Текстовый файл (*.txt)|*.txt",
                Title = "Сохранить результаты."
            };
            if (saveDialog.ShowDialog() == true)
            {
                string filePath = saveDialog.FileName;
                if (filePath.EndsWith(".txt"))
                {
                    SaveToTxtResults(filePath);
                }
                else if (filePath.EndsWith(".xml"))
                {
                    SaveToXmlResults(filePath);
                }
            }
        }
        public SimulationResults LoadResults()
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "XML файл (*.xml)|*.xml|Текстовый файл (*.txt)|*.txt",
                Title = "Загрузить результаты."
            };

            if (openDialog.ShowDialog() == true)
            {
                string filePath = openDialog.FileName;
                if (filePath.EndsWith(".txt"))
                {
                    LoadFromTxtResults(filePath);
                }
                else if (filePath.EndsWith(".xml"))
                {
                    LoadFromXmlResults(filePath);
                }
            }
            return results;
        }
        private void SaveToTxt(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"Время моделирования: {parameters.SimulationTime}");
                    writer.WriteLine($"Тип области: {parameters.BoundType}");
                    writer.WriteLine($"Центр X: {parameters.CenterX}");
                    writer.WriteLine($"Центр Y: {parameters.CenterY}");
                    writer.WriteLine($"Радиус: {parameters.Radius}");
                    writer.WriteLine($"Ширина прямоугольника: {parameters.Width}");
                    writer.WriteLine($"Высота прямоугольника: {parameters.Height}");
                    writer.WriteLine($"Vmax: {parameters.Vmax}");
                    writer.WriteLine($"Tmax: {parameters.Tmax}");
                    writer.WriteLine($"Omax: {parameters.Omax}");
                    writer.WriteLine($"Количество точек: {parameters.PointCount}");
                    writer.WriteLine($"Тип движения: {parameters.PointType}");
                    writer.WriteLine($"vmax: {parameters.vmax}");
                    writer.WriteLine($"tmax: {parameters.tmax}");
                    writer.WriteLine($"Тип распеделения видимости: {parameters.PointVisibleType}");
                    writer.WriteLine($"Tmax видимости: {parameters.TmaxVisible}");
                    writer.WriteLine($"Tmax невидимости: {parameters.TmaxInvisible}");
                    writer.WriteLine($"Тип области сенсора: {parameters.SensorBoundType}");
                    writer.WriteLine($"Коефициент: {parameters.SensorKoef}");
                    writer.WriteLine($"Eps: {parameters.SensorEps}");
                    writer.WriteLine($"MinPts: {parameters.SensorMinPts}");
                    writer.WriteLine($"Поле зрения сенсора: {parameters.SensorMaxDetectDist}");
                    writer.WriteLine($"Режим сенсора: {parameters.SensorType}");
                    writer.WriteLine($"Ширина области: {parameters.AreaWidth}");
                    writer.WriteLine($"Высота области: {parameters.AreaHeight}");
                    writer.WriteLine($"Количество параллельных запусков: {parameters.ParallelCount}");
                    writer.WriteLine($"Скрывать окна параллельного запуска: {parameters.ParallelHidden}");
                    writer.WriteLine("Точки для многоугольника");
                    foreach (Point point in parameters.Points)
                    {
                        writer.WriteLine($"{point.X} {point.Y}");
                    }
                }
                MessageBox.Show("Параметры успешно сохранены в TXT!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения TXT: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveToXml(string filePath)
        {
            try
            {
                XElement root = new XElement("SimulationParameters",
                    new XElement("SimulationTime", parameters.SimulationTime),
                    new XElement("BoundType", parameters.BoundType),
                    new XElement("CenterX", parameters.CenterX),
                    new XElement("CenterY", parameters.CenterY),
                    new XElement("Radius", parameters.Radius),
                    new XElement("Width", parameters.Width),
                    new XElement("Height", parameters.Height),
                    new XElement("Vmax", parameters.Vmax),
                    new XElement("Tmax", parameters.Tmax),
                    new XElement("Omax", parameters.Omax),
                    new XElement("Points",
                        from point in parameters.Points
                        select new XElement("Point",
                            new XElement("X", point.X),
                            new XElement("Y", point.Y))),
                    new XElement("PointCount", parameters.PointCount),
                    new XElement("PointType", parameters.PointType),
                    new XElement("vmax", parameters.vmax),
                    new XElement("tmax", parameters.tmax),
                    new XElement("PointVisibleType", parameters.PointVisibleType),
                    new XElement("TmaxVisible", parameters.TmaxVisible),
                    new XElement("TmaxInvisible", parameters.TmaxInvisible),
                    new XElement("SensorBoundType", parameters.SensorBoundType),
                    new XElement("SensorKoef", parameters.SensorKoef),
                    new XElement("SensorEps", parameters.SensorEps),
                    new XElement("SensorMinPts", parameters.SensorMinPts),
                    new XElement("SensorMaxDetectDist", parameters.SensorMaxDetectDist),
                    new XElement("SensorType", parameters.SensorType),
                    new XElement("AreaWidth", parameters.AreaWidth),
                    new XElement("AreaHeight", parameters.AreaHeight),
                    new XElement("ParallelCount", parameters.ParallelCount),
                    new XElement("ParallelHidden", parameters.ParallelHidden)
                );

                root.Save(filePath);
                MessageBox.Show("Результаты успешно сохранены в XML!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadFromTxt(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                {
                    MessageBox.Show("Файл параметров поврежден или содержит недостаточно данных!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int pointsHeaderIndex = Array.FindIndex(lines, l => l.Trim().StartsWith("Точки для многоугольника"));
                if (pointsHeaderIndex < 0)
                {
                    pointsHeaderIndex = lines.Length;
                }

                var values = new Dictionary<string, string>();
                for (int i = 0; i < pointsHeaderIndex; i++)
                {
                    string line = lines[i];
                    int separatorIndex = line.IndexOf(':');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }
                    values[line.Substring(0, separatorIndex).Trim()] = line.Substring(separatorIndex + 1).Trim();
                }

                parameters.SimulationTime = ParseDouble(values, "Время моделирования", parameters.SimulationTime);
                parameters.BoundType = ParseString(values, "Тип области", parameters.BoundType);
                parameters.CenterX = ParseDouble(values, "Центр X", parameters.CenterX);
                parameters.CenterY = ParseDouble(values, "Центр Y", parameters.CenterY);
                parameters.Radius = ParseDouble(values, "Радиус", parameters.Radius);
                parameters.Width = ParseDouble(values, "Ширина прямоугольника", parameters.Width);
                parameters.Height = ParseDouble(values, "Высота прямоугольника", parameters.Height);
                parameters.Vmax = ParseDouble(values, "Vmax", parameters.Vmax);
                parameters.Tmax = ParseDouble(values, "Tmax", parameters.Tmax);
                parameters.Omax = ParseDouble(values, "Omax", parameters.Omax);
                parameters.PointCount = ParseInt(values, "Количество точек", parameters.PointCount);
                parameters.PointType = ParseString(values, "Тип движения", parameters.PointType);
                parameters.vmax = ParseDouble(values, "vmax", parameters.vmax);
                parameters.tmax = ParseDouble(values, "tmax", parameters.tmax);
                parameters.PointVisibleType = ParseString(values, "Тип распеделения видимости", parameters.PointVisibleType);
                parameters.TmaxVisible = ParseDouble(values, "Tmax видимости", parameters.TmaxVisible);
                parameters.TmaxInvisible = ParseDouble(values, "Tmax невидимости", parameters.TmaxInvisible);
                parameters.SensorBoundType = ParseString(values, "Тип области сенсора", parameters.SensorBoundType);
                parameters.SensorKoef = ParseInt(values, "Коефициент", parameters.SensorKoef);
                parameters.SensorEps = ParseDouble(values, "Eps", parameters.SensorEps);
                parameters.SensorMinPts = ParseInt(values, "MinPts", parameters.SensorMinPts);
                parameters.SensorMaxDetectDist = ParseDouble(values, "Поле зрения сенсора", parameters.SensorMaxDetectDist);
                parameters.SensorType = ParseBool(values, "Режим сенсора", parameters.SensorType);
                parameters.AreaWidth = ParseDouble(values, "Ширина области", parameters.AreaWidth);
                parameters.AreaHeight = ParseDouble(values, "Высота области", parameters.AreaHeight);
                parameters.ParallelCount = ParseInt(values, "Количество параллельных запусков", parameters.ParallelCount);
                parameters.ParallelHidden = ParseBool(values, "Скрывать окна параллельного запуска", parameters.ParallelHidden);

                parameters.Points = new List<Point>();
                for (int i = pointsHeaderIndex + 1; i < lines.Length; i++)
                {
                    string[] temp = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length < 2)
                    {
                        continue;
                    }
                    if (TryParseDouble(temp[0], out double x) && TryParseDouble(temp[1], out double y))
                    {
                        parameters.Points.Add(new Point(x, y));
                    }
                }
                MessageBox.Show("Параметры загружены из TXT!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки TXT: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadFromXml(string filePath)
        {
            try
            {
                XElement root = XElement.Load(filePath);
                LoadParametersFromXmlRoot(root);
                MessageBox.Show("Параметры загружены из XML!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadFromStart()
        {
            if (File.Exists("parameters.xml"))
            {
                try
                {
                    XElement root = XElement.Load("parameters.xml");
                    LoadParametersFromXmlRoot(root);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadParametersFromXmlRoot(XElement root)
        {
            parameters.SimulationTime = ParseDouble(root.Element("SimulationTime")?.Value, parameters.SimulationTime);
            parameters.BoundType = root.Element("BoundType")?.Value ?? "Круглая";
            parameters.CenterX = ParseDouble(root.Element("CenterX")?.Value, parameters.CenterX);
            parameters.CenterY = ParseDouble(root.Element("CenterY")?.Value, parameters.CenterY);
            parameters.Radius = ParseDouble(root.Element("Radius")?.Value, parameters.Radius);
            parameters.Width = ParseDouble(root.Element("Width")?.Value, parameters.Width);
            parameters.Height = ParseDouble(root.Element("Height")?.Value, parameters.Height);
            parameters.Vmax = ParseDouble(root.Element("Vmax")?.Value, parameters.Vmax);
            parameters.Tmax = ParseDouble(root.Element("Tmax")?.Value, parameters.Tmax);
            parameters.Omax = ParseDouble(root.Element("Omax")?.Value, parameters.Omax);

            var pointsElement = root.Element("Points");
            if (pointsElement != null)
            {
                parameters.Points = pointsElement.Elements("Point")
                    .Select(p => new Point(
                        ParseDouble(p.Element("X")?.Value, 0),
                        ParseDouble(p.Element("Y")?.Value, 0)))
                    .ToList();
            }
            else
            {
                parameters.Points = new List<Point>();
            }

            parameters.PointCount = ParseInt(root.Element("PointCount")?.Value, parameters.PointCount);
            parameters.PointType = root.Element("PointType")?.Value ?? "Статическое";
            parameters.vmax = ParseDouble(root.Element("vmax")?.Value, parameters.vmax);
            parameters.tmax = ParseDouble(root.Element("tmax")?.Value, parameters.tmax);
            parameters.PointVisibleType = root.Element("PointVisibleType")?.Value ?? "Равномерное";
            parameters.TmaxVisible = ParseDouble(root.Element("TmaxVisible")?.Value, parameters.TmaxVisible);
            parameters.TmaxInvisible = ParseDouble(root.Element("TmaxInvisible")?.Value, parameters.TmaxInvisible);
            parameters.SensorBoundType = root.Element("SensorBoundType")?.Value ?? "Выпуклая";
            parameters.SensorKoef = ParseInt(root.Element("SensorKoef")?.Value, parameters.SensorKoef);
            parameters.SensorEps = ParseDouble(root.Element("SensorEps")?.Value, parameters.SensorEps);
            parameters.SensorMinPts = ParseInt(root.Element("SensorMinPts")?.Value, parameters.SensorMinPts);
            parameters.SensorMaxDetectDist = ParseDouble(root.Element("SensorMaxDetectDist")?.Value, parameters.SensorMaxDetectDist);
            parameters.SensorType = ParseBool(root.Element("SensorType")?.Value, parameters.SensorType);
            parameters.AreaWidth = ParseDouble(root.Element("AreaWidth")?.Value, parameters.AreaWidth);
            parameters.AreaHeight = ParseDouble(root.Element("AreaHeight")?.Value, parameters.AreaHeight);
            parameters.ParallelCount = ParseInt(root.Element("ParallelCount")?.Value, parameters.ParallelCount);
            parameters.ParallelHidden = ParseBool(root.Element("ParallelHidden")?.Value, parameters.ParallelHidden);
        }

        private static bool TryParseDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                || double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        private static double ParseDouble(string? value, double defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }
            return TryParseDouble(value, out double parsed) ? parsed : defaultValue;
        }

        private static int ParseInt(string? value, int defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }
            return int.TryParse(value, out int parsed) ? parsed : defaultValue;
        }

        private static bool ParseBool(string? value, bool defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }
            return bool.TryParse(value, out bool parsed) ? parsed : defaultValue;
        }

        private static string ParseString(Dictionary<string, string> values, string key, string defaultValue)
        {
            return values.TryGetValue(key, out string? value) ? value : defaultValue;
        }

        private static double ParseDouble(Dictionary<string, string> values, string key, double defaultValue)
        {
            return values.TryGetValue(key, out string? value) ? ParseDouble(value, defaultValue) : defaultValue;
        }

        private static int ParseInt(Dictionary<string, string> values, string key, int defaultValue)
        {
            return values.TryGetValue(key, out string? value) ? ParseInt(value, defaultValue) : defaultValue;
        }

        private static bool ParseBool(Dictionary<string, string> values, string key, bool defaultValue)
        {
            return values.TryGetValue(key, out string? value) ? ParseBool(value, defaultValue) : defaultValue;
        }

        private void SaveToTxtResults(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"Количество точек: {results.PointCount}");
                    writer.WriteLine($"Площадь области: {results.HullArea}");
                    writer.WriteLine($"Площадь истинной области: {results.TrueHullArea}");
                    writer.WriteLine($"Отклонение центра: {results.CenterDelay}");
                    writer.WriteLine($"Пересечение областей: {results.IntersectionArea}");
                    writer.WriteLine(string.Join(":", results.HullAreaList.Select(a => a.ToString(CultureInfo.InvariantCulture))));
                    writer.WriteLine(string.Join(":", results.TrueHullAreaList.Select(a => a.ToString(CultureInfo.InvariantCulture))));
                    writer.WriteLine(string.Join(":", results.CenterDelayList.Select(c => c.ToString(CultureInfo.InvariantCulture))));
                }
                MessageBox.Show("Результаты успешно сохранены в TXT!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения TXT: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveToXmlResults(string filePath)
        {
            try
            {
                XElement root = new XElement("SimulationResults",
                    new XElement("PointCount", results.PointCount),
                    new XElement("HullArea", results.HullArea),
                    new XElement("TrueHullArea", results.TrueHullArea),
                    new XElement("CenterDelay", results.CenterDelay),
                    new XElement("IntersectionArea", results.IntersectionArea),
                    new XElement("HullAreaList",
                        from area in results.HullAreaList
                        select new XElement("Area", area)),
                    new XElement("TrueHullAreaList",
                        from area in results.TrueHullAreaList
                        select new XElement("Area", area)),
                    new XElement("CenterDelayList",
                        from center in results.CenterDelayList
                        select new XElement("Center", center))
                );

                root.Save(filePath);
                MessageBox.Show("Параметры успешно сохранены в XML!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadFromTxtResults(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 8)
                {
                    MessageBox.Show("Файл параметров поврежден или содержит недостаточно данных!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var culture = CultureInfo.InvariantCulture;

                results.PointCount = int.Parse(lines[0].Split(':')[1].Trim());
                results.HullArea = double.Parse(lines[1].Split(':')[1].Trim(), culture);
                results.TrueHullArea = double.Parse(lines[2].Split(':')[1].Trim(), culture);
                results.CenterDelay = double.Parse(lines[3].Split(':')[1].Trim(), culture);
                results.IntersectionArea = double.Parse(lines[4].Split(':')[1].Trim(), culture);

                results.HullAreaList = lines[5]
                    .Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.Parse(s, culture))
                    .ToList();

                results.TrueHullAreaList = lines[6]
                    .Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.Parse(s, culture))
                    .ToList();

                results.CenterDelayList = lines[7]
                    .Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.Parse(s, culture))
                    .ToList();

                MessageBox.Show("Результаты загружены из TXT!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки TXT: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadFromXmlResults(string filePath)
        {
            try
            {
                XElement root = XElement.Load(filePath);

                results.PointCount = int.Parse(root.Element("PointCount")?.Value ?? "0");
                results.HullArea = double.Parse(root.Element("HullArea")?.Value ?? "0", CultureInfo.InvariantCulture);
                results.TrueHullArea = double.Parse(root.Element("TrueHullArea")?.Value ?? "0", CultureInfo.InvariantCulture);
                results.CenterDelay = double.Parse(root.Element("CenterDelay")?.Value ?? "0", CultureInfo.InvariantCulture);
                results.IntersectionArea = double.Parse(root.Element("IntersectionArea")?.Value ?? "0", CultureInfo.InvariantCulture);

                results.HullAreaList = root.Element("HullAreaList")?
                    .Elements("Area")
                    .Select(a => double.Parse(a.Value, CultureInfo.InvariantCulture))
                    .ToList() ?? new List<double>();

                results.TrueHullAreaList = root.Element("TrueHullAreaList")?
                    .Elements("Area")
                    .Select(a => double.Parse(a.Value, CultureInfo.InvariantCulture))
                    .ToList() ?? new List<double>();

                results.CenterDelayList = root.Element("CenterDelayList")?
                    .Elements("Center")
                    .Select(c => double.Parse(c.Value, CultureInfo.InvariantCulture))
                    .ToList() ?? new List<double>();
                MessageBox.Show("Результаты загружены из XML!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки XML: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
