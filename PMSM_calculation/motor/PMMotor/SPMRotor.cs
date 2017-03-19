/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class SPMRotor : AbstractRotor
    {
        public enum PoleType
        {
            Normal = 1
        }

        #region Input params

        //Ansys-like params                
        [ParamInfo(HelpText = "Magnet thickness")]
        public double ThickMag { get; set; }//lm  

        [ParamInfo(HelpText = "Magnet width in degree (0-180)")]
        public double GammaM { get; set; }

        [ParamInfo(HelpText = "")]
        public PoleType Poletype { get; set; }

        [ParamInfo(HelpText = "Rotate rotor an angle in preview")]
        public double PreRotateAngle { get; set; }

        // steel characteristic            
        public PointBH[] BH { get; set; }        
        public double Lam_fill { get; set; }//lamination fill factor
        public double Lam_d { get; set; }//lamination thickness 

        [ParamInfo(HelpText = "Specific mass of steel (kg/m3)")]
        public double Steel_ro { get; set; }

        [ParamInfo(HelpText = "Eddy current loss at 1T 50Hz (W/kg)")]
        public double P_eddy_10_50 { get; set; }
        [ParamInfo(HelpText = "Hysteresis loss at 1T 50Hz (W/kg)")]
        public double P_hysteresis_10_50 { get; set; }

        // magnet parameters
        public double Hc { get; set; }
        public double mu_M { get; set; } //relative mu magnet
        public double Br { get { return Hc * Constants.mu_0 * mu_M; } }

        [ParamInfo(HelpText = "Specific mass of magnet (kg/m3)")]
        public double Magnet_ro { get; set; }

        // data for build FEMM model
        internal int Group_Lines_Rotor = 101;//lines, arcs
        internal int Group_BlockLabel_Magnet_Air = 102;//air, magnet block label
        internal int Group_BlockLabel_Steel = 110;//steel block label        

        //internal String AirMaterialName = "Air (Rotor)";
        internal String MagnetMaterialName = "NdFeB 32 MGOe (Rotor)";
        internal String SteelMaterialName = "M-19 Steel (Rotor)";

        internal String BoundaryProperty = "Boundary (Rotor)";

        #endregion

        #region Readonly/Output

        public double Rrotor { get { return RGap - ThickMag; } }

        // All points related to rotor's geometric 
        internal double xA { get; private set; }
        internal double yA { get; private set; }
        internal double xB { get; private set; }
        internal double yB { get; private set; }

        internal double xR { get; private set; }
        internal double yR { get; private set; }
        internal double xS { get; private set; }
        internal double yS { get; private set; }
        internal double xRR { get; private set; }
        internal double yRR { get; private set; }
        internal double xSS { get; private set; }
        internal double ySS { get; private set; }

        #endregion

        public SPMRotor()
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

            // points            
            double gammaMradmec2 = GammaM / 180 * alpha;
            xA = RGap * Math.Cos(gammaMradmec2);
            yA = RGap * Math.Sin(gammaMradmec2);
            xB = Rrotor * Math.Cos(gammaMradmec2);
            yB = Rrotor * Math.Sin(gammaMradmec2);

            // point far right of rotors
            xR = RGap;
            yR = 0;
            xS = RGap * Math.Cos(alpha);
            yS = RGap * Math.Sin(alpha);
            xRR = Rrotor;
            yRR = 0;
            xSS = Rrotor * Math.Cos(alpha);
            ySS = Rrotor * Math.Sin(alpha);

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
            //femm.mi_addmaterialAir(AirMaterialName);
            femm.mi_addmaterialMagnet(MagnetMaterialName, mu_M, Hc, 0);
            femm.mi_addmaterialSteel(SteelMaterialName, 1, 1, Lam_d, Lam_fill, FEMM.LaminationType.NotLaminated,
                BH.Select(p => p.b).ToArray(), BH.Select(p => p.h).ToArray());

            // create boundary data
            femm.mi_addboundprop_Prescribed_A(BoundaryProperty, 0, 0, 0, 0);

            /////// Build 1 poles     
            ////Half one                   

            // arcsegments AC
            double a = GammaM / 180 * alphaDegree;
            femm.mi_addArcEx(xR, yR, xA, yA, a, 1, Group_Lines_Rotor);
            // arcsegment RS
            femm.mi_addArcEx(xRR, yRR, xSS, ySS, alphaDegree, 1, Group_Lines_Rotor);
            // segments (magnet)
            femm.mi_addSegmentEx(xA, yA, xB, yB, Group_Lines_Rotor);

            // blocks                        
            // magnet block label
            double magBlockLabelX = (xR + xRR) / 2;
            double magBlockLabelY = 0;
            femm.mi_addBlockLabelEx(magBlockLabelX, magBlockLabelY, MagnetMaterialName, Group_BlockLabel_Magnet_Air, 180);//+180 but set back later
            //femm.mi_addBlockLabelEx(magBlockLabelX, -magBlockLabelY, MagnetBlockName, Group_Label, 90 - alphaM * 180 / Math.PI + 180);

            ///// mirrored all
            femm.mi_clearselected();
            femm.mi_selectgroup(Group_Lines_Rotor);
            femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
            femm.mi_mirror(0, 0, 1, 0, FEMM.EditMode.group);

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
            femm.mi_setblockprop(MagnetMaterialName, true, 0, "", 0, Group_BlockLabel_Magnet_Air, 0);

            if (fullbuild)
            {
                //////// Build p-1 pair of poles remaining
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Rotor);
                femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
                femm.mi_copyrotate(0, 0, 360 / p, p - 1, FEMM.EditMode.group);
            }

            /////// The remaining: not symmetrical: add label for rotor steel
            femm.mi_addBlockLabelEx(RYoke / 2, 0, SteelMaterialName, Group_BlockLabel_Steel);


            if (fullbuild)
            {
                // pre-rotate rotor (only in fullbuild)
                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines_Rotor);
                femm.mi_selectgroup(Group_BlockLabel_Magnet_Air);
                femm.mi_selectgroup(Group_BlockLabel_Steel);
                femm.mi_moverotate(0, 0, PreRotateAngle, FEMM.EditMode.group);
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
            PointF pR = new PointF((float)xR, (float)yR);
            PointF pS = new PointF((float)xS, (float)yS);
            PointF pRR = new PointF((float)xRR, (float)yRR);
            PointF pSS = new PointF((float)xSS, (float)ySS);

            float Rrotor = (float)this.Rrotor;
            float alphaDegree = (float)this.alphaDegree;
            float RGap = (float)this.RGap;
            float gammaMDeg_2 = (float)(this.GammaM / 180 * alphaDegree);

            using (Pen pen = new Pen(Color.Black, 1.0F / scale))
            using (Brush brushred = new SolidBrush(Color.Red))
            using (Brush brushblue = new SolidBrush(Color.Blue))
            {
                // for 2*p poles
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
                        graphics.DrawArc(pen, -Rrotor, -Rrotor, 2 * Rrotor, 2 * Rrotor, -alphaDegree, 2 * alphaDegree);

                        //rotor line
                        graphics.DrawArc(pen, -RGap, -RGap, 2 * RGap, 2 * RGap, -gammaMDeg_2, 2 * gammaMDeg_2);

                        // fill color magnets to identify N/S
                        Brush brfill = (i % 2 == 0 ? brushred : brushblue);
                        var gpath = new GraphicsPath();
                        gpath.AddArc(-RGap, -RGap, 2 * RGap, 2 * RGap, 0, gammaMDeg_2);
                        gpath.AddLine(pB, pA);
                        gpath.AddArc(-Rrotor, -Rrotor, 2 * Rrotor, 2 * Rrotor, gammaMDeg_2, -gammaMDeg_2);
                        gpath.AddLine(pRR, pR);
                        graphics.FillPath(brfill, gpath);
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
