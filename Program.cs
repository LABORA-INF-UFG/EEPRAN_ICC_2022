using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ILOG.Concert;

using System.Xml;
using ILOG.CPLEX;

namespace modelo_william {
	class Program {
		static void Main(string[] args) {
			var    option   = new JsonSerializerOptions {WriteIndented = true};
			string linkPath = @"D:\Workspace\code\csharp\OperationResearch\modelo_william\test_topo_10_hier_links.json";
			string nodePath = @"D:\Workspace\code\csharp\OperationResearch\modelo_william\test_topo_10_hier_nodes.json";
			string pathPath = @"D:\Workspace\code\csharp\OperationResearch\modelo_william\paths.json";
			
			StreamReader linkReader = new StreamReader(linkPath);
			string       jsonLinks  = linkReader.ReadToEnd();
			Link         links      = JsonSerializer.Deserialize<Link>(jsonLinks);
			
			StreamReader nodeReader = new StreamReader(nodePath);
			string       jsonNodes  = nodeReader.ReadToEnd();
			Node         nodes      = JsonSerializer.Deserialize<Node>(jsonNodes);
			
			StreamReader pathReader = new StreamReader(pathPath);
			string       jsonPaths  = pathReader.ReadToEnd();
			Path         paths      = JsonSerializer.Deserialize<Path>(jsonPaths);

			Utils.Init(links, nodes, paths);
			Utils.CreateModel();

		}
	}

	internal class Route {
		public uint         Id      { get; set; }
		public string       Source  { get; set; }
		public uint         Target  { get; set; }
		public List<uint>   Seq     { get; set; }
		public List<uint[]> P1      { get; set; }
		public List<uint[]> P2      { get; set; }
		public List<uint[]> P3      { get; set; }
		public float        DelayP1 { get; set; }
		public float        DelayP2 { get; set; }
		public float        DelayP3 { get; set; }

		public int QtyCrs() {
			var counter = 0;
			if (P1.Count > 0) ++counter;
			if (P2.Count > 0) ++counter;
			if (P3.Count > 0) ++counter;
			return counter;
		}
	}
	internal class Cr {
		public uint   Id           { get; set; }
		public uint   NumBs        { get; set; }
		public float  Cpu          { get; set; }
		public ushort StaticPower  { get; set; }
		public ushort DynamicPower { get; set; }
	}
	internal class Drc {
		public uint   Id          { get; set; }
		public float  CuCpuUsage  { get; set; }
		public float  DuCpuUsage  { get; set; }
		public float  RuCpuUsage  { get; set; }
		public ushort[] FsCu        { get; set; }
		public ushort[] FsDu        { get; set; }
		public ushort[] FsRu        { get; set; }
		public float  DelayBh     { get; set; }
		public float  DelayMh     { get; set; }
		public float  DelayFh     { get; set; }
		public float  BandwidthBh { get; set; }
		public float  BandwidthMh { get; set; }
		public float  BandwidthFh { get; set; }
		public ushort QtyCRs      { get; set; }
	}
	internal class Fs {
		public ushort Id       { get; set; }
		public ushort CpuUsage { get; set; }
	}
	internal class Ru {
		public ushort Id           { get; set; }
		public uint   AssociatedCr { get; set; }
	}

	internal static class Utils {
		public static Dictionary<uint, Route> Routes;
		public static Dictionary<uint, Cr>    Crs;
		public static Dictionary<uint, Drc>   Drcs;
		public static Dictionary<uint, Fs>    Fses;
		public static Dictionary<uint, Ru>    Rus;
		public static uint[][]                 LinkCapacities;

		static Utils() { }

		private static uint[] String2Tuple(string text) {
			uint[] tuple           = new uint[2];
			int    endFirstNumber  = text.IndexOf(',', 0);
			int    endSecondNumber = text.IndexOf(')', 0);
			tuple[0] = UInt32.Parse(text.Substring(1, endFirstNumber - 1));
			tuple[1] = UInt32.Parse(text.Substring(endFirstNumber+2, endSecondNumber-endFirstNumber-2));
			return tuple;
		}

		private static string GetStringKey(Route route, Drc drc, Ru ru) {
			return $"({route.Id},{drc.Id},{ru.Id})";
		}
		
		private static Dictionary<uint, Route> ProcessRoutes(Link links, Node nodes, Path paths) {
			Dictionary<uint, Route> routes = new Dictionary<uint, Route>();

			float[][] delays = new float[nodes.Count()][];
			LinkCapacities = new uint[nodes.Count()][];
			for (int i = 0; i < nodes.Count(); ++i) {
				delays[i]         = new float[nodes.Count()];
				LinkCapacities[i] = new uint[nodes.Count()];
			}

			foreach (LinkData link in links.links) {
				delays[link.fromNode][link.toNode]         = link.delay;
				LinkCapacities[link.fromNode][link.toNode] = link.capacity;
			}
			
			foreach (PathData path in paths.paths.Values) {
				Route route = new Route();
				route.Id = path.id;
				
				route.P1 = new List<uint[]>();
				foreach (string s in path.p1) {
					route.P1.Add(String2Tuple(s));
				}
				route.P2 = new List<uint[]>();
				foreach (string s in path.p2) {
					route.P2.Add(String2Tuple(s));
				}
				route.P3 = new List<uint[]>();
				foreach (string s in path.p3) {
					route.P3.Add(String2Tuple(s));
				}

				route.Seq    = path.seq;
				route.Source = path.source;
				route.Target = path.target;

				route.DelayP1 = 0.0f;
				foreach (uint[] tuple in route.P1) {
					route.DelayP1 += delays[tuple[0]][tuple[1]];
				}
				
				route.DelayP2 = 0.0f;
				foreach (uint[] tuple in route.P2) {
					route.DelayP2 += delays[tuple[0]][tuple[1]];
				}
				
				route.DelayP3 = 0.0f;
				foreach (uint[] tuple in route.P3) {
					route.DelayP3 += delays[tuple[0]][tuple[1]];
				}
				
				routes.Add(path.id, route);
			}
			
			return routes;
		}

		private static Dictionary<uint, Cr> ProcessCrs(Node nodes) {
			return nodes.nodes.ToDictionary(node => node.nodeNumber, node => new Cr {
				Id           = node.nodeNumber,
				NumBs        = 0,
				Cpu          = node.cpu,
				StaticPower  = node.StaticPower,
				DynamicPower = node.DynamicPower
			});
		}

		private static Dictionary<uint, Drc> ProcessDrcs() {
			Dictionary<uint, Drc> drcs = new Dictionary<uint, Drc>();
			drcs.Add(1, new Drc {Id = 1, CuCpuUsage = 0.49f, DuCpuUsage = 2.058f, RuCpuUsage = 2.352f, 
					FsCu = new ushort[] {8}, FsDu = new ushort[]{7, 6, 5, 4, 3, 2}, FsRu = new ushort[] {1, 0}, 
					DelayBh = 10.0f, DelayMh = 10.0f, DelayFh = 0.25f, 
					BandwidthBh = 9.9f, BandwidthMh = 13.2f, BandwidthFh = 42.6f, QtyCRs = 3});
			drcs.Add(2, new Drc {Id = 2, CuCpuUsage = 0.98f, DuCpuUsage = 1.568f, RuCpuUsage = 2.352f, 
				FsCu = new ushort[] {8, 7}, FsDu = new ushort[]{6, 5, 4, 3, 2}, FsRu = new ushort[] {1, 0}, 
				DelayBh = 10.0f, DelayMh = 10.0f, DelayFh = 0.25f, 
				BandwidthBh = 9.9f, BandwidthMh = 13.2f, BandwidthFh = 42.6f, QtyCRs = 3});
			drcs.Add(4, new Drc {Id = 4, CuCpuUsage = 0.49f, DuCpuUsage = 1.225f, RuCpuUsage = 3.185f, 
				FsCu = new ushort[] {8}, FsDu = new ushort[]{7, 6, 5, 4, 3}, FsRu = new ushort[] {2, 1, 0}, 
				DelayBh = 10.0f, DelayMh = 10.0f, DelayFh = 0.25f, 
				BandwidthBh = 9.9f, BandwidthMh = 13.2f, BandwidthFh = 13.6f, QtyCRs = 3});
			drcs.Add(5, new Drc {Id = 5, CuCpuUsage = 0.98f, DuCpuUsage = 0.735f, RuCpuUsage = 3.185f, 
				FsCu = new ushort[] {8, 7}, FsDu = new ushort[]{6, 5, 4, 3}, FsRu = new ushort[] {2, 1, 0}, 
				DelayBh = 10.0f, DelayMh = 10.0f, DelayFh = 0.25f, 
				BandwidthBh = 9.9f, BandwidthMh = 13.2f, BandwidthFh = 42.6f, QtyCRs = 3});
			
			drcs.Add(6, new Drc {Id = 6, CuCpuUsage = 0.0f, DuCpuUsage = 0.49f, RuCpuUsage = 4.41f, 
				FsCu = new ushort[] {}, FsDu = new ushort[]{8}, FsRu = new ushort[] {7, 6, 5, 4, 3, 2, 1, 0}, 
				DelayBh = 0.0f, DelayMh = 10.0f, DelayFh = 10.0f, 
				BandwidthBh = 0.0f, BandwidthMh = 9.9f, BandwidthFh = 13.2f, QtyCRs = 2});
			drcs.Add(7, new Drc {Id = 7, CuCpuUsage = 0.0f, DuCpuUsage = 3.0f, RuCpuUsage = 3.92f, 
				FsCu = new ushort[] {}, FsDu = new ushort[]{8, 7}, FsRu = new ushort[] {6, 5, 4, 3, 2, 1, 0}, 
				DelayBh = 0.0f, DelayMh = 10.0f, DelayFh = 10.0f, 
				BandwidthBh = 0.0f, BandwidthMh = 9.9f, BandwidthFh = 13.2f, QtyCRs = 2});
			drcs.Add(9, new Drc {Id = 9, CuCpuUsage = 0.0f, DuCpuUsage = 2.54f, RuCpuUsage = 2.354f, 
				FsCu = new ushort[] {}, FsDu = new ushort[]{8, 7, 6, 5, 4, 3, 2}, FsRu = new ushort[] {1, 0}, 
				DelayBh = 0.0f, DelayMh = 10.0f, DelayFh = 0.25f, 
				BandwidthBh = 0.0f, BandwidthMh = 9.9f, BandwidthFh = 42.6f, QtyCRs = 2});
			drcs.Add(10, new Drc {Id = 10, CuCpuUsage = 0.0f, DuCpuUsage = 1.71f, RuCpuUsage = 3.185f, 
				FsCu = new ushort[] {}, FsDu = new ushort[]{8, 7, 6, 5, 4, 3}, FsRu = new ushort[] {2, 1, 0}, 
				DelayBh = 0.0f, DelayMh = 10.0f, DelayFh = 0.25f, 
				BandwidthBh = 0.0f, BandwidthMh = 3.0f, BandwidthFh = 13.6f, QtyCRs = 2});
			
			drcs.Add(8, new Drc {Id = 8, CuCpuUsage = 0.0f, DuCpuUsage = 0.0f, RuCpuUsage = 4.9f, 
				FsCu = new ushort[] {}, FsDu = new ushort[]{}, FsRu = new ushort[] {8, 7, 6, 5, 4, 3, 2, 1, 0}, 
				DelayBh = 0.0f, DelayMh = 0.0f, DelayFh = 10.0f, 
				BandwidthBh = 0.0f, BandwidthMh = 0.0f, BandwidthFh = 9.9f, QtyCRs = 1});
			
			return drcs;
		}

		private static Dictionary<uint, Fs> ProcessFss() {
			Dictionary<uint, Fs> fss = new Dictionary<uint, Fs>();
			for (ushort i = 0; i <= 8; ++i) {
				fss.Add(i, new Fs {
					Id       = i,
					CpuUsage = 2
				});
			}

			return fss;
		}

		private static Dictionary<uint, Ru> ProcessRus(Node nodes) {
			Dictionary<uint, Ru> rus = new Dictionary<uint, Ru>();
			ushort                 i   = 1;
			foreach (var node in nodes.nodes.Where(node => node.RU == 1)) {
				rus.Add(i, new Ru {
					Id           = i,
					AssociatedCr = node.nodeNumber
				});
				++i;
			}
			
			return rus;
		}

		public static void Init(Link links, Node nodes, Path paths) {
			Routes = ProcessRoutes(links, nodes, paths);
			Crs    = ProcessCrs(nodes);
			Rus    = ProcessRus(nodes);
			Drcs   = ProcessDrcs();
			Fses   = ProcessFss();
		}

		public static void CreateModel() {
			Cplex model = new Cplex();

			#region CountDictionaries
			uint lastRoute = 0;
			foreach (uint key in Routes.Keys) {
				lastRoute = Math.Max(lastRoute, key);
			}

			uint lastDrc = 0;
			foreach (uint key in Drcs.Keys) {
				lastDrc = Math.Max(lastDrc, key);
			}
			
			uint lastRu = 0;
			foreach (uint key in Rus.Keys) {
				lastRu = Math.Max(lastRu, key);
			}
			
			uint lastCr = 0;
			foreach (uint key in Crs.Keys) {
				lastCr = Math.Max(lastCr, key);
			}
			#endregion
			
			#region Decision Variable X
			
			Dictionary<string, IIntVar> x    = new Dictionary<string, IIntVar>();
			Dictionary<string, uint[]>  keys = new Dictionary<string, uint[]>();

			// initialize Dictionary x for the decision variables and
			// Dictionary keys for recovering the IDs from the string keys later
			foreach (Route route in Routes.Values) {
				foreach (Drc drc in Drcs.Values) {
					foreach (Ru ru in Rus.Values) {
						if (route.Seq[2] != ru.AssociatedCr) { continue; }
						if (drc.QtyCRs != route.QtyCrs()) { continue; }
						x.Add(GetStringKey(route, drc, ru), model.IntVar(0, 1, $"Xe{{{route.Id},{drc.Id}}}_{{{ru.Id}}}"));
						keys.Add(GetStringKey(route, drc, ru), new uint[] {route.Id, drc.Id, ru.Id});
					}
				}
			}

			#endregion

			#region Objective Function
			var ceilFunction = model.LinearIntExpr();
			var dynamicCostFunction  = model.LinearNumExpr();
			var staticCostFunction   = model.NumExpr();
			
			foreach (Cr cr in Crs.Values) {
				
				for (ushort f = 0; f <= 8; ++f) {
					foreach ((string stringKey, IIntVar decisionVarX) in x) {
						Route route = Routes[keys[stringKey][0]];
						Drc   drc   = Drcs[keys[stringKey][1]];

						// u Indicates if the CR cr is part of the Path route
						var u = 0;
						if (route.Seq.Contains(cr.Id)) {
							u = 1;
						}

						// m Indicates if CR cr runs de VNF f fom de RU ru according to DRC drc
						var m = 0;
						if (cr.Id == route.Seq[0] && drc.FsCu.Contains(f)) {
							m = 1;
						} else if (cr.Id == route.Seq[1] && drc.FsDu.Contains(f)) {
							m = 1;
						} else if (cr.Id == route.Seq[2] && drc.FsRu.Contains(f)) {
							m = 1;
						}

						dynamicCostFunction.AddTerm(decisionVarX,
							u * m * Fses[f].CpuUsage * cr.DynamicPower / cr.Cpu);

						ceilFunction.AddTerm(decisionVarX, u * m);
					}
				}

				staticCostFunction = model.Sum(model.Prod(model.Min(ceilFunction, 1.0), cr.StaticPower), staticCostFunction);
				ceilFunction.Clear();
			}
			
			model.Minimize(model.Sum(staticCostFunction, dynamicCostFunction));
			#endregion

			#region Bottleneck Restriction
			
			var fsesInCrFunction    = model.LinearIntExpr();
			var aggregationFunction = model.NumExpr();

			foreach (Cr cr in Crs.Values) {
				for (ushort f = 0; f <= 8; ++f) {
					ceilFunction.Clear();
					fsesInCrFunction.Clear();
					
					foreach (var (stringKey, decisionVarX) in x) {
						Route route = Routes[keys[stringKey][0]];
						Drc drc = Drcs[keys[stringKey][1]];

						// u Indicates if the CR cr is part of the Path route
						var u = 0;
						if (route.Seq.Contains(cr.Id)) {
							u = 1;
						}

						// m Indicates if CR cr runs de VNF f fom de RU ru according to DRC drc
						var m = 0;
						if (cr.Id == route.Seq[0] && drc.FsCu.Contains(f)) {
							m = 1;
						} else if (cr.Id == route.Seq[1] && drc.FsDu.Contains(f)) {
							m = 1;
						} else if (cr.Id == route.Seq[2] && drc.FsRu.Contains(f)) {
							m = 1;
						}

						fsesInCrFunction.AddTerm(decisionVarX, u * m);
						ceilFunction.AddTerm(decisionVarX, u * m);
					}
					
					aggregationFunction = model.Sum( model.Sum( model.Prod( model.Min(ceilFunction, 1.0), -1.0 ), fsesInCrFunction ), aggregationFunction );
				}
			}

			IRange bottleneck = model.AddGe(aggregationFunction, 0.0, "Bottleneck Restriction");

			#endregion

			#region One Route Restriction
			
			var auxFunction = model.LinearIntExpr();
			
			foreach (Ru ru in Rus.Values) {
				
				foreach (Drc drc in Drcs.Values) {
					foreach (Route route in Routes.Values) {
						if (x.TryGetValue(GetStringKey(route, drc, ru), out IIntVar decisionVarX)) {
							auxFunction.AddTerm(decisionVarX, 1);
						}
					}
				}
				
				model.AddEq(auxFunction, 1, $"Route restriction for ru {ru.Id}");
				auxFunction.Clear();
			}
			
			#endregion

			#region Link Bandwidth Restriction
			
			var bandwidthFunction = model.LinearNumExpr();

			for (var i = 0; i < LinkCapacities.Length; ++i) {
				for (var j = 0; j < LinkCapacities.Length; ++j) {
					if (i == j) continue;
					bandwidthFunction.Clear();

					foreach ((string stringKey, IIntVar decisionVarX) in x) {
						Route route = Routes[keys[stringKey][0]];
						Drc   drc   = Drcs[keys[stringKey][1]];
						
						foreach (uint[] link in route.P1) {
							if ((link[0] == i && link[1] == j) || (link[0] == j && link[1] == i)) {
								bandwidthFunction.AddTerm(decisionVarX, drc.BandwidthBh);
							}
						}

						foreach (uint[] link in route.P2) {
							if ((link[0] == i && link[1] == j) || (link[0] == j && link[1] == i)) {
								bandwidthFunction.AddTerm(decisionVarX, drc.BandwidthMh);
							}
						}

						foreach (uint[] link in route.P3) {
							if ((link[0] == i && link[1] == j) || (link[0] == j && link[1] == i)) {
								bandwidthFunction.AddTerm(decisionVarX, drc.BandwidthFh);
							}
						}
					}
					if (bandwidthFunction.ToString().Equals("0")) continue;
					model.AddLe(bandwidthFunction, LinkCapacities[i][j], $"Bandwidth Restriction for Link[{i}][{j}]");
				}
			}
			
			#endregion

			#region Delay Restrictions
			
			foreach ((string stringKey, IIntVar decisionVarX) in x) {
				Route route = Routes[keys[stringKey][0]];
				Drc   drc   = Drcs[keys[stringKey][1]];
				if (drc.QtyCRs == 3) {
					model.AddLe(model.Prod(decisionVarX, route.DelayP1), drc.DelayBh,
						$"BH Delay Restriction for {stringKey}");
				}

				if (drc.QtyCRs >= 2) {
					model.AddLe(model.Prod(decisionVarX, route.DelayP2), drc.DelayMh,
						$"MH Delay Restriction for {stringKey}");
				}

				model.AddLe(model.Prod(decisionVarX, route.DelayP3), drc.DelayFh, 
					$"FH Delay Restriction for {stringKey}");
			}
			
			#endregion
			
			#region Processing Capacity Restriction
			
			var processingFunction = model.LinearNumExpr();

			foreach (Cr cr in Crs.Values) {
				if (cr.Id == 0) continue; // we dont have functions running on the Core
				processingFunction.Clear();
				
				for (ushort f = 0; f <= 8; ++f) {
					foreach ((string stringKey, IIntVar decisionVarX) in x) {
						Route route = Routes[keys[stringKey][0]];
						Drc   drc   = Drcs[keys[stringKey][1]];
						Ru    ru    = Rus[keys[stringKey][2]];
						
						var u = 0;
						if (route.Seq.Contains(cr.Id)) {
							u = 1;
						}

						var m = 0;
						if (route.Seq[2] != ru.AssociatedCr) {
							m = 0;
						} else if (cr.Id == route.Seq[0] && drc.FsCu.Contains(f)) {
							m = 1;
						} else if (cr.Id == route.Seq[1] && drc.FsDu.Contains(f)) {
							m = 1;
						} else if (cr.Id == route.Seq[2] && drc.FsRu.Contains(f)) {
							m = 1;
						}
						
						if (route.Seq[0] == cr.Id) {
							processingFunction.AddTerm(decisionVarX, u * m * drc.CuCpuUsage);
						} else if (route.Seq[1] == cr.Id) {
							processingFunction.AddTerm(decisionVarX, u * m * drc.DuCpuUsage);
						} else if (route.Seq[2] == cr.Id) {
							processingFunction.AddTerm(decisionVarX, u * m * drc.RuCpuUsage);
						}
					}
				}
				
				model.AddLe(processingFunction, cr.Cpu, $"Processing Restriction for cr {cr.Id}");
			}

			#endregion
			
			model.ExportModel("model.lp");
		}
	}
}
