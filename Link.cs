using System.Collections.Generic;

namespace modelo_william {
	public class Link {
		public List<LinkData> links { get; set; }

		public Link() {
			links = new List<LinkData>();
		}

		public void Add(LinkData link) {
			links.Add(link);
		}

		public int Count() {
			return links.Count;
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