/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;


namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class VPMStaticAnalyser : PMStaticAnalyser
    {
        protected override void measureInOpenedFem(FEMM femm)
        {
            // Begin to measure
            FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

            VPMMotor Motor = this.Motor as VPMMotor;
            VPMRotor Rotor = Motor.Rotor;
            Stator3Phase Stator = Motor.Stator;
            GeneralParameters GeneralParams = Motor.GeneralParams;
            AirgapNormal Airgap = Motor.Airgap;
            PMStaticResults Results = this.Results as PMStaticResults;

            double xS = Rotor.Rrotor * Math.Cos(Rotor.alpha*0.9999);
            double yS = Rotor.Rrotor * Math.Sin(Rotor.alpha*0.9999);

            // get phiD            
            femm.mo_addcontour(xS, yS);
            //femm.mo_selectpoint(Rotor.xR, Rotor.yR);
            femm.mo_addcontour(xS, -yS);
            femm.mo_bendcontour(-360 / (2 * Rotor.p), 1);
            lir = femm.mo_lineintegral_full();
            Results.phiD = Math.Abs(lir.totalBn);

            // get phiM
            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xD, Rotor.yD);
            femm.mo_selectpoint(Rotor.xG, Rotor.yG);
            FEMM.LineIntegralResult rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            Results.phiM = Math.Abs(rr.totalBn * 2);

            // get phib
            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xH, Rotor.yH);
            femm.mo_selectpoint(Rotor.xH, -Rotor.yH);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            Results.phib = Math.Abs(rr.totalBn);

            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xE, Rotor.yE);
            femm.mo_selectpoint(Rotor.xA, Rotor.yA);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            Results.phib += Math.Abs(rr.totalBn * 2);

            // get phisigmaFe
            femm.mo_clearcontour();
            femm.mo_addcontour(Rotor.xA, Rotor.yA);
            femm.mo_addcontour(xS, yS);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            Results.phiFe = Math.Abs(rr.totalBn * 2);

            // get phisigmaS
            double xZ = (Stator.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * Rotor.p) - 2 * Math.PI / 180);
            double yZ = (Stator.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * Rotor.p) - 2 * Math.PI / 180);

            femm.mo_clearcontour();
            femm.mo_selectpoint(Rotor.xS, Rotor.yS);
            femm.mo_selectpoint(xZ, yZ);
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Bn);
            Results.phisigmaS = Math.Abs(rr.totalBn * 2);

            // get FM
            femm.mo_clearcontour();
            femm.mo_addcontour((Rotor.xI + Rotor.xF) / 2, (Rotor.yI + Rotor.yF) / 2);
            femm.mo_addcontour((Rotor.xD + Rotor.xG) / 2, (Rotor.yD + Rotor.yG) / 2);            
            rr = femm.mo_lineintegral(FEMM.LineIntegralType.Ht);
            Results.FM = Math.Abs(rr.totalHt);

            // get B_airgap
            int n = 128;
            Results.Bairgap = new PointD[n * 2];
            double RR = (Rotor.Rrotor + Stator.Rinstator) / 2;
            for (int i = 0; i < n; i++)
            {
                double a = -(i - n / 2.0) / n * 2 * Rotor.alpha;
                double px = RR * Math.Cos(a);
                double py = RR * Math.Sin(a);

                FEMM.PointValues pv = femm.mo_getpointvalues(px, py);
                Results.Bairgap[i].X = 2 * Rotor.alpha * RR * i / n;
                Results.Bairgap[i].Y = pv.B1 * Math.Cos(a) + pv.B2 * Math.Sin(a);
                if (double.IsNaN(Results.Bairgap[i].Y))
                    Results.Bairgap[i].Y = 0;

                if (Results.Bdelta_max < Math.Abs(Results.Bairgap[i].Y))
                    Results.Bdelta_max = Math.Abs(Results.Bairgap[i].Y);
            }

            // make a mirror (odd function)
            double dd = 2 * Rotor.alpha * RR;
            for (int i = 0; i < n; i++)
            {
                Results.Bairgap[i + n].X = Results.Bairgap[i].X + dd;
                Results.Bairgap[i + n].Y = -Results.Bairgap[i].Y;
            }
            double wd = Rotor.gammaMedeg / 180 * (Rotor.Rrotor + Airgap.delta / 2) * 2 * Math.PI / (2 * Rotor.p);
            Results.Bdelta = Results.phiD / (GeneralParams.MotorLength * wd * 1e-6);

            // psiM
            Dictionary<String, FEMM.CircuitProperties> cps = Stator.getCircuitsPropertiesInAns(femm);
            if (cps.ContainsKey("A") && cps.ContainsKey("B") && cps.ContainsKey("C"))
            {
                Fdq fdq = ParkTransform.abc_dq(cps["A"].fluxlinkage, cps["B"].fluxlinkage, cps["C"].fluxlinkage, 0);
                Results.psiM = fdq.Magnitude;
            }
            else Results.psiM = double.NaN;

            femm.mo_close();
        }
    }
}
