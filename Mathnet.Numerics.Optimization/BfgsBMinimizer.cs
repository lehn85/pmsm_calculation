using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace MathNet.Numerics.Optimization
{

    public class BfgsBMinimizer
    {
        public double GradientTolerance { get; set; }
        public double ParameterTolerance { get; set; }
        public int MaximumIterations { get; set; }
        public double FunctionProgressTolerance { get; set; }

        public BfgsBMinimizer(double gradient_tolerance, double parameter_tolerance, double function_progress_tolerance, int maximum_iterations = 1000)
        {
            this.GradientTolerance = gradient_tolerance;
            this.ParameterTolerance = parameter_tolerance;
            this.MaximumIterations = maximum_iterations;
            this.FunctionProgressTolerance = function_progress_tolerance;
        }

        public MinimizationOutput FindMinimum(IObjectiveFunction objective, Vector<double> lower_bound, Vector<double> upper_bound, Vector<double> initial_guess)
        {
            if (!objective.GradientSupported)
                throw new IncompatibleObjectiveException("Gradient not supported in objective function, but required for BFGS minimization.");

            if (!(objective is ObjectiveChecker))
                objective = new ObjectiveChecker(objective, this.ValidateObjective, this.ValidateGradient, null);

            // Check that dimensions match
            if (lower_bound.Count != upper_bound.Count || lower_bound.Count != initial_guess.Count)
                throw new ArgumentException("Dimensions of bounds and/or initial guess do not match.");

            // Check that initial guess is feasible
            for (int ii = 0; ii < initial_guess.Count; ++ii)
                if (initial_guess[ii] < lower_bound[ii] || initial_guess[ii] > upper_bound[ii])
                    throw new ArgumentException("Initial guess is not in the feasible region");

            IEvaluation initial_eval = objective.Evaluate(initial_guess);

            // Check that we're not already done
            ExitCondition current_exit_condition = this.ExitCriteriaSatisfied(initial_eval, null, lower_bound, upper_bound,0);
            if (current_exit_condition != ExitCondition.None)
                return new MinimizationOutput(initial_eval, 0, current_exit_condition);

            // Set up line search algorithm
            var line_searcher = new StrongWolfeLineSearch(1e-4, 0.9, Math.Max(this.ParameterTolerance,1e-5), max_iterations: 1000);

            // Declare state variables
            IEvaluation candidate_point, previous_point;
            double step_size;
            Vector<double> gradient, step, line_search_direction, reduced_solution1, reduced_gradient, reduced_initial_point, reduced_cauchy_point, solution1;
            Matrix<double> pseudo_hessian, reduced_hessian;
            List<int> reduced_map;

            // First step
            pseudo_hessian = DiagonalMatrix.CreateIdentity(initial_guess.Count);

            // Determine active set
            var gradient_projection_result = QuadraticGradientProjectionSearch.search(initial_eval.Point, initial_eval.Gradient, pseudo_hessian, lower_bound, upper_bound);
            var cauchy_point = gradient_projection_result.Item1;
            var fixed_count = gradient_projection_result.Item2;
            var is_fixed = gradient_projection_result.Item3;
            var free_count = lower_bound.Count - fixed_count;

            if (free_count > 0)
            {
                reduced_gradient = new DenseVector(free_count);
                reduced_hessian = new DenseMatrix(free_count, free_count);
                reduced_map = new List<int>(free_count);
                reduced_initial_point = new DenseVector(free_count);
                reduced_cauchy_point = new DenseVector(free_count);

                CreateReducedData(initial_eval.Point, cauchy_point, is_fixed, lower_bound, upper_bound, initial_eval.Gradient, pseudo_hessian, reduced_initial_point, reduced_cauchy_point, reduced_gradient, reduced_hessian, reduced_map);

                // Determine search direction and maximum step size
                reduced_solution1 = reduced_initial_point + reduced_hessian.Cholesky().Solve(-reduced_gradient);

                solution1 = reduced_to_full(reduced_map, reduced_solution1, cauchy_point);
            }
            else
            {
                solution1 = cauchy_point;
            }

            var direction_from_cauchy = solution1 - cauchy_point;
            var max_step_from_cauchy_point = FindMaxStep(cauchy_point, direction_from_cauchy, lower_bound, upper_bound);

            var solution2 = cauchy_point + Math.Min(max_step_from_cauchy_point, 1.0) * direction_from_cauchy;

            line_search_direction = solution2 - initial_eval.Point;
            var max_line_search_step = FindMaxStep(initial_eval.Point, line_search_direction, lower_bound, upper_bound);
            var est_step_size = -initial_eval.Gradient * line_search_direction / (line_search_direction * pseudo_hessian * line_search_direction);

            var starting_step_size = Math.Min(Math.Max(est_step_size, 1.0), max_line_search_step);

            // Line search
            LineSearchOutput result;
            try
            {
                result = line_searcher.FindConformingStep(objective, initial_eval, line_search_direction, starting_step_size, upper_bound:max_line_search_step);
            }
            catch (Exception e)
            {
                throw new InnerOptimizationException("Line search failed.", e);
            }

            previous_point = initial_eval;
            candidate_point = result.FunctionInfoAtMinimum;
            gradient = candidate_point.Gradient;
            step = candidate_point.Point - initial_guess;
            step_size = result.FinalStep;

            // Subsequent steps
            int iterations;
            int total_line_search_steps = result.Iterations;
            int iterations_with_nontrivial_line_search = result.Iterations > 0 ? 0 : 1;
            int steepest_descent_resets = 0;
            for (iterations = 1; iterations < this.MaximumIterations; ++iterations)
            {
                // Do BFGS update
                var y = candidate_point.Gradient - previous_point.Gradient;

                double sy = step * y;
                if (sy > 0.0) // only do update if it will create a positive definite matrix
                {
                    double sts = step * step;
                    //inverse_pseudo_hessian = inverse_pseudo_hessian + ((sy + y * inverse_pseudo_hessian * y) / Math.Pow(sy, 2.0)) * step.OuterProduct(step) - ((inverse_pseudo_hessian * y.ToColumnMatrix()) * step.ToRowMatrix() + step.ToColumnMatrix() * (y.ToRowMatrix() * inverse_pseudo_hessian)) * (1.0 / sy);
                    var Hs = pseudo_hessian * step;
                    var sHs = step * pseudo_hessian * step;
                    pseudo_hessian = pseudo_hessian + y.OuterProduct(y) * (1.0 / sy) - Hs.OuterProduct(Hs) * (1.0 / sHs);
                }
                else
                {
                    steepest_descent_resets += 1;
                    //pseudo_hessian = LinearAlgebra.Double.DiagonalMatrix.Identity(initial_guess.Count);
                }

                // Determine active set
                gradient_projection_result = QuadraticGradientProjectionSearch.search(candidate_point.Point, candidate_point.Gradient, pseudo_hessian, lower_bound, upper_bound);
                cauchy_point = gradient_projection_result.Item1;
                fixed_count = gradient_projection_result.Item2;
                is_fixed = gradient_projection_result.Item3;
                free_count = lower_bound.Count - fixed_count;

                if (free_count > 0)
                {
                    reduced_gradient = new DenseVector(free_count);
                    reduced_hessian = new DenseMatrix(free_count, free_count);
                    reduced_map = new List<int>(free_count);
                    reduced_initial_point = new DenseVector(free_count);
                    reduced_cauchy_point = new DenseVector(free_count);

                    CreateReducedData(candidate_point.Point, cauchy_point, is_fixed, lower_bound, upper_bound, candidate_point.Gradient, pseudo_hessian, reduced_initial_point, reduced_cauchy_point, reduced_gradient, reduced_hessian, reduced_map);

                    // Determine search direction and maximum step size
                    reduced_solution1 = reduced_initial_point + reduced_hessian.Cholesky().Solve(-reduced_gradient);

                    solution1 = reduced_to_full(reduced_map, reduced_solution1, cauchy_point);
                }
                else
                {
                    solution1 = cauchy_point;
                }

                direction_from_cauchy = solution1 - cauchy_point;
                max_step_from_cauchy_point = FindMaxStep(cauchy_point, direction_from_cauchy, lower_bound, upper_bound);
                //var cauchy_eval = objective.Evaluate(cauchy_point);

                solution2 = cauchy_point + Math.Min(max_step_from_cauchy_point, 1.0) * direction_from_cauchy;

                line_search_direction = solution2 - candidate_point.Point;
                max_line_search_step = FindMaxStep(candidate_point.Point, line_search_direction, lower_bound, upper_bound);

                //line_search_direction = solution1 - candidate_point.Point;
                //max_line_search_step = FindMaxStep(candidate_point.Point, line_search_direction, lower_bound, upper_bound);

                if (max_line_search_step == 0.0)
                {
                    line_search_direction = cauchy_point - candidate_point.Point;
                    max_line_search_step = FindMaxStep(candidate_point.Point, line_search_direction, lower_bound, upper_bound);
                }

                est_step_size = -candidate_point.Gradient * line_search_direction / (line_search_direction * pseudo_hessian * line_search_direction);

                starting_step_size = Math.Min(Math.Max(est_step_size, 1.0), max_line_search_step);

                // Line search
                try
                {
                    result = line_searcher.FindConformingStep(objective, candidate_point, line_search_direction, starting_step_size, upper_bound: max_line_search_step);
                    //result = line_searcher.FindConformingStep(objective, cauchy_eval, direction_from_cauchy, Math.Min(1.0, max_step_from_cauchy_point), upper_bound: max_step_from_cauchy_point);
                }
                catch (Exception e)
                {
                    throw new InnerOptimizationException("Line search failed.", e);
                }

                iterations_with_nontrivial_line_search += result.Iterations > 0 ? 1 : 0;
                total_line_search_steps += result.Iterations;

                step_size = result.FinalStep;
                step = result.FunctionInfoAtMinimum.Point - candidate_point.Point;
                previous_point = candidate_point;
                candidate_point = result.FunctionInfoAtMinimum;

                current_exit_condition = this.ExitCriteriaSatisfied(candidate_point, previous_point, lower_bound, upper_bound, iterations);
                if (current_exit_condition != ExitCondition.None)
                    break;
            }

            if (iterations == this.MaximumIterations && current_exit_condition == ExitCondition.None)
                throw new MaximumIterationsException(String.Format("Maximum iterations ({0}) reached.", this.MaximumIterations));

            return new MinimizationWithLineSearchOutput(candidate_point, iterations, current_exit_condition, total_line_search_steps, iterations_with_nontrivial_line_search);
        }

        private static Vector<double> reduced_to_full(List<int> reduced_map, Vector<double> reduced_vector, Vector<double> full_vector)
        {
            var output = (Vector<double>)full_vector.Clone();
            for (int ii = 0; ii < reduced_map.Count; ++ii)
                output[reduced_map[ii]] = reduced_vector[ii];
            return output;
        }

        private static double FindMaxStep(Vector<double> starting_point, Vector<double> search_direction, Vector<double> lower_bound, Vector<double> upper_bound)
        {
            double max_step = Double.PositiveInfinity;
            for (int ii = 0; ii < starting_point.Count; ++ii)
            {
                double param_max_step;
                if (search_direction[ii] > 0)
                    param_max_step = (upper_bound[ii] - starting_point[ii]) / search_direction[ii];
                else if (search_direction[ii] < 0)
                    param_max_step = (starting_point[ii] - lower_bound[ii]) / -search_direction[ii];
                else
                    param_max_step = Double.PositiveInfinity;

                if (param_max_step < max_step)
                    max_step = param_max_step;
            }
            return max_step;
        }

        private static void CreateReducedData(Vector<double> initial_point, Vector<double> cauchy_point, List<bool> is_fixed, Vector<double> lower_bound, Vector<double> upper_bound, Vector<double> gradient, Matrix<double> pseudo_hessian, Vector<double> reduced_initial_point, Vector<double> reduced_cauchy_point, Vector<double> reduced_gradient, Matrix<double> reduced_hessian, List<int> reduced_map)
        {
            int ll = 0;
            for (int ii = 0; ii < lower_bound.Count; ++ii)
            {
                if (!is_fixed[ii])
                {
                    // hessian
                    int mm = 0;
                    for (int jj = 0; jj < lower_bound.Count; ++jj)
                    {
                        if (!is_fixed[jj])
                        {
                            reduced_hessian[ll, mm++] = pseudo_hessian[ii, jj];
                        }
                    }

                    // gradient 
                    reduced_initial_point[ll] = initial_point[ii];
                    reduced_cauchy_point[ll] = cauchy_point[ii];
                    reduced_gradient[ll] = gradient[ii];
                    ll += 1;
                    reduced_map.Add(ii);
                    
                }
            }
        }

        private static double VERY_SMALL = 1e-15;
        private ExitCondition ExitCriteriaSatisfied(IEvaluation candidate_point, IEvaluation last_point, Vector<double> lower_bound, Vector<double> upper_bound, int iterations)
        {
            Vector<double> rel_grad = new MathNet.Numerics.LinearAlgebra.Double.DenseVector(candidate_point.Point.Count);
            double relative_gradient = 0.0;
            double normalizer = Math.Max(Math.Abs(candidate_point.Value), 1.0);
            for (int ii = 0; ii < rel_grad.Count; ++ii)
            {
                var projected_gradient = 0.0;
                
                bool at_lower_bound = candidate_point.Point[ii] - lower_bound[ii] < VERY_SMALL;
                bool at_upper_bound = upper_bound[ii] - candidate_point.Point[ii] < VERY_SMALL;
                
                if (at_lower_bound && at_upper_bound)
                    projected_gradient = 0.0;
                else if (at_lower_bound)
                    projected_gradient = Math.Min(candidate_point.Gradient[ii], 0.0);
                else if (at_upper_bound)
                    projected_gradient = Math.Max(candidate_point.Gradient[ii], 0.0);
                else
                    projected_gradient = candidate_point.Gradient[ii];

                double tmp = projected_gradient * Math.Max(Math.Abs(candidate_point.Point[ii]), 1.0) / normalizer;
                relative_gradient = Math.Max(relative_gradient, Math.Abs(tmp));
            }
            if (relative_gradient < this.GradientTolerance)
            {
                return ExitCondition.RelativeGradient;
            }

            if (last_point != null)
            {
                double most_progress = 0.0;
                for (int ii = 0; ii < candidate_point.Point.Count; ++ii)
                {
                    var tmp = Math.Abs(candidate_point.Point[ii] - last_point.Point[ii]) / Math.Max(Math.Abs(last_point.Point[ii]), 1.0);
                    most_progress = Math.Max(most_progress, tmp);
                }
                if (most_progress < this.ParameterTolerance)
                {
                    return ExitCondition.LackOfProgress;
                }

                double function_change = candidate_point.Value - last_point.Value;
                if (iterations > 500 && function_change < 0 && Math.Abs(function_change) < this.FunctionProgressTolerance)
                    return ExitCondition.LackOfProgress;
            }

            return ExitCondition.None;
        }

        private void ValidateGradient(IEvaluation eval)
        {
            foreach (var x in eval.Gradient)
            {
                if (Double.IsNaN(x) || Double.IsInfinity(x))
                    throw new EvaluationException("Non-finite gradient returned.", eval);
            }
        }

        private void ValidateObjective(IEvaluation eval)
        {
            if (Double.IsNaN(eval.Value) || Double.IsInfinity(eval.Value))
                throw new EvaluationException("Non-finite objective function returned.", eval);
        }
    }
}
