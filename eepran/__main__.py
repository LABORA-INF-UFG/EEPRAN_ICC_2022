import time
import core.data
import core.path
import core.model
import core.solver


def main():
    link_path = 'topologies/power_test_topo_25_anel_links.json'
    path_path = 'topologies/power_test_topo_25_anel_routes.json'
    node1_path = 'topologies/power_test_topo_25_nodes.json'
    node2_path = 'topologies/power_test_topo_25_nodes_2.json'
    node3_path = 'topologies/power_test_topo_25_nodes_3.json'
    node4_path = 'topologies/power_test_topo_25_nodes_4.json'
    node5_path = 'topologies/power_test_topo_25_nodes_5.json'

    core.path.find_paths(link_path, node1_path, path_path, is_ring_topology=True)

    text = ''

    time_start = time.time()
    model, bottleneck = core.model.build_eepran_model(link_path, node1_path, path_path, is_ring_topology=True)
    pareto_set = core.solver.solve_eepran_model(model, bottleneck)
    core.data.export_solution_data('solutions/power_test_topo_25_hier_n1_l2.txt', pareto_set)
    time_end = time.time()
    text += 'teste_1: {}'.format(time_end - time_start)

    # time_start = time.time()
    # model, bottleneck = core.model.build_eepran_model(link_path, node2_path, path_path, is_ring_topology=False)
    # pareto_set = core.solver.solve_eepran_model(model, bottleneck)
    # core.data.export_solution_data('solutions/power_test_topo_25_hier_n2_l2.txt', pareto_set)
    # time_end = time.time()
    # text += ' - teste_2: {}'.format(time_end - time_start)

    # time_start = time.time()
    # model, bottleneck = core.model.build_eepran_model(link_path, node3_path, path_path, is_ring_topology=False)
    # pareto_set = core.solver.solve_eepran_model(model, bottleneck)
    # core.data.export_solution_data('solutions/power_test_topo_25_hier_n3_l2.txt', pareto_set)
    # time_end = time.time()
    # text += ' - teste_3: {}'.format(time_end - time_start)

    # time_start = time.time()
    # model, bottleneck = core.model.build_eepran_model(link_path, node4_path, path_path, is_ring_topology=False)
    # pareto_set = core.solver.solve_eepran_model(model, bottleneck)
    # core.data.export_solution_data('solutions/power_test_topo_25_hier_n4_l2.txt', pareto_set)
    # time_end = time.time()
    # text += ' - teste_4: {}'.format(time_end - time_start)
    #
    # time_start = time.time()
    # model, bottleneck = core.model.build_eepran_model(link_path, node5_path, path_path, is_ring_topology=False)
    # pareto_set = core.solver.solve_eepran_model(model, bottleneck)
    # core.data.export_solution_data('solutions/power_test_topo_25_hier_n5_l2.txt', pareto_set)
    # time_end = time.time()
    # text += ' - teste_5: {}'.format(time_end - time_start)

    print(text)


if __name__ == '__main__':
    main()
