/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Text;
using System.IO;
using System.Windows.Forms;
using log4net.Config;
using System.Linq;
using calc_from_geometryOfMotor.motor.PMMotor;
using fastJSON;
using System.Collections.Generic;

namespace calc_from_geometryOfMotor
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // logger
            byte[] xmlbytes = Encoding.ASCII.GetBytes(Properties.Resources.log4net_config);
            MemoryStream stream = new MemoryStream(xmlbytes);
            XmlConfigurator.Configure(stream);
            stream.Close();

            JSON.Parameters.KVStyleStringDictionary = false;
            JSON.Parameters.SerializeNullValues = false;            
            JSON.Parameters.UseExtensions = false;
            JSON.Parameters.UseEscapedUnicode = false;

            //testMatlab();
            //return;

            // start
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }

        static void testMatlab()
        {
            String resultfile = @"E:\zAspirant\FEMM\pmsm\3\m1\AFF5F2D02F1259AF592F2A9F97EA7AF7\MMAnalysis\results_B9BECAAE30D8040B936583CFFDE3E39F.txt";
            PM_MMAnalysisResults results = (PM_MMAnalysisResults)PM_MMAnalysisResults.loadResultsFromFile(typeof(PM_MMAnalysisResults), resultfile);

            String id_values = "[";
            String psid_values = "[";
            var sortedResultsById = results.ListResults.OrderBy(r => r.FluxLinkage_dq.d).ToArray();
            foreach (var item in sortedResultsById)
            {
                id_values += item.Idq.d + " ";
                psid_values += item.FluxLinkage_dq.d + " ";
            }

            id_values += "]";
            psid_values += "]";

            String iq_values = "[";
            String psiq_values = "[";
            var sortedResultsByIq = results.ListResults.OrderBy(r => r.FluxLinkage_dq.q).ToArray();
            foreach (var item in sortedResultsByIq)
            {
                iq_values += item.Idq.q + " ";
                psiq_values += item.FluxLinkage_dq.q + " ";
            }

            iq_values += "]";
            psiq_values += "]";

            MATLAB ml = MATLAB.DefaultInstance;
            String pathtomodel = @"E:\MatlabProject\zAspirant\pm\";
            String modelname = "m1_test";
            String pathtoblock = "m1_test/PMSM";
            ml.ChangeWorkingFolder(pathtomodel);
            ml.load_system(modelname);
            ml.set_param(pathtoblock, "id_values", id_values);
            ml.set_param(pathtoblock, "iq_values", iq_values);
            ml.set_param(pathtoblock, "psid_values", psid_values);
            ml.set_param(pathtoblock, "psiq_values", psiq_values);

            ml.save_system(modelname, "m1_test_auto");//save as new name
            ml.close_system("m1_test_auto");//close it
        }
    }
}
