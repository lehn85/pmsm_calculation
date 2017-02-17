using System;
using System.Collections.Generic;
using System.Linq;

namespace calc_from_geometryOfMotor.motor
{
    public class Transient3PhaseMotorAnalyser : AbstractTransientAnalyser
    {
        // stator currents formular string (will be called on each step to update)
        public String IA { get; set; }
        public String IB { get; set; }
        public String IC { get; set; }

        protected override AbstractFEMResults NewResults()
        {
            return new Transient3PhaseMotorResults(this);
        }

        protected override AbstractFEMResults LoadResults(string fromfile)
        {
            return (Transient3PhaseMotorResults)Transient3PhaseMotorResults.loadResultsFromFile(typeof(Transient3PhaseMotorResults), fromfile);
        }

        // lock Lua statement processing so that only one thread is executing at one time
        private Object lockLua = new Object();

        protected override void DoModifyStator(TransientStepArgs args, FEMM femm)
        {
            // run script to update IA,IB,IC            
            NLua.Lua lua_state = LuaHelper.GetLuaState();
            String[] currentFormulas = { IA, IB, IC };
            String[] circuitNames = { "A", "B", "C" };

            lock (lockLua)
            {
                try
                {
                    lua_state["step"] = args.step;
                    lua_state["time"] = args.time;
                    lua_state["omega"] = RotorSpeedRadSecond;

                    for (int i = 0; i < currentFormulas.Length; i++)
                    {
                        double Ix = (double)lua_state.DoString("return " + currentFormulas[i])[0];
                        args.currents[circuitNames[i]] = Ix;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Lua error :" + ex.Message);
                }
            }

            base.DoModifyStator(args, femm);
        }

        #region Sample Config

        public static Transient3PhaseMotorAnalyser GetSampleMe(AbstractMotor motor)
        {
            Transient3PhaseMotorAnalyser ta = new Transient3PhaseMotorAnalyser();

            ta.AnalysisName = "Transient\\sample";
            ta.Motor = motor;
            ta.RotorSpeed = -3000;//rpm
            ta.EndTime = Math.Abs(120 / ta.RotorSpeedDegreeSecond);
            ta.StepCount = 120;
            ta.StartAngle = 40;
            ta.IA = "80";
            ta.IB = "-40";
            ta.IC = "-40";

            return ta;
        }

        #endregion
    }

    public class Transient3PhaseMotorResults : TransientResults
    {
        public Transient3PhaseMotorResults(AbstractTransientAnalyser analyser)
            : base(analyser)
        {
        }

        public Transient3PhaseMotorResults()
            : base()
        {
        }

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            IDictionary<String, object> dict = base.BuildResultsForDisplay();

            double[] psiAs = FluxLinkageOf("A");
            double[] psiBs = FluxLinkageOf("B");
            double[] psiCs = FluxLinkageOf("C");
            dict.Add("FluxLinkage(A)", new ListPointD(Times, psiAs));
            dict.Add("FluxLinkage(B)", new ListPointD(Times, psiBs));
            dict.Add("FluxLinkage(C)", new ListPointD(Times, psiCs));

            double[] iAs = CurrentOf("A");
            double[] iBs = CurrentOf("B");
            double[] iCs = CurrentOf("C");
            dict.Add("Current(A)", new ListPointD(Times, iAs));
            dict.Add("Current(B)", new ListPointD(Times, iBs));
            dict.Add("Current(C)", new ListPointD(Times, iCs));

            double[] uAs = VoltageOf("A");
            double[] uBs = VoltageOf("B");
            double[] uCs = VoltageOf("C");
            dict.Add("Voltage(A)", new ListPointD(Times, uAs));
            dict.Add("Voltage(B)", new ListPointD(Times, uBs));
            dict.Add("Voltage(C)", new ListPointD(Times, uCs));

            double[] eAs = InducedVoltageOf("A");
            double[] eBs = InducedVoltageOf("B");
            double[] eCs = InducedVoltageOf("C");
            dict.Add("InducedVoltage(A)", new ListPointD(Times, eAs));
            dict.Add("InducedVoltage(B)", new ListPointD(Times, eBs));
            dict.Add("InducedVoltage(C)", new ListPointD(Times, eCs));

            double[] eABs = Enumerable.Range(0, Count).Select(i => eAs[i] - eBs[i]).ToArray();
            double[] eBCs = Enumerable.Range(0, Count).Select(i => eBs[i] - eCs[i]).ToArray();
            double[] eCAs = Enumerable.Range(0, Count).Select(i => eCs[i] - eAs[i]).ToArray();
            dict.Add("InducedVoltage(A-B)", new ListPointD(Times, eABs));
            dict.Add("InducedVoltage(B-C)", new ListPointD(Times, eBCs));
            dict.Add("InducedVoltage(C-A)", new ListPointD(Times, eCAs));

            dict.Add("FluxLinkage(D)", new ListPointD(Times, FluxLinkage_D()));
            dict.Add("FluxLinkage(Q)", new ListPointD(Times, FluxLinkage_Q()));
            dict.Add("Current(D)", new ListPointD(Times, Current_D()));
            dict.Add("Current(Q)", new ListPointD(Times, Current_Q()));
            dict.Add("E(D)", new ListPointD(Times, InducedVoltage_D()));
            dict.Add("E(Q)", new ListPointD(Times, InducedVoltage_Q()));

            //double[] ud = Voltage_D();
            //double[] uq = Voltage_Q();
            //dict.Add("U(d)", new ListPointD(Times, ud));
            //dict.Add("U(q)", new ListPointD(Times, uq));

            int p = Analyser.Motor.Rotor.p;
            //// other calculation
            // torque
            double[] torqueabc = Enumerable.Range(0, Count)
                .Select(i => (eAs[i] * iAs[i] + eBs[i] * iBs[i] + eCs[i] * iCs[i]) / Analyser.RotorSpeedRadSecond)
                .ToArray();
            dict.Add("Torques(from abc)", new ListPointD(Times, torqueabc));

            double[] torquedq = Enumerable.Range(0, Count)
                .Select(i => 1.5 * p * (fluxlinkageDQ[i].d * currentDQ[i].q - fluxlinkageDQ[i].q * currentDQ[i].d))
                .ToArray();
            dict.Add("Torques(from dq)", new ListPointD(Times, torquedq));                                   

            double[] power_EIabc = Enumerable.Range(0, Count)
                .Select(i => eAs[i] * iAs[i] + eBs[i] * iBs[i] + eCs[i] * iCs[i])
                .ToArray();
            dict.Add("Power(from EIabc)", new ListPointD(Times, power_EIabc));

            double[] power_UIabc = Enumerable.Range(0, Count)
                .Select(i => (uAs[i] + eAs[i]) * iAs[i] + (uBs[i] + eBs[i]) * iBs[i] + (uCs[i] + eCs[i]) * iCs[i])
                .ToArray();
            dict.Add("Power(from UIabc)", new ListPointD(Times, power_UIabc));

            double[] power_Rabc = Enumerable.Range(0, Count)
                .Select(i => uAs[i] * iAs[i] + uBs[i] * iBs[i] + uCs[i] * iCs[i])
                .ToArray();
            dict.Add("Power(from Rabc)", new ListPointD(Times, power_Rabc));

            // power abc            
            dict.Add("WindingLoss", power_Rabc.Average());

            return dict;
        }

        #region Further conversion (abc)->(dq)

        // Fluxlinkage
        private Fdq[] fluxlinkageDQ;

        public Fdq[] FluxLinkageDQ()
        {
            if (fluxlinkageDQ != null)
                return fluxlinkageDQ;

            if (!ListResults[0].CircuitProperties.ContainsKey("A") ||
                !ListResults[0].CircuitProperties.ContainsKey("B") ||
                !ListResults[0].CircuitProperties.ContainsKey("C"))
                return null;
            var stator = Analyser.Motor.Stator as Stator3Phase;
            fluxlinkageDQ = new Fdq[ListResults.Length];
            for (int i = 0; i < ListResults.Length; i++)
            {
                double fA = ListResults[i].CircuitProperties["A"].fluxlinkage;
                double fB = ListResults[i].CircuitProperties["B"].fluxlinkage;
                double fC = ListResults[i].CircuitProperties["C"].fluxlinkage;
                double theta_e = (ListResults[i].RotorAngle - stator.VectorMMFAngle) * Analyser.Motor.Rotor.p * Math.PI / 180;

                fluxlinkageDQ[i] = ParkTransform.abc_dq(fA, fB, fC, theta_e);
            }

            return fluxlinkageDQ;
        }

        public double[] FluxLinkage_D()
        {
            Fdq[] psidq = FluxLinkageDQ();
            if (psidq == null)
                return null;

            return psidq.Select(p => p.d).ToArray();
        }

        public double[] FluxLinkage_Q()
        {
            Fdq[] psidq = FluxLinkageDQ();
            if (psidq == null)
                return null;

            return psidq.Select(p => p.q).ToArray();
        }

        // Current
        private Fdq[] currentDQ;

        public Fdq[] CurrentDQ()
        {
            if (currentDQ != null)
                return currentDQ;

            if (!ListResults[0].CircuitProperties.ContainsKey("A") ||
                !ListResults[0].CircuitProperties.ContainsKey("B") ||
                !ListResults[0].CircuitProperties.ContainsKey("C"))
                return null;

            currentDQ = new Fdq[ListResults.Length];
            var stator = Analyser.Motor.Stator as Stator3Phase;
            for (int i = 0; i < ListResults.Length; i++)
            {
                double fA = ListResults[i].CircuitProperties["A"].current;
                double fB = ListResults[i].CircuitProperties["B"].current;
                double fC = ListResults[i].CircuitProperties["C"].current;
                double theta_e = (ListResults[i].RotorAngle - stator.VectorMMFAngle) * Analyser.Motor.Rotor.p * Math.PI / 180;

                currentDQ[i] = ParkTransform.abc_dq(fA, fB, fC, theta_e);
            }
            return currentDQ;
        }

        public double[] Current_D()
        {
            Fdq[] Idq = CurrentDQ();
            if (Idq == null)
                return null;

            return Idq.Select(p => p.d).ToArray();
        }

        public double[] Current_Q()
        {
            Fdq[] Idq = CurrentDQ();
            if (Idq == null)
                return null;

            return Idq.Select(p => p.q).ToArray();
        }

        // voltage (on resistors)
        private Fdq[] voltageDQ;

        public Fdq[] VoltageDQ()
        {
            if (voltageDQ != null)
                return voltageDQ;

            if (!ListResults[0].CircuitProperties.ContainsKey("A") ||
                !ListResults[0].CircuitProperties.ContainsKey("B") ||
                !ListResults[0].CircuitProperties.ContainsKey("C"))
                return null;

            voltageDQ = new Fdq[ListResults.Length];
            var stator = Analyser.Motor.Stator as Stator3Phase;
            for (int i = 0; i < ListResults.Length; i++)
            {
                double fA = ListResults[i].CircuitProperties["A"].volts;
                double fB = ListResults[i].CircuitProperties["B"].volts;
                double fC = ListResults[i].CircuitProperties["C"].volts;
                double theta_e = (ListResults[i].RotorAngle - stator.VectorMMFAngle) * Analyser.Motor.Rotor.p * Math.PI / 180;

                voltageDQ[i] = ParkTransform.abc_dq(fA, fB, fC, theta_e);
            }
            return voltageDQ;
        }

        public double[] Voltage_D()
        {
            Fdq[] Vdq = VoltageDQ();
            if (Vdq == null)
                return null;

            return Vdq.Select(p => p.d).ToArray();
        }

        public double[] Voltage_Q()
        {
            Fdq[] Vdq = VoltageDQ();
            if (Vdq == null)
                return null;

            return Vdq.Select(p => p.q).ToArray();
        }

        // Induced voltage (dPsi/dt)
        private Fdq[] inducedVoltageDQ;

        public Fdq[] InducedVoltageDQ()
        {
            if (inducedVoltageDQ != null)
                return inducedVoltageDQ;

            if (!ListResults[0].CircuitProperties.ContainsKey("A") ||
                !ListResults[0].CircuitProperties.ContainsKey("B") ||
                !ListResults[0].CircuitProperties.ContainsKey("C"))
                return null;

            inducedVoltageDQ = new Fdq[ListResults.Length];
            var stator = Analyser.Motor.Stator as Stator3Phase;
            double[] eAs = InducedVoltageOf("A");
            double[] eBs = InducedVoltageOf("B");
            double[] eCs = InducedVoltageOf("C");

            for (int i = 0; i < ListResults.Length; i++)
            {
                double theta_e = (ListResults[i].RotorAngle - stator.VectorMMFAngle) * Analyser.Motor.Rotor.p * Math.PI / 180;

                inducedVoltageDQ[i] = ParkTransform.abc_dq(eAs[i], eBs[i], eCs[i], theta_e);
            }
            return inducedVoltageDQ;
        }

        public double[] InducedVoltage_D()
        {
            Fdq[] Edq = InducedVoltageDQ();
            if (Edq == null)
                return null;

            return Edq.Select(p => p.d).ToArray();
        }

        public double[] InducedVoltage_Q()
        {
            Fdq[] Edq = InducedVoltageDQ();
            if (Edq == null)
                return null;

            return Edq.Select(p => p.q).ToArray();
        }

        #endregion
    }
}
