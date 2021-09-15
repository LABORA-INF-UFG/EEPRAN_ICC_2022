using System;
using System.Collections.Generic;

namespace modelo_william {
	public class Node {
		public Node(uint number, double cpu, string model, ushort ru, uint tdp, float staticPercentage) {
			Number           = number;
			Cpu              = cpu;
			Model            = model;
			Tdp              = tdp;
			StaticPercentage = staticPercentage;
			Ru               = ru;
		}

		public uint   Number           { get; }
		public double Cpu              { get; }
		public string Model            { get; }
		public uint   Tdp              { get; }
		public float  StaticPercentage { get; }
		public ushort Ru               { get; }
	}
}