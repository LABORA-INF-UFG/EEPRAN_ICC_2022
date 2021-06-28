using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace modelo_william {

	internal static class Constants {
		public static int MaxPaths = 600;
	}
	
	public class Path {
		public Dictionary<string, PathData> PathList { get; set; }

		public Path() {
			PathList = new Dictionary<string, PathData>();
		}
		
		public void FindPaths(string linkPath, string nodePath) {

			if (!File.Exists(nodePath)) {
				throw new Exception("File '"+nodePath+"' not found.");
			}
			if (!File.Exists(linkPath)) {
				throw new Exception("File '"+linkPath+"' not found.");
			}
			
			StreamReader linkReader, nodeReader;
			
			linkReader = new StreamReader(linkPath);
			string jsonLinks = linkReader.ReadToEnd();
			Link   links     = JsonSerializer.Deserialize<Link>(jsonLinks);
			
			nodeReader = new StreamReader(nodePath);
			string jsonNodes = nodeReader.ReadToEnd();
			Node nodes = JsonSerializer.Deserialize<Node>(jsonNodes);

			if (nodes == null || links == null) {
				throw new Exception("Failed to parse json.");
			}

			Graph graph = new Graph(links.Count(), nodes.Count(), Constants.MaxPaths);
			foreach (LinkData link in links.LinkList) {
				uint source      = Math.Min(link.fromNode, link.toNode);
				uint destination = Math.Max(link.fromNode, link.toNode);
				graph.AddEdge(source, destination);
			}

			List<uint> destinations = new List<uint>();
			foreach (NodeData node in nodes.NodeList) {
				if (node.RU == 1) destinations.Add(node.nodeNumber);
			}

			List<uint[]> rawPaths        = new List<uint[]>();
			foreach (uint destination in destinations) {
				rawPaths.AddRange(graph.FindAllPaths(0, destination));
			}

			foreach (uint[] rawPath in rawPaths) {
				foreach (uint node in rawPath) {
					Console.Write($" -> {node}");
				}
				Console.WriteLine("");
			}

			/* start path data assign */
			uint count = 2, id = 1;
			
			foreach (uint[] path in rawPaths) {
				for (int i = 0; i < path.Length-1; ++i) {
					if (i != count) continue;
					PathData pathData = new PathData();
					pathData.seq.Add(path[1]);
					pathData.p1.Add($"({path[0]}, {path[1]})");
						
					pathData.seq.Add(path[i]);
					for (int j = 1; j < path.Length-1; ++j) {
						if (j != i) {
							pathData.p2.Add($"({path[j]}, {path[j+1]})");
						} else {
							break;
						}
					}

					pathData.seq.Add(path[^1]);
					for (int j = i; j < path.Length-1; ++j) {
						if (j != path.Length - 1) {
							pathData.p3.Add($"({path[j]}, {path[j+1]})");
						} else {
							break;
						}
					}

					pathData.id     = id;
					pathData.target = path[^1];
					pathData.source = "CN";
					++count;

					bool append = true;
					if (PathList.Count > 0) {
						foreach (var pd in PathList) {
							if (pd.Value == null) continue;
							if (pd.Value.p1.Equals(pathData.p1) && pd.Value.p2.Equals(pathData.p2) &&
							    pd.Value.p3.Equals(pathData.p3) && pd.Value.id != pathData.id) {
								append = false;
							}
						}
					}

					if (!append) continue;
					PathList.Add($"path-{id}", pathData);
					++id;
				}

				count = 2;
			}

			count = 1;
			foreach (uint[] path in rawPaths) {
				for (int i = 0; i < path.Length - 1; ++i) {
					if (i != count) continue;
					PathData pathData = new PathData();
					pathData.seq.Add(path[0]);

					pathData.seq.Add(path[i]);
					for (int j = 0; j < path.Length-1; ++j) {
						if (j != i) {
							pathData.p2.Add($"({path[j]}, {path[j+1]})");
						}
						if (j+1 == i) {
							break;
						}
					}

					pathData.seq.Add(path[^1]);
					for (int j = i; j < path.Length-1; ++j) {
						if (j != path.Length - 1) {
							pathData.p3.Add($"({path[j]}, {path[j+1]})");
						}
						if (j+1 == i) {
							break;
						}
					}

					pathData.id     = id;
					pathData.target = path[^1];
					pathData.source = "CN";
					++count;

					bool append = true;
					if (PathList.Count > 0) {
						foreach (var pd in PathList) {
							if (pd.Value == null) continue;
							if (pd.Value.p1.Equals(pathData.p1) && pd.Value.p2.Equals(pathData.p2) &&
							    pd.Value.p3.Equals(pathData.p3)) {
								append = false;
							}
						}
					}

					if (!append) continue;
					PathList.Add($"path-{id}", pathData);
					++id;
				}

				count = 1;
			}
			
			foreach (uint[] path in rawPaths) {
				PathData pathData = new PathData();
				pathData.seq.Add(0);

				pathData.seq.Add(0);

				pathData.seq.Add(path[^1]);
				for (int j = 0; j < path.Length-1; ++j) {
					if (j != path.Length - 1) {
						pathData.p3.Add($"({path[j]}, {path[j+1]})");
					}
					if (j+1 == path.Length - 1) {
						break;
					}
				}

				pathData.id     = id;
				pathData.target = path[^1];
				pathData.source = "CN";
				++count;

				bool append = true;
				if (PathList.Count > 0) {
					foreach (var pd in PathList) {
						if (pd.Value == null) continue;
						if (pd.Value.p1.Equals(pathData.p1) && pd.Value.p2.Equals(pathData.p2) &&
						    pd.Value.p3.Equals(pathData.p3) && pd.Value.id != pathData.id) {
							append = false;
						}
					}
				}

				if (!append) continue;
				PathList.Add($"path-{id}", pathData);
				++id;
			}

			Console.WriteLine($"{PathList.Count} paths configurations successfully found");
		}
	}

	public class PathData {
		public List<string> p2     { get; set; }
		public List<string> p3     { get; set; }
		public List<string> p1     { get; set; }
		public uint         target { get; set; }
		public List<uint>   seq    { get; set; }
		public string       source { get; set; }
		public uint         id     { get; set; }

		public PathData() {
			p2 = new List<string>();
			p3 = new List<string>();
			p1 = new List<string>();
			seq = new List<uint>();
		}
	}

	internal class Graph {
		private          int                     _edgeCount;
		private readonly bool[]                   _nodeHasBeenVisited;
		private readonly Dictionary<uint, uint[]> _edges;
		private          int                      _pathsCount;
		private readonly int                      _maxPaths;

		public Graph(int edgeCount, int nodeCount, int maxPaths) {
			_edgeCount          = edgeCount;
			_maxPaths           = maxPaths;
			_nodeHasBeenVisited = new bool[nodeCount];
			_edges              = new Dictionary<uint, uint[]>();
		}

		public void AddEdge(uint source, uint destination) {
			if (_edges.TryGetValue(source, out uint[] values)) {
				uint[] aux = new uint[values.Length+1];
				values.CopyTo(aux, 0);
				aux[values.Length] = destination;
				_edges[source]     = aux;
			} else {
				uint[] aux = new uint[1];
				aux[0]         = destination;
				_edges.Add(source, aux);
			}
		}

		private void FindAllPaths(uint src, uint dest, Stack<uint> path, List<uint[]> paths) {
			_nodeHasBeenVisited[src] = true;
			path.Push(src);

			if (src == dest) {
				if (!(path.Count > 4 && path.Contains(1) && path.Contains(2))) {
					if (++_pathsCount < _maxPaths + 1) {
						paths.Add(path.Reverse().ToArray());
					}
				}
			} else {
				if (_edges.ContainsKey(src)) {
					foreach (uint p in _edges[src]) {
						if (!_nodeHasBeenVisited[p]) {
							FindAllPaths(p, dest, path, paths);
						}
					}
				}
			}

			path.Pop();
			_nodeHasBeenVisited[src] = false;
		}

		public List<uint[]> FindAllPaths(uint src, uint dest) {
			Stack<uint>  path  = new Stack<uint>();
			List<uint[]> paths = new List<uint[]>();
			
			for (int i = 0; i < _nodeHasBeenVisited.Length; ++i) {
				_nodeHasBeenVisited[i] = false;
			}
			_pathsCount = 0;
			
			FindAllPaths(src, dest, path, paths);
			return paths;
		}
	}
}