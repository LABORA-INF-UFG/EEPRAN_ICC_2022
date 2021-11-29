import util.constants
from core.data import *
from collections import defaultdict


class Graph:
    def __init__(self, vertices):
        self.vertices = vertices
        self.graph = defaultdict(list)
        self.cost = {}
        self.path_list = []
        self.actual_paths_count = 0

    @staticmethod
    def _is_cycle(path: list) -> bool:
        return len(path) > 4 and 1 in path and 2 in path

    def _has_exceeded_max_paths(self) -> bool:
        return self.actual_paths_count >= util.constants.Constants.K

    def _find_all_paths(self, source: int, destination: int, has_been_visited: list, actual_path: list):
        has_been_visited[source] = True
        actual_path.append(source)

        if source == destination:
            if not self._is_cycle(actual_path):
                self.actual_paths_count += 1
                if not self._has_exceeded_max_paths():
                    self.path_list.append(actual_path.copy())
        else:
            for node in self.graph[source]:
                if has_been_visited[node]:
                    continue
                self._find_all_paths(node, destination, has_been_visited, actual_path)

        actual_path.pop()
        has_been_visited[source] = False

    def add_edge(self, source: int, destination: int, delay: float = 0):
        self.graph[source].append(destination)
        self.cost[(source, destination, 'delay')] = delay

    def find_all_paths(self, source: int, destination: int):
        has_been_visited = [False] * self.vertices
        path = []
        self.actual_paths_count = 0
        self._find_all_paths(source, destination, has_been_visited, path)


def _construct_graph(link_list: list, node_list: list, is_ring: bool) -> Graph:
    graph = Graph(len(node_list)+1)
    for link in link_list:
        source = min(link.from_node, link.to_node)
        destination = max(link.from_node, link.to_node)
        graph.add_edge(source, destination, link.delay)
        if is_ring:
            graph.add_edge(destination, source, link.delay)
    return graph


def _process_routes(graph: Graph, cr_dict):
    route_list = []
    identifier = 1

    # 3 Nodes route processing
    for path in graph.path_list:
        count = 2
        for position in range(0, len(path) - 1):
            if position == count:
                for hw1 in cr_dict[path[1]]:
                    sequence = [(path[1], hw1.number)]
                    p1 = []
                    edge = [path[0], path[1]]
                    p1.append(edge)

                    for hw2 in cr_dict[path[position]]:
                        sequence2 = sequence.copy()
                        sequence2.append((path[position], hw2.number))
                        p2 = []
                        for i in range(1, len(path) - 1):
                            if i != position:
                                edge = [path[i], path[i+1]]
                                p2.append(edge)
                            if i+1 == position:
                                break

                        for hw3 in cr_dict[path[-1]]:
                            sequence3 = sequence2.copy()
                            sequence3.append((path[-1], hw3.number))
                            p3 = []
                            for i in range(position, len(path) - 1):
                                if i != len(path)-1:
                                    edge = [path[i], path[i+1]]
                                    p3.append(edge)
                                if i+1 == position:
                                    break

                            delay_p1 = 0.0
                            for edge in p1:
                                delay_p1 += graph.cost[(edge[0], edge[1], 'delay')]
                            delay_p2 = 0.0
                            for edge in p2:
                                delay_p2 += graph.cost[(edge[0], edge[1], 'delay')]
                            delay_p3 = 0.0
                            for edge in p3:
                                delay_p3 += graph.cost[(edge[0], edge[1], 'delay')]

                            new_route = Route(identifier, 'CN', path[-1], sequence3,
                                              p1, p2, p3, delay_p1, delay_p2, delay_p3)
                            if new_route not in route_list:
                                route_list.append(new_route)
                                identifier += 1
                count += 1

    # 2 Nodes route processing
    for path in graph.path_list:
        count = 1
        for position in range(0, len(path) - 1):
            if position == count:
                sequence = [(0, 0)]
                p1 = []

                for hw2 in cr_dict[path[position]]:
                    sequence2 = sequence.copy()
                    sequence2.append((path[position], hw2.number))
                    p2 = []
                    for i in range(0, len(path) - 1):
                        if i != position:
                            edge = [path[i], path[i + 1]]
                            p2.append(edge)
                        if i + 1 == position:
                            break

                    for hw3 in cr_dict[path[-1]]:
                        sequence3 = sequence2.copy()
                        sequence3.append((path[-1], hw3.number))
                        p3 = []
                        for i in range(position, len(path) - 1):
                            if i != len(path) - 1:
                                edge = [path[i], path[i + 1]]
                                p3.append(edge)
                            if i+1 == position:
                                break

                        delay_p1 = 0.0
                        delay_p2 = 0.0
                        for edge in p2:
                            delay_p2 += graph.cost[(edge[0], edge[1], 'delay')]
                        delay_p3 = 0.0
                        for edge in p3:
                            delay_p3 += graph.cost[(edge[0], edge[1], 'delay')]

                        new_route = Route(identifier, 'CN', path[-1], sequence3, p1, p2, p3, delay_p1, delay_p2, delay_p3)
                        if new_route not in route_list:
                            route_list.append(new_route)
                            identifier += 1
                count += 1

    # 1 Node route processing
    for path in graph.path_list:
        for position in range(0, len(path) - 1):
            sequence = [(0, 0), (0, 0)]
            p1 = []
            p2 = []

            for hw3 in cr_dict[path[-1]]:
                sequence3 = sequence.copy()
                sequence3.append((path[-1], hw3.number))
                p3 = []
                for i in range(0, len(path)-1):
                    if i != len(path)-1:
                        edge = [path[i], path[i+1]]
                        p3.append(edge)
                    if i+1 == len(path)-1:
                        break

                delay_p1 = 0.0
                delay_p2 = 0.0
                delay_p3 = 0.0
                for edge in p3:
                    delay_p3 += graph.cost[(edge[0], edge[1], 'delay')]

                new_route = Route(identifier, 'CN', path[-1], sequence3, p1, p2, p3, delay_p1, delay_p2, delay_p3)
                if new_route not in route_list:
                    route_list.append(new_route)
                    identifier += 1

    return route_list


def find_paths(link_file_path: str, node_file_path: str, route_file_path: str, is_ring_topology: bool):
    link_list = import_link_data(link_file_path)
    node_list = import_node_data(node_file_path)
    graph = _construct_graph(link_list, node_list, is_ring_topology)
    cr_dict = process_cr_dict(node_list)

    destination_list = []
    for node in node_list:
        if node.ru == 1 and node.cr not in destination_list:
            destination_list.append(node.cr)

    for destination in destination_list:
        graph.find_all_paths(0, destination)

    route_list = _process_routes(graph, cr_dict)
    print("{} route configurations successfully found!".format(len(route_list)))
    export_route_data(route_file_path, route_list)
