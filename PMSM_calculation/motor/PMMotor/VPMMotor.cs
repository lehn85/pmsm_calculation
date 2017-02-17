using fastJSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class VPMMotor : AbstractMotor
    {
        //3 phase stator
        public new Stator3Phase Stator
        {
            get
            {
                return (Stator3Phase)base.Stator;
            }
            set
            {
                base.Stator = value;
            }
        }

        // rotor with V-shaped permanent magnets
        public new VPMRotor Rotor
        {
            get
            {
                return (VPMRotor)base.Rotor;
            }
            set
            {
                base.Rotor = value;
            }

        }

        // Airgap that for this type of motor
        public new AirgapNormal Airgap
        {
            get
            {
                return (AirgapNormal)base.Airgap;
            }
            set
            {
                base.Airgap = value;
            }

        }

        #region Analyser attached to motor

        private PMAnalyticalAnalyser analyticalAnalyser;
        public override AbstractAnalyticalAnalyser GetAnalyticalAnalyser()
        {
            if (!isPointsCoordCalculated)
                CalcPointsCoordinates();

            if (analyticalAnalyser == null)
                analyticalAnalyser = new PMAnalyticalAnalyser();

            // General
            analyticalAnalyser.L = GeneralParams.MotorLength;

            // Stator            
            // count of teeth that have flux lines go through
            double Nz = Rotor.gammaMedeg / 180 * Stator.Q / (2 * Rotor.p);
            // teeth length
            double lz = Stator.HS0 + Stator.HS1 + Stator.HS2 + Stator.RS;
            // teeth width
            double wz = Stator.alpha * (2 * Stator.Rinstator + 2 * Stator.HS0 + 2 * Stator.HS1 + Stator.HS2) - (Stator.BS1 + Stator.BS2) / 2;
            // yoke width
            double wy = Stator.Rstator - Stator.xF;
            // yoke length
            double ly = 0.5 * (Stator.DiaYoke - wy) * Math.PI / (2 * Rotor.p);
            // slot width
            double wslot = 2 * Stator.Rinstator * Math.PI / Stator.Q - wz;

            Console.WriteLine("alphaM = " + (Rotor.alphaM * 180 / Math.PI * 2));

            analyticalAnalyser.Sz = Nz * wz * GeneralParams.MotorLength * 1e-6;
            analyticalAnalyser.Sy = 2 * wy * GeneralParams.MotorLength * 1e-6;
            analyticalAnalyser.lz = lz * 1e-3;
            analyticalAnalyser.ly = ly * 1e-3;
            analyticalAnalyser.Pslot = Constants.mu_0 * wslot * GeneralParams.MotorLength / lz * 1e-3;

            // Rotor
            /////assign for shorten variables
            //material
            double Br = Rotor.Br;
            double mu_0 = Constants.mu_0;
            double mu_M = Rotor.mu_M;
            double Bsat = Rotor.Bsat;
            double Hc = Rotor.Hc;
            //stator (may be use a general class to store general data)
            double L = GeneralParams.MotorLength;

            // magnet
            double wm = Rotor.WidthMag;
            double lm = Rotor.ThickMag;

            //calc sizes of barrier
            double wb1 = Rotor.HRib / 2;
            double lb1 = Math.Sqrt(Math.Pow(Rotor.xA - Rotor.xC, 2) + Math.Pow(Rotor.yA - Rotor.yC, 2));//AC
            if (lb1 < wb1)
                lb1 = wb1;
            double wb2 = Math.Sqrt(Math.Pow(Rotor.xB - Rotor.xE, 2) + Math.Pow(Rotor.yB - Rotor.yE, 2));//BE
            double lb2 = Rotor.B1;
            double wb3 = Math.Sqrt(Math.Pow(Rotor.xH - Rotor.xJ, 2) + Math.Pow(Rotor.yH - Rotor.yJ, 2));//GJ
            double lb3 = Rotor.B1;
            double wb4 = (Rotor.yJ + Rotor.yK) / 2;
            double lb4 = Rotor.xK - Rotor.xJ;

            //steel bridge
            double wFe = Rotor.Rrotor - Rotor.R1;
            double lFe = Math.Sqrt(Math.Pow(Rotor.xA - Rotor.xC, 2) + Math.Pow(Rotor.yA - Rotor.yC, 2));//AC
            if (lFe < wFe)
                lFe = wFe;
            double wFe2 = Rotor.yK + Rotor.yJ;
            double lFe2 = Rotor.xK - Rotor.xJ;

            //refine by poletype:
            if (Rotor.Poletype == VPMRotor.PoleType.MiddleAir)//middle is air then no steel
                wFe2 = 0;
            else wb4 = 0;//else, no air             

            // magnet parameters
            analyticalAnalyser.phiR = Br * wm * L * 1e-6;
            analyticalAnalyser.PM = mu_0 * mu_M * wm * L / lm * 1e-3;
            analyticalAnalyser.Fc = Hc * lm * 1e-3;

            // leakage path parameters
            analyticalAnalyser.phiHFe = Bsat * 2 * wFe * L * 1e-6;
            analyticalAnalyser.PFe = mu_0 * 2 * wFe * L / lFe * 1e-3;
            double Pb1 = mu_0 * wb1 * L / lb1;
            double Pb2 = mu_0 * wb2 * L / lb2;
            double Pb3 = mu_0 * wb3 * L / lb3;
            double Pb4 = mu_0 * wb4 * L / lb4;
            analyticalAnalyser.Pb = 2 * (Pb1 + Pb2 + Pb3 + Pb4) * 1e-3;

            // some sizes (rotor)
            analyticalAnalyser.wm = wm;
            analyticalAnalyser.lm = lm;
            analyticalAnalyser.wm2 = 2 * (Math.Sqrt(Math.Pow(Rotor.xC - Rotor.xK, 2) + Math.Pow(Rotor.yC - Rotor.yK, 2))) + 2 * wFe;

            // Airgap
            // calc Kc from other parameters
            //double tz = 2 * Stator.Rinstator * Math.PI / Stator.Q;
            //double bp = wslot;
            //double bp_on_delta = bp / Airgap.delta;
            //double cp = bp_on_delta * bp_on_delta / (5 + bp_on_delta);
            //double kc = tz / (tz - cp * Airgap.delta);            
            double kc = Airgap.Kc;

            analyticalAnalyser.delta = Airgap.delta;
            analyticalAnalyser.wd = Rotor.gammaMedeg / 180 * (Rotor.Rrotor + Airgap.delta / 2) * 2 * Math.PI / (2 * Rotor.p);
            analyticalAnalyser.Pd = Constants.mu_0 * analyticalAnalyser.wd * GeneralParams.MotorLength / (Airgap.delta * kc) * 1e-3;
            analyticalAnalyser.gammaM = Rotor.gammaMedeg;

            // Material (stator considering only)
            analyticalAnalyser.Barray = Stator.BH.Select(p => p.b).ToArray();
            analyticalAnalyser.Harray = Stator.BH.Select(p => p.h).ToArray();

            //other
            analyticalAnalyser.p = Rotor.p;
            analyticalAnalyser.Q = Stator.Q;
            analyticalAnalyser.Nstrand = Stator.NStrands;

            double wf = (180 - Rotor.gammaMedeg) / 180.0 * 2 * Rotor.alpha * Rotor.Rrotor;
            double pp = mu_0 * wf * L / lm * 1e-3;
            double Pdelta = analyticalAnalyser.Pd;// * 180 / Rotor.gammaM;
            analyticalAnalyser.PMd = 1 / (1 / (analyticalAnalyser.PM + analyticalAnalyser.Pb + pp) + 1 / Pdelta);
            analyticalAnalyser.PMq = Pdelta;

            // set motor
            analyticalAnalyser.Motor = this;

            return analyticalAnalyser;
        }

        private VPMStaticAnalyser staticAnalyser;
        public override AbstractStaticAnalyser GetStaticAnalyser()
        {
            if (staticAnalyser == null)
                staticAnalyser = new VPMStaticAnalyser();
            staticAnalyser.Motor = this;
            return staticAnalyser;
        }

        private PM_MMAnalyser mmAnalyser;
        public override AbstractMMAnalyser GetMMAnalyser()
        {
            if (mmAnalyser == null)
                mmAnalyser = PM_MMAnalyser.GetSampleMe(this);

            return mmAnalyser;
        }

        #endregion

        #region Properties

        public override double Volume
        {
            get
            {
                return Rotor.Volume + Stator.Volume;
            }
        }

        public override double Mass
        {
            get
            {
                return Rotor.Mass + Stator.Mass;
            }
        }

        #endregion

        #region DerivedParameters

        public override Dictionary<string, object> GetDerivedParameters()
        {
            var dict = base.GetDerivedParameters();

            dict.Add("AlphaM", Rotor.alphaMDegree * 2);
            dict.Add("GammaM", Rotor.gammaMedeg);

            dict.Add("airgap", Airgap.delta);

            // mass, volume            
            //double Volume_Rotor = Rotor.RGap * Rotor.RGap * Math.PI * GeneralParams.MotorLength * 1e-9;
            //double Volume_Slot_Total = Stator.Q * (Stator.BS1 + Stator.BS2) * (Stator.HS1 + Stator.HS2) / 2 * GeneralParams.MotorLength * 1e-9;
            //double Volume_Magnet = 2 * Rotor.p * Rotor.ThickMag * Rotor.WidthMag * GeneralParams.MotorLength * 1e-9;
            //double Volume_Copper_Total = Stator.Q * Stator.NStrands * (Stator.WireDiameter / 2) * (Stator.WireDiameter / 2) * Math.PI * GeneralParams.MotorLength * 1e-9;
            //double Volume_Stator = (Stator.Rstator * Stator.Rstator - Stator.Rinstator * Stator.Rinstator) * Math.PI * GeneralParams.MotorLength * 1e-9 - Volume_Slot_Total;

            dict.Add("Volume Rotor", Rotor.Volume);
            dict.Add("Volume Stator", Stator.Volume);
            dict.Add("Volume Slot (Total) m3", Stator.VolumeSlot);
            dict.Add("Volume Copper (Total) m3", Stator.VolumeCopper);
            dict.Add("Volume Magnet (Total) m3", Rotor.VolumeMagnet);
            dict.Add("Slot fill factor (%)", Stator.VolumeCopper / Stator.VolumeSlot * 100);

            //double m_rotor = Volume_Rotor * Rotor.Steel_ro;
            //double m_stator = Volume_Stator * Stator.Steel_ro;
            //double m_copper = Volume_Copper_Total * Stator.Copper_ro;
            //double m_magnet = Volume_Magnet * Rotor.Magnet_ro;
            dict.Add("Mass Rotor", Rotor.Mass);
            dict.Add("Mass Stator", Stator.Mass);
            dict.Add("Mass Copper", Stator.MassCopper);
            dict.Add("Mass Magnet", Rotor.MassMagnet);
            dict.Add("Mass Total", Mass);

            return dict;
        }

        #endregion

        #region Default-Sample generation

        public static VPMMotor GetSampleMotor()
        {
            ////// all length is in mm
            //////
            VPMMotor m = new VPMMotor();

            // general information
            GeneralParameters gp = new GeneralParameters();
            gp.MotorLength = 125;
            gp.FullBuildFEMModel = false;

            m.GeneralParams = gp;

            // materials                        
            //steel            
            PointBH[] BH = JSON.ToObject<List<PointBH>>(Properties.Resources.bhsample).ToArray();

            //stator
            Stator3Phase sp = new Stator3Phase();
            sp.Q = 36;
            sp.DiaYoke = 191;
            sp.DiaGap = 126;

            sp.HS0 = 0.5;
            sp.HS1 = 1;
            sp.HS2 = 13.3;
            sp.BS0 = 3;
            sp.BS1 = 5;
            sp.BS2 = 8.2;
            sp.RS = 0.5;

            sp.Kfill = 0.8;
            sp.WindingsConfig = "A:1-7,12-18,13-19,24-30,25-31,36-6;B:8-14,9-15,20-26,21-27,32-2,33-3;C:4-10,5-11,16-22,17-23,28-34,29-35";
            sp.NStrands = 10;
            //wire
            sp.WireConduct = 58;
            sp.WireType = FEMM.WireType.MagnetWire;
            sp.WireDiameter = 1.2;
            sp.Copper_ro = 8930;
            //steel
            sp.Lam_fill = 0.98;
            sp.Lam_d = 0.635;
            sp.BH = BH;
            sp.Steel_ro = 7650;
            sp.P_eddy_10_50 = 2.5;
            sp.P_hysteresis_10_50 = 0;

            m.Stator = sp;

            //airgap
            AirgapNormal airgap = new AirgapNormal();
            airgap.Kc = 1.1;
            m.Airgap = airgap;

            //rotor
            VPMRotor rotor = new VPMRotor();

            // steel
            rotor.BH = BH;
            rotor.Lam_fill = 0.98;
            rotor.Lam_d = 0.635;
            rotor.Bsat = 1.9;
            rotor.Steel_ro = 7650;

            //magnet
            rotor.Hc = 883310;
            rotor.mu_M = 1.045;
            rotor.Magnet_ro = 7500;

            rotor.ThickMag = 6; // magnet length
            rotor.B1 = 5;
            rotor.p = 3; // pair poles                        
            rotor.DiaGap = 126 - 2 * 0.6; // airgap
            rotor.D1 = 124;
            rotor.O1 = 2;
            rotor.O2 = 30;
            rotor.Dminmag = 5;
            rotor.WidthMag = 42;
            rotor.DiaYoke = 32;
            rotor.Rib = 10;
            rotor.HRib = 2;
            rotor.Poletype = VPMRotor.PoleType.MiddleSteelBridgeRectangle;

            m.Rotor = rotor;

            return m;
        }

        #endregion
    }
}
