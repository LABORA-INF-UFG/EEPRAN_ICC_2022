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
			string     jsonLinks = linkReader.ReadToEnd();
			List<Link> links     = JsonSerializer.Deserialize<List<Link>>(jsonLinks);
			linkReader.Close();
			
			nodeReader = new StreamReader(nodePath);
			string     jsonNodes = nodeReader.ReadToEnd();
			List<Node> nodes     = JsonSerializer.Deserialize<List<Node>>(jsonNodes);
			nodeReader.Close();

			if (nodes == null || links == null) {
				throw new Exception("Failed to parse json.");
			}

			Graph graph = new Graph(links.Count(), nodes.Count(), Constants.MaxPaths);
			foreach (var link in links) {
				uint source      = Math.Min(link.FromNode, link.ToNode);
				uint destination = Math.Max(link.FromNode, link.ToNode);
				graph.AddEdge(source, destination);
			}

			List<uint> destinations = new List<uint>();
			foreach (var node in nodes) {
				if (node.Ru == 1) destinations.Add(node.Number);
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
					pathData.Seq.Add(path[1]);
					pathData.P1.Add($"({path[0]}, {path[1]})");
						
					pathData.Seq.Add(path[i]);
					for (int j = 1; j < path.Length-1; ++j) {
						if (j != i) {
							pathData.P2.Add($"({path[j]}, {path[j+1]})");
						} else {
							break;
						}
					}

					pathData.Seq.Add(path[^1]);
					for (int j = i; j < path.Length-1; ++j) {
						if (j != path.Length - 1) {
							pathData.P3.Add($"({path[j]}, {path[j+1]})");
						} else {
							break;
						}
					}

					pathData.Id     = id;
					pathData.Target = path[^1];
					pathData.Source = "CN";
					++count;

					bool append = true;
					if (PathList.Count > 0) {
						foreach (var pd in PathList) {
							if (pd.Value == null) continue;
							if (pd.Value.P1.Equals(pathData.P1) && pd.Value.P2.Equals(pathData.P2) &&
							    pd.Value.P3.Equals(pathData.P3) && pd.Value.Id != pathData.Id) {
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
					pathData.Seq.Add(path[0]);

					pathData.Seq.Add(path[i]);
					for (int j = 0; j < path.Length-1; ++j) {
						if (j != i) {
							pathData.P2.Add($"({path[j]}, {path[j+1]})");
						}
						if (j+1 == i) {
							break;
						}
					}

					pathData.Seq.Add(path[^1]);
					for (int j = i; j < path.Length-1; ++j) {
						if (j != path.Length - 1) {
							pathData.P3.Add($"({path[j]}, {path[j+1]})");
						}
						if (j+1 == i) {
							break;
						}
					}

					pathData.Id     = id;
					pathData.Target = path[^1];
					pathData.Source = "CN";
					++count;

					bool append = true;
					if (PathList.Count > 0) {
						foreach (var pd in PathList) {
							if (pd.Value == null) continue;
							if (pd.Value.P1.Equals(pathData.P1) && pd.Value.P2.Equals(pathData.P2) &&
							    pd.Value.P3.Equals(pathData.P3)) {
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
				pathData.Seq.Add(0);

				pathData.Seq.Add(0);

				pathData.Seq.Add(path[^1]);
				for (int j = 0; j < path.Length-1; ++j) {
					if (j != path.Length - 1) {
						pathData.P3.Add($"({path[j]}, {path[j+1]})");
					}
					if (j+1 == path.Length - 1) {
						break;
					}
				}

				pathData.Id     = id;
				pathData.Target = path[^1];
				pathData.Source = "CN";
				++count;

				bool append = true;
				if (PathList.Count > 0) {
					foreach (var pd in PathList) {
						if (pd.Value == null) continue;
						if (pd.Value.P1.Equals(pathData.P1) && pd.Value.P2.Equals(pathData.P2) &&
						    pd.Value.P3.Equals(pathData.P3) && pd.Value.Id != pathData.Id) {
							append = false;
						}
					}
				}

				if (!append) continue;
				PathList.Add($"path-{id}", pathData);
				++id;
			}

			Console.WriteLine($"{PathList.Count} Path configurations successfully found");
		}
	}

	public class PathData {
		public uint         Id     { get; set; }
		public string       Source { get; set; }
		public uint         Target { get; set; }
		public List<uint>   Seq    { get; set; }
		public List<string> P1     { get; set; }
		public List<string> P2     { get; set; }
		public List<string> P3     { get; set; }

		public PathData() {
			Seq = new List<uint>();
			P1 = new List<string>();
			P2 = new List<string>();
			P3 = new List<string>();
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
			_nodeHasBeenVisited = new bool[nodeCount+1];
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