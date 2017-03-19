/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Linq;

namespace calc_from_geometryOfMotor.motor
{
    public class AirgapNormal : AbstractAirgap
    {        
        // input
        public double Kc { get; set; }//Coefficient carter 

        //readonly
        internal double delta { get; set; }

        internal String AirMaterialName = "Air";
        internal int Group_Lines_Airgap = 300; //for boundary in partial motor
        internal int Group_Fixed_BlockLabel_Airgap = 310;

        public override void CalculatePoints()
        {
            var Rotor = Motor.Rotor;
            var Stator = Motor.Stator;

            delta = (Stator.DiaGap - Rotor.DiaGap) / 2.0;

            base.CalculatePoints();
        }

        public override void BuildInFEMM(FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            var Rotor = Motor.Rotor;
            var Stator = Motor.Stator;

            // create material
            femm.mi_addmaterialAir(AirMaterialName);

            femm.mi_addBlockLabelEx(Rotor.RGap + delta * 0.8, 0, AirMaterialName, Group_Fixed_BlockLabel_Airgap);

            if (!Motor.GeneralParams.FullBuildFEMModel)
            {
                //build boundary of motor: 2 lines, anti-periodic
                String boundaryName = "airgap-apb-0";
                int Group_Lines = Group_Lines_Airgap;

                double x1 = Rotor.RGap * Math.Cos(Rotor.alpha);
                double y1 = Rotor.RGap * Math.Sin(Rotor.alpha);
                double x2 = Stator.RGap * Math.Cos(Rotor.alpha);
                double y2 = Stator.RGap * Math.Sin(Rotor.alpha);

                femm.mi_addSegmentEx(x1, y1, x2, y2, Group_Lines);
                femm.mi_addSegmentEx(x1, -y1, x2, -y2, Group_Lines);

                femm.mi_addboundprop_AntiPeriodic(boundaryName);

                femm.mi_clearselected();
                femm.mi_selectgroup(Group_Lines);
                femm.mi_setsegmentprop(boundaryName, 0, true, false, Group_Lines);
            }

            base.BuildInFEMM(femm);
        }

        /// <summary>
        /// Call this BEFORE rotating rotor
        /// </summary>
        public override void RemoveBoundary(FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            if (Motor.GeneralParams.FullBuildFEMModel)
                return;

            femm.mi_clearselected();
            femm.mi_selectgroup(Group_Lines_Airgap);
            femm.mi_deleteselectedsegments();

            base.RemoveBoundary(femm);
        }

        /// <summary>
        /// Call this AFTER rotating rotor to make sure boundary conditional in airgap is solid
        /// Assuming the file FEMM was created using the same motor
        /// </summary>
        public override void AddBoundaryAtAngle(double rotateAngleDeg, FEMM femm = null)
        {
            if (Motor.GeneralParams.FullBuildFEMModel)
                return;

            if (femm == null)
                femm = FEMM.DefaultFEMM;

            var Rotor = Motor.Rotor;
            var Stator = Motor.Stator;

            //build boundary of motor: 2 lines, anti-periodic
            String[] boundaryNames = new String[] { "airgap-apb-1", "airgap-apb-2", "airgap-apb-3" };
            int[] Group_Lines = { Group_Lines_Airgap+1,
                                Group_Lines_Airgap+2,
                                Group_Lines_Airgap+3};

            // re-add boundary (airgap)
            double rotateAngleRad = rotateAngleDeg * Math.PI / 180;
            double x1 = Rotor.RGap * Math.Cos(Rotor.alpha + rotateAngleRad);
            double y1 = Rotor.RGap * Math.Sin(Rotor.alpha + rotateAngleRad);
            double x2 = (Rotor.RGap + delta / 2) * Math.Cos(Rotor.alpha + rotateAngleRad);
            double y2 = (Rotor.RGap + delta / 2) * Math.Sin(Rotor.alpha + rotateAngleRad);
            double x3 = (Rotor.RGap + delta / 2) * Math.Cos(Rotor.alpha);
            double y3 = (Rotor.RGap + delta / 2) * Math.Sin(Rotor.alpha);
            double x4 = Stator.RGap * Math.Cos(Rotor.alpha);
            double y4 = Stator.RGap * Math.Sin(Rotor.alpha);

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
                double x = (Rotor.RGap + delta * 0.2) * Math.Cos(rotateAngleRad);
                double y = (Rotor.RGap + delta * 0.2) * Math.Sin(rotateAngleRad);
                femm.mi_addBlockLabelEx(x, y, AirMaterialName, Group_Fixed_BlockLabel_Airgap);
            }

            base.AddBoundaryAtAngle(rotateAngleDeg, femm);
        }
    }
}
