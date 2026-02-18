using KR2.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KR2.Points
{
    public class SimPoint
    {
        public enum State { Passive, Active }
        public enum Distribution { Uniform, Exponential }

        public double X, Y;
        public State CurrentState;
        public Distribution PassiveDistribution;

        private double _stateTimeRemaining;
        private readonly Random _random = new Random();

        // Параметры распределений
        public double PassiveUniformMin = 0;
        public double PassiveUniformMax = 5.0;
        public double PassiveExponentialLambda = 0.5;
        public double ActiveUniformMin = 0;
        public double ActiveUniformMax = 2.0;
        Canvas? area;
        Ellipse? Ellipse;
        public SimPoint(double x, double y, Canvas? area, SimulationParameters parameters, Distribution passiveDistribution = Distribution.Uniform)
        {
            X=x;
            Y=y;
            CurrentState = State.Passive;
            PassiveDistribution = passiveDistribution;
            this.area = area;
            PassiveUniformMax = parameters.TmaxInvisible;
            ActiveUniformMax = parameters.TmaxVisible;
            if (area != null)
            {
                Ellipse = new Ellipse
                {
                    Width = 10,
                    Height = 10
                };
                area.Children.Add(Ellipse);
            }
            ResetStateTimer();
        }
        public void Update(double deltaTime)
        {
            _stateTimeRemaining -= deltaTime;

            if (_stateTimeRemaining <= 0)
            {
                SwitchState();
                ResetStateTimer();
            }
            Draw();
        }

        private void SwitchState()
        {
            CurrentState = CurrentState == State.Passive ? State.Active : State.Passive;
        }

        private void ResetStateTimer()
        {
            _stateTimeRemaining = CurrentState switch
            {
                State.Passive => GetPassiveTime(),
                State.Active => GetActiveTime(),
                _ => throw new InvalidOperationException("Unknown state")
            };
        }

        private double GetPassiveTime()
        {
            return PassiveDistribution switch
            {
                Distribution.Uniform =>
                    PassiveUniformMin + _random.NextDouble() * (PassiveUniformMax - PassiveUniformMin),
                Distribution.Exponential =>
                    -Math.Log(1 - _random.NextDouble()) / PassiveExponentialLambda,
                _ => throw new InvalidOperationException("Unknown distribution")
            };
        }

        private double GetActiveTime()
        {
            return ActiveUniformMin + _random.NextDouble() * (ActiveUniformMax - ActiveUniformMin);
        }
        public void Add(Vector vector)
        {
            X += vector.X;
            Y += vector.Y;
        }

        private void Draw()
        {
            if (Ellipse == null)
            {
                return;
            }
            Canvas.SetLeft(Ellipse, X-5);
            Canvas.SetTop(Ellipse, Y-5);
            if (CurrentState == State.Active)
                Ellipse.Fill = Brushes.Green;
            else Ellipse.Fill= Brushes.Red;
        }
    }
}
