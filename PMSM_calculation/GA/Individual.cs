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
//  $Id: Genome.cs,v 1.1 2003/08/19 20:59:05 barry Exp $


using System;
using System.Collections;
using btl.generic;
using System.Collections.Generic;

namespace btl.generic
{
    /// <summary>
    /// Summary description for Genome.
    /// </summary>
    public class Individual
    {
        public Individual(int gen_count, int fitness_count, bool createGenes = true)
        {
            m_genes = new double[gen_count];
            m_fitness = new double[fitness_count];
            if (createGenes)
                CreateGenes();
        }

        public Individual(double[] genes, double[] f)
        {
            m_genes = new double[genes.Length];
            for (int i = 0; i < m_genes.Length; i++)
                m_genes[i] = genes[i];

            m_fitness = new double[f.Length];
            for (int i = 0; i < m_fitness.Length; i++)
                m_fitness[i] = f[i];
        }

        /// <summary>
        /// Cloning
        /// </summary>
        /// <param name="original"></param>
        public Individual(Individual original)
            : this(original.Genes, original.Fitness)
        {
            
        }


        private void CreateGenes()
        {
            // DateTime d = DateTime.UtcNow;
            for (int i = 0; i < m_genes.Length; i++)
                m_genes[i] = m_random.NextDouble();
        }

        public void Crossover(ref Individual parent2, out Individual child1, out Individual child2)
        {
            int pos = (int)(m_random.NextDouble() * m_genes.Length);
            child1 = new Individual(m_genes.Length, m_fitness.Length, false);
            child2 = new Individual(m_genes.Length, m_fitness.Length, false);
            for (int i = 0; i < m_genes.Length; i++)
            {
                if (i < pos)
                {
                    child1.m_genes[i] = m_genes[i];
                    child2.m_genes[i] = parent2.m_genes[i];
                }
                else
                {
                    child1.m_genes[i] = parent2.m_genes[i];
                    child2.m_genes[i] = m_genes[i];
                }
            }
        }


        public void Mutate()
        {
            for (int pos = 0; pos < m_genes.Length; pos++)
            {
                if (m_random.NextDouble() < m_mutationRate)
                    m_genes[pos] = (m_genes[pos] + m_random.NextDouble()) / 2.0;
            }
        }

        public void Output()
        {
            for (int i = 0; i < m_genes.Length; i++)
            {
                System.Console.WriteLine("{0:F4}", m_genes[i]);
            }
            System.Console.Write("\n");
        }

        public void GetValues(ref double[] values)
        {
            for (int i = 0; i < m_genes.Length; i++)
                values[i] = m_genes[i];
        }


        public double[] m_genes;
        private double[] m_fitness;
        static Random m_random = new Random();

        private static double m_mutationRate;

        public double[] Genes
        {
            get
            {
                return m_genes;
            }
        }

        public double[] Fitness
        {
            get
            {
                return m_fitness;
            }
            set
            {
                m_fitness = value;
            }
        }

        public static double MutationRate
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

        public int GenCount
        {
            get
            {
                return m_genes.Length;
            }
        }

        public int FitnessCount
        {
            get
            {
                return m_fitness.Length;
            }
        }

        public int Compare(Individual y2)
        {
            Individual y1 = this;
            var Fy1 = y1.Fitness;
            var Fy2 = y2.Fitness;

            if (Fy1 == null && Fy2 == null)
                return 0;
            if (Fy1 == null)
                return -1;
            if (Fy2 == null)
                return 1;

            if (Fy1.Length != Fy2.Length)
                throw new InvalidOperationException("Fitness length not matched");

            int n = Fy1.Length;

            int Y1MoreOrEqual = 0;
            int Y2MoreOrEqual = 0;
            int Y1More = 0;
            int Y2More = 0;
            for (int i = 0; i < n; i++)
            {
                if (Fy1[i] >= Fy2[i])
                {
                    Y1MoreOrEqual++;
                    if (Fy1[i] > Fy2[i])
                        Y1More++;
                }

                if (Fy1[i] <= Fy2[i])
                {
                    Y2MoreOrEqual++;
                    if (Fy1[i] < Fy2[i])
                        Y2More++;
                }
            }

            // Y1 dominate
            if (Y1MoreOrEqual == n && Y1More > 0)
                return 1;

            // Y2 dominate
            if (Y2MoreOrEqual == n && Y2More > 0)
                return -1;

            // indifferent
            return 0;
        }
    }

    /// <summary>
    /// Compares genomes by fitness
    /// </summary>
    //public sealed class IndividualComparer : IComparer<Individual>
    //   {
    //       public IndividualComparer()
    //       {
    //       }

    //       public int Compare(Individual y1, Individual y2)
    //       {
    //           var Fy1 = y1.Fitness;
    //           var Fy2 = y2.Fitness;
    //           if (Fy1.Length != Fy2.Length)
    //               throw new InvalidOperationException("Fitness length not matched");
    //           if (Fy1 == null && Fy2 == null)
    //               return 0;
    //           if (Fy1 == null)
    //               return -1;
    //           if (Fy2 == null)
    //               return 1;

    //           int n = Fy1.Length;

    //           int Y1MoreOrEqual = 0;
    //           int Y2MoreOrEqual = 0;
    //           int Y1More = 0;
    //           int Y2More = 0;
    //           for(int i = 0; i < n; i++)
    //           {
    //               if (Fy1[i] >= Fy2[i])
    //               {
    //                   Y1MoreOrEqual++;
    //                   if (Fy1[i] > Fy2[i])
    //                       Y1More++;
    //               }

    //               if (Fy1[i] <= Fy2[i])
    //               {
    //                   Y2MoreOrEqual++;
    //                   if (Fy1[i] < Fy2[i])
    //                       Y2More++;
    //               }
    //           }

    //           // Y1 dominate
    //           if (Y1MoreOrEqual == n && Y1More > 0)
    //               return 1;

    //           // Y2 dominate
    //           if (Y2MoreOrEqual == n && Y2More > 0)
    //               return -1;

    //           // indifferent
    //           return 0;
    //       }        
    //   }
}
