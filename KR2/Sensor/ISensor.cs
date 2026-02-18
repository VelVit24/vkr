using KR2.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KR2.Sensor
{
    internal interface ISensor
    {
        public void Update() { }
        public void SetKoef(int k) { }
        public void SetClusterParam(double eps, int minPts) { }
        public SimulationResults GetSimulationResults() { return new SimulationResults(); }
    }
}
