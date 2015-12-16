﻿using System;
using System.Collections.Generic;
using AIFH_Vol3.Core.General;
using AIFH_Vol3.Core.General.Data;
using AIFH_Vol3.Core.Learning;
using AIFH_Vol3.Core.Normalize;
using AIFH_Vol3.Core.Error;
using AIFH_Vol3_Core.Core.General.Data;

namespace AIFH_Vol3.Examples.Learning
{
    /// <summary>
    /// Base class for many of the iteration based examples.  It will loop over iterations and 
    /// display stats.
    /// </summary>
    public class SimpleLearn
    {
        /// <summary>
        /// Perform training iterations.
        /// </summary>
        /// <param name="train">The learning algorithm.</param>
        /// <param name="maxIterations">The max number of iterations.</param>
        /// <param name="targetScore">The target score.</param>
        /// <param name="shouldMinimize">True, if we should minimize.</param>
        public void PerformIterations(ILearningMethod train, int maxIterations, double targetScore, bool shouldMinimize)
        {
            int iterationNumber = 0;
            bool done = false;

            do
            {
                iterationNumber++;

                train.Iteration();

                if (train.Done)
                {
                    done = true;
                }
                else if (iterationNumber >= maxIterations)
                {
                    done = true;
                }
                else if (shouldMinimize && train.LastError < targetScore)
                {
                    done = true;
                }
                else if (!shouldMinimize && train.LastError > targetScore)
                {
                    done = true;
                }

                Console.WriteLine("Iteration #" + iterationNumber + ", Score=" + train.LastError + ", " + train.Status);
            } while (!done);

            train.FinishTraining();
            Console.WriteLine("Final score: " + train.LastError);
        }

        /// <summary>
        /// Query a regression algorithm and see how close it matches the training data.
        /// </summary>
        /// <param name="alg">The algorithm to evaluate.</param>
        /// <param name="theTrainingData">The training data.</param>
        public static void Query(IRegressionAlgorithm alg, IList<BasicData> theTrainingData)
        {
            foreach (BasicData data in theTrainingData)
            {
                double[] output = alg.ComputeRegression(data.Input);
                Console.WriteLine(VectorUtil.DoubleArrayToString(data.Input) + " -> " + VectorUtil.DoubleArrayToString(output) + ", Ideal: " + VectorUtil.DoubleArrayToString(data.Ideal));
            }
        }

        /// <summary>
        /// Query a regression algorithm using equilateral encoding.
        /// </summary>
        /// <param name="alg">The algorithm being used.</param>
        /// <param name="theTrainingData">The training data.</param>
        /// <param name="items">The category items classified.</param>
        /// <param name="high">The high value.</param>
        /// <param name="low">The low value.</param>
        public static void QueryEquilateral(
                IRegressionAlgorithm alg,
                IList<BasicData> theTrainingData,
                IDictionary<String, int> items,
                double high, double low)
        {
            // first, we need to invert the items.  Right now it maps from category to index.  We need index to category.
            IDictionary<int, String> invMap = new Dictionary<int, string>();
            foreach (string key in items.Keys)
            {
                int value = items[key];
                invMap[value] = key;
            }

            // now we can query
            Equilateral eq = new Equilateral(items.Count, high, low);
            foreach (BasicData data in theTrainingData)
            {
                double[] output = alg.ComputeRegression(data.Input);
                int idealIndex = eq.Decode(data.Ideal);
                int actualIndex = eq.Decode(output);
                Console.WriteLine(VectorUtil.DoubleArrayToString(data.Input) + " -> " + invMap[actualIndex]
                        + ", Ideal: " + invMap[idealIndex]);
            }
        }

        /// <summary>
        /// Query a regression algorithm using one-of-n encoding.
        /// </summary>
        /// <param name="alg">The algorithm being used.</param>
        /// <param name="theTrainingData">The training data.</param>
        /// <param name="items">The category items classified.</param>
        public static void QueryOneOfN(
                IRegressionAlgorithm alg,
                IList<BasicData> theTrainingData,
                IDictionary<String, int> items)
        {
            // first, we need to invert the items.  Right now it maps from category to index.  We need index to category.
            IDictionary<int, String> invMap = new Dictionary<int, string>();
            foreach (string key in items.Keys)
            {
                int value = items[key];
                invMap[value] = key;
            }

            // now we can query
            foreach (BasicData data in theTrainingData)
            {
                double[] output = alg.ComputeRegression(data.Input);
                int idealIndex = VectorUtil.MaxIndex(data.Ideal);
                int actualIndex = VectorUtil.MaxIndex(output);
                Console.WriteLine(VectorUtil.DoubleArrayToString(data.Input) + " -> "
                    + invMap[actualIndex]
                        + ", Ideal: " + invMap[idealIndex]);
            }
        }

        /// <summary>
        /// Train and stop when the validation set does not improve anymore.
        /// </summary>
        /// <param name="train">The trainer to use.</param>
        /// <param name="model">The model that is trained.</param>
        /// <param name="validationData">The validation data.</param>
        /// <param name="tolerate">Number of iterations to tolerate no improvement to the validation error.</param>
        /// <param name="errorCalc">The error calculation method.</param>
        public void PerformIterationsEarlyStop(ILearningMethod train,
                                               IRegressionAlgorithm model,
                                               IList<BasicData> validationData,
                                               int tolerate,
                                               IErrorCalculation errorCalc)
        {
            int iterationNumber = 0;
            bool done = false;
            double bestError = double.PositiveInfinity;
            int badIterations = 0;

            do
            {
                iterationNumber++;

                train.Iteration();
                double validationError = DataUtil.CalculateRegressionError(validationData, model, errorCalc);

                if (validationError < bestError)
                {
                    badIterations = 0;
                    bestError = validationError;
                }
                else
                {
                    badIterations++;
                }

                if (train.Done)
                {
                    done = true;
                }
                else if (validationError > bestError && badIterations > tolerate)
                {
                    done = true;
                }
                else if (Double.IsNaN(train.LastError))
                {
                    Console.WriteLine("Training failed.");
                    done = true;
                }

                Console.WriteLine("Iteration #" + iterationNumber
                        + ", Iteration Score=" + train.LastError
                        + ", Validation Score=" + validationError
                        + ", " + train.Status);
            } while (!done);

            train.FinishTraining();
            Console.WriteLine("Final score: " + train.LastError);
        }

        public void PerformIterationsClassifyEarlyStop(ILearningMethod train,
                                               IClassificationAlgorithm model,
                                               IList<BasicData> validationData,
                                               int tolerate)
        {
            int iterationNumber = 0;
            bool done = false;
            double bestError = double.PositiveInfinity;
            int badIterations = 0;

            do
            {
                iterationNumber++;

                train.Iteration();
                double validationError = DataUtil.CalculateClassificationError(validationData, model);

                if (validationError < bestError)
                {
                    badIterations = 0;
                    bestError = validationError;
                }
                else
                {
                    badIterations++;
                }

                if (train.Done)
                {
                    done = true;
                }
                else if (badIterations > tolerate)
                {
                    done = true;
                }
                else if (Double.IsNaN(train.LastError))
                {
                    Console.WriteLine("Training failed.");
                    done = true;
                }

                Console.WriteLine("Iteration #" + iterationNumber
                        + ", Iteration Score=" + train.LastError
                        + ", Validation Incorrect= %" + validationError * 100.0
                        + ", " + train.Status);
            } while (!done);

            train.FinishTraining();
            Console.WriteLine("Final score: " + bestError * 100.0);
        }

    }
}
