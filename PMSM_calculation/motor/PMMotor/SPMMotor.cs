/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using fastJSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class SPMMotor : AbstractMotor
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
        public new SPMRotor Rotor
        {
            get
            {
                return (SPMRotor)base.Rotor;
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
            double Nz = Rotor.GammaM / 180 * Stator.Q / (2 * Rotor.p);
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
            double Hc = Rotor.Hc;
            //stator (may be use a general class to store general data)
            double L = GeneralParams.MotorLength;

            // magnet
            double wm = (Rotor.GammaM / 180 * 2 * Rotor.alpha) * (Rotor.RGap + Rotor.Rrotor) / 2;
            double lm = Rotor.ThickMag;

            // magnet parameters
            analyticalAnalyser.phiR = Br * wm * L * 1e-6;
            analyticalAnalyser.PM = mu_0 * mu_M * wm * L / lm * 1e-3;
            analyticalAnalyser.Fc = Hc * lm * 1e-3;

            // leakage path parameters
            analyticalAnalyser.phiHFe = 0;
            analyticalAnalyser.PFe = 0;
            analyticalAnalyser.Pb = 0;

            // some sizes (rotor)
            analyticalAnalyser.wm = wm;
            analyticalAnalyser.lm = lm;
            analyticalAnalyser.gammaM = Rotor.GammaM;

            // Airgap
            analyticalAnalyser.delta = Airgap.delta;
            analyticalAnalyser.wd = Rotor.GammaM / 180 * (Rotor.RGap + Airgap.delta / 2) * 2 * Math.PI / (2 * Rotor.p);
            analyticalAnalyser.Pd = mu_0 * analyticalAnalyser.wd * GeneralParams.MotorLength / (Airgap.delta * Airgap.Kc) * 1e-3;

            // Material (stator considering only)
            analyticalAnalyser.Barray = Stator.BH.Select(p => p.b).ToArray();
            analyticalAnalyser.Harray = Stator.BH.Select(p => p.h).ToArray();

            //other
            analyticalAnalyser.p = Rotor.p;
            analyticalAnalyser.Q = Stator.Q;
            analyticalAnalyser.Nstrand = Stator.NStrands;

            // magnet and airgap together
            double wPd = (2 * Rotor.alpha) * (Rotor.RGap + Rotor.Rrotor) / 2;//all arc
            double lPd = lm + Airgap.delta * Airgap.Kc;
            analyticalAnalyser.PMd = mu_0 * mu_M * wPd * L / lPd * 1e-3;
            analyticalAnalyser.PMq = analyticalAnalyser.PMd;            

            // set motor
            analyticalAnalyser.Motor = this;

            return analyticalAnalyser;
        }

        private SPMStaticAnalyser staticAnalyser;

        public override AbstractStaticAnalyser GetStaticAnalyser()
        {
            if (staticAnalyser == null)
                staticAnalyser = new SPMStaticAnalyser();
            staticAnalyser.Motor = this;
            return staticAnalyser;
        }

        #endregion

        #region DerivedParameters

        public override Dictionary<string, object> GetDerivedParameters()
        {
            var dict = base.GetDerivedParameters();

            // mass, volume            
            double wm = (Rotor.GammaM / 180 * 2 * Rotor.alpha) * (Rotor.RGap + Rotor.Rrotor) / 2;
            double Volume_Rotor = Rotor.RGap * Rotor.RGap * Math.PI * GeneralParams.MotorLength * 1e-9;
            double Volume_Slot_Total = Stator.Q * (Stator.BS1 + Stator.BS2) * (Stator.HS1 + Stator.HS2) / 2 * GeneralParams.MotorLength * 1e-9;
            double Volume_Magnet = 2 * Rotor.p * Rotor.ThickMag * wm * GeneralParams.MotorLength * 1e-9;
            double Volume_Copper_Total = Stator.Q * Stator.NStrands * (Stator.WireDiameter / 2) * (Stator.WireDiameter / 2) * Math.PI * GeneralParams.MotorLength * 1e-9;
            double Volume_Stator = (Stator.Rstator * Stator.Rstator - Stator.Rinstator * Stator.Rinstator) * Math.PI * GeneralParams.MotorLength * 1e-9 - Volume_Slot_Total;

            dict.Add("Volume Rotor", Volume_Rotor);
            dict.Add("Volume Stator", Volume_Stator);
            dict.Add("Volume Slot (Total) m3", Volume_Slot_Total);
            dict.Add("Volume Copper (Total) m3", Volume_Copper_Total);
            dict.Add("Volume Magnet (Total) m3", Volume_Magnet);
            dict.Add("Slot fill factor (%)", Volume_Copper_Total / Volume_Slot_Total * 100);

            double m_rotor = Volume_Rotor * Rotor.Steel_ro;
            double m_stator = Volume_Stator * Stator.Steel_ro;
            double m_copper = Volume_Copper_Total * Stator.Copper_ro;
            double m_magnet = Volume_Magnet * Rotor.Magnet_ro;
            dict.Add("Mass Rotor", m_rotor);
            dict.Add("Mass Stator", m_stator);
            dict.Add("Mass Copper", m_copper);
            dict.Add("Mass Magnet", m_magnet);
            dict.Add("Mass Total", m_rotor + m_stator + m_copper + m_magnet);

            return dict;
        }

        #endregion

        #region Default-Sample generation

        public static SPMMotor GetSampleMotor()
        {
            ////// all length is in mm
            //////
            SPMMotor m = new SPMMotor();

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
            SPMRotor rotor = new SPMRotor();

            // steel
            rotor.BH = BH;
            rotor.Lam_fill = 0.98;
            rotor.Lam_d = 0.635;
            rotor.Steel_ro = 7650;

            //magnet
            rotor.Hc = 883310;
            rotor.mu_M = 1.045;
            rotor.Magnet_ro = 7500;
            rotor.ThickMag = 6; // magnet length 

            rotor.GammaM = 133;
            rotor.DiaYoke = 32;
            rotor.p = 3; // pair poles                        
            rotor.DiaGap = 126 - 2; // airgap            
            rotor.Poletype = SPMRotor.PoleType.Normal;
            rotor.PreRotateAngle = 0;

            m.Rotor = rotor;

            return m;
        }

        #endregion
    }
}
