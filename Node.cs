using System;
using System.Collections.Generic;

namespace modelo_william {
	public class Node {
		public List<NodeData> nodes { get; set; }

		public Node() {
			nodes = new List<NodeData>();
		}
		
		public void Add(NodeData node) {
			nodes.Add(node);
		}

		public int Count() {
			return nodes.Count;
		}
	}
	
	public class NodeData {
		public uint   nodeNumber   { get; set; }
		public string nodeType     { get; set; }
		public ushort cpu          { get; set; }
		public ushort RAM          { get; set; }
		public ushort RU           { get; set; }
		public ushort StaticPower  { get; set; }
		public ushort DynamicPower { get; set; }
	}
}