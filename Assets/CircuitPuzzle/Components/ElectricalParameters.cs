using System;
using UnityEngine;

namespace CircuitPuzzle.Components
{
    /// <summary>Authored SI-unit values. From/to define the positive voltage and current direction.</summary>
    [Serializable]
    public sealed class ElectricalParameters
    {
        [SerializeField] private string fromTerminalId;
        [SerializeField] private string toTerminalId;
        [SerializeField] private double voltageVolts;
        [SerializeField] private double resistanceOhms;

        public string FromTerminalId => fromTerminalId;
        public string ToTerminalId => toTerminalId;
        public double VoltageVolts => voltageVolts;
        public double ResistanceOhms => resistanceOhms;

        public ElectricalParameters(string fromTerminalId, string toTerminalId,
            double voltageVolts = 0, double resistanceOhms = 0)
        {
            this.fromTerminalId = fromTerminalId;
            this.toTerminalId = toTerminalId;
            this.voltageVolts = voltageVolts;
            this.resistanceOhms = resistanceOhms;
        }

        internal ElectricalParameters Copy() => new ElectricalParameters(
            FromTerminalId, ToTerminalId, VoltageVolts, ResistanceOhms);
    }
}
