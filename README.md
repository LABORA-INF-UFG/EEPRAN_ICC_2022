# Bi-objective Optimization for Energy Efficiency and Centralization Level in Virtualized RAN

This repository contains the implementation of EEP-RAN model and the evaluation performed for the article **Bi-objective Optimization for Energy Efficiency and Centralization Level in Virtualized RAN** presented at [ICC 2022](https://icc2022.ieee-icc.org/).

- [Requirements](#Requirements)
- [Description](#Description)
- [Evaluation](#Evaluation)

## Requirements
EEP-RAN was tested in Ubuntu 20.10 (LTS) using Python 3.8.12, and the following packages are needed:
- docplex
- IBM CPLEX (20.10.0)
- matplotlib (3.5.1)

## Description
This model finds a set of pareto-optimal solutions for the placement of RAN VNFs that minimizes the energy consumption while maximizing the centralization of Network Functions.

In our evaluation we used two topologies, named Topology 1 (T1) and Topology 2 (T2) to compare different topology scenarios where Computing Resources (CR) are composed of different distributions of two computing platforms (H1 and H2).

<p align="center">
  <img src="https://github.com/LABORA-INF-UFG/EEPRAN_ICC_2022/blob/main/figures/topo_fig.png"/>
</p>

We used five settings in the evaluation: (i) Random, where the type of the CRs are randomly chosen from H1 and H2, independent of their locations; (ii) Only H1, where all CRs in the topology are of
type H1; (iii) Only H2, where all CRs in the topology are of type H2; (iv) H1 Root, where CRs close to the core are of type H1, while the others are of type H2; and (v) H2 Root, where CRs close to the core are of type H2 and the others are of type H1.

## Evaluation
The detailed steps performed for the evaluation can be found in the notebooks:
- [01_model.ipynb](01_model.ipynb) - the model is used find the solutions for each topology and setting
- [02_analysis.ipynb](02_analysis.ipynb) - provides a set of charts for a better visualization of the solutions

## EEP-RAN IEEE ICC 2022 Paper

More information can be found on [EEP-RAN IEEE ICC 2022 Paper](https://ieeexplore.ieee.org/document/9838898)

### Citation
```
@INPROCEEDINGS{pires:22,  
  author={Pires, William and de Almeida, Gabriel and Correa, Sand and Both, Cristiano and Pinto, Leizer and Cardoso, Kleber},  
  booktitle={ICC 2022 - IEEE International Conference on Communications},   
  title={Bi-objective Optimization for Energy Efficiency and Centralization Level in Virtualized RAN},   
  year={2022},  
  volume={},  
  number={},  
  pages={1034-1039},  
  doi={10.1109/ICC45855.2022.9838898}
}
```
