//This is DEMO for Divisive Data Resorting algorithm, introduced by Andrew Polar
//and Mike Poluektov.

//It generates input/output of stochastic system, builds ensemble of models
//and compares expectation, standard deviation and distribution of modelled
//outputs to so-called exact values, i.e. values generated by Monte-Carlo.

using System;
using System.Collections.Generic;
using System.IO;
using DDR;

namespace TKAR
{
    class Program
    {
        static void GetExpectationAndVariance(double[] y, out double expectation, out double variance, out double std)
        {
            expectation = 0.0;
            foreach (double d in y)
            {
                expectation += d;
            }
            expectation /= (double)(y.Length);

            variance = 0.0;
            foreach (double d in y)
            {
                variance += (d - expectation) * (d - expectation);
            }
            std = Math.Sqrt(variance / (double)(y.Length));
            variance /= (double)(y.Length - 1);
        }

        static void Main(string[] args)
        {
            DataHolder dh = new DataHolder();
            dh.BuildFormulaData(0.8, 10000);
            //dh.SaveData(@"..\..\..\InputOutput.csv");

            Resorter resorter = new Resorter();
            resorter.ReadData(dh);
            Console.WriteLine("Data is read now sorting ...\n");

            resorter.Resort(1);
            resorter.Resort(2);
            resorter.Resort(4);
            resorter.Resort(8);
            Console.WriteLine("Resorting is finished now building the ensemble ...\n");

            Ensemble ensemble = new Ensemble(Resorter._inputs, Resorter._target);
            ensemble.BuildModels(8);

            int NTests = 100;
            int KSTRejected = 0;
            List<double[]> validationSample = new List<double[]>();
            List<double> ensembleE = new List<double>();
            List<double> ensembleS = new List<double>();
            List<double> monteE = new List<double>();
            List<double> monteS = new List<double>();

            for (int n = 0; n < NTests; ++n)
            {
                double[] randomInput = dh.GetRandomInput();
                validationSample.Add(randomInput);
                double[] MonteCarloOutput = dh.GetStatData(randomInput, 1024);
                double[] EnsembleOutput = ensemble.GetOutput(randomInput);

                double ee;
                double es;
                double ev;
                GetExpectationAndVariance(EnsembleOutput, out ee, out ev, out es);
                ensembleE.Add(ee);
                ensembleS.Add(es);

                double me;
                double ms;
                double mv;
                GetExpectationAndVariance(MonteCarloOutput, out me, out mv, out ms);
                monteE.Add(me);
                monteS.Add(ms);

                Array.Sort(MonteCarloOutput);
                Array.Sort(EnsembleOutput);

                if (true == Static.KSTRejected005(MonteCarloOutput, EnsembleOutput))
                {
                    ++KSTRejected;
                }
            }

            //using (StreamWriter sw = new StreamWriter(@"..\..\..\InputOnly.csv"))
            //{
            //    for (int i = 0; i < validationSample.Count; ++i)
            //    {
            //        string line = "";
            //        foreach (double d in validationSample[i])
            //        {
            //            line += String.Format("{0:0.0000}, ", d);
            //        }
            //        line = line.Substring(0, line.Length - 2);
            //        sw.WriteLine(line);
            //    }
            //    sw.Flush();
            //    sw.Close();
            //}

            Console.WriteLine("Kolmogorov-Smirnov rejected data sets {0} out of {1}", KSTRejected, NTests);
            Console.WriteLine("Correlation for expectation {0:0.0000}", DDR.Static.PearsonCorrelation(ensembleE.ToArray(), monteE.ToArray()));
            Console.WriteLine("Correlation for std {0:0.0000}", DDR.Static.PearsonCorrelation(ensembleS.ToArray(), monteS.ToArray()));
        }
    }
}
