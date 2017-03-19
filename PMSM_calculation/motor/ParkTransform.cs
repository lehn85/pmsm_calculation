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
using MathNet.Numerics;

namespace calc_from_geometryOfMotor
{
    public class ParkTransform
    {
        /// <summary>
        /// Convert from abc to dq
        /// Theta is radian
        /// </summary>
        /// <param name="a">fa</param>
        /// <param name="b">fb</param>
        /// <param name="c">fc</param>
        /// <param name="theta">Angle (radian) from 0a to 0d (counter clockwise)</param>
        /// <returns></returns>
        public static Fdq abc_dq(double a, double b, double c, double theta)
        {
            Fdq fdq = new Fdq();
            fdq.d = 2.0 / 3 * (Math.Cos(theta) * a + Math.Cos(theta - 2 * Math.PI / 3) * b + Math.Cos(theta + 2 * Math.PI / 3) * c);
            fdq.q = -2.0 / 3 * (Math.Sin(theta) * a + Math.Sin(theta - 2 * Math.PI / 3) * b + Math.Sin(theta + 2 * Math.PI / 3) * c);
            return fdq;
        }

        /// <summary>
        /// Convert abc to dq
        /// Theta is radian
        /// </summary>
        /// <param name="fabc">fabc</param>
        /// <param name="theta">Angle from 0a to 0d (counter clockwise)</param>
        /// <returns></returns>
        public static Fdq abc_dq(Fabc fabc, double theta)
        {
            return abc_dq(fabc.a, fabc.b, fabc.c, theta);
        }

        /// <summary>
        /// Convert from dq to abc
        /// </summary>
        /// <param name="d"></param>
        /// <param name="q"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static Fabc dq_abc(double d, double q, double theta)
        {
            Fabc fabc = new Fabc();
            fabc.a = Math.Cos(theta) * d - Math.Sin(theta) * q;
            fabc.b = Math.Cos(theta - 2 * Math.PI / 3) * d - Math.Sin(theta - 2 * Math.PI / 3) * q;
            fabc.c = Math.Cos(theta + 2 * Math.PI / 3) * d - Math.Sin(theta + 2 * Math.PI / 3) * q;
            return fabc;
        }

        public static Fabc dq_abc(Fdq fdq, double theta)
        {
            return dq_abc(fdq.d, fdq.q, theta);
        }
    }

    public struct Fabc
    {
        public double a { get; set; }
        public double b { get; set; }
        public double c { get; set; }
    }

    public struct Fdq
    {
        public double d { get; set; }
        public double q { get; set; }

        public double Magnitude
        {
            get
            {
                return Math.Sqrt(d * d + q * q);
            }
        }

        public double Phase
        {
            get
            {
                double phase = Math.Atan(q / d) * 180 / Math.PI;
                if (phase < 0)
                    phase += 180;
                return phase;
            }
        }
    }
}
