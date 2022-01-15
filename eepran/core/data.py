import json
from typing import List
from collections import namedtuple, defaultdict

ComputingCore = namedtuple('ComputingCore', 'cr_dict ru_list')
Ru = namedtuple('Ru', 'identifier associated_cr')
Hardware = namedtuple('Hardware', 'identifier cr cpu static_power dynamic_power')
VarKey = namedtuple('VarKey', 'route drc ru')
Pareto = namedtuple('Pareto', 'iteration power_consumption centralization solution')
Sequence = namedtuple('Sequence', 'cr hw')


class Link:
    def __init__(self, capacity: int, delay: float, from_node: int, to_node: int):
        self.capacity = capacity
        self.delay = delay
        self.from_node = from_node
        self.to_node = to_node


class Node:
    def __init__(self, number: int, cr: int, cpu: int, model: str, tdp: float, static_percentage: float, ru: int):
        self.number = number
        self.cr = cr
        self.cpu = cpu
        self.model = model
        self.tdp = tdp
        self.static_percentage = static_percentage
        self.ru = ru


class Route:
    def __init__(self, identifier: int, source: str, target: int, sequence: list, p1: list,
                 p2: list, p3: list, delay_p1: float, delay_p2: float, delay_p3: float):
        self.identifier = identifier
        self.source = source
        self.target = target
        self.sequence = sequence
        self.p1 = p1
        self.p2 = p2
        self.p3 = p3
        self.delay_p1 = delay_p1
        self.delay_p2 = delay_p2
        self.delay_p3 = delay_p3

    def __hash__(self):
        hash_key = [j for i in (self.p1 + self.p2 + self.p3 + self.sequence) for j in i]
        return hash(frozenset(hash_key))

    def __eq__(self, other):
        return self.p1 == other.p1 and self.p2 == other.p2 and self.p3 == other.p3 and self.sequence == other.sequence

    def qty_cr(self) -> int:
        """ :returns: An int representing the amount of CR's available on the route """
        if self.sequence[0] != [0, 0]:
            return 3
        elif self.sequence[1] != [0, 0]:
            return 2
        else:
            return 1

    def contains(self, hw: Hardware) -> bool:
        """ Check if Hardware hw is part of the route. """
        return [hw.cr, hw.identifier] in self.sequence

    def is_cu(self, hw: Hardware) -> bool:
        """ Check if Hardware hw is CU in the route. """
        return [hw.cr, hw.identifier] == self.sequence[0]

    def is_du(self, hw: Hardware) -> bool:
        """ Check if Hardware hw is DU in the route. """
        return [hw.cr, hw.identifier] == self.sequence[1]

    def is_ru(self, hw: Hardware) -> bool:
        """ Check if Hardware hw is RU in the route. """
        return [hw.cr, hw.identifier] == self.sequence[2]

    def is_destination(self, ru: Ru) -> bool:
        """ Check if Ru ru is the destination of the route. """
        return self.target == ru.associated_cr


class Drc:
    def __init__(self, identifier: int, cu_cpu_usage: float, du_cpu_usage: float, ru_cpu_usage: float,
                 fs_cu: list, fs_du: list, fs_ru: list, delay_bh: float, delay_mh: float, delay_fh: float,
                 bandwidth_bh: float, bandwidth_mh: float, bandwidth_fh: float, qty_cr: int):
        self.identifier = identifier
        self.cu_cpu_usage = cu_cpu_usage
        self.du_cpu_usage = du_cpu_usage
        self.ru_cpu_usage = ru_cpu_usage
        self.fs_cu = fs_cu
        self.fs_du = fs_du
        self.fs_ru = fs_ru
        self.delay_bh = delay_bh
        self.delay_mh = delay_mh
        self.delay_fh = delay_fh
        self.bandwidth_bh = bandwidth_bh
        self.bandwidth_mh = bandwidth_mh
        self.bandwidth_fh = bandwidth_fh
        self.qty_cr = qty_cr

    def __hash__(self):
        return hash(self.identifier)

    def __eq__(self, other):
        return self.identifier == other.identifier


def import_link_data(link_file_path: str) -> list:
    link_list = []
    with open(link_file_path) as link_file:
        raw_data = json.load(link_file)
        list_data = raw_data['LinkList']
        for data in list_data:
            link_list.append(Link(data['Capacity'],
                                  data['Delay'],
                                  data['FromNode'],
                                  data['ToNode']))
    return link_list


def import_node_data(node_file_path: str) -> list:
    node_list = []
    with open(node_file_path) as link_file:
        raw_data = json.load(link_file)
        list_data = raw_data['NodeList']
        for data in list_data:
            node_list.append(Node(data['Number'],
                                  data['Cr'],
                                  data['Cpu'],
                                  data['Model'],
                                  data['Tdp'],
                                  data['StaticPercentage'],
                                  data['Ru']))
    return node_list


def import_route_data(route_file_path: str) -> list:
    route_list = []
    with open(route_file_path) as route_file:
        raw_data = json.load(route_file)
        dict_data = raw_data['PathList']
        for data_key in dict_data:
            data = dict_data[data_key]
            route_list.append(Route(data['Id'],
                                    data['Source'],
                                    data['Target'],
                                    data['Seq'],
                                    data['P1'],
                                    data['P2'],
                                    data['P3'],
                                    data['DelayP1'],
                                    data['DelayP2'],
                                    data['DelayP3']))
    return route_list


def export_route_data(route_file_path: str, route_list: list):
    route_data_list = {}
    for route in route_list:
        route_data = {"Id": route.identifier,
                      "Source": route.source,
                      "Target": route.target,
                      "Seq": route.sequence,
                      "P1": route.p1,
                      "P2": route.p2,
                      "P3": route.p3,
                      "DelayP1": route.delay_p1,
                      "DelayP2": route.delay_p2,
                      "DelayP3": route.delay_p3}
        route_data_list["path-{}".format(route.identifier)] = route_data
    data = {"PathList": route_data_list}
    with open(route_file_path, 'w') as path_file:
        json.dump(data, path_file, indent=2)


def export_solution_data(solution_file_path: str, pareto_set: list):
    solution_text = ''
    consumption_set = []
    centralization_set = []
    for pareto in pareto_set:
        consumption_set.append(pareto.power_consumption)
        centralization_set.append(pareto.centralization)

    solution_text += 'Consumption    set = {}\n'.format(str(consumption_set))
    solution_text += 'centralization set = {}\n'.format(str(centralization_set))

    for pareto in pareto_set:
        solution_text += '\n-------- Iteration {} --------\n'.format(str(pareto.iteration))
        solution_text += 'Objective      value: {}\n'.format(str(pareto.power_consumption))
        solution_text += 'Centralization value: {}\n'.format(str(pareto.centralization))
        solution_text += 'Solution:\n'
        for key in pareto.solution:
            var_key: VarKey = key
            solution_text += '    RU {} uses DRC {} on route ({}, {}, {})\n'.format(var_key.ru.identifier,
                                                                                    var_key.drc.identifier,
                                                                                    var_key.route.sequence[0],
                                                                                    var_key.route.sequence[1],
                                                                                    var_key.route.sequence[2])
    with open(solution_file_path, 'w') as solution_file:
        solution_file.write(solution_text)


def process_min_energy_solution_data(solution_set: list) -> str:
    solution_text = ''
    consumption_set = []
    centralization_set = []
    for solution in solution_set:
        consumption_set.append(solution.power_consumption)
        centralization_set.append(solution.centralization)

    solution_text += 'Consumption    set = {}\n'.format(str(consumption_set))
    solution_text += 'centralization set = {}\n'.format(str(centralization_set))

    for solution in solution_set:
        solution_text += '\n-------- Centralization {}% --------\n'.format(str(solution.iteration))
        solution_text += 'Objective      value: {:.2f}\n'.format(solution.power_consumption)
        solution_text += 'Centralization value: {:.2f}\n'.format(solution.centralization)
        solution_text += 'Centralization percentage: {:.2f}%\n'.format(solution.centralization / 154 * 100)
        hws = []
        crs = []
        for key in solution.solution:
            var_key: VarKey = key
            for node in var_key.route.sequence:
                hws.append(node[1])
                crs.append(node[0])
        solution_text += 'Hardware Count: {}\n'.format(len(set(hws)))
        solution_text += 'CR Count: {}\n'.format(len(set(crs)))

    return solution_text


def process_cr_dict(node_list: list):
    cr_dict = defaultdict(list)
    for node in node_list:
        cr_dict[node.cr].append(node)
    return cr_dict


def process_computing_core(node_list: list) -> ComputingCore:
    cr_dict = defaultdict(list)
    ru_list = []
    identifier = 1
    for node in node_list:
        cr_dict[node.cr].append(Hardware(identifier=node.number,
                                         cr=node.cr,
                                         cpu=node.cpu,
                                         static_power=(node.tdp * node.static_percentage),
                                         dynamic_power=(node.tdp * (1 - node.static_percentage))
                                         ))
        if node.ru == 1:
            ru_list.append(Ru(identifier=identifier,
                              associated_cr=node.cr))
            identifier += 1
    return ComputingCore(cr_dict=cr_dict, ru_list=ru_list)