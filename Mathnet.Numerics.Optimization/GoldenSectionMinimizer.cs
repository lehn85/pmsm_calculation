using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathNet.Numerics.Optimization
{
    public class GoldenSectionMinimizer
    {
        public double XTolerance { get; set; }
        public int MaximumIterations { get; set; }
        public int MaximumExpansionSteps { get; set; }
        public double LowerExpansionFactor { get; set; }
        public double UpperExpansionFactor { get; set; }

        public GoldenSectionMinimizer(double x_tolerance=1e-5, int max_iterations=1000, int max_expansion_steps=10, double lower_expansion_factor = 2.0, double upper_expansion_factor = 2.0)
        {
            this.XTolerance = x_tolerance;
            this.MaximumIterations = max_iterations;
            this.MaximumExpansionSteps = max_expansion_steps;
            this.LowerExpansionFactor = lower_expansion_factor;
            this.UpperExpansionFactor = upper_expansion_factor;
        }

        public MinimizationOutput1D FindMinimum(IObjectiveFunction1D objective, double lower_bound, double upper_bound)
        {
            if (!(objective is ObjectiveChecker1D))
                objective = new ObjectiveChecker1D(objective, this.ValueChecker, null, null);

            if (upper_bound <= lower_bound)
                throw new OptimizationException("Lower bound must be lower than upper bound.");

            double middle_point_x = lower_bound + (upper_bound - lower_bound) / (1 + _golden_ratio);
            IEvaluation1D lower = objective.Evaluate(lower_bound);
            IEvaluation1D middle = objective.Evaluate(middle_point_x);
            IEvaluation1D upper = objective.Evaluate(upper_bound);

            int expansion_steps = 0;
            while ( (expansion_steps < this.MaximumExpansionSteps) && (upper.Value < middle.Value || lower.Value < middle.Value))
            {
                if (lower.Value < middle.Value)
                {
                    lower_bound = 0.5 * (upper_bound + lower_bound) - this.LowerExpansionFactor*0.5*(upper_bound - lower_bound);
                    lower = objective.Evaluate(lower_bound);
                }

                if (upper.Value < middle.Value)
                {
                    upper_bound = 0.5 * (upper_bound + lower_bound) + this.UpperExpansionFactor*0.5*(upper_bound - lower_bound);
                    upper = objective.Evaluate(upper_bound);
                }

                middle_point_x = lower_bound + (upper_bound - lower_bound) / (1 + _golden_ratio);
                middle = objective.Evaluate(middle_point_x);

                expansion_steps += 1;
            }

            if (upper.Value < middle.Value || lower.Value < middle.Value)
                throw new OptimizationException("Lower and upper bounds do not necessarily bound a minimum.");

            int iterations = 0;
            while (Math.Abs(upper.Point - lower.Point) > this.XTolerance && iterations < this.MaximumIterations)
            {
                double test_x = lower.Point + (upper.Point - middle.Point);
                var test = objective.Evaluate(test_x);

                if (test.Point < middle.Point)
                {
                    if (test.Value > middle.Value)
                    {
                        lower = test;
                    }
                    else
                    {
                        upper = middle;
                        middle = test;
                    }
                }
                else
                {
                    if (test.Value > middle.Value)
                    {
                        upper = test;
                    }
                    else
                    {
                        lower = middle;
                        middle = test;
                    }
                }

                iterations += 1;
            }

            if (iterations == this.MaximumIterations)
                throw new MaximumIterationsException("Max iterations reached.");
            else
                return new MinimizationOutput1D(middle, iterations, ExitCondition.BoundTolerance);
            
        }

        private void ValueChecker(IEvaluation1D eval)
        {
            if (Double.IsNaN(eval.Value) || Double.IsInfinity(eval.Value))
                throw new EvaluationException("Objective function returned non-finite value.", eval);
        }
        private static double _golden_ratio = (1.0 + Math.Sqrt(5)) / 2.0;
    }
}
