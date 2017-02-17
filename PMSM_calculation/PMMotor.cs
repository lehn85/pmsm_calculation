using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using log4net;
using System.Reflection;
using System.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Jayrock.Json.Conversion;
using System.Security.Cryptography;
using NLua;
using System.Threading;
using ZedGraph;

namespace calc_from_geometryOfMotor
{
    #region Support classes

    public class ParamValidationInfo
    {
        public enum MessageType
        {
            Info, Warning, Error
        }

        public MessageType msgType;
        public String ParamName { get; private set; }//param which invalid
        public String message { get; private set; }

        public ParamValidationInfo(String p, string msg, MessageType t)
        {
            ParamName = p;
            message = msg;
            msgType = t;
        }
    }

    public class PointBH
    {
        public double b { get; set; }
        public double h { get; set; }

        public PointBH(double b, double h)
        {
            this.b = b;
            this.h = h;
        }

        public PointBH() { }
    }

    #endregion

    public class PMMotor
    {
        private static readonly ILog log = LogManager.GetLogger("PMSMGeo");

        #region Subclasses (parts) of motor

        public class GeneralParameters
        {
            // general params
            [ParamInfo(HelpText = "Length of motor")]
            public double MotorLength { get; set; }//motor length            

            // general params
            [ParamInfo(HelpText = "Build fullmodel in femm?")]
            public bool FullBuildFEMModel { get; set; }
        }

        public class MaterialParameters
        {
            // steel characteristic            
            public PointBH[] BH { get; set; }
            public double Bsat { get; set; }//value of flux density, which is considered saturated
            public double Lam_fill { get; set; }//lamination fill factor
            public double Lam_d { get; set; }//lamination thickness            

            // magnet parameters
            public double Hc { get; set; }
            public double mu_M { get; set; } //relative mu magnet
            public readonly double mu_0 = 4 * Math.PI * 1e-7;
            public double Br { get; set; }

            // copper-wire properties
            [ParamInfo(HelpText = "Wire type")]
            public FEMM.WireType wireType { get; set; }

            [ParamInfo(HelpText = "Electrical conductivity of copper (MS/m)")]
            public double CDuct { get; set; }

            [ParamInfo(HelpText = "Diameter of wire")]
            public double Dwire { get; set; }


            // data for build FEMM model
            internal int Group_Lines_Rotor = 101;//lines, arcs
            internal int Group_BlockLabel_Rotor = 102;//air, magnet block label
            internal int Group_Fixed_BlockLabel_Rotor = 110;//steel block label

            internal int Group_Lines_Stator = 201;//lines, arcs
            internal int Group_BlockLabel_Stator = 202;//copper, ...
            internal int Group_Fixed_BlockLabel_Stator = 210;

            internal int Group_Lines_Airgap = 300; //for boundary in partial motor
            internal int Group_Fixed_BlockLabel_Airgap = 310;

            internal String AirMaterialName = "Air";
            internal String MagnetMaterialName = "NdFeB 32 MGOe";
            internal String SteelMaterialName = "M-19 Steel";
            internal String WireMaterialName = "Copper";

            internal String BoundaryProperty = "Boundary";

            internal void readBHCurveFromFile(String fn)
            {
                List<PointBH> myBH = new List<PointBH>();
                using (StreamReader reader = new StreamReader(fn))
                {
                    reader.ReadLine();//skip first line
                    while (!reader.EndOfStream)
                    {
                        String s = reader.ReadLine();
                        String[] ss = s.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        myBH.Add(new PointBH(double.Parse(ss[0]), double.Parse(ss[1])));
                    }
                }

                BH = myBH.ToArray();
            }

            public double[] getBarray()
            {
                if (BH == null)
                    return null;

                List<double> b = new List<double>();
                foreach (PointBH bh in BH)
                {
                    b.Add(bh.b);
                }

                return b.ToArray();
            }

            public double[] getHarray()
            {
                if (BH == null)
                    return null;

                List<double> h = new List<double>();
                foreach (PointBH bh in BH)
                {
                    h.Add(bh.h);
                }

                return h.ToArray();
            }

            internal void BuildMaterialDataInFEMM(FEMM femm = null)
            {
                if (femm == null)
                    femm = FEMM.DefaultFEMM;

                femm.mi_addmaterialAir(AirMaterialName);
                femm.mi_addmaterialMagnet(MagnetMaterialName, mu_M, Hc, 0);
                femm.mi_addmaterialSteel(SteelMaterialName, 1, 1, Lam_d, Lam_fill, FEMM.LaminationType.NotLaminated, getBarray(), getHarray());
                femm.mi_addmaterialCopper(WireMaterialName, CDuct, wireType, 1, Dwire);

                femm.mi_addboundprop_Prescribed_A(BoundaryProperty, 0, 0, 0, 0);
            }
        }

        public class StatorPart
        {
            // link back to parent (motor)            
            internal PMMotor motor { get; set; }

            public StatorPart()
            {
                isPointsCoordCalculated = false;
            }

            #region Input params

            // Ansys style (slot size)    
            [ParamInfo(HelpText = "")]
            public double BS0 { get; set; }

            [ParamInfo(HelpText = "")]
            public double BS1 { get; set; }

            [ParamInfo(HelpText = "")]
            public double BS2 { get; set; }

            [ParamInfo(HelpText = "")]
            public double HS0 { get; set; }

            [ParamInfo(HelpText = "")]
            public double HS1 { get; set; }

            [ParamInfo(HelpText = "")]
            public double HS2 { get; set; }

            [ParamInfo(HelpText = "")]
            public double RS { get; set; }

            //Stator parameters
            //public int q { get; set; }//slot per phase per pole
            [ParamInfo(HelpText = "Slot number")]
            public int Q { get; set; }//slot count    

            [ParamInfo(HelpText = "Outer diameter of stator")]
            public double Dstator { get; set; }

            [ParamInfo(HelpText = "Inner diameter of stator")]
            public double Dinstator { get; set; }

            [ParamInfo(HelpText = "Slot fill factor")]
            public double Kfill { get; set; }

            /// <summary>
            /// WindingsConfig encode the winding like this:
            /// A:1-7,2-8;B:3-9,4-10;C:5-11,6-12
            /// A-winding name (series for now), separated by semicolon (;)
            /// 1-7 - slot of each coil go separate by comma (,)            
            /// </summary>
            [ParamInfo(HelpText = "Windings Configuration")]
            public String WindingsConfig { get; set; }

            [ParamInfo(HelpText = "Number of strands in each slot")]
            public int NStrands { get; set; }

            [ParamInfo(HelpText = "Pre-rotate stator angle")]
            public double PreRotateAngle { get; set; }

            #endregion

            #region Internal/Readonly (for motor object only)

            // readonly from outside
            internal double wz { get; private set; }//teeth width
            internal double lz { get; private set; }//teeth length
            internal double wy { get; private set; }//yoke width
            internal double ly { get; private set; }//yoke length
            internal double wslot { get; private set; }//slot width            
            internal double Nz { get; private set; }//teeth count per pole
            internal double q { get; private set; }//number of slot(teeth) per phase per pole (1,2, something)

            // readonly
            internal double Rinstator { get { return Dinstator / 2; } }
            internal double Rstator { get { return Dstator / 2; } }
            internal double alpha { get; private set; }
            internal double alphaDegree { get { return alpha * 180 / Math.PI; } }
            internal double DiaGap { get { return Dinstator; } }//ansys style Dinstator (near airgap)
            internal double DiaYoke { get { return Dstator; } }//ansys style Dstator (far from airgap)
            internal double Sslot { get; private set; }//square of slot

            // coordinates (slot)
            internal double xA { get; private set; }
            internal double yA { get; private set; }
            internal double xB { get; private set; }
            internal double yB { get; private set; }
            internal double xC { get; private set; }
            internal double yC { get; private set; }
            internal double xCC { get; private set; }
            internal double yCC { get; private set; }
            internal double xD { get; private set; }
            internal double yD { get; private set; }
            internal double xE { get; private set; }
            internal double yE { get; private set; }
            internal double xF { get; private set; }
            internal double yF { get; private set; }

            // coordinates (coil)
            internal double xC2 { get; private set; }
            internal double yC2 { get; private set; }
            internal double xCC2 { get; private set; }
            internal double yCC2 { get; private set; }
            internal double xD2 { get; private set; }
            internal double yD2 { get; private set; }
            internal double xE2 { get; private set; }
            internal double yE2 { get; private set; }
            internal double xF2 { get; private set; }
            internal double yF2 { get; private set; }

            // actually translated windings from WindingsConfig string
            // coil (ANSYS: coil) - 1 slot
            internal class Coil
            {
                public String inCircuit;
                public int Nturns;//minus is in, plus is out
            }
            internal List<Coil> coils;
            // circuit (ANSYS:winding group) data
            internal class Circuit
            {
                public String name;
                public FEMM.CircuitType circuitType;
                public double current;

                // readonly-from outside- as measurement result
                public double flux_linkage { get; internal set; }
                public double voltage { get; internal set; }

                public Circuit(String name, FEMM.CircuitType ct, double i)
                {
                    this.name = name;
                    circuitType = ct;
                    current = i;
                }
            }
            internal List<Circuit> circuits;

            // check if points was calculated or not
            internal bool isPointsCoordCalculated { get; private set; }

            [JsonIgnore]
            internal List<ParamValidationInfo> ListParamsValidation { get; private set; }

            private void addValidationInfo(String p, String errormsg, ParamValidationInfo.MessageType t)
            {
                if (ListParamsValidation == null)
                    ListParamsValidation = new List<ParamValidationInfo>();

                ListParamsValidation.Add(new ParamValidationInfo(p, errormsg, t));
            }

            #endregion

            /// <summary>
            /// Calculate points' coordinate
            /// </summary>
            internal void CalcCoordinates()
            {
                alpha = Math.PI / Q;

                xA = Rinstator * Math.Cos(alpha);
                yA = Rinstator * Math.Sin(alpha);
                yB = BS0 / 2;
                xB = Math.Sqrt(Rinstator * Rinstator - yB * yB);
                xC = xB + HS0;
                yC = yB;

                // check error and fix BS1 (minimum = BS0)
                if (BS1 < BS0)
                {
                    log.Info("BS1 changed from " + BS1 + " to " + BS0);
                    BS1 = BS0;
                }

                //check error and fix HS1 (maximum =(BS1-BS0)/2)
                if (HS1 > (BS1 - BS0) / 2)
                {
                    log.Info("HS1 changed from " + HS1 + " to " + ((BS1 - BS0) / 2));
                    HS1 = (BS1 - BS0) / 2;
                }
                xCC = xC;
                yCC = yC + (BS1 - BS0) / 2 - HS1;

                xD = xC + HS1;
                yD = BS1 / 2;

                xE = HS2 + xD;
                yE = BS2 / 2;
                xF = xE + RS;
                yF = yE - RS;

                // Slot conductor points
                Sslot = Math.PI * HS1 * HS1 / 2 +
                        Math.PI * RS * RS / 2 +
                        2 * HS1 * yCC +
                        2 * RS * yF +
                        (yD + yE) * (xE - xD);

                double Scu_base = (yCC + yF) * (xE - xD);
                double c = Scu_base - Kfill * Sslot;
                if (c <= 0)
                {
                    double a = Math.PI / 2 * (HS1 * HS1 + RS * RS);
                    double b = 2 * HS1 * yCC + 2 * RS * yF + (HS1 + RS) * (xE - xD);
                    double dd = b * b - 4 * a * c;
                    double kk = (-b + Math.Sqrt(dd)) / (2 * a);
                    kk = 1 - kk;

                    xC2 = xC + HS1 * kk;
                    yC2 = yC;
                    xCC2 = xCC + HS1 * kk;
                    yCC2 = yCC;
                    xD2 = xD;
                    yD2 = yD - HS1 * kk;
                    xE2 = xE;
                    yE2 = yE - RS * kk;
                    xF2 = xF - RS * kk;
                    yF2 = yF;
                }
                else
                {
                    double kk = Math.Sqrt(Kfill * Sslot / Scu_base);
                    double xKK = (xD * yF + xE * yCC) / (yF + yCC);
                    xC2 = xKK - (xKK - xD) * kk;
                    yC2 = yCC * kk;
                    xD2 = xC2;
                    yD2 = yC2;
                    xCC2 = xC2;
                    yCC2 = yC2;
                    xE2 = xKK + (xE - xKK) * kk;
                    yE2 = yF * kk;
                    xF2 = xE2;
                    yF2 = yE2;
                }

                // Slot circuit info
                coils = new List<Coil>(Q);
                for (int i = 0; i < Q; i++)
                    coils.Add(new Coil());

                circuits = new List<Circuit>();

                try //anything can go wrong when handle string, parse int
                {
                    String[] s1 = WindingsConfig.Split(';');
                    foreach (String wd in s1)//each coil (circuit)
                    {
                        //each one like this: A:1-7,8-9
                        String[] s2 = wd.Split(':', ',', '-');//s2[0]=A, s2[i]=1,s[i+1]=7,...                    

                        circuits.Add(new Circuit(s2[0], FEMM.CircuitType.series, 0));

                        int i = 1;
                        while (i < s2.Length)
                        {
                            int inslot = int.Parse(s2[i]) - 1;
                            int outslot = int.Parse(s2[i + 1]) - 1;
                            i += 2;

                            coils[inslot].inCircuit = s2[0];
                            coils[inslot].Nturns = -NStrands;

                            coils[outslot].inCircuit = s2[0];
                            coils[outslot].Nturns = NStrands;
                        }
                    }

                    //foreach (SlotConductorInfo sci in slotsCircuits)
                    //{
                    //    log.Info("Slot " + (slotsCircuits.IndexOf(sci) + 1) + " " + sci.inCircuit + " " + sci.Nturns);
                    //}
                }
                catch (Exception e)
                {
                    addValidationInfo("WindingsConfig", e.Message, ParamValidationInfo.MessageType.Error);
                    log.Error(e.Message);
                }

                // other parameters 
                RotorPart Rotor = motor.Rotor;
                lz = HS0 + HS1 + HS2 + RS;
                wz = alpha * (2 * Rinstator + 2 * HS0 + 2 * HS1 + HS2) - (BS1 + BS2) / 2;
                wy = Rstator - xF;
                ly = 0.5 * (Dstator - wy) * Math.PI / (2 * Rotor.p);
                wslot = 2 * Rinstator * Math.PI / Q - wz;
                Nz = Rotor.gammaM / 180 * Q / (2 * Rotor.p);
                int PhaseCount = (circuits.Count > 0) ? circuits.Count : 3;
                q = Q / (PhaseCount * 2 * Rotor.p);

                isPointsCoordCalculated = true;
            }

            /// <summary>
            /// Build stator part in GMC
            /// </summary>
            /// <param name="gmc"></param>
            /// <param name="Rotor"></param>
            /// <param name="Airgap"></param>
            /// <param name="MaterialParams"></param>
            /// <returns></returns>
            internal GeneralMagnetCircuit BuildStatorInGMC(GeneralMagnetCircuit gmc)
            {
                if (gmc == null)
                    gmc = new GeneralMagnetCircuit();

                RotorPart Rotor = motor.Rotor;
                StatorPart Stator = motor.Stator;
                MaterialParameters MaterialParams = motor.MaterialParams;
                GeneralParameters generalParams = motor.GeneralParams;

                // stator teeth and yoke                
                gmc.Sz = Nz * wz * generalParams.MotorLength * 1e-6;
                gmc.Sy = 2 * wy * generalParams.MotorLength * 1e-6;
                gmc.lz = lz * 1e-3;
                gmc.ly = ly * 1e-3;
                gmc.Pslot = MaterialParams.mu_0 * wslot * generalParams.MotorLength / lz * 1e-3;

                gmc.L = generalParams.MotorLength;

                return gmc;
            }

            /// <summary>
            /// Build Stator in FEMM
            /// </summary>
            /// <param name="MaterialParams"></param>
            internal void BuildStatorInFEMM(FEMM femm = null)
            {
                if (femm == null)
                    femm = FEMM.DefaultFEMM;

                MaterialParameters MaterialParams = motor.MaterialParams;

                int Group_Lines = MaterialParams.Group_Lines_Stator;
                int Group_BlockLabel = MaterialParams.Group_BlockLabel_Stator;
                int Group_Fixed_Blocklabel = MaterialParams.Group_Fixed_BlockLabel_Stator;

                String AirBlockName = MaterialParams.AirMaterialName;
                String MagnetBlockName = MaterialParams.MagnetMaterialName;
                String SteelBlockName = MaterialParams.SteelMaterialName;

                RotorPart Rotor = motor.Rotor;
                bool fullbuild = motor.GeneralParams.FullBuildFEMModel;

                /////// Build half slot     
                ////segments - lines
                femm.mi_addSegmentEx(xB, yB, xC, yC, Group_Lines);//BC
                femm.mi_addSegmentEx(xD, yD, xE, yE, Group_Lines);//DE
                femm.mi_addSegmentEx(xF, yF, xF, 0, Group_Lines);//FF1
                if (yC != yCC)
                    femm.mi_addSegmentEx(xC, yC, xCC, yCC, Group_Lines);//CC'                                                

                //// arcsegments 
                //BA
                double a = (Math.Atan(yA / xA) - Math.Atan(yB / xB)) * 180 / Math.PI;
                femm.mi_addArcEx(xB, yB, xA, yA, a, 1, Group_Lines);
                //DC'
                femm.mi_addArcEx(xD, yD, xCC, yCC, 90, 30, Group_Lines);
                //EF
                femm.mi_addArcEx(xF, yF, xE, yE, 90, 30, Group_Lines);

                //// the coil
                femm.mi_addSegmentEx(xD2, yD2, xE2, yE2, Group_Lines);
                femm.mi_addSegmentEx(xF2, yF2, xF2, 0, Group_Lines);
                femm.mi_addSegmentEx(xCC2, yCC2, xCC2, 0, Group_Lines);
                //CC2-D2
                femm.mi_addArcEx(xD2, yD2, xCC2, yCC2, 90, 30, Group_Lines);
                //EF
                femm.mi_addArcEx(xF2, yF2, xE2, yE2, 90, 30, Group_Lines);

                ////// mirrored half to one
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines);
                femm.mi_mirror(0, 0, 1, 0, FEMM.EditMode.group);

                //////// Build Q slots (copy)               
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines);
                femm.mi_selectgroup(Group_BlockLabel);
                if (fullbuild)
                    femm.mi_copyrotate(0, 0, 360 / Q, Q - 1, FEMM.EditMode.group);
                else
                {
                    femm.mi_copyrotate(0, 0, 360 / Q, Q / (2 * Rotor.p) - 1, FEMM.EditMode.group);

                    // rotate so they match with rotor
                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines);
                    femm.mi_selectgroup(Group_BlockLabel);

                    double shiftangle = -Rotor.alphaDegree + 180.0 / Q;
                    femm.mi_moverotate(0, 0, shiftangle, FEMM.EditMode.group);
                }

                ////// Block labels (steel)               
                femm.mi_addBlockLabelEx((xF + Rstator) / 2, 0, SteelBlockName, Group_Fixed_Blocklabel);

                /////// Stator outer lines (2 arcs)
                if (fullbuild)
                {
                    femm.mi_addArcEx(Rstator, 0, -Rstator, 0, 180, 10, Group_Lines);
                    femm.mi_addArcEx(-Rstator, 0, Rstator, 0, 180, 10, Group_Lines);
                }
                else
                {
                    femm.mi_addArcEx(Rstator * Math.Cos(Rotor.alpha), -Rstator * Math.Sin(Rotor.alpha),
                        Rstator * Math.Cos(Rotor.alpha), Rstator * Math.Sin(Rotor.alpha), 2 * Rotor.alphaDegree, 10, Group_Lines);
                }

                // Set boundary condition for outline stator
                femm.mi_clearselected();
                if (fullbuild)
                {
                    femm.mi_selectarcsegment(0, Rstator);
                    femm.mi_selectarcsegment(0, -Rstator);
                }
                else
                    femm.mi_selectarcsegment(Rstator, 0);

                femm.mi_setarcsegmentprop(10, MaterialParams.BoundaryProperty, false, Group_Lines);

                /////// Wire, circuits in slot
                foreach (Circuit c in circuits)
                {
                    femm.mi_addcircprop(c.name, c.current, c.circuitType);
                }

                double r = (xD + xE) / 2;
                foreach (Coil sci in coils)
                {
                    int i = coils.IndexOf(sci);

                    if (fullbuild)
                    {
                        //angle go clockwise from 3 o'clock (=0 degree in decarter), 
                        double aa = -2 * Math.PI * i / Q;
                        double x = r * Math.Cos(aa);
                        double y = r * Math.Sin(aa);
                        femm.mi_addBlockLabelEx(x, y, MaterialParams.WireMaterialName, Group_Fixed_Blocklabel, sci.inCircuit, sci.Nturns);
                    }
                    else
                    {
                        //angle go clockwise from 3 o'clock (=0 degree in decarter), shift +pi/Q (to match rotor)
                        double aa = -2 * Math.PI * (i - 0.5) / Q;
                        double x = r * Math.Cos(aa);
                        double y = r * Math.Sin(aa);
                        if (aa > -Rotor.alpha || aa < -2 * Math.PI + Rotor.alpha)
                            femm.mi_addBlockLabelEx(x, y, MaterialParams.WireMaterialName, Group_Fixed_Blocklabel, sci.inCircuit, sci.Nturns);
                    }
                }

                if (fullbuild)
                {
                    //pre-rotate stator (fullbuild only)
                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines);
                    femm.mi_selectgroup(Group_BlockLabel);
                    femm.mi_selectgroup(Group_Fixed_Blocklabel);
                    femm.mi_moverotate(0, 0, PreRotateAngle, FEMM.EditMode.group);
                }

                if (!fullbuild)
                {
                    //build boundary of motor: 2 lines, anti-periodic
                    String boundaryName = "stator-apb-1";
                    double x1 = Rinstator * Math.Cos(Rotor.alpha);
                    double y1 = Rinstator * Math.Sin(Rotor.alpha);
                    double x2 = Rstator * Math.Cos(Rotor.alpha);
                    double y2 = Rstator * Math.Sin(Rotor.alpha);

                    femm.mi_addSegmentEx(x1, y1, x2, y2, Group_Lines);
                    femm.mi_addSegmentEx(x1, -y1, x2, -y2, Group_Lines);

                    femm.mi_addboundprop_AntiPeriodic(boundaryName);

                    femm.mi_clearselected();
                    femm.mi_selectsegment(x2, y2);
                    femm.mi_selectsegment(x2, -y2);
                    femm.mi_setsegmentprop(boundaryName, 0, true, false, Group_Lines);
                }
            }

            /// <summary>
            /// Set current amps
            /// </summary>
            /// <param name="currents"></param>
            /// <param name="femm"></param>
            public void SetStatorCurrentsInFEMM(IDictionary<String, double> currents, FEMM femm)
            {
                if (currents == null)
                    return;
                // edit some transient params (usually stator currents) ?
                var tkeys = currents.Keys;
                foreach (String key in tkeys)
                    femm.mi_modifycircuitCurrent(key, currents[key]);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="Ox"></param>
            /// <param name="Oy"></param>
            /// <param name="scale"></param>
            internal void DrawStator(Graphics graphics, float Ox, float Oy, float scale)
            {
                PointF pA = NewPoint(xA, yA);
                PointF pB = NewPoint(xB, yB);
                PointF pC = NewPoint(xC, yC);
                PointF pCC = NewPoint(xCC, yCC);
                PointF pD = NewPoint(xD, yD);
                PointF pE = NewPoint(xE, yE);
                PointF pF = NewPoint(xF, yF);

                // draw Q slots/teeth
                using (Pen pen = new Pen(Color.Black, 1.0F / scale))
                {
                    for (int i = 0; i < Q; i++)
                    {
                        float rotate = (float)(i * 360.0 / Q);
                        float[] scaleYs = { scale, -scale };

                        // draw lines, arcs and mirror of them using scale and -scale transform
                        foreach (float scaleY in scaleYs)
                        {
                            // transform system coordinate
                            graphics.ResetTransform();
                            graphics.TranslateTransform(Ox, Oy);
                            graphics.ScaleTransform(scale, scaleY);
                            graphics.RotateTransform(rotate - Math.Sign(scaleY) * (float)PreRotateAngle);

                            //draw
                            graphics.DrawLine(pen, pB, pC);
                            graphics.DrawLine(pen, pD, pE);
                            graphics.DrawLine(pen, (float)xF, (float)yF, (float)xF, 0);
                            graphics.DrawLine(pen, pC, pCC);

                            // AB
                            float startAngle = (float)(Math.Atan(yB / xB) * 180 / Math.PI);
                            float sweepAngle = (float)((Math.Atan(yA / xA) - Math.Atan(yB / xB)) * 180 / Math.PI);
                            graphics.DrawArc(pen, -(float)Rinstator, -(float)Rinstator,
                                2 * (float)Rinstator, 2 * (float)Rinstator, startAngle, sweepAngle);

                            if (HS1 > 0)
                                graphics.DrawArc(pen, (float)(xD - HS1), (float)(yCC - HS1), (float)(2 * HS1), (float)(2 * HS1), 90, 90);
                            if (RS > 0)
                                graphics.DrawArc(pen, (float)(xE - RS), (float)(yF - RS), (float)(2 * RS), (float)(2 * RS), 0, 90);

                            //draw                        
                            graphics.DrawLine(pen, (float)xD2, (float)yD2, (float)xE2, (float)yE2);
                            graphics.DrawLine(pen, (float)xF2, (float)yF2, (float)xF2, 0);
                            graphics.DrawLine(pen, (float)xCC2, (float)yCC2, (float)xCC2, 0);

                            double r1 = xD2 - xC2;
                            double r2 = xF2 - xE2;
                            if (r1 > 0)
                                graphics.DrawArc(pen, (float)(xD2 - r1), (float)(yCC2 - r1), (float)(2 * r1), (float)(2 * r1), 90, 90);
                            if (r2 > 0)
                                graphics.DrawArc(pen, (float)(xE2 - r2), (float)(yF2 - r2), (float)(2 * r2), (float)(2 * r2), 0, 90);
                        }


                    }

                    //draw stator outerline
                    graphics.ResetTransform();
                    graphics.TranslateTransform(Ox, Oy);
                    graphics.ScaleTransform(scale, scale);
                    graphics.DrawEllipse(pen, -(float)Rstator, -(float)Rstator, 2 * (float)Rstator, 2 * (float)Rstator);
                }

                graphics.ResetTransform();
                graphics.TranslateTransform(Ox, Oy);
                graphics.ScaleTransform(scale, scale);

                // draw label of slot (coil, +- as direction of current)
                using (Pen pen = new Pen(Color.Black, 1.0F / scale))
                using (Font font = new Font("Arial", 10.0f / scale))
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;

                    double r = (xD + xE) / 2;
                    double r2 = xF + 15.0f / scale;

                    foreach (Coil sci in coils)
                    {
                        int i = coils.IndexOf(sci);
                        double aa = 2 * Math.PI * i / Q - PreRotateAngle * Math.PI / 180;//angle go clockwise from 3 o'clock (=0 degree in decarter)

                        float x = (float)(r * Math.Cos(aa));
                        float y = (float)(r * Math.Sin(aa)) - font.Height / 2.0f;
                        graphics.DrawString(sci.inCircuit + (sci.Nturns < 0 ? "+" : "-"), font, brush, x, y, sf);

                        x = (float)(r2 * Math.Cos(aa));
                        y = (float)(r2 * Math.Sin(aa)) - font.Height / 2.0f;
                        graphics.DrawString((i + 1).ToString(), font, brush, x, y, sf);
                        //graphics.DrawRectangle(pen, rect.Left, rect.Top, rect.Width, rect.Height);
                    }
                }
            }

            private PointF NewPoint(double x, double y)
            {
                return new PointF((float)x, (float)y);
            }

            #region Measure output (currents, flux, ...)

            /// <summary>
            /// Get all circuits properties in an opened window femm (with opened ans file)
            /// </summary>
            /// <param name="femm"></param>
            /// <returns></returns>
            public Dictionary<String, FEMM.CircuitProperties> getCircuitsPropertiesInAns(FEMM femm)
            {
                if (circuits.Count == 0)
                    return null;
                Dictionary<String, FEMM.CircuitProperties> circuitProperties = new Dictionary<string, FEMM.CircuitProperties>();
                foreach (Circuit c in circuits)
                {
                    FEMM.CircuitProperties cp = femm.mo_getcircuitproperties(c.name);
                    if (!motor.GeneralParams.FullBuildFEMModel)
                    {
                        cp.fluxlinkage *= 2 * motor.Rotor.p;
                        cp.volts *= 2 * motor.Rotor.p;
                    }

                    circuitProperties.Add(cp.name, cp);
                }

                return circuitProperties;
            }

            #endregion
        }

        public class RotorPart
        {
            // link back to parent (motor)            
            internal PMMotor motor { get; set; }

            public enum PoleType
            {
                MiddleAir = 5,
                MiddleSteelBridgeTrapezoid = 4,
                MiddleSteelBridgeRectangle = 3
            }

            #region Input params

            //Ansys-like params
            [ParamInfo(HelpText = "Diameter of Rotor")]
            public double DiaGap { get; set; }//2*Rrotor

            [ParamInfo(HelpText = "Diameter of Shaft")]
            public double DiaYoke { get; set; }//shaft diameter

            [ParamInfo(HelpText = "Diameter to the steel bridge")]
            public double D1 { get; set; }

            [ParamInfo(HelpText = "Magnet thickness")]
            public double ThickMag { get; set; }//lm

            [ParamInfo(HelpText = "Magnets width (total)")]
            public double WidthMag { get; set; }//2*wm

            [ParamInfo(HelpText = "Distance between 2 magnets in 2 poles")]
            public double Rib { get; set; }//2*wFe2

            [ParamInfo(HelpText = "Hrib")]
            public double HRib { get; set; }

            [ParamInfo(HelpText = "")]
            public double O1 { get; set; }

            [ParamInfo(HelpText = "")]
            public double O2 { get; set; }

            [ParamInfo(HelpText = "")]
            public double B1 { get; set; }

            [ParamInfo(HelpText = "")]
            public double Dminmag { get; set; }

            [ParamInfo(HelpText = "Number of pair of poles")]
            public int p { get; set; }//pair of poles            

            [ParamInfo(HelpText = "")]
            public PoleType poletype { get; set; }

            [ParamInfo(HelpText = "Pre-rotate rotor angle")]
            public double PreRotateAngle { get; set; }

            #endregion

            #region Readonly/Output

            //// my params (readonly)
            internal double Rrotor { get { return DiaGap / 2; } }
            internal double R1 { get { return D1 / 2; } }
            internal double alpha { get { return 2 * Math.PI / (4 * p); } }
            internal double alphaDegree { get { return 180.0 / (2 * p); } }
            internal double alphaM { get; private set; }//angle between magnet and the Ox
            internal double alphaMDegree { get { return alphaM * 180 / Math.PI; } }
            internal double gammaM { get; private set; }//angle 0-180 indicate magnet open angle (electrical angle)            

            // All points related to rotor's geometric 
            internal double xA { get; private set; }
            internal double yA { get; private set; }
            internal double xB { get; private set; }
            internal double yB { get; private set; }
            internal double xC { get; private set; }
            internal double yC { get; private set; }
            internal double xD { get; private set; }
            internal double yD { get; private set; }
            internal double xE { get; private set; }
            internal double yE { get; private set; }
            internal double xF { get; private set; }
            internal double yF { get; private set; }
            internal double xG { get; private set; }
            internal double yG { get; private set; }
            internal double xH { get; private set; }
            internal double yH { get; private set; }
            internal double xI { get; private set; }
            internal double yI { get; private set; }
            internal double xJ { get; private set; }
            internal double yJ { get; private set; }
            internal double xK { get; private set; }
            internal double yK { get; private set; }

            internal double xK0 { get; private set; }
            internal double xR { get; private set; }
            internal double yR { get; private set; }
            internal double xS { get; private set; }
            internal double yS { get; private set; }

            #endregion

            public RotorPart()
            {
                isPointsCoordCalculated = false;
            }

            internal bool isPointsCoordCalculated { get; private set; }

            internal List<ParamValidationInfo> ListParamsValidation { get; private set; }

            private void addValidationInfo(String p, String errormsg, ParamValidationInfo.MessageType t)
            {
                if (ListParamsValidation == null)
                    ListParamsValidation = new List<ParamValidationInfo>();

                ListParamsValidation.Add(new ParamValidationInfo(p, errormsg, t));
            }

            /// <summary>
            /// Calculate all points' coordinates
            /// </summary>
            internal void CalcCoordinates()
            {
                if (ListParamsValidation == null)
                    ListParamsValidation = new List<ParamValidationInfo>();
                ListParamsValidation.Clear();

                double a = Math.Tan(alpha);
                double b = Rib / 2 * Math.Cos(alpha);

                xA = (a * b + Math.Sqrt(a * a * R1 * R1 + R1 * R1 - b * b)) / (a * a + 1);
                yA = a * xA - b;
                xB = xA - HRib * Math.Cos(alpha);
                yB = yA - HRib * Math.Sin(alpha);

                xJ = DiaYoke / 2 + O2;
                yJ = O1 / 2;
                alphaM = Math.Atan((yB - yJ) / (xB - xJ));
                xK0 = xJ - O1 / (2 * Math.Tan(alphaM)) + B1 / Math.Sin(alphaM);

                // calc K depends on poletype
                switch (poletype)
                {
                    case RotorPart.PoleType.MiddleSteelBridgeTrapezoid:
                        //need to limit O1, xJ, alphaM or something collides (require yK>=0)
                        double oldO1 = O1;
                        while (true)
                        {
                            xK = xJ + B1 * Math.Sin(alphaM);
                            yK = yJ - B1 * Math.Cos(alphaM);
                            if (yK < 0)
                                O1 += 0.01;
                            else break;

                            //recalculate J,alphaM,xK0 because O1 changed
                            yJ = O1 / 2;
                            alphaM = Math.Atan((yB - yJ) / (xB - xJ));
                            xK0 = xJ - O1 / (2 * Math.Tan(alphaM)) + B1 / Math.Sin(alphaM);
                        }
                        if (oldO1 != O1)
                        {
                            String msg = "O1 changed from:" + oldO1 + " to " + O1;
                            addValidationInfo("O1", msg, ParamValidationInfo.MessageType.Warning);
                            //log.Warn(msg);
                        }

                        break;

                    case RotorPart.PoleType.MiddleSteelBridgeRectangle:
                        xK = xJ + B1 / Math.Sin(alphaM);
                        yK = yJ;
                        break;

                    case RotorPart.PoleType.MiddleAir://5
                    default:
                        xK = xK0;
                        yK = 0;
                        break;
                }

                //limit the Dminmag or it collides
                double previousDmm = Dminmag;
                double dmmm = O1 - 2 * B1 * Math.Cos(alphaM);
                if (Dminmag < dmmm)
                    Dminmag = dmmm;
                if (poletype == RotorPart.PoleType.MiddleSteelBridgeRectangle && Dminmag < O1)
                    Dminmag = O1;
                if (previousDmm != Dminmag)
                {
                    String msg = "Dminmag changed from:" + previousDmm + " to " + Dminmag;
                    addValidationInfo("Dminmag", msg, ParamValidationInfo.MessageType.Warning);
                    //log.Warn(msg);
                }

                xI = Dminmag / (2 * Math.Tan(alphaM)) + xK0;
                yI = Dminmag / 2;
                xH = xI - B1 * Math.Sin(alphaM);
                yH = yI + B1 * Math.Cos(alphaM);
                xG = xI - ThickMag * Math.Sin(alphaM);
                yG = yI + ThickMag * Math.Cos(alphaM);

                //check WidthMag limits
                double b_ = xI * Math.Cos(alphaM) + yI * Math.Sin(alphaM);
                double c_ = xI * xI + yI * yI - R1 * R1;
                double maxWidthMag = 2 * Math.Sqrt(b_ * b_ - c_) - 2 * b_;
                if (WidthMag > maxWidthMag)
                {
                    addValidationInfo("WidthMag", "WidthMag is too big. Max: " + maxWidthMag, ParamValidationInfo.MessageType.Error);
                    //log.Error("WidthMag maximum allowed:" + maxWidthMag);
                }

                xF = xI + WidthMag / 2 * Math.Cos(alphaM);
                yF = yI + WidthMag / 2 * Math.Sin(alphaM);

                xE = xF - B1 * Math.Sin(alphaM);
                yE = yF + B1 * Math.Cos(alphaM);

                xD = xF - ThickMag * Math.Sin(alphaM);
                yD = yF + ThickMag * Math.Cos(alphaM);

                double c = Math.Tan(alphaM);
                double d = Math.Tan(alphaM) * xK0;
                xC = (c * d + Math.Sqrt(c * c * R1 * R1 + R1 * R1 - d * d)) / (c * c + 1);
                yC = c * xC - d;

                //open angle of magnet (eletrical degree)
                gammaM = 2 * Math.Atan(yC / xC) * p * 180 / Math.PI;

                // point far right of rotors
                xR = Rrotor;
                yR = 0;
                xS = Rrotor * Math.Cos(alpha);
                yS = Rrotor * Math.Sin(alpha);

                // 
                isPointsCoordCalculated = true;
            }

            /// <summary>
            /// Build rotor parts in GMC
            /// </summary>
            /// <param name="gmc"></param>
            internal GeneralMagnetCircuit BuildRotorInGMC(GeneralMagnetCircuit gmc)
            {
                ///// create GMC if null
                if (gmc == null)
                    gmc = new GeneralMagnetCircuit();
                                
                MaterialParameters MaterialParams = motor.MaterialParams;
                GeneralParameters generalParams = motor.GeneralParams;

                /////assign for shorten variables
                //material
                double Br = MaterialParams.Br;
                double mu_0 = MaterialParams.mu_0;
                double mu_M = MaterialParams.mu_M;
                double Bsat = MaterialParams.Bsat;
                double Hc = MaterialParams.Hc;                
                //stator (may be use a general class to store general data)
                double L = generalParams.MotorLength;

                // magnet
                double wm = WidthMag / 2;
                double lm = ThickMag;

                //calc sizes of barrier
                double wb1 = HRib / 2;
                double lb1 = Math.Sqrt(Math.Pow(xA - xC, 2) + Math.Pow(yA - yC, 2));//AC
                double wb2 = Math.Sqrt(Math.Pow(xB - xE, 2) + Math.Pow(yB - yE, 2));//BE
                double lb2 = B1;
                double wb3 = Math.Sqrt(Math.Pow(xH - xJ, 2) + Math.Pow(yH - yJ, 2));//GJ
                double lb3 = B1;
                double wb4 = (yJ + yK) / 2;
                double lb4 = xK - xJ;

                //steel bridge
                double wFe = Rrotor - R1;
                double lFe = Math.Sqrt(Math.Pow(xA - xC, 2) + Math.Pow(yA - yC, 2));//AC
                double wFe2 = yK + yJ;
                double lFe2 = xK - xJ;

                //refine by poletype:
                if (poletype == RotorPart.PoleType.MiddleAir)//middle is air then no steel
                    wFe2 = 0;
                else wb4 = 0;//else, no air             

                // magnet parameters
                gmc.phiR = Br * 2 * wm * L * 1e-6;
                gmc.PM = mu_0 * mu_M * 2 * wm * L / lm * 1e-3;
                gmc.Fc = Hc * lm * 1e-3;

                // leakage path parameters
                gmc.phiHFe = Bsat * 2 * wFe * L * 1e-6;
                gmc.PFe = mu_0 * 2 * wFe * L / lFe * 1e-3;
                double Pb1 = mu_0 * wb1 * L / lb1;
                double Pb2 = mu_0 * wb2 * L / lb2;
                double Pb3 = mu_0 * wb3 * L / lb3;
                double Pb4 = mu_0 * wb4 * L / lb4;
                gmc.Pb = 2 * (Pb1 + Pb2 + Pb3 + Pb4) * 1e-3;

                // some sizes (rotor)
                gmc.wm = wm;
                gmc.lm = lm;


                return gmc;
            }

            /// <summary>
            /// Assuming a FEMM window is opened, build rotor into that
            /// </summary>
            /// <param name="MaterialParams"></param>
            internal void BuildRotorInFEMM(FEMM femm = null)
            {
                if (femm == null)
                    femm = FEMM.DefaultFEMM;

                MaterialParameters MaterialParams = motor.MaterialParams;

                int Group_Lines = MaterialParams.Group_Lines_Rotor;
                int Group_Label = MaterialParams.Group_BlockLabel_Rotor;
                int Group_Rotor_Steel = MaterialParams.Group_Fixed_BlockLabel_Rotor;

                String AirBlockName = MaterialParams.AirMaterialName;
                String MagnetBlockName = MaterialParams.MagnetMaterialName;
                String SteelBlockName = MaterialParams.SteelMaterialName;

                bool fullbuild = motor.GeneralParams.FullBuildFEMModel;

                /////// Build 1 poles     
                ////Half one       
                // segments (magnet)
                femm.mi_addSegmentEx(xA, yA, xB, yB, Group_Lines);//AB
                femm.mi_addSegmentEx(xB, yB, xE, yE, Group_Lines);//BE
                femm.mi_addSegmentEx(xD, yD, xE, yE, Group_Lines);//DE
                femm.mi_addSegmentEx(xE, yE, xF, yF, Group_Lines);//EF
                femm.mi_addSegmentEx(xF, yF, xC, yC, Group_Lines);//FC
                femm.mi_addSegmentEx(xD, yD, xG, yG, Group_Lines);//DG
                femm.mi_addSegmentEx(xH, yH, xG, yG, Group_Lines);//HG
                femm.mi_addSegmentEx(xH, yH, xI, yI, Group_Lines);//HI
                femm.mi_addSegmentEx(xI, yI, xF, yF, Group_Lines);//IF
                femm.mi_addSegmentEx(xH, yH, xJ, yJ, Group_Lines);//HJ
                femm.mi_addSegmentEx(xI, yI, xK, yK, Group_Lines);//IK
                if (poletype == RotorPart.PoleType.MiddleAir)
                    femm.mi_addSegmentEx(xJ, yJ, xJ, 0, Group_Lines);//JJ1
                else femm.mi_addSegmentEx(xJ, yJ, xK, yK, Group_Lines);//JK                                               

                // arcsegments AC
                double a = (Math.Atan(yA / xA) - Math.Atan(yC / xC)) * 180 / Math.PI;
                femm.mi_addArcEx(xC, yC, xA, yA, a, 1, Group_Lines);
                // arcsegment RS
                femm.mi_addArcEx(xR, yR, xS, yS, alphaDegree, 1, Group_Lines);

                // blocks
                // air block label
                double air1X = (xA + xB + xC) / 3;
                double air1Y = (yA + yB + yC) / 3;
                femm.mi_addBlockLabelEx(air1X, air1Y, AirBlockName, Group_Label);
                //FEMM.mi_addBlockLabelEx(air1X, -air1Y, AirBlockName, Group_Label);

                if (poletype != RotorPart.PoleType.MiddleAir)
                {
                    double air2X = (xJ + xK + xI + xH) / 4;
                    double air2Y = (yJ + yK + yI + yH) / 4;
                    femm.mi_addBlockLabelEx(air2X, air2Y, AirBlockName, Group_Label);
                    //  femm.mi_addBlockLabelEx(air2X, -air2Y, AirBlockName, Group_Label);
                }

                // magnet block label
                double magBlockLabelX = (xD + xI) / 2;
                double magBlockLabelY = (yD + yI) / 2;
                femm.mi_addBlockLabelEx(magBlockLabelX, magBlockLabelY, MagnetBlockName, Group_Label, alphaM * 180 / Math.PI - 90 + 180);//+180 but set back later
                //femm.mi_addBlockLabelEx(magBlockLabelX, -magBlockLabelY, MagnetBlockName, Group_Label, 90 - alphaM * 180 / Math.PI + 180);

                ///// mirrored all
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines);
                femm.mi_selectgroup(Group_Label);
                femm.mi_mirror(0, 0, 1, 0, FEMM.EditMode.group);

                //// the remainings: not symmetrical
                if (poletype == RotorPart.PoleType.MiddleAir)
                    femm.mi_addBlockLabelEx((xJ + xK0) / 2, 0, AirBlockName, Group_Label);

                ////////// Build 2 poles
                // copy pole if full build
                if (fullbuild)
                {
                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines);
                    femm.mi_selectgroup(Group_Label);
                    femm.mi_copyrotate(0, 0, 360 / (2 * p), 1, FEMM.EditMode.group);
                }
                // rotate the magnet direction (of the first pole)
                femm.mi_clearselected();
                femm.mi_selectlabel(magBlockLabelX, magBlockLabelY);
                femm.mi_setblockprop(MagnetBlockName, true, 0, "", alphaM * 180 / Math.PI - 90, Group_Label, 0);
                femm.mi_clearselected();
                femm.mi_selectlabel(magBlockLabelX, -magBlockLabelY);
                femm.mi_setblockprop(MagnetBlockName, true, 0, "", 90 - alphaM * 180 / Math.PI, Group_Label, 0);

                if (fullbuild)
                {
                    //////// Build p-1 pair of poles remaining
                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines);
                    femm.mi_selectgroup(Group_Label);
                    femm.mi_copyrotate(0, 0, 360 / p, p - 1, FEMM.EditMode.group);
                }

                /////// The remaining: not symmetrical: add label for rotor steel
                femm.mi_addBlockLabelEx((DiaYoke / 2 + O2 / 2), 0, SteelBlockName, Group_Rotor_Steel);


                if (fullbuild)
                {
                    // pre-rotate rotor (only in fullbuild)
                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines);
                    femm.mi_selectgroup(Group_Label);
                    femm.mi_selectgroup(Group_Rotor_Steel);
                    femm.mi_moverotate(0, 0, PreRotateAngle, FEMM.EditMode.group);
                }

                if (!fullbuild)
                {
                    //build boundary of motor: 2 lines, anti-periodic
                    String boundaryName = "rotor-apb-1";
                    femm.mi_addSegmentEx(0, 0, xS, yS, Group_Lines);
                    femm.mi_addSegmentEx(0, 0, xS, -yS, Group_Lines);

                    femm.mi_addboundprop_AntiPeriodic(boundaryName);

                    femm.mi_clearselected();
                    femm.mi_selectsegment(xS, yS);
                    femm.mi_selectsegment(xS, -yS);
                    femm.mi_setsegmentprop(boundaryName, 0, true, false, Group_Lines);
                }
            }

            internal void RotateRotorInFEMM(double rotorAngle, FEMM femm = null)
            {
                MaterialParameters MaterialParams = motor.MaterialParams;
                femm.mi_selectgroup(MaterialParams.Group_Lines_Rotor);
                femm.mi_selectgroup(MaterialParams.Group_BlockLabel_Rotor);
                femm.mi_selectgroup(MaterialParams.Group_Fixed_BlockLabel_Rotor);
                femm.mi_moverotate(0, 0, rotorAngle, FEMM.EditMode.group);
            }

            /// <summary>
            /// Draw rotor with graphics object
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="Ox"></param>
            /// <param name="Oy"></param>
            /// <param name="scale"></param>
            internal void DrawRotor(Graphics graphics, float Ox, float Oy, float scale)
            {
                PointF pA = NewPoint(xA, yA);
                PointF pB = NewPoint(xB, yB);
                PointF pC = NewPoint(xC, yC);
                PointF pD = NewPoint(xD, yD);
                PointF pE = NewPoint(xE, yE);
                PointF pF = NewPoint(xF, yF);
                PointF pG = NewPoint(xG, yG);
                PointF pH = NewPoint(xH, yH);
                PointF pI = NewPoint(xI, yI);
                PointF pJ = NewPoint(xJ, yJ);
                PointF pK = NewPoint(xK, yK);

                float startAngle = (float)(Math.Atan(yC / xC) * 180 / Math.PI);
                float sweepAngle = (float)((Math.Atan(yA / xA) - Math.Atan(yC / xC)) * 180 / Math.PI);
                float Rrotor = (float)this.Rrotor;
                float alpha = (float)(this.alphaDegree);
                float R1 = (float)this.R1;
                using (Pen pen = new Pen(Color.Black, 1.0F / scale))
                {
                    for (int i = 0; i < 2 * p; i++)
                    {
                        float rotate = (float)(i * 180.0 / p);
                        float[] scaleYs = { scale, -scale };
                        foreach (float scaleY in scaleYs)
                        {
                            // transform system coordinate
                            graphics.ResetTransform();
                            graphics.TranslateTransform(Ox, Oy);
                            graphics.ScaleTransform(scale, scaleY);
                            graphics.RotateTransform(rotate - Math.Sign(scaleY) * (float)PreRotateAngle);

                            //draw
                            graphics.DrawLine(pen, pA, pB);
                            graphics.DrawLine(pen, pB, pE);
                            graphics.DrawLine(pen, pD, pE);
                            graphics.DrawLine(pen, pE, pF);
                            graphics.DrawLine(pen, pF, pC);
                            graphics.DrawLine(pen, pD, pG);
                            graphics.DrawLine(pen, pG, pH);
                            graphics.DrawLine(pen, pH, pI);
                            graphics.DrawLine(pen, pI, pF);
                            graphics.DrawLine(pen, pH, pJ);
                            graphics.DrawLine(pen, pI, pK);
                            if (poletype == RotorPart.PoleType.MiddleAir)
                                graphics.DrawLine(pen, (float)xJ, (float)yJ, (float)xJ, 0);//JJ1
                            else graphics.DrawLine(pen, pJ, pK);//JK
                            graphics.DrawArc(pen, -R1, -R1, 2 * R1, 2 * R1, startAngle, sweepAngle);

                            //rotor line
                            graphics.DrawArc(pen, -Rrotor, -Rrotor, 2 * Rrotor, 2 * Rrotor, 0, alpha);

                            //graphics.DrawLine(pen, 0, 0, (float)xS, (float)yS);
                        }
                    }
                }
            }

            private PointF NewPoint(double x, double y)
            {
                return new PointF((float)x, (float)y);
            }

            #region Measure rotor (torque,...)

            public double getTorqueInAns(FEMM femm)
            {
                femm.mo_clearblock();
                femm.mo_groupselectblock(motor.MaterialParams.Group_BlockLabel_Rotor);
                femm.mo_groupselectblock(motor.MaterialParams.Group_Fixed_BlockLabel_Rotor);
                double torque = femm.mo_blockintegral(FEMM.BlockIntegralType.Steady_state_weighted_stress_tensor_torque);

                if (!motor.GeneralParams.FullBuildFEMModel)
                    torque *= 2 * motor.Rotor.p;

                return torque;
            }

            #endregion
        }

        public class AirgapPart
        {
            // link back to parent (motor)            
            internal PMMotor motor { get; set; }

            // input
            public double Kc { get; set; }//Coefficient carter 

            //readonly
            internal double delta { get; set; }
            internal double wd { get; set; }

            /// <summary>
            /// Calculate params like delta (airgap), wd (width) from Rotor, Stator geometrical
            /// </summary>
            /// <param name="Rotor"></param>
            /// <param name="Stator"></param>
            /// <param name="MaterialParams"></param>
            internal void CalculateAirgapParams()
            {
                RotorPart Rotor = motor.Rotor;
                StatorPart Stator = motor.Stator;

                delta = Stator.Rinstator - Rotor.Rrotor;
                wd = Rotor.gammaM / 180 * (Rotor.Rrotor + delta / 2) * 2 * Math.PI / (2 * Rotor.p);
            }

            internal void BuildAirgapInGMC(GeneralMagnetCircuit gmc)
            {
                if (gmc == null)
                    gmc = new GeneralMagnetCircuit();

                RotorPart Rotor = motor.Rotor;
                StatorPart Stator = motor.Stator;
                MaterialParameters MaterialParams = motor.MaterialParams;

                // airgap
                gmc.Pd = MaterialParams.mu_0 * wd * motor.GeneralParams.MotorLength / (delta * Kc) * 1e-3;

                //some sizes
                gmc.wd = wd;
                gmc.delta = delta;
            }

            internal void BuildAirgapInFEMM(FEMM femm = null)
            {
                if (femm == null)
                    femm = FEMM.DefaultFEMM;

                RotorPart Rotor = motor.Rotor;
                StatorPart Stator = motor.Stator;
                MaterialParameters MaterialParams = motor.MaterialParams;

                femm.mi_addBlockLabelEx(Rotor.Rrotor + delta * 0.8, 0, MaterialParams.AirMaterialName, MaterialParams.Group_Fixed_BlockLabel_Airgap);

                if (!motor.GeneralParams.FullBuildFEMModel)
                {
                    //build boundary of motor: 2 lines, anti-periodic
                    String boundaryName = "airgap-apb-0";
                    int Group_Lines = MaterialParams.Group_Lines_Airgap;

                    double x1 = Rotor.Rrotor * Math.Cos(Rotor.alpha);
                    double y1 = Rotor.Rrotor * Math.Sin(Rotor.alpha);
                    double x2 = Stator.Rinstator * Math.Cos(Rotor.alpha);
                    double y2 = Stator.Rinstator * Math.Sin(Rotor.alpha);

                    femm.mi_addSegmentEx(x1, y1, x2, y2, Group_Lines);
                    femm.mi_addSegmentEx(x1, -y1, x2, -y2, Group_Lines);

                    femm.mi_addboundprop_AntiPeriodic(boundaryName);

                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines);
                    femm.mi_setsegmentprop(boundaryName, 0, true, false, Group_Lines);
                }
            }

            /// <summary>
            /// Call this BEFORE rotating rotor
            /// </summary>
            internal void MakeWayInAirgapBeforeRotationInFEMM(FEMM femm = null)
            {
                if (femm == null)
                    femm = FEMM.DefaultFEMM;

                if (motor.GeneralParams.FullBuildFEMModel)
                    return;

                femm.mi_clearselected();
                femm.mi_selectgroup(motor.MaterialParams.Group_Lines_Airgap);
                femm.mi_deleteselectedsegments();
            }

            /// <summary>
            /// Call this AFTER rotating rotor to make sure boundary conditional in airgap is solid
            /// Assuming the file FEMM was created using the same motor
            /// </summary>
            internal void ModifyAirgapAfterRotationInFEMM(double rotateAngleDeg, FEMM femm = null)
            {
                if (motor.GeneralParams.FullBuildFEMModel)
                    return;

                if (femm == null)
                    femm = FEMM.DefaultFEMM;

                RotorPart Rotor = motor.Rotor;
                StatorPart Stator = motor.Stator;
                MaterialParameters MaterialParams = motor.MaterialParams;

                //build boundary of motor: 2 lines, anti-periodic
                String[] boundaryNames = new String[] { "airgap-apb-1", "airgap-apb-2", "airgap-apb-3" };
                int[] Group_Lines = { MaterialParams.Group_Lines_Airgap+1,
                                MaterialParams.Group_Lines_Airgap+2,
                                MaterialParams.Group_Lines_Airgap+3};

                // re-add boundary (airgap)
                double rotateAngleRad = rotateAngleDeg * Math.PI / 180;
                double x1 = Rotor.Rrotor * Math.Cos(Rotor.alpha + rotateAngleRad);
                double y1 = Rotor.Rrotor * Math.Sin(Rotor.alpha + rotateAngleRad);
                double x2 = (Rotor.Rrotor + delta / 2) * Math.Cos(Rotor.alpha + rotateAngleRad);
                double y2 = (Rotor.Rrotor + delta / 2) * Math.Sin(Rotor.alpha + rotateAngleRad);
                double x3 = (Rotor.Rrotor + delta / 2) * Math.Cos(Rotor.alpha);
                double y3 = (Rotor.Rrotor + delta / 2) * Math.Sin(Rotor.alpha);
                double x4 = Stator.Rinstator * Math.Cos(Rotor.alpha);
                double y4 = Stator.Rinstator * Math.Sin(Rotor.alpha);

                // add segment
                femm.mi_addSegmentEx(x1, y1, x2, y2, Group_Lines[0]);
                femm.mi_addSegmentEx(x3, y3, x4, y4, Group_Lines[2]);
                femm.mi_addArcEx(x3, y3, x2, y2, rotateAngleDeg, 1, Group_Lines[1]);

                // add boundary
                foreach (String bn in boundaryNames)
                    femm.mi_addboundprop_AntiPeriodic(bn);

                // set boundary
                for (int i = 0; i < Group_Lines.Count(); i++)
                {
                    femm.mi_clearselected();
                    femm.mi_selectgroup(Group_Lines[i]);
                    femm.mi_setsegmentprop(boundaryNames[i], 0, true, false, Group_Lines[i]);
                    femm.mi_setarcsegmentprop(1, boundaryNames[i], false, Group_Lines[i]);
                }

                // copy boundary
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines[0]);
                femm.mi_selectgroup(Group_Lines[1]);
                femm.mi_selectgroup(Group_Lines[2]);
                femm.mi_copyrotate(0, 0, -2 * Rotor.alphaDegree, 1, FEMM.EditMode.group);

                // special case: airgap becomes 2 sections
                if (Math.Abs(Math.Abs(rotateAngleDeg) - 2 * Rotor.alphaDegree) < 1e-8)
                {
                    double x = (Rotor.Rrotor + delta * 0.2) * Math.Cos(rotateAngleRad);
                    double y = (Rotor.Rrotor + delta * 0.2) * Math.Sin(rotateAngleRad);
                    femm.mi_addBlockLabelEx(x, y, MaterialParams.AirMaterialName, MaterialParams.Group_Fixed_BlockLabel_Airgap);
                }
            }
        }

        #region set parts (Rotor,Stator,Airgap) to motor

        public GeneralParameters GeneralParams { get; private set; }
        public void setGeneralParameters(GeneralParameters g)
        {
            GeneralParams = g;
        }

        public RotorPart Rotor { get; private set; }
        public void setRotorParameters(RotorPart r)
        {
            Rotor = r;
            Rotor.motor = this;
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

        public StatorPart Stator { get; private set; }
        public void setStatorParameters(StatorPart s)
        {
            Stator = s;
            Stator.motor = this;//link back
        }

        public AirgapPart Airgap { get; private set; }
        public void setAirgap(AirgapPart c)
        {
            Airgap = c;
            Airgap.motor = this;
        }

        #endregion

        [JsonIgnore]
        public List<ParamValidationInfo> ListParamsValidation { get; internal set; }

        public bool IsParamsValid()
        {
            // if calculation hasn't been done
            if (ListParamsValidation == null)
                return false;

            foreach (ParamValidationInfo pvi in ListParamsValidation)
                if (pvi.msgType == ParamValidationInfo.MessageType.Error)
                    return false;

            return true;
        }

        #endregion

        #region CalcPointsCoordinates

        private bool isPointsCoordCalculated = false;

        public void CalcPointsCoordinates()
        {
            if (Stator == null)
                throw new ArgumentNullException("StatorParams null");
            if (Rotor == null)
                throw new ArgumentNullException("RotorParams null");
            if (MaterialParams == null)
                throw new ArgumentNullException("StatorParams null");
            if (Airgap == null)
                throw new ArgumentNullException("AirgapParams null");

            Rotor.CalcCoordinates();

            Stator.CalcCoordinates();

            Airgap.CalculateAirgapParams();

            ListParamsValidation = new List<ParamValidationInfo>();
            if (Rotor.ListParamsValidation != null)
                ListParamsValidation.AddRange(Rotor.ListParamsValidation);
            if (Stator.ListParamsValidation != null)
                ListParamsValidation.AddRange(Stator.ListParamsValidation);

            isPointsCoordCalculated = true;
        }

        #endregion

        #region General magnetic circuit build for calculation

        private GeneralMagnetCircuit gmc;

        /// <summary>
        /// Make a GMC, All lengths are converted from mm to m
        /// </summary>
        /// <returns></returns>
        public GeneralMagnetCircuit rebuildGMC()
        {
            if (!isPointsCoordCalculated)
                throw new InvalidOperationException("Points need to be calculated first");

            gmc = new GeneralMagnetCircuit();

            // Material
            gmc.Barray = MaterialParams.getBarray();
            gmc.Harray = MaterialParams.getHarray();

            // build rotor in GMC
            Rotor.BuildRotorInGMC(gmc);

            // build stator in GMC
            Stator.BuildStatorInGMC(gmc);

            // build airgap in GMC (Pd)
            Airgap.BuildAirgapInGMC(gmc);

            return gmc;
        }

        public GeneralMagnetCircuit getGMC()
        {
            if (gmc == null)
                rebuildGMC();
            return gmc;
        }

        #endregion

        #region Draw preview

        public void DrawPreview(Graphics graphics, float Ox, float Oy, float scale)
        {
            Rotor.DrawRotor(graphics, Ox, Oy, scale);
            Stator.DrawStator(graphics, Ox, Oy, scale);
        }

        #endregion

        #region Make femm model file

        [JsonIgnore]
        public String Path_FEMMFile { get; set; }

        [JsonIgnore]
        public String Path_ANSFile
        {
            get
            {
                if (Path_FEMMFile != null)
                    return Path.GetDirectoryName(Path_FEMMFile) + "\\" + Path.GetFileNameWithoutExtension(Path_FEMMFile) + ".ans";
                else return "";
            }
        }

        /// <summary>
        /// Make a femm model using parameters
        /// This will check if file existed and use parameters the same to this one or not.
        /// If not, build new one
        /// </summary>
        /// <param name="outfile">Output femm model</param>
        /// <param name="original">The original femm model to insert into</param>
        /// <param name="forcebuild">True if build even file existed</param>
        /// <returns>0-OK,1-File existed, analyzed,-1-ERROR</returns>
        public void MakeAFEMMModelFile(String outfile, String original = "", bool analyze = true)
        {
            // make sure coordinates were calculated
            if (!isPointsCoordCalculated)
                CalcPointsCoordinates();

            Path_FEMMFile = outfile;

            FEMM femm = FEMM.DefaultFEMM;
            // create new or open an exist
            if (original == "")
                femm.newdocument(FEMM.DocumentType.Magnetic);
            else femm.open(original);

            // setup problems params
            femm.mi_probdef(0, FEMM.UnitsType.millimeters, FEMM.ProblemType.planar, 1e-8, GeneralParams.MotorLength, 7, FEMM.ACSolverType.Succ_Approx);

            // create material data
            MaterialParams.BuildMaterialDataInFEMM();

            // build a rotor in femm
            Rotor.BuildRotorInFEMM();

            // build a stator in femm
            Stator.BuildStatorInFEMM();

            // build airgap (put label)
            Airgap.BuildAirgapInFEMM();

            // clear selected, refresh, and go to natural zoom
            femm.mi_clearselected();
            femm.mi_zoomnatural();

            // save as
            if (Path.GetDirectoryName(outfile) == "")
                outfile = Path.GetDirectoryName(original) + "\\" + outfile;
            femm.mi_saveas(outfile);

            if (analyze)
                femm.mi_analyze();

            //FEMM.mi_close();

            if (analyze)
            {
                String md5_ = Utils.CalculateMD5Hash(JsonConvert.ExportToString(this));
                log.Info("FEMM built and analyzed: " + Path_FEMMFile + ". MD5=" + md5_);

                // mark the FEM file md5_ encoded params
                FEMM.mi_modifyFEMMComment(Path_FEMMFile, md5_);
            }
        }

        public bool isFEMandANSFileReady()
        {
            // check if FEMM and ANS file existed or not
            if (File.Exists(Path_FEMMFile) && File.Exists(Path_ANSFile))
            {
                // check if FEMM was built using which parameters (use md5 to check)
                String md5 = FEMM.mi_getFEMMComment(Path_FEMMFile);
                if (md5 == Utils.CalculateMD5Hash(JsonConvert.ExportToString(this)))
                {
                    //log.Info("No Action needed. File existed and analyzed: " + Path_FEMMFile);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Static magnetism analysis results from ans file (FEM)

        private String staticResultsFile;

        public class StaticMagnetismResults
        {
            public String MD5String { get; set; }
            /// <summary>
            /// Distribution of B in airgap. Will be positive to negative. Odd function.
            /// So if use fourier analysis, -Im(Xk) is the magnitude
            /// </summary>
            public PointD[] Bairgap { get; internal set; }
            public double Bdelta_max { get; internal set; }
            public double Bdelta { get; internal set; }
            public double wd { get; internal set; }
            public double phiD { get; internal set; }
            public double phiM { get; internal set; }
            public double phiSigma { get; internal set; }
            public double Fy { get; internal set; }
            public double Fz { get; internal set; }
            public double FM { get; internal set; }
            public double phib { get; internal set; }
            public double phiFe { get; internal set; }
            public double phisigmaS { get; internal set; }
            public double psiM { get; internal set; }

            public void saveResultsToFile(String fn)
            {
                using (StreamWriter sw = new StreamWriter(fn))
                {
                    String str = JsonConvert.ExportToString(this);
                    sw.Write(str);
                }
            }

            public static StaticMagnetismResults loadResultsFromFile(String fn)
            {
                if (!File.Exists(fn))
                    return null;

                using (StreamReader sr = new StreamReader(fn))
                {
                    try
                    {
                        StaticMagnetismResults results = JsonConvert.Import<StaticMagnetismResults>(sr);
                        return results;
                    }
                    catch (Exception e)
                    {
                        log.Error("Fail open " + fn + ".Error: " + e.Message);
                        return null;
                    }
                }
            }
        }

        [JsonIgnore]
        public StaticMagnetismResults StaticResults { get; private set; }

        public StaticMagnetismResults LoadStaticResultsFromDisk()
        {
            String md5 = GetMD5String();

            // if already loaded, just return
            if (StaticResults != null && StaticResults.MD5String == md5)
                return StaticResults;

            // try load from file
            staticResultsFile = Utils.GenPathfile(Path_ANSFile, ".txt");
            StaticResults = StaticMagnetismResults.loadResultsFromFile(staticResultsFile);

            // make sure config and result match, otherwise consider staticresults is null
            if (StaticResults == null || StaticResults.MD5String != md5)
                StaticResults = null;

            return StaticResults;
        }

        /// <summary>
        /// Assuming ansfile contains the same motor parameters as this one.
        /// Assuming Calculation points and GMC build were done.
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public StaticMagnetismResults doMeasure(String fn = "", FEMM femm = null)
        {
            if (fn == "")
                fn = this.Path_ANSFile;//read from default file that attached to this motor

            if (!File.Exists(fn))
                return null;

            StaticResults = LoadStaticResultsFromDisk();

            if (StaticResults != null)
                return StaticResults;

            StaticResults = new StaticMagnetismResults();
            StaticResults.wd = Airgap.wd;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (femm == null)
                femm = FEMM.DefaultFEMM;

            // open ans file
            femm.open(fn);

            FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

            // measure            
            femm.mo_selectpoint(Rotor.xS, Rotor.yS);
            femm.mo_selectpoint(Rotor.xR, Rotor.yR);
            femm.mo_selectpoint(Rotor.xS, -Rotor.yS);
            //femm.mo_bendcontour(-360 / (2 * Rotor.p), 1);
            lir = femm.mo_lineintegral_full();
            StaticResults.phiD = Math.Abs(lir.totalBn);

            // get phiM
            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xD, Rotor.yD);
            femm.mo_selectpoint(Rotor.xG, Rotor.yG);
            FEMM.LineIntegralResult rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            StaticResults.phiM = Math.Abs(rr.totalBn * 2);

            // get phib
            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xH, Rotor.yH);
            femm.mo_selectpoint(Rotor.xH, -Rotor.yH);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            StaticResults.phib = Math.Abs(rr.totalBn);

            // get phisigmaFe
            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xB, Rotor.yB);
            femm.mo_selectpoint(Rotor.xS, Rotor.yS);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            StaticResults.phiFe = Math.Abs(rr.totalBn * 2);

            // get phisigmaS
            double xZ = (Stator.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * Rotor.p) - 2 * Math.PI / 180);
            double yZ = (Stator.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * Rotor.p) - 2 * Math.PI / 180);

            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xS, Rotor.yS);
            femm.mo_selectpoint(xZ, yZ);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            StaticResults.phisigmaS = Math.Abs(rr.totalBn * 2);

            // get B_airgap
            int n = 128;
            StaticResults.Bairgap = new PointD[n * 2];
            double RR = (Rotor.Rrotor + Stator.Rinstator) / 2;
            for (int i = 0; i < n; i++)
            {
                double a = -(i - n / 2.0) / n * 2 * Rotor.alpha;
                double px = RR * Math.Cos(a);
                double py = RR * Math.Sin(a);

                FEMM.PointValues pv = femm.mo_getpointvalues(px, py);
                StaticResults.Bairgap[i].X = 2 * Rotor.alpha * RR * i / n;
                StaticResults.Bairgap[i].Y = pv.B1 * Math.Cos(a) + pv.B2 * Math.Sin(a);
                if (double.IsNaN(StaticResults.Bairgap[i].Y))
                    StaticResults.Bairgap[i].Y = 0;

                if (StaticResults.Bdelta_max < Math.Abs(StaticResults.Bairgap[i].Y))
                    StaticResults.Bdelta_max = Math.Abs(StaticResults.Bairgap[i].Y);
            }

            // make a mirror (odd function)
            double dd = 2 * Rotor.alpha * RR;
            for (int i = 0; i < n; i++)
            {
                StaticResults.Bairgap[i + n].X = StaticResults.Bairgap[i].X + dd;
                StaticResults.Bairgap[i + n].Y = -StaticResults.Bairgap[i].Y;
            }
            StaticResults.Bdelta = StaticResults.phiD / (GeneralParams.MotorLength * Airgap.wd * 1e-6);

            // psiM
            Dictionary<String, FEMM.CircuitProperties> cps = Stator.getCircuitsPropertiesInAns(femm);
            if (cps.ContainsKey("A") && cps.ContainsKey("B") && cps.ContainsKey("C"))
            {
                Fdq fdq = ParkTransform.abc_dq(cps["A"].fluxlinkage, cps["B"].fluxlinkage, cps["C"].fluxlinkage, 0);
                StaticResults.psiM = fdq.Magnitude;
            }
            else StaticResults.psiM = double.NaN;

            //save results to disk
            StaticResults.MD5String = GetMD5String();
            StaticResults.saveResultsToFile(staticResultsFile);

            // check stopwatch
            sw.Stop();
            log.Info("Measurement took " + sw.ElapsedMilliseconds + "ms");

            femm.mo_close();

            return StaticResults;
        }

        #endregion

        #region MD5

        public String GetMD5String()
        {
            return Utils.CalculateMD5Hash(JsonConvert.ExportToString(this));
        }

        #endregion

        #region Sample Motor

        public static PMMotor GetSampleMotor()
        {
            ////// all length is in mm
            //////
            PMMotor m = new PMMotor();

            // general information
            PMMotor.GeneralParameters gp = new PMMotor.GeneralParameters();
            gp.MotorLength = 125;
            gp.FullBuildFEMModel = false;

            m.setGeneralParameters(gp);

            //stator
            PMMotor.StatorPart sp = new PMMotor.StatorPart();
            sp.Q = 36;
            sp.Dstator = 191;
            sp.Dinstator = 126;

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
            sp.PreRotateAngle = 85;

            m.setStatorParameters(sp);

            //coeff
            PMMotor.AirgapPart coeff = new PMMotor.AirgapPart();
            coeff.Kc = 1.1;
            m.setAirgap(coeff);

            // materials
            PMMotor.MaterialParameters md = new PMMotor.MaterialParameters();
            //magnet
            md.Hc = 883310;
            md.mu_M = 1.045;

            //steel            
            String BHcurvefile = @"E:\MatlabProject\zAspirant\analytical\BH.txt";
            md.readBHCurveFromFile(BHcurvefile);
            md.Lam_fill = 0.98;
            md.Lam_d = 0.635;
            md.Bsat = 1.9;

            //wire
            md.CDuct = 58;
            md.wireType = FEMM.WireType.MagnetWire;
            md.Dwire = 1.2;

            m.setMaterialData(md);

            //rotor
            PMMotor.RotorPart rip = new PMMotor.RotorPart();
            rip.ThickMag = 6; // magnet length
            rip.B1 = 5;
            rip.p = 3; // pair poles                        
            rip.DiaGap = 126 - 2 * 0.6; // airgap
            rip.D1 = 124;
            rip.O1 = 2;
            rip.O2 = 30;
            rip.Dminmag = 5;
            rip.WidthMag = 42;
            rip.DiaYoke = 32;
            rip.Rib = 10;
            rip.HRib = 2;
            rip.poletype = PMMotor.RotorPart.PoleType.MiddleSteelBridgeRectangle;

            m.setRotorParameters(rip);

            return m;
        }

        #endregion
    }
}
