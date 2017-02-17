using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace calc_from_geometryOfMotor
{
    class A
    {
        public static void calcWithoutCurrent()
        {
            // all length is in mm
            Geo_PMSM_MagnetVShape m = MyMotor();

            // do some internal calculation first            
            Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
            rip.lm = 3.5; // magnet length
            rip.p = 3; // pair poles
            rip.wb2min = 2 / 2; // min barrier width
            rip.wFe = 1; // Fe width 
            rip.wFe2 = 1;
            rip.alphaM = 40 * Math.PI / 180; // magnet angle
            rip.Rrotor = 126 / 2 - 0.6; // airgap
            m.setRotorParameters(rip);

            bool makeFEMMModel = true;
            bool getFEMMData = true;
            String femFile = @"E:\zAspirant\FEMM\pmsm\3\whatever.FEM";
            String ansFile = @"E:\zAspirant\FEMM\pmsm\3\whatever.ans";
            // make a femm model file
            if (makeFEMMModel)
            {
                m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                // open, analyze it
                FEMM.open(femFile);

                //edit file a little 2,6 pair poles (if need full teeth)
                if (false)
                {
                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);
                }

                FEMM.mi_analyze();
                FEMM.mi_close();
                //File.Delete(femFile);//delete it                                
            }

            double phiM = 0;
            double phib = 0;
            double phisigmaFe = 0;
            double phisigmaS = 0;
            FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

            if (getFEMMData)
            {
                // open solution file, measure the flux in airgap
                double xG = m.RotorParams.Rrotor * Math.Cos(2 * Math.PI / (4 * m.RotorParams.p));
                double yG = m.RotorParams.Rrotor * Math.Sin(2 * Math.PI / (4 * m.RotorParams.p));
                FEMM.open(ansFile);
                FEMM.mo_selectpoint(xG, yG);
                FEMM.mo_selectpoint(xG, -yG);
                FEMM.mo_bendcontour(-360 / (2 * m.RotorParams.p), 1);
                lir = FEMM.mo_lineintegral_full();

                // get phiM
                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xA, m.yA);
                FEMM.mo_selectpoint(m.xD, m.yD);
                FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phiM = Math.Abs(rr.totalBn * 2);

                // get phib
                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xD, m.yD);
                FEMM.mo_selectpoint(m.xD, -m.yD);
                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phib = Math.Abs(rr.totalBn);

                // get phisigmaFe
                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xA, m.yA);
                FEMM.mo_selectpoint(m.xG, m.yG);
                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phisigmaFe = Math.Abs(rr.totalBn * 2);

                // get phisigmaS
                double xZ = (m.StatorParams.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.RotorParams.p) - 2 * Math.PI / 180);
                double yZ = (m.StatorParams.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.RotorParams.p) - 2 * Math.PI / 180);

                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xG, m.yG);
                FEMM.mo_selectpoint(xZ, yZ);
                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phisigmaS = Math.Abs(rr.totalBn * 2);

                FEMM.mo_close();

                FEMM.quit();
            }

            // build model magnetic circuit
            GeneralMagnetCircuit gmc = m.buildGMC();

            // get B-H curve from file
            gmc.B = myB.ToArray();
            gmc.H = myH.ToArray();

            // non-linear computing
            gmc.calc();

            Console.WriteLine("phiD from FEMM model:" + lir.totalBn);
            Console.WriteLine("phiD from analytical calculation:" + gmc.phiD);
            Console.WriteLine("  Difference: " + (gmc.phiD - lir.totalBn) / lir.totalBn * 100 + " %");
            Console.WriteLine("Leakage FEMM:" + (phib + phisigmaFe));
            Console.WriteLine("Leakage analytical: " + (gmc.phiFe + gmc.phib));
            Console.WriteLine("PhiM (FEMM):" + phiM);
            Console.WriteLine("PhiM (analytic):" + gmc.phiM);
            Console.WriteLine("Fz,Fy (analytic):" + gmc.Fz + "\t" + gmc.Fy);
        }

        public static void testBuildMotor()
        {
            // all length is in mm
            Geo_PMSM_MagnetVShape_Ansys m = MyMotorRMxprt();

            // do some internal calculation first            
            Geo_PMSM_MagnetVShape_Ansys.RotorClass rip = new Geo_PMSM_MagnetVShape_Ansys.RotorClass();
            rip.ThickMag = 6; // magnet length
            rip.B1 = 5;
            rip.p = 3; // pair poles                        
            rip.DiaGap = 126 - 2 * 0.6; // airgap
            rip.D1 = 124;
            rip.O1 = 2;
            rip.O2 = 30;
            rip.Dminmag = 5;
            rip.WidthMag = 40;
            rip.DiaYoke = 32;
            rip.Rib = 2;
            rip.HRib = 2;
            rip.poletype = Geo_PMSM_MagnetVShape_Ansys.RotorClass.PoleType.MiddleSteelBridgeRectangle;
            m.setRotorParameters(rip);
            m.CalcPointsCoordinates();

            bool makeFEMMModel = false;
            bool getFEMMData = false;
            String femFile = @"E:\zAspirant\FEMM\pmsm\3\whatever_ansystype.FEM";
            String ansFile = @"E:\zAspirant\FEMM\pmsm\3\whatever_ansystype.ans";
            // make a femm model file
            if (makeFEMMModel)
            {
                m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                //// open, analyze it
                FEMM.open(femFile);

                //edit file a little 2,6 pair poles (if need full teeth)                
                FEMM.mi_analyze();
                FEMM.mi_close();
                //File.Delete(femFile);//delete it                                
            }

            double phiM = 0;
            double phib = 0;
            double phisigmaFe = 0;
            double phisigmaS = 0;
            FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

            if (getFEMMData)
            {
                // open solution file, measure the flux in airgap
                double xG = m.Rotor.Rrotor * Math.Cos(2 * Math.PI / (4 * m.Rotor.p));
                double yG = m.Rotor.Rrotor * Math.Sin(2 * Math.PI / (4 * m.Rotor.p));
                FEMM.open(ansFile);
                FEMM.mo_selectpoint(xG, yG);
                FEMM.mo_selectpoint(xG, -yG);
                FEMM.mo_bendcontour(-360 / (2 * m.Rotor.p), 1);
                lir = FEMM.mo_lineintegral_full();

                // get phiM
                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xD, m.yD);
                FEMM.mo_selectpoint(m.xG, m.yG);
                FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phiM = Math.Abs(rr.totalBn * 2);

                // get phib
                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xH, m.yH);
                FEMM.mo_selectpoint(m.xH, -m.yH);
                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phib = Math.Abs(rr.totalBn);

                // get phisigmaFe
                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(m.xB, m.yB);
                FEMM.mo_selectpoint(xG, yG);
                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phisigmaFe = Math.Abs(rr.totalBn * 2);

                // get phisigmaS
                double xZ = (m.StatorParams.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.Rotor.p) - 2 * Math.PI / 180);
                double yZ = (m.StatorParams.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.Rotor.p) - 2 * Math.PI / 180);

                FEMM.mo_clearcontour();
                FEMM.mo_selectpoint(xG, yG);
                FEMM.mo_selectpoint(xZ, yZ);
                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                phisigmaS = Math.Abs(rr.totalBn * 2);

                FEMM.mo_close();

                FEMM.quit();
            }

            // build model magnetic circuit
            GeneralMagnetCircuit gmc = m.buildGMC();

            // get B-H curve from file
            gmc.B = myB.ToArray();
            gmc.H = myH.ToArray();

            // non-linear computing
            gmc.calc();

            Console.WriteLine("phiD from FEMM model:" + lir.totalBn);
            Console.WriteLine("phiD from analytical calculation:" + gmc.phiD);
            Console.WriteLine("Average Bdelta from FEMM model:" + lir.avgBn);
            Console.WriteLine("Average Bdelta from analytical calculation:" + gmc.phiD / lir.surface_area);
            Console.WriteLine("Bdelta Max from analytical calculation:" + gmc.phiD / lir.surface_area * 180 / m.Rotor.gammaM);
            Console.WriteLine("   Difference: " + (gmc.phiD - lir.totalBn) / lir.totalBn * 100 + " %");
            Console.WriteLine("Leakage FEMM:" + (phib + phisigmaFe));
            Console.WriteLine("Leakage analytical: " + (gmc.phiFe + gmc.phib));
            Console.WriteLine("PhiM (FEMM):" + phiM);
            Console.WriteLine("PhiM (analytic):" + gmc.phiM);
            Console.WriteLine("Fz,Fy (analytic):" + gmc.Fz + "\t" + gmc.Fy);
        }

        /*
        public static void calcWithStatorCurrent()
        {
            // all length is in mm
            Geo_PMSM_MagnetVShape m = MyMotor();

            // do some internal calculation first           
            Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
            rip.lm = 2.5; // magnet length
            rip.p = 3; // pair poles
            rip.wb2min = 2 / 2; // min barrier width
            rip.wFe = 1; // Fe width 
            rip.wFe2 = 1;
            rip.alphaM = 45 * Math.PI / 180; // magnet angle
            rip.delta = 0.6; // airgap

            m.buildRotorWithParameters(rip);

            double ev = 205;
            double Imax = 50;
            double IA = Imax * Math.Cos(ev * Math.PI / 180);
            double IB = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);
            double IC = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);


            if (false)
            {
                String femFile = @"E:\zAspirant\FEMM\pmsm\3\whatever.FEM";
                String ansfile = @"E:\zAspirant\FEMM\pmsm\3\whatever.ans";
                m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                FEMM.open(femFile);

                FEMM.mi_modifycircuitCurrent("A", IA);
                FEMM.mi_modifycircuitCurrent("B", IB);
                FEMM.mi_modifycircuitCurrent("C", IC);

                FEMM.mi_selectgroup(1001);
                FEMM.mi_selectgroup(1002);
                FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);

                FEMM.mi_analyze();

                FEMM.mi_saveas(femFile);
                FEMM.mi_close();
            }

            GeneralMagnetCircuit gmc = m.buildGMC();
            gmc.B = myB.ToArray();
            gmc.H = myH.ToArray();

            int N = 10;
            double[] Fstator = {0 + IB*N - IC*N, 
                                IA*N + IB*N - IC*N, 
                                IA*N +  0   -IC * N, 
                                IA*N - IB * N - IC * N, 
                                IA*N - IB * N +0, 
                                IA*N - IB*N  +IC*N};

            gmc.Pa = gmc.Pd / m.Nz / 4;
            Console.WriteLine("Method 2:");
            double[] phid_1t = gmc.calc_with_mmfStator_v2(Fstator, @"E:\zAspirant\FEMM\pmsm\3\whatever_v2.txt");
            Console.WriteLine("Result:" + gmc.phiD);
            for (int i = 0; i < phid_1t.Length; i++)
                Console.WriteLine(phid_1t[i]);
            Console.WriteLine("\nMethod 1:");
            phid_1t = gmc.calc_with_mmfStator(Fstator, @"E:\zAspirant\FEMM\pmsm\3\whatever.txt");
            Console.WriteLine("Result:" + gmc.phiD);
            for (int i = 0; i < phid_1t.Length; i++)
                Console.WriteLine(phid_1t[i]);
        }

        //rotate the stator (relatively the rotor) slowly .2 degree each step for 50 times
        // check the leakage 
        public static void testWithStatorCurrent_teethConsidering()
        {
            bool makeFEMMModel = false;
            bool getFEMMData = true;

            String outputResultFile = @"E:\zAspirant\FEMM\pmsm\3\out\testLeakage_teeth_when_rotor_moving.txt";
            StreamWriter writer = new StreamWriter(outputResultFile);
            writer.WriteLine("Test calculation with mmf rotating from stator. p=3. q=2. N=10. alpham=40o. delta = 0.6mm. lm=0.5mm. Stator distributed windings");
            writer.WriteLine("Ev\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tphiZ1pole");

            //String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\testLeakage_teeth_when_rotor_moving\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\testLeakage_teeth_when_rotor_moving\";
            //Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            int ev = 0;
            double divi = 5;

            // read now
            while (ev <= 50)
            {
                String casename = String.Format("{0:D3}", ev);

                Geo_PMSM_MagnetVShape m = MyMotor();

                // data input (mm and rad)
                Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                rip.lm = 0.5; // magnet length
                rip.p = 3; // pair poles
                rip.wb2min = 2 / 2; // min barrier width
                rip.wFe = 1; // Fe width 
                rip.wFe2 = 1;
                rip.alphaM = 40 * Math.PI / 180; // magnet angle
                rip.delta = 0.6; // airgap

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiM = 0;
                double phib = 0;
                double phisigmaFe = 0;
                double phisigmaS = 0;
                double phiZ = 0;

                double Imax = 0;
                double IA = Imax * Math.Cos(ev * Math.PI / 180);
                double IB = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);
                double IC = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    // max leak (p=3)
                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, ev / divi, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiM
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiM = Math.Abs(rr.totalBn * 2);

                    // get phib
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.mo_selectpoint(m.xD, -m.yD);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phib = Math.Abs(rr.totalBn);

                    // get phisigmaFe
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaFe = Math.Abs(rr.totalBn * 2);

                    // get phisigmaS
                    double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                    double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    FEMM.mo_addcontour(xZ, yZ);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaS = Math.Abs(rr.totalBn * 2);

                    // get phiZ1pole - a flux that = phiD-phisigmaS
                    xZ = (m.Rinstator + m.lz / 2 + 5) * Math.Cos(2 * Math.PI / (4 * m.p));
                    yZ = (m.Rinstator + m.lz / 2 + 5) * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.mo_clearcontour();
                    FEMM.mo_addcontour(xZ, yZ);
                    FEMM.mo_addcontour(xZ, -yZ);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiZ = Math.Abs(rr.totalBn);

                    // save a bitmap
                    //FEMM.mo_clearcontour();
                    //FEMM.mo_zoom(36.2, 18.1, 88.9, 41.9);
                    //FEMM.mo_showcontourplot(199, -0.0292123255330979, 0.0292123255330979, FEMM.ContourPlotType.both);
                    //FEMM.mo_savebitmap(@"E:\zAspirant\FEMM\pmsm\3\out\testLeakage_teeth_when_rotor_moving\bitmaps\" + casename + ".bmp");

                    FEMM.mo_close();
                }

                //write result to screen
                Console.WriteLine(casename + " # " + lir.totalBn);
                Console.WriteLine();

                // write the result to a text file
                String aline =
                            ev / divi + "\t"
                            + lir.totalBn + "\t"
                            + phiM + "\t"
                            + phib + "\t"
                            + phisigmaFe + "\t"
                            + phisigmaS + "\t"
                            + phiZ;
                writer.WriteLine(aline);
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // next ev value
                ev += 1;
            }

            writer.Close();
        }

        /// <summary>
        /// Test: Fstator = const 90degree
        /// lm change
        /// check flux in the tooth, which is located in between 2 poles
        /// </summary>
        public static void testFstatorLm()
        {
            // using ans file from previous calculation
            // using parameters from the result txt file 
            // matching using case number and parameters
            String outfolder = @"E:\zAspirant\FEMM\pmsm\3\out\";

            bool makeFEMMModel = true;
            bool getFEMMData = true;

            StreamWriter writer = new StreamWriter(outfolder + "testFstatorLm.txt");
            writer.WriteLine("Test calculation with mmf from stator. IA=0,IB=-43.3,IC=43.3. p=3. q=2. N=10. Stator distributed windings");
            writer.WriteLine("case_number\tp\tdelta\talpham\tlm\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tPhiD(analytic)\tphid123456...");
            double IA = 0;
            double IB = -43.3;
            double IC = 43.3;

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\testFstatorLm\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\testFstatorLm\";
            Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            double lm = 0.5;
            int k = 1;

            while (lm < 3)
            {
                String casename = String.Format("{0:D3}", k);
                // all length is in mm
                Geo_PMSM_MagnetVShape m = MyMotor();

                // data input
                Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                rip.lm = lm; // magnet length
                rip.p = 3; // pair poles
                rip.wb2min = 2 / 2; // min barrier width
                rip.wFe = 1; // Fe width 
                rip.wFe2 = 1;
                rip.alphaM = 45 * Math.PI / 180; // magnet angle
                rip.delta = 0.6; // airgap

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiM = 0;
                double phib = 0;
                double phisigmaFe = 0;
                double phisigmaS = 0;

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, 1, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiM
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiM = Math.Abs(rr.totalBn * 2);

                    // get phib
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.mo_selectpoint(m.xD, -m.yD);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phib = Math.Abs(rr.totalBn);

                    // get phisigmaFe
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaFe = Math.Abs(rr.totalBn * 2);

                    // get phisigmaS
                    double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                    double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    FEMM.mo_selectpoint(xZ, yZ);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaS = Math.Abs(rr.totalBn * 2);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                double[] phid_1t = gmc.calc_with_mmfStator(Fstator, calc_result_outputFolder + casename + ".txt");

                //write result to screen
                Console.WriteLine(casename + "#" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                String aline;
                // write the result to a text file
                aline = casename + "\t"
                            + 3 + "\t"
                            + 0.6 + "\t"
                            + 45 + "\t"
                            + lm + "\t"
                            + lir.totalBn + '\t'
                            + phiM + '\t'
                            + phib + '\t'
                            + phisigmaFe + '\t'
                            + phisigmaS + '\t'
                            + gmc.phiD;
                writer.Write(aline + '\t');
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // increase lm,k
                lm += 0.5;
                k += 1;
            }

            writer.Close();
        }

        /// <summary>
        /// Test: Fstator pulsate at 90degree
        /// lm=const
        /// check flux in the tooth, which is located in between 2 poles
        /// </summary>
        public static void testFstatorPulsateAt90()
        {
            // using ans file from previous calculation
            // using parameters from the result txt file 
            // matching using case number and parameters
            String outfolder = @"E:\zAspirant\FEMM\pmsm\3\out\";

            bool makeFEMMModel = true;
            bool getFEMMData = true;

            StreamWriter writer = new StreamWriter(outfolder + "testFstatorPulsateLm.txt");
            writer.WriteLine("Test calculation with mmf from stator. I=50. p=3. q=2. N=10. Stator distributed windings");
            writer.WriteLine("case_number\tp\tdelta\talpham\tlm\tI\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tPhiD(analytic)\tphid123456...");
            double IA = 0;
            double IB = -43.3;
            double IC = 43.3;

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\testFstatorPulsateLm\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\testFstatorPulsateLm\";
            Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            double I = -50;
            int k = 1;

            while (I < 50)
            {
                String casename = String.Format("{0:D3}", k);
                // all length is in mm
                Geo_PMSM_MagnetVShape m = MyMotor();

                // data input
                Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                rip.lm = 1.5; // magnet length
                rip.p = 3; // pair poles
                rip.wb2min = 2 / 2; // min barrier width
                rip.wFe = 1; // Fe width 
                rip.wFe2 = 1;
                rip.alphaM = 45 * Math.PI / 180; // magnet angle
                rip.delta = 0.6; // airgap

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiM = 0;
                double phib = 0;
                double phisigmaFe = 0;
                double phisigmaS = 0;

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                IA = I * Math.Cos(90 * Math.PI / 180);
                IB = I * Math.Cos(90 * Math.PI / 180 - 2 * Math.PI / 3);
                IC = I * Math.Cos(90 * Math.PI / 180 + 2 * Math.PI / 3);

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiM
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiM = Math.Abs(rr.totalBn * 2);

                    // get phib
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.mo_selectpoint(m.xD, -m.yD);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phib = Math.Abs(rr.totalBn);

                    // get phisigmaFe
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaFe = Math.Abs(rr.totalBn * 2);

                    // get phisigmaS
                    double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                    double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    FEMM.mo_selectpoint(xZ, yZ);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaS = Math.Abs(rr.totalBn * 2);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                double[] phid_1t = gmc.calc_with_mmfStator(Fstator, calc_result_outputFolder + casename + ".txt");

                //write result to screen
                Console.WriteLine(casename + "#" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                String aline;
                // write the result to a text file
                aline = casename + "\t"
                            + 3 + "\t"
                            + 0.6 + "\t"
                            + 45 + "\t"
                            + 1.5 + "\t"
                            + I + "\t"
                            + lir.totalBn + '\t'
                            + phiM + '\t'
                            + phib + '\t'
                            + phisigmaFe + '\t'
                            + phisigmaS + '\t'
                            + gmc.phiD;
                writer.Write(aline + '\t');
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // increase lm,k
                I += 5;
                k += 1;
            }

            writer.Close();
        }

        public static void doScan_WithoutCurrent()
        {
            int[] pps = { 2, 3, 6 };
            double[] deltas = { 0.3, 0.4, 0.6, 0.8, 1 };
            int p;
            double delta;
            double alpham;
            double lm;
            int k = 1;

            StreamWriter writer = new StreamWriter(@"E:\zAspirant\FEMM\pmsm\3\out\scan1_kc.txt");
            writer.WriteLine("Test scan 1 (kc):");
            writer.WriteLine("Variable run: p, delta, alpham, lm");
            writer.WriteLine("case_number\tp\tdelta\talpham\tlm\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tPhiD(analytic)\tphiM(a)\tphib(a)\tphiSigmaFe(a)");
            writer.Flush();

            for (int i = 0; i < pps.Length; i++) // loop 1
            {
                p = pps[i];
                for (int j = 0; j < deltas.Length; j++) // loop 2
                {
                    delta = deltas[j];
                    alpham = 360 / (4 * p) + 10;
                    while (alpham <= 89) // loop 3
                    {
                        lm = 0.5;
                        while (lm < 3) // loop 4
                        {
                            // all length is in mm
                            Geo_PMSM_MagnetVShape m = MyMotor();

                            // data input
                            Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                            rip.lm = lm; // magnet length
                            rip.p = p; // pair poles
                            rip.wb2min = 2 / 2; // min barrier width
                            rip.wFe = 1; // Fe width 
                            rip.wFe2 = 1;
                            rip.alphaM = alpham * Math.PI / 180; // magnet angle
                            rip.delta = delta; // airgap

                            // build rotor params
                            m.buildRotorWithParameters(rip);


                            bool makeFEMMModel = false;// false if skip
                            bool getFEMMData = false;//false if skip
                            FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();
                            String casename = String.Format("{0:D5}", k);
                            String outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\Scan2\";
                            String femFile = outputDir + casename + ".FEM";
                            String ansFile = outputDir + casename + ".ans";

                            double phiM = 0;
                            double phib = 0;
                            double phisigmaFe = 0;
                            double phisigmaS = 0;


                            // make a femm model file
                            if (makeFEMMModel)
                            {
                                m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                                // open, analyze it
                                FEMM.open(femFile);

                                //edit file a little
                                if (p == 3)
                                {
                                    FEMM.mi_selectgroup(1001);
                                    FEMM.mi_selectgroup(1002);
                                    FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);
                                }

                                FEMM.mi_analyze(true);
                                FEMM.mi_close();
                                File.Delete(femFile);//delete it                                
                            }

                            if (getFEMMData)
                            {
                                // open solution file, measure the flux in airgap
                                double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                                double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                                FEMM.open(ansFile);
                                FEMM.mo_selectpoint(xG, yG);
                                FEMM.mo_selectpoint(xG, -yG);
                                FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                                lir = FEMM.mo_lineintegral_full();

                                // get phiM
                                FEMM.mo_clearcontour();
                                FEMM.mo_selectpoint(m.xA, m.yA);
                                FEMM.mo_selectpoint(m.xD, m.yD);
                                FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                                phiM = Math.Abs(rr.totalBn * 2);

                                // get phib
                                FEMM.mo_clearcontour();
                                FEMM.mo_selectpoint(m.xD, m.yD);
                                FEMM.mo_selectpoint(m.xD, -m.yD);
                                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                                phib = Math.Abs(rr.totalBn);

                                // get phisigmaFe
                                FEMM.mo_clearcontour();
                                FEMM.mo_selectpoint(m.xA, m.yA);
                                FEMM.mo_selectpoint(m.xG, m.yG);
                                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                                phisigmaFe = Math.Abs(rr.totalBn * 2);

                                // get phisigmaS
                                double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                                double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                                FEMM.mo_clearcontour();
                                FEMM.mo_selectpoint(m.xG, m.yG);
                                FEMM.mo_selectpoint(xZ, yZ);
                                rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                                phisigmaS = Math.Abs(rr.totalBn * 2);

                                FEMM.mo_close();
                            }

                            // build model magnetic circuit for analytical calculation
                            GeneralMagnetCircuit gmc = m.buildGMC();

                            // copy B,H to calculate
                            gmc.B = myB.ToArray();
                            gmc.H = myH.ToArray();

                            // non-linear computing
                            gmc.calc();

                            // write the result to a text file
                            String aline = casename + "\t"
                                        + p + "\t"
                                        + delta + "\t"
                                        + alpham + "\t"
                                        + lm + "\t"
                                        + lir.totalBn + '\t'
                                        + phiM + '\t'
                                        + phib + '\t'
                                        + phisigmaFe + '\t'
                                        + phisigmaS + "\t"
                                        + gmc.phiD + '\t'
                                        + gmc.phiM + "\t"
                                        + gmc.phib + "\t"
                                        + gmc.phiFe;
                            writer.WriteLine(aline);
                            writer.Flush();//flush now to prevent losing data
                            //to console
                            Console.WriteLine(aline);

                            // increase the params
                            lm += 0.5;

                            k++;//case number
                        } //end while lm

                        // increase the param                        
                        if (alpham == 89)
                            break;

                        alpham += 5;
                        if (alpham > 89)
                            alpham = 89;//last chance
                    }//end while alpham
                }// end for delta
            }// end for p

            writer.Close();
        }

        public static void doScan_WithCurrentStator()
        {
            // using ans file from previous calculation
            // using parameters from the result txt file 
            // matching using case number and parameters

            String datafile = @"E:\zAspirant\FEMM\pmsm\3\out\scan2.txt";
            StreamReader reader = new StreamReader(datafile);

            // skip 3 lines
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();

            bool makeFEMMModel = false;
            bool getFEMMData = false;

            StreamWriter writer = new StreamWriter(Path.GetDirectoryName(datafile) + "\\" + Path.GetFileNameWithoutExtension(datafile) + "_withoutCurrent_phid1.txt");
            writer.WriteLine("Test calculation with mmf from stator. IA=-50,IB=25,IC=25. p=3. q=2. N=10. Stator distributed windings");
            writer.WriteLine("case_number\tp\tdelta\talpham\tlm\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tPhiD(analytic)\tphid123456...");

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\Scan2_statorMMF\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\Scan2_statorMMF\";


            // read now
            while (!reader.EndOfStream)
            {
                String aline = reader.ReadLine();
                String[] subs = aline.Split('\t');

                String casename = subs[0];
                int p = int.Parse(subs[1]);
                double delta = double.Parse(subs[2]);
                double alpham = double.Parse(subs[3]);
                double lm = double.Parse(subs[4]);

                if (p != 3) continue;

                // all length is in mm
                Geo_PMSM_MagnetVShape m = MyMotor();

                // data input
                Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                rip.lm = lm; // magnet length
                rip.p = p; // pair poles
                rip.wb2min = 2 / 2; // min barrier width
                rip.wFe = 1; // Fe width 
                rip.wFe2 = 1;
                rip.alphaM = alpham * Math.PI / 180; // magnet angle
                rip.delta = delta; // airgap

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiM = 0;
                double phib = 0;
                double phisigmaFe = 0;
                double phisigmaS = 0;

                double IA = -50;
                double IB = 25;
                double IC = 25;

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    //FEMM.mi_selectgroup(1001);
                    //FEMM.mi_selectgroup(1002);
                    //FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiM
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiM = Math.Abs(rr.totalBn * 2);

                    // get phib
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.mo_selectpoint(m.xD, -m.yD);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phib = Math.Abs(rr.totalBn);

                    // get phisigmaFe
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaFe = Math.Abs(rr.totalBn * 2);

                    // get phisigmaS
                    double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                    double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    FEMM.mo_selectpoint(xZ, yZ);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaS = Math.Abs(rr.totalBn * 2);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                double[] phid_1t = gmc.calc_with_mmfStator(Fstator, calc_result_outputFolder + casename + ".txt");

                //write result to screen
                Console.WriteLine(casename + "#" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                // write the result to a text file
                aline = casename + "\t"
                            + p + "\t"
                            + delta + "\t"
                            + alpham + "\t"
                            + lm + "\t"
                            + lir.totalBn + '\t'
                            + phiM + '\t'
                            + phib + '\t'
                            + phisigmaFe + '\t'
                            + phisigmaS + '\t'
                            + gmc.phiD;
                writer.Write(aline + '\t');
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);
            }

            writer.Close();
            reader.Close();
        }

        public static void doCalc_1Motor_with_flux_stator_rotate()
        {
            // using ans file from previous calculation
            // using parameters from the result txt file 
            // matching using case number and parameters

            bool makeFEMMModel = false;
            bool getFEMMData = true;

            String outputResultFile = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate_maxleak_v2.txt";
            StreamWriter writer = new StreamWriter(outputResultFile);
            writer.WriteLine("Test calculation with mmf rotating from stator. p=3. q=2. N=10. alpham=45o. delta = 0.6mm. lm=2.5mm. Stator distributed windings");
            writer.WriteLine("Ev\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tPhiDZ(fem)\tPhiD(analytic)\tphid123456...");

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate_maxleak\result_gmc_v2\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate_maxleak\";
            Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            int ev = 0;

            // read now
            while (ev <= 360)
            {
                String casename = String.Format("{0:D3}", ev);

                Geo_PMSM_MagnetVShape m = MyMotor();

                // data input (mm and rad)
                Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                rip.lm = 2.5; // magnet length
                rip.p = 3; // pair poles
                rip.wb2min = 2 / 2; // min barrier width
                rip.wFe = 1; // Fe width 
                rip.wFe2 = 1;
                rip.alphaM = 45 * Math.PI / 180; // magnet angle
                rip.delta = 0.6; // airgap

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiM = 0;
                double phib = 0;
                double phisigmaFe = 0;
                double phisigmaS = 0;
                double phiDz = 0;

                double Imax = 50;
                double IA = Imax * Math.Cos(ev * Math.PI / 180);
                double IB = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);
                double IC = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    // max leak (p=3)
                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiM
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiM = Math.Abs(rr.totalBn * 2);

                    // get phib
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.mo_selectpoint(m.xD, -m.yD);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phib = Math.Abs(rr.totalBn);

                    // get phisigmaFe
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaFe = Math.Abs(rr.totalBn * 2);

                    // get phisigmaS
                    double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                    double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    FEMM.mo_selectpoint(xZ, yZ);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaS = Math.Abs(rr.totalBn * 2);

                    // get phiDz
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(55, 34.5);
                    FEMM.mo_selectpoint(57.6, -30.6);
                    FEMM.mo_bendcontour(-60, 1);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiDz = Math.Abs(rr.totalBn);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                gmc.Pa = gmc.Pd / m.Nz / 4;//additional parameter for method2

                double[] phid_1t = gmc.calc_with_mmfStator_v2(Fstator, calc_result_outputFolder + casename + ".txt");

                //write result to screen
                Console.WriteLine(casename + "#" + lir.totalBn + "\t" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                // write the result to a text file
                String aline =
                            ev + "\t"
                            + lir.totalBn + "\t"
                            + phiM + "\t"
                            + phib + "\t"
                            + phisigmaFe + "\t"
                            + phisigmaS + "\t"
                            + phiDz + "\t"
                            + gmc.phiD;
                writer.Write(aline + "\t");
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // next ev value
                ev += 5;
            }

            writer.Close();
        }

        public static void doCalc_1Motor_with_flux_stator_pulsate()
        {
            // using ans file from previous calculation
            // using parameters from the result txt file 
            // matching using case number and parameters

            bool makeFEMMModel = false;
            bool getFEMMData = false;

            String outputResultFile = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxpulsate_kc.txt";

            StreamWriter writer = new StreamWriter(outputResultFile);
            writer.WriteLine("Test calculation with mmf pulsating from stator. p=3. q=2. N=10. alpham=45o. delta = 0.6mm. lm=2.5mm. Stator distributed windings");
            writer.WriteLine("I\tPhiD(femm)\tphiM\tphib\tphisigmaFe\tphisigmaS\tPhiD(analytic)\tphid123456...");

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxpulsate\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxpulsate\";
            Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            int I = -50;
            double ev = 0;

            // read now
            while (I <= 50)
            {
                String casename = String.Format("{0:D3}", I);

                Geo_PMSM_MagnetVShape m = MyMotor();

                // data input (mm and rad)
                Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
                rip.lm = 2.5; // magnet length
                rip.p = 3; // pair poles
                rip.wb2min = 2 / 2; // min barrier width
                rip.wFe = 1; // Fe width 
                rip.wFe2 = 1;
                rip.alphaM = 45 * Math.PI / 180; // magnet angle
                rip.delta = 0.6; // airgap

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiM = 0;
                double phib = 0;
                double phisigmaFe = 0;
                double phisigmaS = 0;

                double Imax = I;
                double IA = Imax * Math.Cos(ev * Math.PI / 180);
                double IB = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);
                double IC = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    //FEMM.mi_selectgroup(1001);
                    //FEMM.mi_selectgroup(1002);
                    //FEMM.mi_moverotate(0, 0, 5, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiM
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiM = Math.Abs(rr.totalBn * 2);

                    // get phib
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xD, m.yD);
                    FEMM.mo_selectpoint(m.xD, -m.yD);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phib = Math.Abs(rr.totalBn);

                    // get phisigmaFe
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xA, m.yA);
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaFe = Math.Abs(rr.totalBn * 2);

                    // get phisigmaS
                    double xZ = (m.Rinstator + 5) * Math.Cos(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);
                    double yZ = (m.Rinstator + 5) * Math.Sin(2 * Math.PI / (4 * m.p) - 2 * Math.PI / 180);

                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(m.xG, m.yG);
                    FEMM.mo_selectpoint(xZ, yZ);
                    rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phisigmaS = Math.Abs(rr.totalBn * 2);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                double[] phid_1t = gmc.calc_with_mmfStator(Fstator, calc_result_outputFolder + casename + ".txt");

                //write result to screen
                Console.WriteLine(casename + "#" + lir.totalBn + "\t" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                // write the result to a text file
                String aline =
                            I + "\t"
                            + lir.totalBn + "\t"
                            + phiM + "\t"
                            + phib + "\t"
                            + phisigmaFe + "\t"
                            + phisigmaS + "\t"
                            + gmc.phiD;
                writer.Write(aline + "\t");
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // next ev value
                I += 5;
            }

            writer.Close();
        }

        /// <summary>
        /// Measure flux linkage
        /// </summary>
        public static void measure_1Motor_with_flux_stator_rotate()
        {
            bool makeFEMMModel = false;
            bool getFEMMData = true;
            double rotateRotor = 0;//rotate rotor: p=3, 0: 6 teeth, 5:1/2+5+1/2

            String outputResultFile = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate_psi_moment.txt";
            StreamWriter writer = new StreamWriter(outputResultFile);
            writer.WriteLine("Test calculation with mmf rotating from stator. p=3. q=2. N=10. alpham=45o. delta = 0.6mm. lm=2.5mm. Stator distributed windings");
            writer.WriteLine("Ev\tPhiD(femm)\tPhiDZ(fem)\tPsiA(f)\tPsiB(f)\tPsiC(f)\tWf\tWc\tTorque(f)\tPhiD(analytic)\tPsi_A\tPsi_B\tPsi_C\tphid123456...");

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate\";
            Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            int ev = 0;
            int incr = 5;

            // read now
            while (ev <= 360)
            {
                String casename = String.Format("{0:D3}", ev);

                Geo_PMSM_MagnetVShape m = MyMotor();

                // build rotor params
                m.buildRotorWithParameters(RotorType1());

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiDz = 0;

                double psiA = 0;
                double psiB = 0;
                double psiC = 0;

                double Wf = 0;
                double Wc = 0;

                double torque = 0;

                double Imax = 50;
                double IA = Imax * Math.Cos(ev * Math.PI / 180);
                double IB = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);
                double IC = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    // max leak (p=3)
                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, rotateRotor, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiDz
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(55, 34.5);
                    FEMM.mo_selectpoint(57.6, -30.6);
                    FEMM.mo_bendcontour(-60, 1);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiDz = Math.Abs(rr.totalBn);

                    // get Psi
                    FEMM.CircuitProperties cp;
                    cp = FEMM.mo_getcircuitproperties("A");
                    psiA = cp.fluxlinkage;
                    cp = FEMM.mo_getcircuitproperties("B");
                    psiB = cp.fluxlinkage;
                    cp = FEMM.mo_getcircuitproperties("C");
                    psiC = cp.fluxlinkage;

                    // get block integral
                    FEMM.mo_groupselectblock(m.Group_Rotor_Steel);
                    FEMM.mo_groupselectblock(m.Group_Label);

                    torque = FEMM.mo_blockintegral(FEMM.BlockIntegralType.Steady_state_weighted_stress_tensor_torque);
                    Wf = FEMM.mo_blockintegral(FEMM.BlockIntegralType.Magnetic_field_energy);
                    Wc = FEMM.mo_blockintegral(FEMM.BlockIntegralType.Magnetic_field_coenergy);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                gmc.Pa = gmc.Pd / m.Nz / 4;//additional parameter for method2

                double[] phid_1t = gmc.calc_with_mmfStator(Fstator, calc_result_outputFolder + casename + ".txt");

                double psiA2 = m.p * 2 * N * (phid_1t[1] + phid_1t[2] + phid_1t[3] + phid_1t[4] + phid_1t[5]);
                double psiB2 = -m.p * 2 * N * (phid_1t[3] + phid_1t[4] + phid_1t[5] - phid_1t[0] - phid_1t[1]);
                double psiC2 = m.p * 2 * N * (phid_1t[5] - phid_1t[0] - phid_1t[1] - phid_1t[2] - phid_1t[3]);

                //write result to screen
                Console.WriteLine(casename + "#" + lir.totalBn + "\t" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                // write the result to a text file
                String aline =
                            ev + "\t"
                            + lir.totalBn + "\t"
                            + phiDz + "\t"
                            + psiA + "\t"
                            + psiB + "\t"
                            + psiC + "\t"
                            + Wf + "\t"
                            + Wc + "\t"
                            + torque + "\t"
                            + gmc.phiD + "\t"
                            + psiA2 + "\t"
                            + psiB2 + "\t"
                            + psiC2;
                writer.Write(aline + "\t");
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // next ev value
                ev += incr;
            }

            writer.Close();
        }

        public static void measure_1Motor_with_flux_stator_pulsate()
        {
            // using ans file from previous calculation
            // using parameters from the result txt file 
            // matching using case number and parameters

            bool makeFEMMModel = false;
            bool getFEMMData = true;
            double rotateRotor = 0;

            // data input (mm and rad)
            Geo_PMSM_MagnetVShape.RotorParameters rip = RotorType1();

            String outputResultFile = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxpulsate_rotor1.txt";

            StreamWriter writer = new StreamWriter(outputResultFile);
            writer.WriteLine("Test calculation with mmf pulsating from stator. p=3. q=2. N=10. alpham=45o. delta = 0.6mm. lm=2.5mm. Stator distributed windings");
            writer.WriteLine("I\tPhiD(femm)\tPhiDZ(fem)\tPsiA(f)\tPsiB(f)\tPsiC(f)\tWf\tWc\tTorque(f)\tPhiD(analytic)\tPsi_A\tPsi_B\tPsi_C\tphid123456...");

            String calc_result_outputFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxpulsate\result_gmc\";
            String ans_outputDir = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxpulsate\";
            Directory.CreateDirectory(calc_result_outputFolder);
            Directory.CreateDirectory(ans_outputDir);

            int I = -50;
            double ev = 0;
            int incr = 5;

            // read now
            while (I <= 50)
            {
                String casename = String.Format("{0:D3}", I);

                Geo_PMSM_MagnetVShape m = MyMotor();

                // build rotor params
                m.buildRotorWithParameters(rip);

                FEMM.LineIntegralResult lir = new FEMM.LineIntegralResult();

                double phiDz = 0;

                double psiA = 0;
                double psiB = 0;
                double psiC = 0;

                double Wf = 0;
                double Wc = 0;

                double torque = 0;

                double Imax = I;
                double IA = Imax * Math.Cos(ev * Math.PI / 180);
                double IB = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);
                double IC = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);

                String femFile = ans_outputDir + casename + ".FEM";
                String ansFile = ans_outputDir + casename + ".ans";

                if (makeFEMMModel)
                {
                    m.MakeAFEMMModelFile(femFile, @"E:\zAspirant\FEMM\pmsm\3\stator_only.FEM");

                    FEMM.open(femFile);

                    FEMM.mi_modifycircuitCurrent("A", IA);
                    FEMM.mi_modifycircuitCurrent("B", IB);
                    FEMM.mi_modifycircuitCurrent("C", IC);

                    // max leak (p=3)
                    FEMM.mi_selectgroup(1001);
                    FEMM.mi_selectgroup(1002);
                    FEMM.mi_moverotate(0, 0, rotateRotor, FEMM.EditMode.group);

                    FEMM.mi_analyze(true);

                    FEMM.mi_saveas(femFile);
                    FEMM.mi_close();
                }

                if (getFEMMData)
                {
                    // open solution file, measure the flux in airgap
                    double xG = m.Rrotor * Math.Cos(2 * Math.PI / (4 * m.p));
                    double yG = m.Rrotor * Math.Sin(2 * Math.PI / (4 * m.p));
                    FEMM.open(ansFile);
                    FEMM.mo_selectpoint(xG, yG);
                    FEMM.mo_selectpoint(xG, -yG);
                    FEMM.mo_bendcontour(-360 / (2 * m.p), 1);
                    lir = FEMM.mo_lineintegral_full();

                    // get phiDz
                    FEMM.mo_clearcontour();
                    FEMM.mo_selectpoint(55, 34.5);
                    FEMM.mo_selectpoint(57.6, -30.6);
                    FEMM.mo_bendcontour(-60, 1);
                    FEMM.LineIntegralResult rr = FEMM.mo_lineintegral(FEMM.LineIntegralType.Bn);
                    phiDz = Math.Abs(rr.totalBn);

                    // get Psi
                    FEMM.CircuitProperties cp;
                    cp = FEMM.mo_getcircuitproperties("A");
                    psiA = cp.fluxlinkage;
                    cp = FEMM.mo_getcircuitproperties("B");
                    psiB = cp.fluxlinkage;
                    cp = FEMM.mo_getcircuitproperties("C");
                    psiC = cp.fluxlinkage;

                    // get torque
                    FEMM.mo_groupselectblock(m.Group_Rotor_Steel);
                    FEMM.mo_groupselectblock(m.Group_Label);

                    torque = FEMM.mo_blockintegral(FEMM.BlockIntegralType.Steady_state_weighted_stress_tensor_torque);
                    Wf = FEMM.mo_blockintegral(FEMM.BlockIntegralType.Magnetic_field_energy);
                    Wc = FEMM.mo_blockintegral(FEMM.BlockIntegralType.Magnetic_field_coenergy);

                    FEMM.mo_close();
                }

                GeneralMagnetCircuit gmc = m.buildGMC();
                gmc.B = myB.ToArray();
                gmc.H = myH.ToArray();

                int N = 10;
                double[] Fstator = { 0 + IB*N - IC*N, 
                                     IA*N + IB*N - IC*N, 
                                     IA*N +  0   -IC * N, 
                                     IA*N - IB * N - IC * N, 
                                     IA*N - IB * N +0, 
                                     IA*N - IB*N  +IC*N};

                gmc.Pa = gmc.Pd / m.Nz / 4;//additional parameter for method2

                double[] phid_1t = gmc.calc_with_mmfStator(Fstator, calc_result_outputFolder + casename + ".txt");

                double psiA2 = m.p * 2 * N * (phid_1t[1] + phid_1t[2] + phid_1t[3] + phid_1t[4] + phid_1t[5]);
                double psiB2 = -m.p * 2 * N * (phid_1t[3] + phid_1t[4] + phid_1t[5] - phid_1t[0] - phid_1t[1]);
                double psiC2 = m.p * 2 * N * (phid_1t[5] - phid_1t[0] - phid_1t[1] - phid_1t[2] - phid_1t[3]);

                //write result to screen
                Console.WriteLine(casename + "#" + lir.totalBn + "\t" + gmc.phiD);
                for (int i = 0; i < phid_1t.Length; i++)
                    Console.Write("{0}\t", phid_1t[i]);
                Console.WriteLine();

                // write the result to a text file
                String aline =
                            I + "\t"
                            + lir.totalBn + "\t"
                            + phiDz + "\t"
                            + psiA + "\t"
                            + psiB + "\t"
                            + psiC + "\t"
                            + Wf + "\t"
                            + Wc + "\t"
                            + torque + "\t"
                            + gmc.phiD + "\t"
                            + psiA2 + "\t"
                            + psiB2 + "\t"
                            + psiC2;
                writer.Write(aline + "\t");
                for (int i = 0; i < phid_1t.Length; i++)
                    writer.Write("{0}\t", phid_1t[i]);
                writer.WriteLine();
                writer.Flush();//flush now to prevent losing data
                //to console
                //Console.WriteLine(aline);

                // next ev value
                I += incr;
            }

            writer.Close();
        }

        public static void doScan_WithoutCurrentStator_6poles()
        {
            
            // scan params 
            int[] pps = { 6 };
            double[] deltas = { 0.3, 0.4, 0.6, 0.8, 1 };
            double[] Ribs = { 2, 5, 8, 11, 14, 17, 20 };
            double[] HRibs = { 1, 2, 3, 4, 5 };
            int p = 3;
            int k = 1;

            String outfolder = @"E:\zAspirant\FEMM\pmsm\4\out\scan1\";
            Directory.CreateDirectory(outfolder);

            String outDataFile = outfolder + "scan1.txt";
            StreamWriter writer = new StreamWriter(outDataFile);

            writer.WriteLine("Test scan 1");
            writer.WriteLine("Variable run:");
            writer.WriteLine("case_number\tp\tdelta\tRib\tHRib\tThickMag\tWidthMag\tPhiD(analytic)\tphiM(a)\tphib(a)\tphiSigmaFe(a)");
            writer.Flush();

            

            for (int i_deltas = 0; i_deltas < deltas.Length; i_deltas++)
            {
                for (int i_Ribs = 0; i_Ribs < Ribs.Length; i_Ribs++)
                {
                    for (int i_HRibs = 0; i_HRibs < HRibs.Length; i_HRibs++)
                    {
                        Geo_RMxprt_PMSMVShape geo = MyMotorRMxprt();
                        Geo_RMxprt_PMSMVShape.RotorInitialParameters rip = new Geo_RMxprt_PMSMVShape.RotorInitialParameters();
                        rip.p = p;
                        rip.delta = deltas[i_deltas];
                        rip.DiaYoke = 32;
                        rip.D1 = 122.8;
                        rip.O1 = 5;
                        rip.O2 = 30;
                        rip.Rib = Ribs[i_Ribs];
                        rip.HRib = HRibs[i_HRibs];
                        rip.Dminmag = 1;
                        rip.ThickMag = 6.4;
                        rip.WidthMag = 32;

                        geo.buildRotorWithParameters(rip);

                        GeneralMagnetCircuit gmc = geo.buildGMC();

                        gmc.calc();

                        //write to text file
                        writer.Write(k + "\t");
                        writer.Write(p + "\t");
                        writer.Write(rip.delta + "\t");
                        writer.Write(rip.Rib + "\t");
                        writer.Write(rip.HRib + "\t");
                        writer.Write(rip.ThickMag+ "\t");
                        writer.Write(rip.WidthMag + "\t");
                        writer.Write(gmc.phiD + "\t");
                        writer.Write(gmc.phiM + "\t");
                        writer.Write(gmc.phib + "\t");
                        writer.Write(gmc.phiFe + "\t");
                        writer.WriteLine();

                        k++;//case number increase
                    }
                }
            }

            writer.Close();
        }

        //double phiDz = 0;

        //double psiA = 0;
        //double psiB = 0;
        //double psiC = 0;

        //double Wf = 0;
        //double Wc = 0;

        //double torque = 0;

        //double Imax = 0;
        //double ev = 0;
        //double IA = Imax * Math.Cos(ev * Math.PI / 180);
        //double IB = Imax * Math.Cos(ev * Math.PI / 180 - 2 * Math.PI / 3);
        //double IC = Imax * Math.Cos(ev * Math.PI / 180 + 2 * Math.PI / 3);

        //int N = 10;
        //double[] Fstator = { 0 + IB*N - IC*N, 
        //                         IA*N + IB*N - IC*N, 
        //                         IA*N +  0   -IC * N, 
        //                         IA*N - IB * N - IC * N, 
        //                         IA*N - IB * N +0, 
        //                         IA*N - IB*N  +IC*N};
        ////double[] Fstator = {                                       
        ////                         IA*N +  0   -IC * N, 
        ////                         IA*N - IB * N - IC * N, 
        ////                         IA*N - IB * N +0, 
        ////                    };

        //double[] phid_1t = gmc.calc_with_mmfStator(Fstator, "");
        //for (int i = 0; i < phid_1t.Length; i++)
        //{
        //    Console.WriteLine("B: {0} \t {1}", i, phid_1t[i] / (geo.wz * geo.L * 1e-6));
        //}

        //Console.WriteLine("2st calc phid:" + gmc.phiD);

        //double psiA2 = geo.p * 2 * N * (phid_1t[0] + phid_1t[1] + phid_1t[2]);
        //double psiB2 = -geo.p * 2 * N * (phid_1t[2] - phid_1t[0] - phid_1t[1]);
        //double psiC2 = geo.p * 2 * N * (-phid_1t[0] - phid_1t[1] - phid_1t[2]);

        //Console.WriteLine();
        //Console.WriteLine("PsiA: " + psiA2);
        //Console.WriteLine("PsiB: " + psiB2);
        //Console.WriteLine("PsiC: " + psiC2);
        //Console.WriteLine();

        public static void makeBitmaps()
        {
            String ansFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate\";
            String bitmapFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate\bitmaps\";
            Directory.CreateDirectory(bitmapFolder);
            for (int i = 0; i <= 360; i += 5)
            {
                String casename = String.Format("{0:D3}", i);

                FEMM.open(ansFolder + casename + ".ans");

                FEMM.mo_zoom(-40, -50, 200, 46);
                FEMM.mo_showcontourplot(40, -0.0305806692523294, 0.0305806692523294, FEMM.ContourPlotType.both);
                FEMM.mo_savebitmap(bitmapFolder + casename + ".bmp");

                FEMM.mo_close();
            }
        }

        public static void autoRename()
        {
            String ansFolder = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate_maxleak\result_gmc\";
            String tempname = @"E:\zAspirant\FEMM\pmsm\3\out\1motor_fluxrotate_maxleak\result_gmc\temp.tmp";
            for (int i = 0; i <= 175; i += 5)
            {
                String casename = String.Format(ansFolder + "{0:D3}.txt", i);
                String othercasename = String.Format(ansFolder + "{0:D3}.txt", 360 - i);

                File.Move(casename, tempname);
                File.Move(othercasename, casename);
                File.Move(tempname, othercasename);
            }
        }
        */
        public static String BHcurvefile = @"E:\MatlabProject\zAspirant\analytical\BH.txt";
        static List<double> myB = new List<double>();
        static List<double> myH = new List<double>();
        public static void readBHCurveFromFile(String fn = "")
        {
            if (fn == "")
                fn = BHcurvefile;
            using (StreamReader reader = new StreamReader(fn))
            {
                reader.ReadLine();//skip first line
                while (!reader.EndOfStream)
                {
                    String s = reader.ReadLine();
                    String[] ss = s.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    myB.Add(double.Parse(ss[0]));
                    myH.Add(double.Parse(ss[1]));
                }
            }
        }

        /// <summary>
        /// Get a motor for all these calculations
        /// Stator predefined
        /// </summary>
        /// <returns></returns>
        public static Geo_PMSM_MagnetVShape MyMotor()
        {
            ////// all length is in mm
            //////
            Geo_PMSM_MagnetVShape m = new Geo_PMSM_MagnetVShape();

            //stator
            Geo_PMSM_MagnetVShape.StatorParameters sp = new Geo_PMSM_MagnetVShape.StatorParameters();
            sp.L = 125;
            sp.q = 2;
            sp.Q = 36;
            sp.Dstator = 191;
            sp.Dinstator = 126;
            sp.wz = 4.85;
            sp.lz = 14.3;
            sp.wy = 18;
            sp.ly = 45;
            m.setStatorParameters(sp);

            //coeff
            Geo_PMSM_MagnetVShape.Coefficients coeff = new Geo_PMSM_MagnetVShape.Coefficients();
            coeff.Kc = 1.15;
            m.setCoefficients(coeff);

            // materials
            Geo_PMSM_MagnetVShape.MaterialParameters md = new Geo_PMSM_MagnetVShape.MaterialParameters();
            md.Hc = 883310;
            md.mu_M = 1.045;
            md.Bsat = 1.9;

            if (myB == null)
                readBHCurveFromFile(BHcurvefile);
            md.B = myB.ToArray();
            md.H = myH.ToArray();

            m.setMaterialData(md);

            return m;
        }

        public static Geo_PMSM_MagnetVShape_Ansys MyMotorRMxprt()
        {
            ////// all length is in mm
            //////
            Geo_PMSM_MagnetVShape_Ansys m = new Geo_PMSM_MagnetVShape_Ansys();

            //stator
            Geo_PMSM_MagnetVShape_Ansys.StatorParameters sp = new Geo_PMSM_MagnetVShape_Ansys.StatorParameters();
            sp.L = 125;
            sp.q = 2;
            sp.Q = 36;
            sp.Dstator = 191;
            sp.Dinstator = 126;
            sp.wz = 4.85;
            sp.lz = 14.3;
            sp.wy = 18;
            sp.ly = 45;
            m.setStatorParameters(sp);

            //coeff
            Geo_PMSM_MagnetVShape_Ansys.Coefficients coeff = new Geo_PMSM_MagnetVShape_Ansys.Coefficients();
            coeff.Kc = 1.15;
            m.setCoefficients(coeff);

            // materials
            Geo_PMSM_MagnetVShape_Ansys.MaterialParameters md = new Geo_PMSM_MagnetVShape_Ansys.MaterialParameters();
            md.Hc = 883310;
            md.mu_M = 1.045;
            md.Bsat = 1.9;

            if (myB == null)
                readBHCurveFromFile(BHcurvefile);
            md.B = myB.ToArray();
            md.H = myH.ToArray();

            m.setMaterialData(md);

            return m;
        }

        /// <summary>
        /// Rotor type 1:
        /// lm=2.5  p=3 alphaM=45   delta=0.6
        /// </summary>
        /// <returns></returns>
        public static Geo_PMSM_MagnetVShape.RotorParameters RotorType1()
        {
            // data input (mm and rad)
            Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
            rip.lm = 2.5; // magnet length
            rip.p = 3; // pair poles
            rip.wb2min = 2 / 2; // min barrier width
            rip.wFe = 1; // Fe width 
            rip.wFe2 = 1;
            rip.alphaM = 45 * Math.PI / 180; // magnet angle
            rip.Rrotor = 126 / 2 - 0.6; // airgap

            return rip;
        }

        public static Geo_PMSM_MagnetVShape.RotorParameters RotorType2()
        {
            // data input (mm and rad)
            Geo_PMSM_MagnetVShape.RotorParameters rip = new Geo_PMSM_MagnetVShape.RotorParameters();
            rip.lm = 3; // magnet length
            rip.p = 3; // pair poles
            rip.wb2min = 2 / 2; // min barrier width
            rip.wFe = 1; // Fe width 
            rip.wFe2 = 1;
            rip.alphaM = 60 * Math.PI / 180; // magnet angle
            rip.Rrotor = 126 / 2 - 0.6; // airgap

            return rip;
        }
    }
}
