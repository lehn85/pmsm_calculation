using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Drawing;

namespace calc_from_geometryOfMotor.motor
{
    public class Stator3Phase : AbstractStator
    {
        private static readonly ILog log = LogManager.GetLogger("PMSMGeo");

        public Stator3Phase()
            : base()
        {
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

        /// <summary>
        /// Vector FA angle, define this corresponding to winding configs. Between A+ and A-.
        /// From center slot 1. to middle A+A-
        /// </summary>
        [ParamInfo(HelpText = "Angle vector mmf FA. pos 3 o'clock to center coilAA'. Used for abc-dq conversion.")]
        public double VectorMMFAngle { get; set; }

        // actually translated windings from WindingsConfig string
        // coil (ANSYS: coil) - 1 slot
        public class Coil
        {
            public String inCircuit;
            public int Nturns;//minus is in, plus is out
        }
        internal List<Coil> coils;

        // circuit (ANSYS:winding group) data
        public class Circuit
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

        [ParamInfo(HelpText = "Slot fill factor")]
        public double Kfill { get; set; }

        // copper-wire properties
        [ParamInfo(HelpText = "Wire type")]
        public FEMM.WireType WireType { get; set; }

        [ParamInfo(HelpText = "Electrical conductivity of copper (MS/m)")]
        public double WireConduct { get; set; } //s=1/ohm,=1/ohm.m

        [ParamInfo(HelpText = "Diameter of wire")]
        public double WireDiameter { get; set; }

        [ParamInfo(HelpText = "Specific mass of steel (kg/m3)")]
        public double Copper_ro { get; set; }

        // Steel material
        public PointBH[] BH { get; set; }
        public double Lam_fill { get; set; }//lamination fill factor
        public double Lam_d { get; set; }//lamination thickness 

        [ParamInfo(HelpText = "Specific mass of steel (kg/m3)")]
        public double Steel_ro { get; set; }

        [ParamInfo(HelpText = "Eddy current loss at 1T 50Hz (W/kg)")]
        public double P_eddy_10_50 { get; set; }

        [ParamInfo(HelpText = "Hysteresis loss at 1T 50Hz (W/kg)")]
        public double P_hysteresis_10_50 { get; set; }

        // data for build FEMM model        
        internal int Group_Lines_Stator = 201;//lines, arcs
        internal int Group_BlockLabel_Wire = 202;//copper, ...
        internal int Group_BlockLabel_Steel = 210;

        internal String SteelMaterialName = "Electric Steel (Stator)";
        internal String WireMaterialName = "Copper";

        internal String BoundaryProperty = "Boundary (Stator)";

        #endregion

        #region Internal/Readonly (for motor object only)

        internal double PreRotateAngle { get { return RotatedForFEMMAngle; } }

        // readonly from outside        
        internal double q { get; private set; }//number of slot(teeth) per phase per pole (1,2, something)

        // readonly
        internal double Rinstator { get { return DiaGap / 2; } }
        internal double Rstator { get { return DiaYoke / 2; } }
        internal double alpha { get; private set; }
        internal double alphaDegree { get { return alpha * 180 / Math.PI; } }
        internal double Sslot { get; private set; }//square of slot
        internal double wirelength_oneturn { get; private set; }
        internal double resistancePhase { get; private set; }

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

        #endregion

        #region Overriden properties and new properties

        public override double Volume
        {
            get
            {
                return (Rstator * Rstator - Rinstator * Rinstator) * Math.PI * Motor.GeneralParams.MotorLength * 1e-9;
            }
        }

        public override double Mass
        {
            get
            {
                return MassSteel + MassCopper;
            }
        }

        public double VolumeSlot
        {
            get
            {
                return Q * (BS1 + BS2) * (HS1 + HS2) / 2 * Motor.GeneralParams.MotorLength * 1e-9;
            }
        }

        public double VolumeSteel
        {
            get
            {
                return (Rstator * Rstator - Rinstator * Rinstator) * Math.PI * Motor.GeneralParams.MotorLength * 1e-9 - VolumeSlot;
            }
        }

        public double MassSteel
        {
            get
            {
                return VolumeSteel * Steel_ro;
            }
        }

        public double VolumeCopper
        {
            get
            {
                return Q * NStrands * (WireDiameter / 2) * (WireDiameter / 2) * Math.PI * Motor.GeneralParams.MotorLength * 1e-9;
            }
        }

        public double MassCopper
        {
            get
            {
                return VolumeCopper * Copper_ro;
            }
        }

        #endregion        


        /// <summary>
        /// Calculate points' coordinate
        /// </summary>
        public override void CalculatePoints()
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

            int coil_step = -1;//coil step [slot]            

            // wrap try-catch because 
            // anything can go wrong when handle string, parse int
            try
            {
                String[] s1 = WindingsConfig.Replace(" ", "").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String wd in s1)//each coil (circuit)
                {
                    //each one like this: A:1-7,8-9
                    String[] s2 = wd.Split(new char[] { ':', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);//s2[0]=A, s2[i]=1,s[i+1]=7,...                    

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

                        // calc coil step to get actual length of wire
                        if (coil_step < 0)
                        {
                            coil_step = outslot - inslot;
                            if (coil_step < 0)
                                coil_step += Q;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                addValidationInfo("WindingsConfig", e.Message, ParamValidationInfo.MessageType.Error);
            }

            // other parameters 
            AbstractRotor Rotor = Motor.Rotor;
            int PhaseCount = (circuits.Count > 0) ? circuits.Count : 3;
            q = Q / (PhaseCount * 2 * Rotor.p);
            double Swire = (WireDiameter / 2) * (WireDiameter / 2) * Math.PI;//mm^2
            // length outside slot = 1span+up-down
            double l2 = coil_step * 2 * Math.PI * (Rinstator + HS0 + HS1 + HS2 / 2) / Q + 2 * Math.Sqrt(Swire * NStrands / Math.PI) * 2.5;
            wirelength_oneturn = 2 * Motor.GeneralParams.MotorLength + 2 * l2;//mm
            resistancePhase = 1 / WireConduct * Rotor.p * q * NStrands * wirelength_oneturn * 1e-3 / Swire;

            // call base method
            base.CalculatePoints();
        }

        /// <summary>
        /// Build Stator in FEMM
        /// </summary>
        /// <param name="MaterialParams"></param>
        public override void BuildInFEMM(FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            AbstractRotor Rotor = Motor.Rotor;
            bool fullbuild = Motor.GeneralParams.FullBuildFEMModel;

            // create material            
            femm.mi_addmaterialSteel(SteelMaterialName, 1, 1, Lam_d, Lam_fill, FEMM.LaminationType.NotLaminated,
                BH.Select(p => p.b).ToArray(), BH.Select(p => p.h).ToArray());
            femm.mi_addmaterialCopper(WireMaterialName, WireConduct, WireType, 1, WireDiameter);

            // create boundary
            femm.mi_addboundprop_Prescribed_A(BoundaryProperty, 0, 0, 0, 0);

            /////// Build half slot     
            ////segments - lines
            femm.mi_addSegmentEx(xB, yB, xC, yC, Group_Lines_Stator);//BC
            femm.mi_addSegmentEx(xD, yD, xE, yE, Group_Lines_Stator);//DE
            femm.mi_addSegmentEx(xF, yF, xF, 0, Group_Lines_Stator);//FF1
            if (yC != yCC)
                femm.mi_addSegmentEx(xC, yC, xCC, yCC, Group_Lines_Stator);//CC'                                                

            //// arcsegments 
            //BA
            double a = (Math.Atan(yA / xA) - Math.Atan(yB / xB)) * 180 / Math.PI;
            femm.mi_addArcEx(xB, yB, xA, yA, a, 1, Group_Lines_Stator);
            //DC'
            femm.mi_addArcEx(xD, yD, xCC, yCC, 90, 30, Group_Lines_Stator);
            //EF
            femm.mi_addArcEx(xF, yF, xE, yE, 90, 30, Group_Lines_Stator);

            //// the coil
            femm.mi_addSegmentEx(xD2, yD2, xE2, yE2, Group_Lines_Stator);
            femm.mi_addSegmentEx(xF2, yF2, xF2, 0, Group_Lines_Stator);
            femm.mi_addSegmentEx(xCC2, yCC2, xCC2, 0, Group_Lines_Stator);
            //CC2-D2
            femm.mi_addArcEx(xD2, yD2, xCC2, yCC2, 90, 30, Group_Lines_Stator);
            //EF
            femm.mi_addArcEx(xF2, yF2, xE2, yE2, 90, 30, Group_Lines_Stator);

            ////// mirrored half to one
            femm.mi_clearselected();
            femm.mi_selectgroup(Group_Lines_Stator);
            femm.mi_mirror(0, 0, 1, 0, FEMM.EditMode.group);

            //////// Build Q slots (copy)               
            femm.mi_clearselected();
            femm.mi_selectgroup(Group_Lines_Stator);
            femm.mi_selectgroup(Group_BlockLabel_Wire);
            if (fullbuild)
                femm.mi_copyrotate(0, 0, 360.0 / Q, Q - 1, FEMM.EditMode.group);
            else
            {
                femm.mi_copyrotate(0, 0, 360.0 / Q, Q / (2 * Rotor.p) - 1, FEMM.EditMode.group);

                // rotate so they match with rotor
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Stator);
                femm.mi_selectgroup(Group_BlockLabel_Wire);
                // rotate angle to 
                double shiftangle = -Rotor.alphaDegree + 180.0 / Q;
                femm.mi_moverotate(0, 0, shiftangle, FEMM.EditMode.group);
            }

            ////// Block labels (steel)               
            femm.mi_addBlockLabelEx((xF + Rstator) / 2, 0, SteelMaterialName, Group_BlockLabel_Steel);

            /////// Stator outer lines (2 arcs)
            if (fullbuild)
            {
                femm.mi_addArcEx(Rstator, 0, -Rstator, 0, 180, 10, Group_Lines_Stator);
                femm.mi_addArcEx(-Rstator, 0, Rstator, 0, 180, 10, Group_Lines_Stator);
            }
            else
            {
                femm.mi_addArcEx(Rstator * Math.Cos(Rotor.alpha), -Rstator * Math.Sin(Rotor.alpha),
                    Rstator * Math.Cos(Rotor.alpha), Rstator * Math.Sin(Rotor.alpha), 2 * Rotor.alphaDegree, 10, Group_Lines_Stator);
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

            femm.mi_setarcsegmentprop(10, BoundaryProperty, false, Group_Lines_Stator);

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
                    femm.mi_addBlockLabelEx(x, y, WireMaterialName, Group_BlockLabel_Wire, sci.inCircuit, sci.Nturns);
                }
                else
                {
                    //angle go clockwise from 3 o'clock (=0 degree in decarter), shift +pi/Q (to match rotor)
                    int nn = Q / (2 * Rotor.p);
                    double aa = -2 * Math.PI * i / Q + (nn % 2 == 0 ? Math.PI / Q : 0);
                    double x = r * Math.Cos(aa);
                    double y = r * Math.Sin(aa);
                    if (aa > -Rotor.alpha || aa < -2 * Math.PI + Rotor.alpha)
                        femm.mi_addBlockLabelEx(x, y, WireMaterialName, Group_BlockLabel_Wire, sci.inCircuit, sci.Nturns);
                }
            }

            if (fullbuild)
            {
                //pre-rotate stator (fullbuild only)
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Stator);
                femm.mi_selectgroup(Group_BlockLabel_Wire);
                femm.mi_selectgroup(Group_BlockLabel_Steel);
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

                femm.mi_addSegmentEx(x1, y1, x2, y2, Group_Lines_Stator);
                femm.mi_addSegmentEx(x1, -y1, x2, -y2, Group_Lines_Stator);

                femm.mi_addboundprop_AntiPeriodic(boundaryName);

                femm.mi_clearselected();
                femm.mi_selectsegment(x2, y2);
                femm.mi_selectsegment(x2, -y2);
                femm.mi_setsegmentprop(boundaryName, 0, true, false, Group_Lines_Stator);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="Ox"></param>
        /// <param name="Oy"></param>
        /// <param name="scale"></param>
        public override void DrawPreview(Graphics graphics, float Ox, float Oy, float scale)
        {
            PointF pA = new PointF((float)xA, (float)yB);
            PointF pB = new PointF((float)xB, (float)yB);
            PointF pC = new PointF((float)xC, (float)yC);
            PointF pCC = new PointF((float)xCC, (float)yCC);
            PointF pD = new PointF((float)xD, (float)yD);
            PointF pE = new PointF((float)xE, (float)yE);
            PointF pF = new PointF((float)xF, (float)yF);

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

        #region With currents

        /// <summary>
        /// Set current amps
        /// </summary>
        /// <param name="currents"></param>
        /// <param name="femm"></param>
        public override void SetStatorCurrentsInFEMM(IDictionary<String, double> currents, FEMM femm)
        {
            if (currents == null)
                return;
            // edit some transient params (usually stator currents) ?
            var tkeys = currents.Keys;
            foreach (String key in tkeys)
                femm.mi_modifycircuitCurrent(key, currents[key]);
        }

        /// <summary>
        /// Get all circuits properties in an opened window femm (with opened ans file)
        /// </summary>
        /// <param name="femm"></param>
        /// <returns></returns>
        public override Dictionary<String, FEMM.CircuitProperties> getCircuitsPropertiesInAns(FEMM femm)
        {
            if (circuits.Count == 0)
                return null;

            Dictionary<String, FEMM.CircuitProperties> circuitProperties = new Dictionary<string, FEMM.CircuitProperties>();
            foreach (Circuit c in circuits)
            {
                FEMM.CircuitProperties cp = femm.mo_getcircuitproperties(c.name);
                if (!Motor.GeneralParams.FullBuildFEMModel)
                {
                    cp.fluxlinkage *= 2 * Motor.Rotor.p;
                    cp.volts *= 2 * Motor.Rotor.p;
                }

                circuitProperties.Add(cp.name, cp);
            }

            return circuitProperties;
        }

        #endregion
    }
}
