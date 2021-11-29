from collections import defaultdict

from docplex.mp.constr import AbstractConstraint
from docplex.mp.model import Model
from core.data import *
import docplex


def _generate_drc_list() -> list:
    return [Drc(identifier=1, cu_cpu_usage=0.49, du_cpu_usage=2.058, ru_cpu_usage=2.352,
                fs_cu=['f8'], fs_du=['f7', 'f6', 'f5', 'f4', 'f3', 'f2'], fs_ru=['f1', 'f0'],
                delay_bh=10.0, delay_mh=10.0, delay_fh=0.25,
                bandwidth_bh=9.9, bandwidth_mh=13.2, bandwidth_fh=42.6, qty_cr=3),
            Drc(identifier=2, cu_cpu_usage=0.98, du_cpu_usage=1.568, ru_cpu_usage=2.352,
                fs_cu=['f8', 'f7'], fs_du=['f6', 'f5', 'f4', 'f3', 'f2'], fs_ru=['f1', 'f0'],
                delay_bh=10.0, delay_mh=10.0, delay_fh=0.25,
                bandwidth_bh=9.9, bandwidth_mh=13.2, bandwidth_fh=42.6, qty_cr=3),
            Drc(identifier=4, cu_cpu_usage=0.49, du_cpu_usage=1.225, ru_cpu_usage=3.185,
                fs_cu=['f8'], fs_du=['f7', 'f6', 'f5', 'f4', 'f3'], fs_ru=['f2', 'f1', 'f0'],
                delay_bh=10.0, delay_mh=10.0, delay_fh=0.25,
                bandwidth_bh=9.9, bandwidth_mh=13.2, bandwidth_fh=13.6, qty_cr=3),
            Drc(identifier=5, cu_cpu_usage=0.98, du_cpu_usage=0.735, ru_cpu_usage=3.185,
                fs_cu=['f8', 'f7'], fs_du=['f6', 'f5', 'f4', 'f3'], fs_ru=['f2', 'f1', 'f0'],
                delay_bh=10.0, delay_mh=10.0, delay_fh=0.25,
                bandwidth_bh=9.9, bandwidth_mh=13.2, bandwidth_fh=13.6, qty_cr=3),

            Drc(identifier=6, cu_cpu_usage=0.0, du_cpu_usage=0.49, ru_cpu_usage=4.41,
                fs_cu=[], fs_du=['f8'], fs_ru=['f7', 'f6', 'f5', 'f4', 'f3', 'f2', 'f1', 'f0'],
                delay_bh=0.0, delay_mh=10.0, delay_fh=10.0,
                bandwidth_bh=0.0, bandwidth_mh=9.9, bandwidth_fh=13.2, qty_cr=2),
            Drc(identifier=7, cu_cpu_usage=0.0, du_cpu_usage=0.98, ru_cpu_usage=3.92,
                fs_cu=[], fs_du=['f8', 'f7'], fs_ru=['f6', 'f5', 'f4', 'f3', 'f2', 'f1', 'f0'],
                delay_bh=0.0, delay_mh=10.0, delay_fh=10.0,
                bandwidth_bh=0.0, bandwidth_mh=9.9, bandwidth_fh=13.2, qty_cr=2),
            Drc(identifier=9, cu_cpu_usage=0.0, du_cpu_usage=2.54, ru_cpu_usage=2.354,
                fs_cu=[], fs_du=['f8', 'f7', 'f6', 'f5', 'f4', 'f3', 'f2'], fs_ru=['f1', 'f0'],
                delay_bh=0.0, delay_mh=10.0, delay_fh=0.25,
                bandwidth_bh=0.0, bandwidth_mh=9.9, bandwidth_fh=42.6, qty_cr=2),
            Drc(identifier=10, cu_cpu_usage=0.0, du_cpu_usage=1.71, ru_cpu_usage=3.185,
                fs_cu=[], fs_du=['f8', 'f7', 'f6', 'f5', 'f4', 'f3'], fs_ru=['f2', 'f1', 'f0'],
                delay_bh=0.0, delay_mh=10.0, delay_fh=0.25,
                bandwidth_bh=0.0, bandwidth_mh=3.0, bandwidth_fh=13.6, qty_cr=2),

            Drc(identifier=8, cu_cpu_usage=0.0, du_cpu_usage=0.0, ru_cpu_usage=4.9,
                fs_cu=[], fs_du=[], fs_ru=['f8', 'f7', 'f6', 'f5', 'f4', 'f3', 'f2', 'f1', 'f0'],
                delay_bh=0.0, delay_mh=0.0, delay_fh=10.0,
                bandwidth_bh=0.0, bandwidth_mh=0.0, bandwidth_fh=9.9, qty_cr=1)]


def _generate_fs_list() -> dict:
    return {'f0': 1.176, 'f1': 1.176, 'f2': 0.833, 'f3': 0.343, 'f4': 0.343,
            'f5': 0.0245, 'f6': 0.0245, 'f7': 0.49, 'f8': 0.49}


def _process_computing_core(node_list: list) -> ComputingCore:
    cr_list = []
    ru_list = []
    identifier = 1
    for node in node_list:
        cr_list.append(Cr(identifier=node.number,
                          cpu=node.cpu,
                          static_power=(node.tdp * node.static_percentage),
                          dynamic_power=(node.tdp * (1 - node.static_percentage))
                          ))
        if node.ru == 1:
            ru_list.append(Ru(identifier=identifier,
                              associated_cr=node.number))
            identifier += 1
    return ComputingCore(crs=cr_list, rus=ru_list)


def _min_max(value1: int, value2: int) -> (int, int):
    if value1 < value2:
        return value1, value2
    else:
        return value2, value1


def build_eepran_model(link_file_path: str, node_file_path: str,
                       route_file_path: str, is_ring_topology: bool) -> {Model, AbstractConstraint}:
    model = Model(name='EEP-Ran Problem', log_output=True)

    # -----------
    # Define Data
    # -----------

    node_list = import_node_data(node_file_path)
    link_list = import_link_data(link_file_path)
    route_list = import_route_data(route_file_path)

    drc_list = _generate_drc_list()
    fs_list = _generate_fs_list()

    computing_core = _process_computing_core(node_list)

    # --------------------------
    # Define Decision Variable X
    # --------------------------

    # list that contains a key for every possible variable
    var_keys = [VarKey(route=route, drc=drc, ru=ru)
                for route in route_list
                for drc in drc_list
                for ru in computing_core.rus
                if route.sequence[2] == ru.associated_cr
                and drc.qty_cr == route.qty_cr()]
    model.x = model.binary_var_dict(
        keys=var_keys, name=lambda vk: 'x_p{}_d{}_b{}'.format(vk[0].identifier, vk[1].identifier, vk[2].identifier)
    )

    # -------------------------
    # Define Objective Function
    # -------------------------

    dynamic_power_function = model.linear_expr()
    static_power_function = model.linear_expr()
    cr: Cr
    for cr in computing_core.crs:
        # Do not count the core
        if cr.identifier == 0:
            continue

        psi_1 = model.linear_expr()
        for fs in fs_list:
            dynamic_power_function += model.sum(model.x[key] * fs_list[fs] * cr.dynamic_power / cr.cpu
                                                for key in var_keys
                                                if cr.identifier in key.route.sequence and (
                                                (cr.identifier == key.route.sequence[0] and fs in key.drc.fs_cu) or
                                                (cr.identifier == key.route.sequence[1] and fs in key.drc.fs_du) or
                                                (cr.identifier == key.route.sequence[2] and fs in key.drc.fs_ru)))
            psi_1 += model.sum(model.x[key]
                               for key in var_keys
                               if cr.identifier in key.route.sequence and (
                               (cr.identifier == key.route.sequence[0] and fs in key.drc.fs_cu) or
                               (cr.identifier == key.route.sequence[1] and fs in key.drc.fs_du) or
                               (cr.identifier == key.route.sequence[2] and fs in key.drc.fs_ru)))
        static_power_function += model.sum(model.min(1, psi_1) * cr.static_power)
    model.minimize(static_power_function + dynamic_power_function)

    # --------------------------------
    # Define Centralization Constraint
    # --------------------------------

    centralization = model.linear_expr()
    for cr in computing_core.crs:
        # Do not count the core
        if cr.identifier == 0:
            continue

        # Sums BS's running the same FS in a single CR
        for fs in fs_list:
            psi_2 = model.sum(model.x[key]
                              for key in var_keys
                              if cr.identifier in key.route.sequence and (
                              (cr.identifier == key.route.sequence[0] and fs in key.drc.fs_cu) or
                              (cr.identifier == key.route.sequence[1] and fs in key.drc.fs_du) or
                              (cr.identifier == key.route.sequence[2] and fs in key.drc.fs_ru)))
            centralization += model.sum(psi_2 - model.min(1, psi_2))
    centralization_constraint = model.add_constraint(centralization >= 0, 'centralization constraint')

    # ------------------------------
    # Define Single Route Constraint
    # ------------------------------

    # each bs must use a single route/drc combination
    for ru in computing_core.rus:
        model.add_constraint(
            model.sum(model.x[key] for key in var_keys if key.ru == ru) == 1, 'single route constraint'
        )

    # -------------------------------
    # Define Link Capacity Constraint
    # -------------------------------

    capacity_expressions = {}
    # define a expression for each link
    for link in link_list:
        source, destination = _min_max(link.from_node, link.to_node)
        capacity_expressions[(source, destination)] = model.linear_expr()

    # sums every drc bandwidth load flowing through a link
    for key in var_keys:
        for link in key.route.p1:
            source, destination = _min_max(link[0], link[1])
            capacity_expressions[(source, destination)].add_term(model.x[key], key.drc.bandwidth_bh)

        for link in key.route.p2:
            source, destination = _min_max(link[0], link[1])
            capacity_expressions[(source, destination)].add_term(model.x[key], key.drc.bandwidth_mh)

        for link in key.route.p3:
            source, destination = _min_max(link[0], link[1])
            capacity_expressions[(source, destination)].add_term(model.x[key], key.drc.bandwidth_fh)

    # the load on every link must not exceed its capacity
    for link in link_list:
        source, destination = _min_max(link.from_node, link.to_node)
        model.add_constraint(capacity_expressions[(source, destination)] <= link.capacity, 'link capacity constraint')

    # ----------------------------
    # Define Link Delay Constraint
    # ----------------------------

    # delay on link must not exceed the drc requirements
    for key in var_keys:
        if key.route.qty_cr() == 3:
            model.add_constraint((model.x[key] * key.route.delay_p1) <= key.drc.delay_bh, 'link delay constraint on bh')
        if key.route.qty_cr() >= 2:
            model.add_constraint((model.x[key] * key.route.delay_p2) <= key.drc.delay_mh, 'link delay constraint on mh')
        model.add_constraint((model.x[key] * key.route.delay_p3) <= key.drc.delay_fh, 'link delay constraint on fh')

    # ------------------------------
    # Processing Capacity Constraint
    # ------------------------------

    for cr in computing_core.crs:
        # Do not count the core
        if cr.identifier == 0:
            continue

        # sums the load of all FS's running on the CR
        processing_expression = model.linear_expr()
        for fs in fs_list:
            processing_expression += model.sum(model.x[key] * fs_list[fs]
                                               for key in var_keys
                                               if cr.identifier in key.route.sequence and (
                                               (cr.identifier == key.route.sequence[0] and fs in key.drc.fs_cu) or
                                               (cr.identifier == key.route.sequence[1] and fs in key.drc.fs_du) or
                                               (cr.identifier == key.route.sequence[2] and fs in key.drc.fs_ru)))
        model.add_constraint(processing_expression <= cr.cpu, 'processing capacity constraint')

    return model, centralization_constraint
