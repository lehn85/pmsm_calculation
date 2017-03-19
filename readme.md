# PMSM calculation
Program helps designing permanent magnetic synchronous motor. I wrote this program to aid me in designing PMSM for electric car in my PhD dissertation. 
Althought the code is kind of messy, it works.

### Features:
- Analytical calculation
- Use program FEMM (http://www.femm.info) for finite elements analysis
- Optimization using genetic algorithm for searching optimum design
- Static analyze motor (rotor stationary)
- Dynamic analyze motor (rotor rotates, current flows defined by functions)
- Build effeciency, losses maps
- Export calculated model of PMSM to Matlab-Simulink as a block
- Export to Advisor (http://adv-vehicle-sim.sourceforge.net), which is a program for car simulations.

### Dependencies:
- Math.NET numerics https://numerics.mathdotnet.com/
- Mathnet.Numerics.Optimization, which is not from Math.NET master branch, I copied from a fork, and I don't remember which. Sorry.
- OxyPlot http://www.oxyplot.org/
- ZedGraph https://github.com/ZedGraph/ZedGraph

Program uses ActiveX component to connect to FEMM and Matlab, which can be found on their sites.

### Videos
https://www.youtube.com/watch?v=aQfktDAtQ60

### Images
<img src="/PMSM_calculation/captures/general.png" alt="General" width="50%"/>
<img src="/PMSM_calculation/captures/optimization-window-3.png" alt="Optimization: Pareto front" width="50%"/>
<img src="/PMSM_calculation/captures/coreloss-map.png" alt="Coreloss map" width="50%"/>
<img src="/PMSM_calculation/captures/eff-map.png" alt="Efficiency map" width="50%"/>