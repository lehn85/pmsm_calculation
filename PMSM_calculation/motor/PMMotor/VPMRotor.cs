using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class VPMRotor : AbstractRotor
    {
        public enum PoleType
        {
            MiddleAir = 5,
            MiddleSteelBridgeTrapezoid = 4,
            MiddleSteelBridgeRectangle = 3
        }

        #region Input params

        //Ansys-like params        
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

        [ParamInfo(HelpText = "")]
        public PoleType Poletype { get; set; }

        // steel characteristic            
        public PointBH[] BH { get; set; }
        public double Bsat { get; set; }//value of flux density, which is considered saturated
        public double Lam_fill { get; set; }//lamination fill factor
        public double Lam_d { get; set; }//lamination thickness  

        [ParamInfo(HelpText = "Specific mass of steel (kg/m3)")]
        public double Steel_ro { get; set; }

        [ParamInfo(HelpText = "Eddy current loss at 1T 50Hz (W/kg)")]
        public double P_eddy_10_50 { get; set; }

        [ParamInfo(HelpText = "Hysteresis loss at 1T 50Hz (W/kg)")]
        public double P_hysteresis_10_50 { get; set; }

        // magnet parameters
        [ParamInfo(HelpText = "Fictive magnetic coercive force (A/m)")]
        public double Hc { get; set; }
        [ParamInfo(HelpText = "Relative magnetic conductivity")]
        public double mu_M { get; set; } //relative mu magnet
        public double Br { get { return Hc * Constants.mu_0 * mu_M; } }
        [ParamInfo(HelpText = "Magnetic coercive force at knee point (A/m)")]
        public double Hck { get; set; }

        [ParamInfo(HelpText = "Specific mass of magnet (kg/m3)")]
        public double Magnet_ro { get; set; }

        // data for build FEMM model
        internal int Group_Lines_Rotor = 101;//lines, arcs
        internal int Group_BlockLabel_Magnet_Air = 102;//air, magnet block label
        internal int Group_BlockLabel_Steel = 110;//steel block label        

        internal String AirMaterialName = "Air (Rotor)";
        internal String MagnetMaterialName = "NdFeB 32 MGOe (Rotor)";
        internal String SteelMaterialName = "Electric Steel (Rotor)";

        internal String BoundaryProperty = "Boundary (Rotor)";

        #endregion

        #region Readonly/Output

        //// derived (readonly)        
        internal double R1 { get { return D1 / 2; } }
        internal double Rrotor { get { return RGap; } }
        internal double alphaM { get; private set; }//angle between magnet and the Ox
        internal double alphaMDegree { get { return alphaM * 180 / Math.PI; } }
        internal double gammaMedeg { get; private set; }//angle 0-180 indicate magnet open angle (electrical angle)   
        internal double gammaMerad { get { return gammaMedeg * Math.PI / 180; } }         

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

        #region Overriden properties

        public override double Volume
        {
            get
            {
                return RGap * RGap * Math.PI * Motor.GeneralParams.MotorLength * 1e-9;
            }
        }

        public override double Mass
        {
            get
            {
                return MassSteel + MassMagnet;
            }
        }

        public override double Inertia
        {
            get
            {
                return 0.5 * Mass * RGap * RGap * 1e-6;
            }
        }

        public double VolumeMagnet
        {
            get
            {
                return 2 * p * ThickMag * WidthMag * Motor.GeneralParams.MotorLength * 1e-9;
            }
        }

        public double MassMagnet
        {
            get
            {
                return VolumeMagnet * Magnet_ro;
            }
        }

        public double VolumeSteel
        {
            get
            {
                return RGap * RGap * Math.PI * Motor.GeneralParams.MotorLength * 1e-9 - VolumeMagnet;
            }
        }

        public double MassSteel
        {
            get
            {
                return VolumeSteel * Steel_ro;
            }
        }

        #endregion

        public VPMRotor()
            : base()
        {

        }

        /// <summary>
        /// Calculate all points' coordinates
        /// </summary>
        public override void CalculatePoints()
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
            switch (Poletype)
            {
                case PoleType.MiddleSteelBridgeTrapezoid:
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

                case PoleType.MiddleSteelBridgeRectangle:
                    xK = xJ + B1 / Math.Sin(alphaM);
                    yK = yJ;
                    break;

                case PoleType.MiddleAir://5
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
            if (Poletype == PoleType.MiddleSteelBridgeRectangle && Dminmag < O1)
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
            if (double.IsNaN(maxWidthMag))
            {
                maxWidthMag = 0;
            }

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
            gammaMedeg = 2 * Math.Atan(yC / xC) * p * 180 / Math.PI;

            // point far right of rotors
            xR = Rrotor;
            yR = 0;
            xS = Rrotor * Math.Cos(alpha);
            yS = Rrotor * Math.Sin(alpha);

            // 
            isPointsCoordCalculated = true;
        }

        /// <summary>
        /// Assuming a FEMM window is opened, build rotor into that
        /// </summary>
        /// <param name="MaterialParams"></param>
        public override void BuildInFEMM(FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            bool fullbuild = Motor.GeneralParams.FullBuildFEMModel;

            // create material data            
            femm.mi_addmaterialAir(AirMaterialName);
            femm.mi_addmaterialMagnet(MagnetMaterialName, mu_M, Hc, 0);
            femm.mi_addmaterialSteel(SteelMaterialName, 1, 1, Lam_d, Lam_fill, FEMM.LaminationType.NotLaminated,
                BH.Select(p => p.b).ToArray(), BH.Select(p => p.h).ToArray());

            // create boundary data
            femm.mi_addboundprop_Prescribed_A(BoundaryProperty, 0, 0, 0, 0);

            /////// Build 1 poles     
            ////Half one       
            // segments (magnet)
            femm.mi_addSegmentEx(xA, yA, xB, yB, Group_Lines_Rotor);//AB
            femm.mi_addSegmentEx(xB, yB, xE, yE, Group_Lines_Rotor);//BE
            femm.mi_addSegmentEx(xD, yD, xE, yE, Group_Lines_Rotor);//DE
            femm.mi_addSegmentEx(xE, yE, xF, yF, Group_Lines_Rotor);//EF
            femm.mi_addSegmentEx(xF, yF, xC, yC, Group_Lines_Rotor);//FC
            femm.mi_addSegmentEx(xD, yD, xG, yG, Group_Lines_Rotor);//DG
            femm.mi_addSegmentEx(xH, yH, xG, yG, Group_Lines_Rotor);//HG
            femm.mi_addSegmentEx(xH, yH, xI, yI, Group_Lines_Rotor);//HI
            femm.mi_addSegmentEx(xI, yI, xF, yF, Group_Lines_Rotor);//IF
            femm.mi_addSegmentEx(xH, yH, xJ, yJ, Group_Lines_Rotor);//HJ
            femm.mi_addSegmentEx(xI, yI, xK, yK, Group_Lines_Rotor);//IK
            if (Poletype == PoleType.MiddleAir)
                femm.mi_addSegmentEx(xJ, yJ, xJ, 0, Group_Lines_Rotor);//JJ1
            else femm.mi_addSegmentEx(xJ, yJ, xK, yK, Group_Lines_Rotor);//JK                                               

            // arcsegments AC
            double a = (Math.Atan(yA / xA) - Math.Atan(yC / xC)) * 180 / Math.PI;
            femm.mi_addArcEx(xC, yC, xA, yA, a, 1, Group_Lines_Rotor);
            // arcsegment RS
            femm.mi_addArcEx(xR, yR, xS, yS, alphaDegree, 1, Group_Lines_Rotor);

            // blocks
            // air block label
            double air1X = (xA + xB + xC) / 3;
            double air1Y = (yA + yB + yC) / 3;
            femm.mi_addBlockLabelEx(air1X, air1Y, AirMaterialName, Group_BlockLabel_Magnet_Air);
            //FEMM.mi_addBlockLabelEx(air1X, -air1Y, AirBlockName, Group_Label);

            if (Poletype != PoleType.MiddleAir)
            {
                double air2X = (xJ + xK + xI + xH) / 4;
                double air2Y = (yJ + yK + yI + yH) / 4;
                femm.mi_addBlockLabelEx(air2X, air2Y, AirMaterialName, Group_BlockLabel_Magnet_Air);
                //  femm.mi_addBlockLabelEx(air2X, -air2Y, AirBlockName, Group_Label);
            }

            // magnet block label
            double magBlockLabelX = (xD + xI) / 2;
            double magBlockLabelY = (yD + yI) / 2;
            femm.mi_addBlockLabelEx(magBlockLabelX, magBlockLabelY, MagnetMaterialName, Group_BlockLabel_Magnet_Air, alphaM * 180 / Math.PI - 90 + 180);//+180 but set back later
            //femm.mi_addBlockLabelEx(magBlockLabelX, -magBlockLabelY, MagnetBlockName, Group_Label, 90 - alphaM * 180 / Math.PI + 180);

            ///// mirrored all
            femm.mi_clearselected();
            femm.mi_selectgroup(Group_Lines_Rotor);
            femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
            femm.mi_mirror(0, 0, 1, 0, FEMM.EditMode.group);

            //// the remainings: not symmetrical
            if (Poletype == PoleType.MiddleAir)
                femm.mi_addBlockLabelEx((xJ + xK0) / 2, 0, AirMaterialName, Group_BlockLabel_Magnet_Air);

            ////////// Build 2 poles
            // copy pole if full build
            if (fullbuild)
            {
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Rotor);
                femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
                femm.mi_copyrotate(0, 0, 360 / (2 * p), 1, FEMM.EditMode.group);
            }
            // rotate the magnet direction (of the first pole)
            femm.mi_clearselected();
            femm.mi_selectlabel(magBlockLabelX, magBlockLabelY);
            femm.mi_setblockprop(MagnetMaterialName, true, 0, "", alphaM * 180 / Math.PI - 90, Group_BlockLabel_Magnet_Air, 0);
            femm.mi_clearselected();
            femm.mi_selectlabel(magBlockLabelX, -magBlockLabelY);
            femm.mi_setblockprop(MagnetMaterialName, true, 0, "", 90 - alphaM * 180 / Math.PI, Group_BlockLabel_Magnet_Air, 0);

            if (fullbuild)
            {
                //////// Build p-1 pair of poles remaining
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Rotor);
                femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
                femm.mi_copyrotate(0, 0, 360 / p, p - 1, FEMM.EditMode.group);
            }

            /////// The remaining: not symmetrical: add label for rotor steel
            femm.mi_addBlockLabelEx((DiaYoke / 2 + O2 / 2), 0, SteelMaterialName, Group_BlockLabel_Steel);


            if (fullbuild)
            {
                // pre-rotate rotor (only in fullbuild)
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Rotor);
                femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
                femm.mi_selectgroup(Group_BlockLabel_Steel);
                femm.mi_moverotate(0, 0, PreviewRotateAngle, FEMM.EditMode.group);
            }

            if (!fullbuild)
            {
                //build boundary of motor: 2 lines, anti-periodic
                String boundaryName = "rotor-apb-1";
                femm.mi_addSegmentEx(0, 0, xS, yS, Group_Lines_Rotor);
                femm.mi_addSegmentEx(0, 0, xS, -yS, Group_Lines_Rotor);

                femm.mi_addboundprop_AntiPeriodic(boundaryName);

                femm.mi_clearselected();
                femm.mi_selectsegment(xS, yS);
                femm.mi_selectsegment(xS, -yS);
                femm.mi_setsegmentprop(boundaryName, 0, true, false, Group_Lines_Rotor);
            }
        }

        public override void RotateRotorInFEMM(double rotorAngle, FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            femm.mi_selectgroup(Group_Lines_Rotor);
            femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
            femm.mi_selectgroup(Group_BlockLabel_Steel);
            femm.mi_moverotate(0, 0, rotorAngle, FEMM.EditMode.group);
        }

        /// <summary>
        /// Draw rotor with graphics object
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="Ox"></param>
        /// <param name="Oy"></param>
        /// <param name="scale"></param>
        public override void DrawPreview(Graphics graphics, float Ox, float Oy, float scale)
        {
            PointF pA = new PointF((float)xA, (float)yA);
            PointF pB = new PointF((float)xB, (float)yB);
            PointF pC = new PointF((float)xC, (float)yC);
            PointF pD = new PointF((float)xD, (float)yD);
            PointF pE = new PointF((float)xE, (float)yE);
            PointF pF = new PointF((float)xF, (float)yF);
            PointF pG = new PointF((float)xG, (float)yG);
            PointF pH = new PointF((float)xH, (float)yH);
            PointF pI = new PointF((float)xI, (float)yI);
            PointF pJ = new PointF((float)xJ, (float)yJ);
            PointF pK = new PointF((float)xK, (float)yK);

            float startAngle = (float)(Math.Atan(yC / xC) * 180 / Math.PI);
            float sweepAngle = (float)((Math.Atan(yA / xA) - Math.Atan(yC / xC)) * 180 / Math.PI);
            float Rrotor = (float)this.Rrotor;
            float alpha = (float)(this.alphaDegree);
            float R1 = (float)this.R1;

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;

            using (Pen pen = new Pen(Color.Black, 1.0F / scale))
            using (Brush brushred = new SolidBrush(Color.Red))
            using (Brush brushblue = new SolidBrush(Color.Blue))
            {
                for (int i = 0; i < 2 * p; i++)
                {
                    float rotate = (float)(i * 180.0 / p);
                    float[] scaleYs = { scale, -scale };
                    //draw 2 symmetrical parts
                    foreach (float scaleY in scaleYs)
                    {
                        // transform system coordinate
                        graphics.ResetTransform();
                        graphics.TranslateTransform(Ox, Oy);
                        graphics.ScaleTransform(scale, scaleY);
                        graphics.RotateTransform(rotate - Math.Sign(scaleY) * (float)PreviewRotateAngle);

                        //draw lines
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
                        if (Poletype == PoleType.MiddleAir)
                            graphics.DrawLine(pen, (float)xJ, (float)yJ, (float)xJ, 0);//JJ1
                        else graphics.DrawLine(pen, pJ, pK);//JK
                        graphics.DrawArc(pen, -R1, -R1, 2 * R1, 2 * R1, startAngle, sweepAngle);

                        //rotor line
                        graphics.DrawArc(pen, -Rrotor, -Rrotor, 2 * Rrotor, 2 * Rrotor, 0, alpha);

                        // fill color magnets to identify N/S
                        Brush brfill = (i % 2 == 0 ? brushred : brushblue);
                        graphics.FillPolygon(brfill, new PointF[] { pD, pF, pI, pG });
                    }
                }
            }
        }

        #region Measure rotor (torque,...)

        public override double getTorqueInAns(FEMM femm)
        {
            femm.mo_clearblock();
            femm.mo_groupselectblock(Group_BlockLabel_Magnet_Air);
            femm.mo_groupselectblock(Group_BlockLabel_Steel);
            double torque = femm.mo_blockintegral(FEMM.BlockIntegralType.Steady_state_weighted_stress_tensor_torque);

            if (!Motor.GeneralParams.FullBuildFEMModel)
                torque *= 2 * p;

            return torque;
        }

        #endregion
    }
}
