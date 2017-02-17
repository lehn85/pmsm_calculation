using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace calc_from_geometryOfMotor
{
    public class Geo_PMSM_MagnetVShape
    {                      
        #region Params

        public class StatorParameters
        {
            // general params 
            public double L { get; set; }//rotor length            

            //Stator only parameters
            public int q { get; set; }//slot per phase per pole
            public int Q { get; set; }//slot count            
            public double Dstator { get; set; }
            public double Dinstator { get; set; }
            public double Rinstator { get; set; }
            public double wz { get; set; }//teeth width
            public double lz { get; set; }//teeth length
            public double wy { get; set; }//yoke width
            public double ly { get; set; }//yoke length
            public double wslot { get; set; }//slot width
            public int Nz { get; set; }//teeth count per pole            
        }

        /// <summary>
        /// Initial parameters of rotor to calculate others
        /// With predefined stator, now make a rotor with initial parameters
        /// </summary>        
        public class RotorParameters
        {
            public double Rrotor { get; set; }
            public double wFe { get; set; }
            public double wFe2 { get; set; }
            public double wb2min { get; set; }
            public double alphaM { get; set; }
            public int p { get; set; }//pair of poles
            public double lm { get; set; }//magnet length            
        }

        public class MaterialParameters
        {
            // steel characteristic
            public double[] B { get; set; }
            public double[] H { get; set; }
            public double Bsat { get; set; }//value of flux density, which is considered saturated

            // magnet parameters
            public double Hc { get; set; }
            public double mu_M { get; set; }
            public readonly double mu_0 = 4 * Math.PI * 1e-7;
            public double Br { get; set; }
        }

        public class Coefficients
        {
            public double Kc { get; set; }//Coefficient carter    
        }

        public RotorParameters RotorParams { get; private set; }
        public void setRotorParameters(RotorParameters r)
        {
            RotorParams = r;
        }

        public MaterialParameters MaterialParams { get; private set; }
        public void setMaterialData(MaterialParameters d)
        {
            MaterialParams = d;
            if (MaterialParams.Hc == 0)
                MaterialParams.Hc = MaterialParams.Br / (MaterialParams.mu_M * MaterialParams.mu_0);
            else if (MaterialParams.Br == 0)
                MaterialParams.Br = MaterialParams.Hc * MaterialParams.mu_0 * MaterialParams.mu_M;
        }

        public StatorParameters StatorParams { get; private set; }
        public void setStatorParameters(StatorParameters s)
        {
            StatorParams = s;
            if (StatorParams.Dinstator == 0)
                StatorParams.Dinstator = StatorParams.Rinstator * 2;
            else if (StatorParams.Rinstator == 0)
                StatorParams.Rinstator = StatorParams.Dinstator / 2;
        }

        public Coefficients Coeffs { get; private set; }
        public void setCoefficients(Coefficients c)
        {
            Coeffs = c;
        }

        #endregion

        #region CalcPointsCoordinates

        private bool isPointsCoordCalculated = false;

        // magnets, barriers points
        public double xA, yA, xB, yB, xC, yC, xD, yD, xF, yF;

        // rotor arc points
        public double xG, yG;

        protected void CalcPointsCoordinates()
        {
            if (StatorParams == null)
                throw new ArgumentNullException("StatorParams null");
            if (RotorParams == null)
                throw new ArgumentNullException("RotorParams null");
            if (MaterialParams == null)
                throw new ArgumentNullException("StatorParams null");

            // just shorten variable            
            double Rinstator = StatorParams.Rinstator;
            double Rrotor = RotorParams.Rrotor;
            double lm = RotorParams.lm;
            double wFe = RotorParams.wFe;
            double alphaM = RotorParams.alphaM;
            double wFe2 = RotorParams.wFe2;
            double wb2min = RotorParams.wb2min;

            // radius of rotor
            double delta = Rinstator - Rrotor;

            //half of angle of pole (physically)            
            double alpha = 2 * Math.PI / (4 * RotorParams.p);

            double aa = lm * Math.Sin(alphaM);
            double bb = Math.Tan(alpha);
            double cc = -lm * Math.Cos(alphaM) - wFe2 / Math.Cos(alpha);
            double dd = Rrotor - wFe;

            // magnets, barrier points
            xA = (-bb * cc - aa + Math.Sqrt(dd * dd * (bb * bb + 1) - Math.Pow(aa * bb - cc, 2))) / (bb * bb + 1);
            yA = Math.Tan(alpha) * xA - wFe2 / Math.Cos(alpha);
            xB = xA + lm * Math.Sin(alphaM);
            yB = yA - lm * Math.Cos(alphaM);
            xC = xB + (wb2min - yB) / Math.Tan(alphaM);
            yC = wb2min;
            xD = xC - lm * Math.Sin(alphaM);
            yD = yC + lm * Math.Cos(alphaM);
            xF = wFe2 * Math.Sin(alpha) + Math.Sqrt(Math.Pow(Rrotor - wFe, 2) - wFe2 * wFe2) * Math.Cos(alpha);
            yF = Math.Tan(alpha) * xF - wFe2 / Math.Cos(alpha);

            // pole points
            xG = Rrotor * Math.Cos(alpha);
            yG = Rrotor * Math.Sin(alpha);

            isPointsCoordCalculated = true;
        }

        #endregion

        #region General magnetic circuit build for calculation

        /// <summary>
        /// Make a GMC, All lengths are converted from mm to m
        /// </summary>
        /// <returns></returns>
        public GeneralMagnetCircuit buildGMC()
        {
            if (!isPointsCoordCalculated)
                CalcPointsCoordinates();

            /////assign for shorten variables
            //material
            double Br = MaterialParams.Br;
            double mu_0 = MaterialParams.mu_0;
            double mu_M = MaterialParams.mu_M;
            double Bsat = MaterialParams.Bsat;
            double Hc = MaterialParams.Hc;
            //coeff
            double Kc = Coeffs.Kc;
            //rotor
            double lm = RotorParams.lm;
            double Rrotor = RotorParams.Rrotor;
            int p = RotorParams.p;
            double wFe = RotorParams.wFe;
            double alphaM = RotorParams.alphaM;
            double wFe2 = RotorParams.wFe2;
            double wb2min = RotorParams.wb2min;
            //stator            
            int Q = StatorParams.Q;
            double Dstator = StatorParams.Dstator;
            double Rinstator = StatorParams.Rinstator;
            double L = StatorParams.L;
            double wy = StatorParams.wy;
            double ly = StatorParams.ly;
            double lz = StatorParams.lz;
            double wz = StatorParams.wz;

            //calc sizes of barrier and bridge 
            double bf = Math.Sqrt(Math.Pow(xF - xB, 2) + Math.Pow(yF - yB, 2));
            double af = Math.Sqrt(Math.Pow(xF - xA, 2) + Math.Pow(yF - yA, 2));
            double wm = (yB - yC) / Math.Sin(RotorParams.alphaM);
            double wb2 = yD + yC;
            double lb2 = xC - xD;
            double lFe = RotorParams.wFe2 + bf;
            double lFe2 = af;
            double wb1 = af / 2;
            double lb1 = bf;

            ///// create GMC
            GeneralMagnetCircuit gmc = new GeneralMagnetCircuit();

            // start calculation permeance
            if (Kc == 0)
                Kc = 1;//default carter coefficient            
            double delta = Rinstator - Rrotor;
            double wslot = 2 * Rinstator * Math.PI / Q - wz;
            int Nz = Q / (2 * p);
            double wd = (Rrotor + delta / 2) * 2 * Math.PI / (2 * p);

            // magnet parameters
            gmc.phiR = Br * 2 * wm * L * 1e-6;
            gmc.PM = mu_0 * mu_M * 2 * wm * L / lm * 1e-3;
            gmc.Fc = Hc * lm * 1e-3;

            // airgap
            gmc.Pd = mu_0 * wd * L / (delta * Kc) * 1e-3;

            // leakage path parameters
            gmc.phiHFe = Bsat * 2 * wFe * L * 1e-6;
            gmc.PFe = mu_0 * 2 * wFe * L / lFe * 1e-3;
            double Pb1 = mu_0 * wb1 * L / lb1;
            double Pb2 = mu_0 * wb2 * L / lb2;
            gmc.Pb = (Pb1 + Pb2) * 1e-3;

            // stator teeth and yoke
            ly = 0.5 * (Dstator - wy) * Math.PI / (2 * p);
            gmc.Sz = Nz * wz * L * 1e-6;
            gmc.Sy = 2 * wy * L * 1e-6;
            gmc.lz = lz * 1e-3;
            gmc.ly = ly * 1e-3;
            gmc.Pslot = mu_0 * wslot * L / lz * 1e-3;

            // steel characteristic
            gmc.Barray = MaterialParams.B;
            gmc.Harray = MaterialParams.H;

            return gmc;
        }

        #endregion

        #region Make femm model file
        public int Group_Lines = 101;
        public int Group_Label = 102;
        public int Group_Rotor_Steel = 103;

        public String AirBlockName = "Air";
        public String MagnetBlockName = "NdFeB 32 MGOe";
        public String SteelBlockName = "M-19 Steel";

        /// <summary>
        /// Make a femm model using parameters from rotor creation process
        /// </summary>
        /// <param name="outfile">Output femm model</param>
        /// <param name="original">The original femm model to insert into</param>
        public void MakeAFEMMModelFile(String outfile, String original = "")
        {
            if (!isPointsCoordCalculated)
                CalcPointsCoordinates();

            // create new or open an exist
            if (original == "")
                FEMM.newdocument(FEMM.DocumentType.Magnetic);
            else FEMM.open(original);

            // 
            double Rrotor = RotorParams.Rrotor;
            double alpha = 2 * Math.PI / (4 * RotorParams.p);
            double alphaM = RotorParams.alphaM;
            int p = RotorParams.p;

            ///// Build 1 poles            
            // point far right of rotors
            double xI = Rrotor;
            double yI = 0;

            // segments (magnet)
            FEMM.mi_addSegmentEx(xA, yA, xB, yB, Group_Lines);//AB
            FEMM.mi_addSegmentEx(xA, yA, xF, yF, Group_Lines);//AF
            FEMM.mi_addSegmentEx(xA, yA, xD, yD, Group_Lines);//AD
            FEMM.mi_addSegmentEx(xB, yB, xC, yC, Group_Lines);//BC
            FEMM.mi_addSegmentEx(xC, yC, xD, yD, Group_Lines);//CD

            // mirrored segments (magnet)
            FEMM.mi_addSegmentEx(xA, -yA, xB, -yB, Group_Lines);//AB
            FEMM.mi_addSegmentEx(xA, -yA, xF, -yF, Group_Lines);//AF
            FEMM.mi_addSegmentEx(xA, -yA, xD, -yD, Group_Lines);//AD
            FEMM.mi_addSegmentEx(xB, -yB, xC, -yC, Group_Lines);//BC
            FEMM.mi_addSegmentEx(xC, -yC, xD, -yD, Group_Lines);//CD

            // barrier segments
            FEMM.mi_addSegmentEx(xC, yC, xC, -yC, Group_Lines);//AB
            FEMM.mi_addSegmentEx(xD, yD, xD, -yD, Group_Lines);//AF

            // arcsegments
            double a = (Math.Atan(yF / xF) - Math.Atan(yB / xB)) * 180 / Math.PI;
            FEMM.mi_addArcEx(xB, yB, xF, yF, a, 1, Group_Lines);
            FEMM.mi_addArcEx(xF, -yF, xB, -yB, a, 1, Group_Lines);

            // arcsegments rotor            
            FEMM.mi_addArcEx(xI, yI, xG, yG, alpha * 180 / Math.PI, 1, Group_Lines);
            FEMM.mi_addArcEx(xG, -yG, xI, yI, alpha * 180 / Math.PI, 1, Group_Lines);

            // blocks
            // air block label
            double air1X = (xA + xB + xF) / 3;
            double air1Y = (yA + yB + yF) / 3;
            FEMM.mi_addBlockLabelEx(air1X, air1Y, AirBlockName, Group_Label);
            FEMM.mi_addBlockLabelEx(air1X, -air1Y, AirBlockName, Group_Label);
            FEMM.mi_addBlockLabelEx((xC + xD) / 2, 0, AirBlockName, Group_Label);

            // magnet block label
            double magBlockLabelX = (xA + xC) / 2;
            double magBlockLabelY = (yA + yC) / 2;
            FEMM.mi_addBlockLabelEx(magBlockLabelX, magBlockLabelY, MagnetBlockName, Group_Label, alphaM * 180 / Math.PI - 90 + 180);//+180 but set back later
            FEMM.mi_addBlockLabelEx(magBlockLabelX, -magBlockLabelY, MagnetBlockName, Group_Label, 90 - alphaM * 180 / Math.PI + 180);

            ///// Build 2 poles
            FEMM.mi_clearselected();
            FEMM.mi_selectgroup(Group_Lines);
            FEMM.mi_selectgroup(Group_Label);
            FEMM.mi_copyrotate(0, 0, 360 / (2 * p), 1, FEMM.EditMode.group);
            // rotate the magnet direction
            FEMM.mi_clearselected();
            FEMM.mi_selectlabel(magBlockLabelX, magBlockLabelY);
            FEMM.mi_setblockprop(MagnetBlockName, true, 0, "", alphaM * 180 / Math.PI - 90, Group_Label, 0);
            FEMM.mi_clearselected();
            FEMM.mi_selectlabel(magBlockLabelX, -magBlockLabelY);
            FEMM.mi_setblockprop(MagnetBlockName, true, 0, "", 90 - alphaM * 180 / Math.PI, Group_Label, 0);

            ///// Build p-1 pair of poles remaining
            FEMM.mi_clearselected();
            FEMM.mi_selectgroup(Group_Lines);
            FEMM.mi_selectgroup(Group_Label);
            FEMM.mi_copyrotate(0, 0, 360 / p, p - 1, FEMM.EditMode.group);

            FEMM.mi_addBlockLabelEx(0, 0, SteelBlockName, Group_Rotor_Steel);

            // clear selected, refresh, and go to natural zoom
            FEMM.mi_clearselected();
            FEMM.mi_zoomnatural();

            // save as
            if (Path.GetDirectoryName(outfile) == "")
                outfile = Path.GetDirectoryName(original) + "\\" + outfile;
            FEMM.mi_saveas(outfile);

            FEMM.mi_close();
        }

        #endregion
    }
}
