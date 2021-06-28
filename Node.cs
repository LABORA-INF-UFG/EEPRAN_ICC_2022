using System;
using System.Collections.Generic;

namespace modelo_william {
	public class Node {
		public List<NodeData> NodeList { get; set; }

		public Node() {
			NodeList = new List<NodeData>();
		}
		
		public void Add(NodeData node) {
			NodeList.Add(node);
		}

		public int Count() {
			return NodeList.Count;
		}
	}
	
	public class NodeData {
		public uint   nodeNumber   { get; set; }
		public string nodeType     { get; set; }
		public double cpu          { get; set; }
		public ushort RAM          { get; set; }
		public ushort RU           { get; set; }
		public ushort StaticPower  { get; set; }
		public ushort DynamicPower { get; set; }
	}
}