from docplex.mp.constr import AbstractConstraint
from docplex.mp.model import Model
from core.data import *
from util.constants import Constants


def solve_eepran_model(model: Model, bottleneck: AbstractConstraint) -> list:
    """ Solves EEPRAN model using epsilon constraint technique.

    :returns: A list of pareto optimal solutions
    """
    pareto_set = []
    for iteration in range(0, Constants.MAX_ITERATIONS):
        print('-------------------------- iteration {} --------------------------'.format(iteration))
        solution = model.solve()

        # stop loop when problem turns infeasible
        if model.get_solve_details().status_code not in Constants.OPTIMAL_CODES:
            print('-------------------------- END --------------------------')
            print(model.get_solve_details())
            break

        # store the information about the new pareto optimal solution
        new_pareto = Pareto(iteration=iteration,
                            power_consumption=solution.get_objective_value(),
                            centralization=solution.get_value(bottleneck.left_expr),
                            solution=[key for key in model.x if model.x[key].solution_value != 0])

        # remove previous solution when dominated by the new one
        if any(pareto_set) and pareto_set[-1].power_consumption >= new_pareto.power_consumption:
            pareto_set.pop()

        pareto_set.append(new_pareto)
        # update epsilon constraint for the next iteration
        bottleneck.right_expr = new_pareto.centralization + 1
    return pareto_set


def solve_eepran_min_energy_model(model: Model, centralization: AbstractConstraint, identifier: int) -> list:
    solution_set = []
    solution = model.solve()

    if model.get_solve_details().status_code not in Constants.OPTIMAL_CODES:
        return []

    new_pareto = Pareto(iteration=identifier,
                        power_consumption=solution.get_objective_value(),
                        centralization=solution.get_value(centralization.left_expr),
                        solution=[key for key in model.x if model.x[key].solution_value != 0])

    solution_set.append(new_pareto)
    return solution_set
