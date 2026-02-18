using System.Globalization;
using System.Windows;
using KR2.Parameters;

namespace KR2
{
    public partial class VariableParallelConfigWindow : Window
    {
        private static readonly string[] AvailableVariables =
        {
            "radius",
            "Omax",
            "Vmax",
            "Tmax",
            "vmax",
            "tmax",
            "TmaxVisible",
            "TmaxInvisible"
        };

        public string SelectedVariable { get; private set; } = "radius";
        public double RangeFrom { get; private set; }
        public double RangeTo { get; private set; }
        public double RangeStep { get; private set; }

        public VariableParallelConfigWindow(SimulationParameters parameters)
        {
            InitializeComponent();
            VariableComboBox.ItemsSource = AvailableVariables;
            ApplyDefaults(parameters);
        }

        private void ApplyDefaults(SimulationParameters parameters)
        {
            if (parameters == null)
            {
                FromTextBox.Text = "1";
                ToTextBox.Text = "5";
                StepTextBox.Text = "1";
                return;
            }

            FromTextBox.Text = parameters.Radius.ToString(CultureInfo.InvariantCulture);
            ToTextBox.Text = parameters.Radius.ToString(CultureInfo.InvariantCulture);
            StepTextBox.Text = "1";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (VariableComboBox.SelectedItem is not string variable)
            {
                MessageBox.Show("Выберите переменную.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!TryParseNumber(FromTextBox.Text, out var from) ||
                !TryParseNumber(ToTextBox.Text, out var to) ||
                !TryParseNumber(StepTextBox.Text, out var step))
            {
                MessageBox.Show("Введите корректные числовые значения диапазона.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (step <= 0)
            {
                MessageBox.Show("Шаг должен быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (from > to)
            {
                MessageBox.Show("Поле 'От' не может быть больше поля 'До'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedVariable = variable;
            RangeFrom = from;
            RangeTo = to;
            RangeStep = step;

            DialogResult = true;
            Close();
        }

        private static bool TryParseNumber(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
