using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;

namespace MathNet.Numerics.Optimization
{
    public static class QuadraticGradientProjectionSearch
    {
        public static Tuple<Vector<double>,int,List<bool>> search(Vector<double> x0, Vector<double> gradient, Matrix<double> hessian, Vector<double> lower_bound, Vector<double> upper_bound)
        {
            List<bool> is_fixed = new List<bool>(x0.Count);
            List<double> breakpoint = new List<double>(x0.Count);
            for (int ii = 0; ii < x0.Count; ++ii)
            {
                breakpoint.Add(0.0);
                is_fixed.Add(false);
                if (gradient[ii] < 0)
                    breakpoint[ii] = (x0[ii] - upper_bound[ii]) / gradient[ii];
                else if (gradient[ii] > 0)
                    breakpoint[ii] = (x0[ii] - lower_bound[ii]) / gradient[ii];
                else
                {
                    if (Math.Abs(x0[ii] - upper_bound[ii]) < 100 * Double.Epsilon || Math.Abs(x0[ii] - lower_bound[ii]) < 100 * Double.Epsilon)
                        breakpoint[ii] = 0.0;
                    else 
                        breakpoint[ii] = Double.PositiveInfinity;
                }
            }

            var ordered_breakpoint = new List<double>(x0.Count);
            ordered_breakpoint.AddRange(breakpoint);
            ordered_breakpoint.Sort();

            // Compute initial state variables
            var d = -gradient;
            for (int ii = 0; ii < d.Count; ++ii)
                if (breakpoint[ii] <= 0.0)
                    d[ii] *= 0.0;


            int jj = -1;
            var x = x0;
            var f1 = gradient * d;
            var f2 = 0.5 * d * hessian * d;
            var s_min = -f1 / f2;
            var max_s = ordered_breakpoint[0];

            if (s_min < max_s)
                return Tuple.Create(x + s_min * d, 0,is_fixed);

            // while minimum of the last quadratic piece observed is beyond the interval searched
            while (true)
            {
                // update data to the beginning of the interval we're searching
                jj += 1;
                x = x + d * max_s;
                max_s = ordered_breakpoint[jj+1] - ordered_breakpoint[jj];                
                
                int fixed_count = 0;
                for (int ii = 0; ii < d.Count; ++ii)
                    if (ordered_breakpoint[jj] >= breakpoint[ii])
                    {
                        d[ii] *= 0.0;
                        is_fixed[ii] = true;
                        fixed_count += 1;
                    }

                if (Double.IsPositiveInfinity(ordered_breakpoint[jj + 1]))
                    return Tuple.Create(x, fixed_count, is_fixed);
                
                f1 = gradient * d + (x - x0) * hessian * d;
                f2 = d * hessian * d;

                s_min = -f1 / f2;

                if (s_min < max_s)
                    return Tuple.Create(x + s_min * d, fixed_count, is_fixed);
                else if (jj + 1 >= ordered_breakpoint.Count - 1)
                {
                    is_fixed[is_fixed.Count - 1] = true;
                    return Tuple.Create(x + max_s * d, lower_bound.Count, is_fixed);
                }
            }
        }
    }
}
