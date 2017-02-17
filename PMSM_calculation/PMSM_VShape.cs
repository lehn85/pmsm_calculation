using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace calc_from_geometryOfMotor
{
    class PMSM_VShape
    {
        Geo_PMSM_MagnetVShape geo;
        GeneralMagnetCircuit gmc;

        public int Nslot;
        public double[] Fs;

        public String OutputFolder {get; set;}
        public String OutputFileResult { get; set; }

        PMSM_VShape(Geo_PMSM_MagnetVShape geo)
        {
            this.geo = geo;
            gmc = geo.buildGMC();
            OutputFolder = ".\\out\\";
            OutputFileResult = OutputFolder + "result.txt";
        }

        public void setBH(double[] b, double[] h)
        {
            //geo.B = b;
            //geo.H = h;
            //gmc.B = b;
            //gmc.H = h;
        }        

        public void doFullCalc()
        {

        }
    }
}
