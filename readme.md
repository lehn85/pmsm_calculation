# PMSM calculation
Program helps designing permanent magnetic synchronous motor. I wrote this program to aid me in designing PMSM for electric car in my PhD dissertation. 

Features:
- Analytical calculation
- Link with FEMM for finite elements analysis
- Optimization using genetic algorithm for searching optimum design
- Static analyze motor (rotor stationary)
- Dynamic analyze motor (rotor rotates, current flows defined by functions)
- Build effeciency, losses maps
- Export calculated model of PMSM to Matlab-Simulink as a block
- Export to Advisor (program for analyzing dynamic characteristic of cars)

Althought the code is a mess, it works.