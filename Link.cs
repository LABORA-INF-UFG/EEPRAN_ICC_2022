using System.Collections.Generic;

namespace modelo_william {
	public class Link {
		public List<LinkData> LinkList { get; set; }

		public Link() {
			LinkList = new List<LinkData>();
		}

		public void Add(LinkData link) {
			LinkList.Add(link);
		}

		public int Count() {
			return LinkList.Count;
		}
	}

	public class LinkData {
		public uint  linkNumber { get; set; }
		public uint  fromNode   { get; set; }
		public uint  toNode     { get; set; }
		public float delay      { get; set; }
		public uint  capacity   { get; set; }
	}
}