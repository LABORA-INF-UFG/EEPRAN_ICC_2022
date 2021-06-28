using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using ILOG.Concert;

using System.Xml;
using ILOG.CPLEX;

namespace modelo_william {
	class Program {
		static void Main(string[] args) {
			var    option   = new JsonSerializerOptions {WriteIndented = true};
			string linkPath = @"D:\Workspace\code\csharp\modelo_william\topologies\green_test_topo_8_links.json";
			string nodePath = @"D:\Workspace\code\csharp\modelo_william\topologies\green_test_topo_8_nodes.json";
			string pathPath = @"D:\Workspace\code\csharp\modelo_william\topologies\green_test_topo_8_paths.json";
			
			//Path path = new Path();
			//path.FindPaths(linkPath, nodePath);
			//File.WriteAllText(pathPath, JsonSerializer.Serialize(path, option));
			
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
			Utils.Solve();
			
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
		public double  Cpu          { get; set; }
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
		public double CpuUsage { get; set; }
	}
	internal class Ru {
		public ushort Id           { get; set; }
		public uint   AssociatedCr { get; set; }
	}

	internal static class Utils {
		public static Cplex                       Model;
		public static Dictionary<string, IIntVar> X;
		public static Dictionary<string, uint[]>  Keys;
		public static IRange                      BottleneckRestriction;
		public static INumExpr                    BottleneckExpr;
		public static Dictionary<uint, Route>     Routes;
		public static Dictionary<uint, Cr>        Crs;
		public static Dictionary<uint, Drc>       Drcs;
		public static Dictionary<uint, Fs>        Fses;
		public static Dictionary<uint, Ru>        Rus;
		public static uint[][]                    LinkCapacities;
		public static float[][]                   LinkDelays;

		static Utils() { }

		private static uint[] String2Tuple(string text) {
			uint[] tuple           = new uint[2];
			int    endFirstNumber  = text.IndexOf(',', 0);
			int    endSecondNumber = text.IndexOf(')', 0);
			tuple[0] = UInt32.Parse(text.Substring(1, endFirstNumber - 1));
			tuple[1] = UInt32.Parse(text.Substring(endFirstNumber + 2, endSecondNumber - endFirstNumber - 2));
			return tuple;
		}

		private static string GetStringKey(Route route, Drc drc, Ru ru) {
			return $"({route.Id},{drc.Id},{ru.Id})";
		}

		private static Dictionary<uint, Route> ProcessRoutes(Link links, Node nodes, Path paths) {
			Dictionary<uint, Route> routes = new Dictionary<uint, Route>();

			LinkDelays     = new float[nodes.Count()][];
			LinkCapacities = new uint[nodes.Count()][];
			for (int i = 0; i < nodes.Count(); ++i) {
				LinkDelays[i]     = new float[nodes.Count()];
				LinkCapacities[i] = new uint[nodes.Count()];
			}

			foreach (LinkData link in links.LinkList) {
				LinkDelays[link.fromNode][link.toNode]     = link.delay;
				LinkCapacities[link.fromNode][link.toNode] = link.capacity;
			}

			foreach (PathData path in paths.PathList.Values) {
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
					route.DelayP1 += LinkDelays[tuple[0]][tuple[1]];
				}

				route.DelayP2 = 0.0f;
				foreach (uint[] tuple in route.P2) {
					route.DelayP2 += LinkDelays[tuple[0]][tuple[1]];
				}

				route.DelayP3 = 0.0f;
				foreach (uint[] tuple in route.P3) {
					route.DelayP3 += LinkDelays[tuple[0]][tuple[1]];
				}

				routes.Add(path.id, route);
			}

			return routes;
		}

		private static Dictionary<uint, Cr> ProcessCrs(Node nodes) {
			return nodes.NodeList.ToDictionary(node => node.nodeNumber, node => new Cr {
				Id           = node.nodeNumber,
				NumBs        = 0,
				Cpu          = node.cpu,
				StaticPower  = node.StaticPower,
				DynamicPower = node.DynamicPower
			});
		}

		private static Dictionary<uint, Drc> ProcessDrcs() {
			Dictionary<uint, Drc> drcs = new Dictionary<uint, Drc>();
			drcs.Add(1, new Drc {
				Id          = 1, CuCpuUsage          = 0.49f, DuCpuUsage = 2.058f, RuCpuUsage = 2.352f,
				FsCu        = new ushort[] {8}, FsDu = new ushort[] {7, 6, 5, 4, 3, 2}, FsRu = new ushort[] {1, 0},
				DelayBh     = 10.0f, DelayMh         = 10.0f, DelayFh = 0.25f,
				BandwidthBh = 9.9f, BandwidthMh      = 13.2f, BandwidthFh = 42.6f, QtyCRs = 3
			});
			drcs.Add(2, new Drc {
				Id          = 2, CuCpuUsage             = 0.98f, DuCpuUsage = 1.568f, RuCpuUsage = 2.352f,
				FsCu        = new ushort[] {8, 7}, FsDu = new ushort[] {6, 5, 4, 3, 2}, FsRu = new ushort[] {1, 0},
				DelayBh     = 10.0f, DelayMh            = 10.0f, DelayFh = 0.25f,
				BandwidthBh = 9.9f, BandwidthMh         = 13.2f, BandwidthFh = 42.6f, QtyCRs = 3
			});
			drcs.Add(4, new Drc {
				Id          = 4, CuCpuUsage          = 0.49f, DuCpuUsage                  = 1.225f, RuCpuUsage = 3.185f,
				FsCu        = new ushort[] {8}, FsDu = new ushort[] {7, 6, 5, 4, 3}, FsRu = new ushort[] {2, 1, 0},
				DelayBh     = 10.0f, DelayMh         = 10.0f, DelayFh                     = 0.25f,
				BandwidthBh = 9.9f, BandwidthMh      = 13.2f, BandwidthFh                 = 13.6f, QtyCRs = 3
			});
			drcs.Add(5, new Drc {
				Id          = 5, CuCpuUsage             = 0.98f, DuCpuUsage               = 0.735f, RuCpuUsage = 3.185f,
				FsCu        = new ushort[] {8, 7}, FsDu = new ushort[] {6, 5, 4, 3}, FsRu = new ushort[] {2, 1, 0},
				DelayBh     = 10.0f, DelayMh            = 10.0f, DelayFh                  = 0.25f,
				BandwidthBh = 9.9f, BandwidthMh         = 13.2f, BandwidthFh              = 13.6f, QtyCRs = 3
			});

			drcs.Add(6, new Drc {
				Id          = 6, CuCpuUsage          = 0.0f, DuCpuUsage       = 0.49f, RuCpuUsage = 4.41f,
				FsCu        = new ushort[] { }, FsDu = new ushort[] {8}, FsRu = new ushort[] {7, 6, 5, 4, 3, 2, 1, 0},
				DelayBh     = 0.0f, DelayMh          = 10.0f, DelayFh         = 10.0f,
				BandwidthBh = 0.0f, BandwidthMh      = 9.9f, BandwidthFh      = 13.2f, QtyCRs = 2
			});
			drcs.Add(7, new Drc {
				Id          = 7, CuCpuUsage          = 0.0f, DuCpuUsage          = 3.0f, RuCpuUsage = 3.92f,
				FsCu        = new ushort[] { }, FsDu = new ushort[] {8, 7}, FsRu = new ushort[] {6, 5, 4, 3, 2, 1, 0},
				DelayBh     = 0.0f, DelayMh          = 10.0f, DelayFh            = 10.0f,
				BandwidthBh = 0.0f, BandwidthMh      = 9.9f, BandwidthFh         = 13.2f, QtyCRs = 2
			});
			drcs.Add(9, new Drc {
				Id          = 9, CuCpuUsage          = 0.0f, DuCpuUsage = 2.54f, RuCpuUsage = 2.354f,
				FsCu        = new ushort[] { }, FsDu = new ushort[] {8, 7, 6, 5, 4, 3, 2}, FsRu = new ushort[] {1, 0},
				DelayBh     = 0.0f, DelayMh          = 10.0f, DelayFh = 0.25f,
				BandwidthBh = 0.0f, BandwidthMh      = 9.9f, BandwidthFh = 42.6f, QtyCRs = 2
			});
			drcs.Add(10, new Drc {
				Id          = 10, CuCpuUsage         = 0.0f, DuCpuUsage = 1.71f, RuCpuUsage = 3.185f,
				FsCu        = new ushort[] { }, FsDu = new ushort[] {8, 7, 6, 5, 4, 3}, FsRu = new ushort[] {2, 1, 0},
				DelayBh     = 0.0f, DelayMh          = 10.0f, DelayFh = 0.25f,
				BandwidthBh = 0.0f, BandwidthMh      = 3.0f, BandwidthFh = 13.6f, QtyCRs = 2
			});

			drcs.Add(8, new Drc {
				Id = 8, CuCpuUsage = 0.0f, DuCpuUsage = 0.0f, RuCpuUsage = 4.9f,
				FsCu = new ushort[] { }, FsDu = new ushort[] { }, FsRu = new ushort[] {8, 7, 6, 5, 4, 3, 2, 1, 0},
				DelayBh = 0.0f, DelayMh = 0.0f, DelayFh = 10.0f,
				BandwidthBh = 0.0f, BandwidthMh = 0.0f, BandwidthFh = 9.9f, QtyCRs = 1
			});

			return drcs;
		}

		private static Dictionary<uint, Fs> ProcessFss() {
			Dictionary<uint, Fs> fss = new Dictionary<uint, Fs>();

			fss.Add(0, new Fs {
				Id       = 0,
				CpuUsage = 1.176,
			});
			fss.Add(1, new Fs {
				Id       = 1,
				CpuUsage = 1.176,
			});
			fss.Add(2, new Fs {
				Id       = 2,
				CpuUsage = 0.833,
			});
			fss.Add(3, new Fs {
				Id       = 3,
				CpuUsage = 0.18375,
			});
			fss.Add(4, new Fs {
				Id       = 4,
				CpuUsage = 0.18375,
			});
			fss.Add(5, new Fs {
				Id       = 1,
				CpuUsage = 0.18375,
			});
			fss.Add(6, new Fs {
				Id       = 1,
				CpuUsage = 0.18375,
			});
			fss.Add(7, new Fs {
				Id       = 7,
				CpuUsage = 0.49,
			});
			fss.Add(8, new Fs {
				Id       = 8,
				CpuUsage = 0.49,
			});

			return fss;
		}

		private static Dictionary<uint, Ru> ProcessRus(Node nodes) {
			Dictionary<uint, Ru> rus = new Dictionary<uint, Ru>();
			ushort               i   = 1;
			foreach (var node in nodes.NodeList.Where(node => node.RU == 1)) {
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
			Model = new Cplex();

			#region Decision Variable X

			X    = new Dictionary<string, IIntVar>();
			Keys = new Dictionary<string, uint[]>();

			// initialize Dictionary x for the decision variables and
			// Dictionary keys for recovering the IDs from the string keys later
			foreach (Route route in Routes.Values) {
				foreach (Drc drc in Drcs.Values) {
					foreach (Ru ru in Rus.Values) {
						if (route.Seq[2] != ru.AssociatedCr) {
							continue;
						}

						if (drc.QtyCRs != route.QtyCrs()) {
							continue;
						}

						X.Add(GetStringKey(route, drc, ru),
							Model.IntVar(0, 1, $"Xe{{{route.Id},{drc.Id}}}_{{{ru.Id}}}"));
						Keys.Add(GetStringKey(route, drc, ru), new uint[] {route.Id, drc.Id, ru.Id});
					}
				}
			}

			#endregion

			#region Objective Function

			var ceilFunction        = Model.LinearIntExpr();
			var dynamicCostFunction = Model.LinearNumExpr();
			var staticCostFunction  = Model.NumExpr();

			foreach (Cr cr in Crs.Values) {
				if (cr.Id == 0) continue;

				for (ushort f = 0; f <= 8; ++f) {
					foreach ((string stringKey, IIntVar decisionVarX) in X) {
						Route route = Routes[Keys[stringKey][0]];
						Drc   drc   = Drcs[Keys[stringKey][1]];

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

				staticCostFunction = Model.Sum(Model.Prod(Model.Min(ceilFunction, 1), cr.StaticPower),
					staticCostFunction);
				ceilFunction.Clear();
			}

			Model.AddMinimize(Model.Sum(staticCostFunction, dynamicCostFunction));

			#endregion

			#region Bottleneck Restriction

			var fsesInCrFunction    = Model.LinearIntExpr();
			BottleneckExpr = Model.NumExpr();

			foreach (Cr cr in Crs.Values) {
				if (cr.Id == 0) continue;
				for (int f = 8; f >= 0; --f) {
					//ceilFunction = Model.LinearIntExpr();
					fsesInCrFunction = Model.LinearIntExpr();

					foreach (var (stringKey, decisionVarX) in X) {
						Route route = Routes[Keys[stringKey][0]];
						Drc   drc   = Drcs[Keys[stringKey][1]];
						Ru    ru    = Rus[Keys[stringKey][2]];

						// u Indicates if the CR cr is part of the Path route
						var u = 0;
						if (route.Seq.Contains(cr.Id)) {
							u = 1;
						}

						// m Indicates if CR cr runs de VNF f fom de RU ru according to DRC drc
						var m = 0;
						if (cr.Id == route.Seq[0] && drc.FsCu.Contains((ushort) f)) {
							m = 1;
						} else if (cr.Id == route.Seq[1] && drc.FsDu.Contains((ushort) f)) {
							m = 1;
						} else if (cr.Id == route.Seq[2] && drc.FsRu.Contains((ushort) f)) {
							m = 1;
						}
						
						if (m == 0 || u == 0) continue;

						fsesInCrFunction.AddTerm(decisionVarX, u * m);
						//ceilFunction.AddTerm(decisionVarX, u * m);
					}

					BottleneckExpr = Model.Sum(Model.Sum(Model.Prod(Model.Min(fsesInCrFunction, 1), -1), fsesInCrFunction), BottleneckExpr);
				}
			}
			
			BottleneckRestriction = Model.AddGe(BottleneckExpr, 0.0, "Bottleneck Restriction");

			#endregion

			#region One Route Restriction

			var auxFunction = Model.LinearIntExpr();

			foreach (Ru ru in Rus.Values) {

				foreach (Drc drc in Drcs.Values) {
					foreach (Route route in Routes.Values) {
						if (X.TryGetValue(GetStringKey(route, drc, ru), out IIntVar decisionVarX)) {
							auxFunction.AddTerm(decisionVarX, 1);
						}
					}
				}

				Model.AddEq(auxFunction, 1, $"Route restriction for ru {ru.Id}");
				auxFunction.Clear();
			}

			#endregion

			#region Link Bandwidth Restriction

			var bandwidthFunction = Model.LinearNumExpr();

			for (var i = 0; i < LinkCapacities.Length; ++i) {
				for (var j = i + 1; j < LinkCapacities.Length; ++j) {
					if (i == j) continue;
					bandwidthFunction.Clear();

					foreach ((string stringKey, IIntVar decisionVarX) in X) {
						Route route = Routes[Keys[stringKey][0]];
						Drc   drc   = Drcs[Keys[stringKey][1]];

						foreach (uint[] link in route.P1) {
							if (link[0] == i && link[1] == j) {
								bandwidthFunction.AddTerm(decisionVarX, drc.BandwidthBh);
							}
						}

						foreach (uint[] link in route.P2) {
							if (link[0] == i && link[1] == j) {
								bandwidthFunction.AddTerm(decisionVarX, drc.BandwidthMh);
							}
						}

						foreach (uint[] link in route.P3) {
							if (link[0] == i && link[1] == j) {
								bandwidthFunction.AddTerm(decisionVarX, drc.BandwidthFh);
							}
						}
					}

					if (bandwidthFunction.ToString().Equals("0")) continue;
					Model.AddLe(bandwidthFunction, LinkCapacities[i][j], $"Bandwidth Restriction for Link[{i}][{j}]");
				}
			}

			#endregion

			#region Delay Restrictions

			foreach ((string stringKey, IIntVar decisionVarX) in X) {
				Route route = Routes[Keys[stringKey][0]];
				Drc   drc   = Drcs[Keys[stringKey][1]];
				if (drc.QtyCRs == 3) {
					Model.AddLe(Model.Prod(decisionVarX, route.DelayP1), drc.DelayBh,
						$"BH Delay Restriction for {stringKey}");
				}

				if (drc.QtyCRs >= 2) {
					Model.AddLe(Model.Prod(decisionVarX, route.DelayP2), drc.DelayMh,
						$"MH Delay Restriction for {stringKey}");
				}

				Model.AddLe(Model.Prod(decisionVarX, route.DelayP3), drc.DelayFh,
					$"FH Delay Restriction for {stringKey}");
			}

			#endregion

			#region Processing Capacity Restriction

			var processingFunction = Model.LinearNumExpr();

			foreach (Cr cr in Crs.Values) {
				if (cr.Id == 0) continue; // we dont have functions running on the Core
				processingFunction.Clear();

				for (ushort f = 0; f <= 8; ++f) {
					foreach ((string stringKey, IIntVar decisionVarX) in X) {
						Route route = Routes[Keys[stringKey][0]];
						Drc   drc   = Drcs[Keys[stringKey][1]];
						Ru    ru    = Rus[Keys[stringKey][2]];

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
							processingFunction.AddTerm(decisionVarX, u * m * drc.CuCpuUsage / drc.FsCu.Length);
						} else if (route.Seq[1] == cr.Id) {
							processingFunction.AddTerm(decisionVarX, u * m * drc.DuCpuUsage / drc.FsDu.Length);
						} else if (route.Seq[2] == cr.Id) {
							processingFunction.AddTerm(decisionVarX, u * m * drc.RuCpuUsage / drc.FsRu.Length);
						}
					}
				}

				Model.AddLe(processingFunction, cr.Cpu, $"Processing Restriction for cr {cr.Id}");
			}

			#endregion

			Model.ExportModel("model.lp");
		}
		
		public static void Solve() {
			for (int i = 0; i < 10; ++i) {
				if (Model.Solve()) {

					Console.WriteLine($"-------- Start Iteration {i} --------");
					Console.WriteLine(Model.ObjValue);

					foreach ((string stringKey, IIntVar decisionVarX) in X) {
						if (Model.GetValue(decisionVarX) > 1 || Model.GetValue(decisionVarX) < 1) continue;
						Console.WriteLine($"{stringKey}: {Model.GetValue(decisionVarX)}");
					}

					var bottleneckValue = Model.GetValue(BottleneckExpr);
					Console.WriteLine($"#{i}: Bottleneck: {bottleneckValue}");
					BottleneckRestriction.LB = bottleneckValue + 1;
				} else {
					Console.WriteLine(Model.GetStatus());
					Console.WriteLine("-------- End --------");
					break;
				}
			}
		}
	}

}
