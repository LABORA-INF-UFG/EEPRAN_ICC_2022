using System.Collections.Generic;

namespace modelo_william {
	public class Link {
		public Link(uint capacity, float delay, uint fromNode, uint toNode) {
			Capacity = capacity;
			Delay    = delay;
			FromNode = fromNode;
			ToNode   = toNode;
		}
		public uint  Capacity   { get; }
		public float Delay      { get; }
		public uint  FromNode   { get; }
		public uint  ToNode     { get; }
	}
}