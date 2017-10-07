//  All code copyright (c) 2003 Barry Lapthorn
//  Website:  http://www.lapthorn.net
//
//  Disclaimer:  
//  All code is provided on an "AS IS" basis, without warranty. The author 
//  makes no representation, or warranty, either express or implied, with 
//  respect to the code, its quality, accuracy, or fitness for a specific 
//  purpose. Therefore, the author shall not have any liability to you or any 
//  other person or entity with respect to any liability, loss, or damage 
//  caused or alleged to have been caused directly or indirectly by the code
//  provided.  This includes, but is not limited to, interruption of service, 
//  loss of data, loss of profits, or consequential damages from the use of 
//  this code.
//
//
//  $Author: barry $
//  $Revision: 1.1 $
//
//  $Id: GA.cs,v 1.1 2003/08/19 20:59:05 barry Exp $

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace btl.generic
{

    public delegate double[] GAFunction(double[] values);

    /// <summary>
    /// Genetic Algorithm class
    /// </summary>
    public class GA
    {
        public event Action<GA, int> StepFinish;

        /// <summary>
        /// Default constructor sets mutation rate to 5%, crossover to 80%, population to 100,
        /// and generations to 2000.
        /// </summary>
        public GA()
        {
            InitialValues();
            m_mutationRate = 0.05;
            m_crossoverRate = 0.80;
            m_populationSize = 100;
            m_generationSize = 2000;
            m_fitnessFile = "";
        }

        public GA(double crossoverRate, double mutationRate, int populationSize, int generationSize, int genomeSize, int fitnessSize)
        {
            InitialValues();
            m_mutationRate = mutationRate;
            m_crossoverRate = crossoverRate;
            m_populationSize = populationSize;
            m_generationSize = generationSize;
            m_genomeSize = genomeSize;
            m_fitnessSize = fitnessSize;
            m_fitnessFile = "";
        }

        public GA(int genomeSize, int fitnessSize)
        {
            InitialValues();
            m_genomeSize = genomeSize;
            m_fitnessSize = fitnessSize;
        }

        public void InitialValues()
        {
            m_elitism = false;
            Cancelled = false;
        }

        /// <summary>
        /// Method which starts the GA executing.
        /// </summary>
        public void Go()
        {
            if (getFitness == null)
                throw new ArgumentNullException("Need to supply fitness function");
            if (m_genomeSize == 0)
                throw new IndexOutOfRangeException("Genome size not set");
            if (m_fitnessSize == 0)
                throw new IndexOutOfRangeException("Fitness size not set");

            Cancelled = false;
            Running = true;

            //  Create the fitness table.
            //m_fitnessTable = new ArrayList();
            m_thisGeneration = new List<Individual>(m_generationSize);
            allGenerations = new List<Individual>();

            Individual.MutationRate = m_mutationRate;

            CreateGenomes();
            RankPopulation();

            // first step events
            if (StepFinish != null)
                StepFinish.Invoke(this, 0);

            StreamWriter outputFitness = null;
            if (m_fitnessFile != "")
            {
                outputFitness = new StreamWriter(m_fitnessFile);

                outputFitness.WriteLine("{0}\t{1}", GenomeSize, FitnessSize);//sizes

                // first gen
                writeData(outputFitness, m_thisGeneration, true);
            }

            for (int i = 0; i < m_generationSize; i++)
            {
                CreateNextGeneration();

                // rank, create non-dominated group
                RankPopulation();

                // check cancel flag after most time-consuming method
                if (Cancelled)
                    break;

                // debug
                if (outputFitness != null)
                {
                    // write this generation data
                    writeData(outputFitness, m_thisGeneration);
                }

                // events
                if (StepFinish != null)
                    StepFinish.Invoke(this, i + 1);
            }

            if (outputFitness != null)
                outputFitness.Close();

            // write final nondominated set
            if (m_fitnessFile != "")
            {
                string fn = Path.GetDirectoryName(m_fitnessFile) + "\\" + Path.GetFileNameWithoutExtension(m_fitnessFile) + "_final" + Path.GetExtension(m_fitnessFile);
                using (StreamWriter sw = new StreamWriter(fn))
                {
                    m_finalNonDominatedSet = buildNonDominatedSetFromGeneration(allGenerations);
                    sw.WriteLine("{0}\t{1}", GenomeSize, FitnessSize);//sizes                 
                    writeData(sw, m_finalNonDominatedSet);
                }
            }



            Running = false;
        }

        private void writeData(StreamWriter sw, List<Individual> data, bool includeGens = true)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Count; i++)
            {
                double[] f = data[i].Fitness;

                // check if this individual is a failure
                if (f == null)
                    continue;

                double[] g = data[i].Genes;
                for (int j = 0; j < g.Length; j++)
                {
                    sb.AppendFormat("{0}\t", g[j]);
                }

                for (int j = 0; j < f.Length; j++)
                {
                    sb.AppendFormat("{0}\t", f[j]);
                }                
                sb.Append("\n");
            }

            sw.Write(sb.ToString());
        }

        /// <summary>
        /// After ranking all the genomes by fitness, use a 'roulette wheel' selection
        /// method.  This allocates a large probability of selection to those with the 
        /// highest fitness.
        /// </summary>
        /// <returns>Random individual biased towards highest fitness</returns>
        //private int RouletteSelection()
        //{
        //    double randomFitness = m_random.NextDouble() * m_totalFitness;
        //    int idx = -1;
        //    int mid;
        //    int first = 0;
        //    int last = m_populationSize - 1;
        //    mid = (last - first) / 2;

        //    //  ArrayList's BinarySearch is for exact values only
        //    //  so do this by hand.
        //    while (idx == -1 && first <= last && !Cancelled)
        //    {
        //        if (randomFitness < (double)m_fitnessTable[mid])
        //        {
        //            last = mid;
        //        }
        //        else if (randomFitness > (double)m_fitnessTable[mid])
        //        {
        //            first = mid;
        //        }
        //        mid = (first + last) / 2;
        //        //  lies between i and i+1
        //        if ((last - first) == 1)
        //            idx = last;
        //    }
        //    return idx;
        //}

        /// <summary>
        /// Rank population and sort in order of fitness.
        /// </summary>
        private void RankPopulation()
        {
            // Calculate fitness
            foreach (Individual g in m_thisGeneration)
            {
                g.Fitness = FitnessFunction(g.Genes);
                if (g.Fitness != null)
                    allGenerations.Add(g);

                if (Cancelled)
                    return;
            }

            // find non-dominated group
            m_nonDominatedSet = buildNonDominatedSetFromGeneration(m_thisGeneration);
        }

        private bool isNondominated(Individual g, List<Individual> generation)
        {
            foreach (var y2 in generation)
            {
                // not itself
                if (g != y2)
                {
                    // if y2 dominates g, then g is dominated
                    if (y2.Compare(g) > 0)
                        return false;
                }
            }

            // not found any y2 that dominates g, meaning g is non dominated
            return true;
        }

        private List<Individual> buildNonDominatedSetFromGeneration(List<Individual> generation)
        {
            // find non-dominated group
            var nonDominatedSet = new List<Individual>();
            foreach (var g in generation)
            {
                if (g.Fitness == null)
                    continue;
                // if g is non-dominated, make a copy of it
                if (isNondominated(g, generation))
                    nonDominatedSet.Add(g);
            }

            return nonDominatedSet;
        }

        public List<Individual> GetFinalNonDominatedSet()
        {
            if (m_finalNonDominatedSet == null)
                m_finalNonDominatedSet = buildNonDominatedSetFromGeneration(allGenerations);

            return m_finalNonDominatedSet;
        }

        /// <summary>
        /// Create the *initial* genomes by repeated calling the supplied fitness function
        /// </summary>
        private void CreateGenomes()
        {
            for (int i = 0; i < m_populationSize; i++)
            {
                Individual g = new Individual(m_genomeSize, m_fitnessSize);
                m_thisGeneration.Add(g);
            }
        }

        private void CreateNextGeneration()
        {
            // no non dominated set?           
            if (m_nonDominatedSet == null)
                return;

            var m_nextGeneration = new List<Individual>(m_generationSize);
            Individual g = null;
            if (m_elitism)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i < m_nonDominatedSet.Count)
                        m_nextGeneration.Add(m_nonDominatedSet[i]);
                }
            }
            //g = m_thisGeneration[m_populationSize - 1];

            for (int i = 0; i < m_populationSize; i += 2)
            {
                int pidx1 = (int)(m_random.NextDouble() * m_nonDominatedSet.Count); //RouletteSelection();
                int pidx2 = (int)(m_random.NextDouble() * m_nonDominatedSet.Count);//RouletteSelection();
                Individual parent1, parent2, child1, child2;
                // must create clone from original set, 
                // otherwise if child created by assigning (not crossover) will mess up original set
                parent1 = new Individual(m_nonDominatedSet[pidx1]);
                parent2 = new Individual(m_nonDominatedSet[pidx2]);

                if (m_random.NextDouble() < m_crossoverRate)
                {
                    parent1.Crossover(ref parent2, out child1, out child2);
                }
                else
                {
                    child1 = parent1;
                    child2 = parent2;
                }
                child1.Mutate();
                child2.Mutate();

                m_nextGeneration.Add(child1);
                m_nextGeneration.Add(child2);
            }

            m_thisGeneration = m_nextGeneration;
        }

        private double m_mutationRate;
        private double m_crossoverRate;
        private int m_populationSize;
        private int m_generationSize;
        private int m_genomeSize;
        private int m_fitnessSize;
        private string m_fitnessFile;
        private bool m_elitism;

        private List<Individual> m_thisGeneration;
        private List<Individual> m_nonDominatedSet;
        private List<Individual> allGenerations;
        private List<Individual> m_finalNonDominatedSet;
        //private ArrayList m_fitnessTable;

        static Random m_random = new Random();

        static private GAFunction getFitness;
        public GAFunction FitnessFunction
        {
            get
            {
                return getFitness;
            }
            set
            {
                getFitness = value;
            }
        }

        #region Properties        

        //  Properties
        public int PopulationSize
        {
            get
            {
                return m_populationSize;
            }
            set
            {
                m_populationSize = value;
            }
        }

        public int Generations
        {
            get
            {
                return m_generationSize;
            }
            set
            {
                m_generationSize = value;
            }
        }

        public int GenomeSize
        {
            get
            {
                return m_genomeSize;
            }
            set
            {
                m_genomeSize = value;
            }
        }

        public int FitnessSize
        {
            get
            {
                return m_fitnessSize;
            }
            set
            {
                m_fitnessSize = value;
            }
        }

        public double CrossoverRate
        {
            get
            {
                return m_crossoverRate;
            }
            set
            {
                m_crossoverRate = value;
            }
        }
        public double MutationRate
        {
            get
            {
                return m_mutationRate;
            }
            set
            {
                m_mutationRate = value;
            }
        }

        public string FitnessFile
        {
            get
            {
                return m_fitnessFile;
            }
            set
            {
                m_fitnessFile = value;
            }
        }

        /// <summary>
        /// Keep previous generation's fittest individual in place of worst in current
        /// </summary>
        public bool Elitism
        {
            get
            {
                return m_elitism;
            }
            set
            {
                m_elitism = value;
            }
        }

        public bool Cancelled { get; set; }
        public bool Running { get; set; }

        public List<Individual> NonDominatedSet
        {
            get
            {
                return m_nonDominatedSet;
            }
        }

        public List<Individual> CurrentPopulation
        {
            get
            {
                return m_thisGeneration;
            }
        }

        #endregion

        //public void GetBest(out double[] values, out double[] fitness)
        //{            
        //    Individual g = m_thisGeneration[m_populationSize - 1];
        //    values = new double[g.GenCount];
        //    g.GetValues(ref values);
        //    fitness = g.Fitness;
        //}

        //public void GetWorst(out double[] values, out double[] fitness)
        //{
        //    GetNthGenome(0, out values, out fitness);
        //}

        //public void GetNthGenome(int n, out double[] values, out double[] fitness)
        //{
        //    if (n < 0 || n > m_populationSize - 1)
        //        throw new ArgumentOutOfRangeException("n too large, or too small");
        //    Individual g = ((Individual)m_thisGeneration[n]);
        //    values = new double[g.GenCount];
        //    g.GetValues(ref values);
        //    fitness = g.Fitness;
        //}
    }
}
