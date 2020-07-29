#region License
// Copyright (c) 2019 Jake Fowler
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without 
// restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following 
// conditions:
//
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cmdty.Core.Simulation.MultiFactor;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using NUnit.Framework;

namespace Cmdty.Core.Simulation.Test
{
    [TestFixture]
    public sealed class MultiFactorSpotPriceSimulatorTest
    {

        // Tests to do:
        // Input parameters:
        //  - Zero vol - simulation equals curve
        //  - Single non-mean reverting factor
        //  - Two non-mean reverting factors
        //  - One more factors with high mean reversion:
        //      - Standard deviation of increments far in future go to zero
        // Outputs to test:
        //  - Expectation equals curve
        //  - Variance is expected
        //  - Characteristic function
        //  - Correlation of increments

        private readonly Dictionary<Day, double> _dailyForwardCurve;
        private readonly DateTime _currentDate;
        private readonly int _seed;
        private MultiFactorSpotSimResults<Day> _singleNonMeanRevertingFactorResults;
        private MultiFactorSpotSimResults<Day> _twoNonMeanRevertingFactorsResults;

        public MultiFactorSpotPriceSimulatorTest()
        {
            _dailyForwardCurve = new Dictionary<Day, double>
            {
                {new Day(2021, 07, 28), 56.85 },
                {new Day(2021, 07, 29), 59.08 },
                {new Day(2021, 07, 30), 62.453 }
            };
            _currentDate = new DateTime(2020, 07, 27);
            _seed = 11;

            SimulateForSingleNonMeanRevertingFactor();
            SimulateForTwoNonMeanRevertingFactors();
        }

        private void SimulateForSingleNonMeanRevertingFactor()
        {
            int numSims = 100000;
            var multiFactorParameters = new MultiFactorParameters<Day>(new[]
            {
                new Factor<Day>(0.0, new DoubleTimeSeries<Day>(new Day(2021, 07, 28), new []{0.45, 0.42, 0.33})),
            }, new double[,]
                            {
                                {1.0},
                            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);

            var simulator = new MultiFactorSpotPriceSimulator<Day>(multiFactorParameters, _currentDate, _dailyForwardCurve,
                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            _singleNonMeanRevertingFactorResults = simulator.Simulate(numSims);

        }

        private void SimulateForTwoNonMeanRevertingFactors()
        {
            int numSims = 100000;
            var multiFactorParameters = new MultiFactorParameters<Day>(new[]
            {
                new Factor<Day>(0.0, new DoubleTimeSeries<Day>(new Day(2021, 07, 28), new []{0.15, 0.12, 0.13})),
                new Factor<Day>(0.0, new DoubleTimeSeries<Day>(new Day(2021, 07, 28), new []{0.11, 0.19, 0.15}))
            }, new double[,]
            {
                {1.0, 0.74},
                {0.74, 1.0},
            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);
 
            var simulator = new MultiFactorSpotPriceSimulator<Day>(multiFactorParameters, _currentDate, _dailyForwardCurve,
                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            _twoNonMeanRevertingFactorsResults = simulator.Simulate(numSims);

        }

        private MultiFactorSpotSimResults<Day> SimulateForZeroVolatility()
        {
            int numSims = 10;
            var multiFactorParameters = new MultiFactorParameters<Day>(new []
            {
                new Factor<Day>(0.0, new DoubleTimeSeries<Day>(new Day(2021, 07, 28), new []{0.0, 0.0, 0.0})), 
                new Factor<Day>(2.5, new DoubleTimeSeries<Day>(new Day(2021, 07, 28), new []{0.0, 0.0, 0.0})), 
                new Factor<Day>(16.2, new DoubleTimeSeries<Day>(new Day(2021, 07, 28), new []{0.0, 0.0, 0.0})), 
            }, new double[,]
                            {
                                {1.0, 0.6, 0.3},
                                {0.6, 1.0, 0.4},
                                {0.3, 0.4, 1.0}
                            });

            Day[] simulatedPeriods = _dailyForwardCurve.Keys.OrderBy(day => day).ToArray();
            var normalSimulator = new MersenneTwisterGenerator(_seed);

            var simulator = new MultiFactorSpotPriceSimulator<Day>(multiFactorParameters, _currentDate, _dailyForwardCurve, 
                                simulatedPeriods, TimeFunctions.Act365, normalSimulator);

            return simulator.Simulate(numSims);

        }

        [Test]
        public void Simulate_ZeroVolatility_PricesEqualForwardCurve()
        {
            MultiFactorSpotSimResults<Day> simResults = SimulateForZeroVolatility();

            IReadOnlyList<Day> simulatedPeriods = simResults.SimulatedPeriods;
            for (int periodIndex = 0; periodIndex < simulatedPeriods.Count; periodIndex++)
            {
                double forwardPrice = _dailyForwardCurve[simulatedPeriods[periodIndex]];
                ReadOnlyMemory<double> simulatedSpotPrices = simResults.SpotPricesForStepIndex(periodIndex);
                for (int simIndex = 0; simIndex < simulatedSpotPrices.Length; simIndex++)
                {
                    Assert.AreEqual(forwardPrice, simulatedSpotPrices.Span[simIndex]);
                }
            }
        }

        [Test]
        public void Simulate_SingleNonMeanRevertingFactor_ExpectedSpotPriceEqualsForwardPrice()
        {
            AssertAverageSimSpotPricesEqualsForwardPrice(_singleNonMeanRevertingFactorResults, _dailyForwardCurve);
        }

        [Test]
        public void Simulate_TwoNonMeanRevertingFactors_ExpectedSpotPriceEqualsForwardPrice()
        {
            AssertAverageSimSpotPricesEqualsForwardPrice(_twoNonMeanRevertingFactorsResults, _dailyForwardCurve);
        }

        private static void AssertAverageSimSpotPricesEqualsForwardPrice<T>(MultiFactorSpotSimResults<T> simResults, Dictionary<T, double> forwardCurve)
            where T : ITimePeriod<T>
        {

            IReadOnlyList<T> simulatedPeriods = simResults.SimulatedPeriods;
            for (int periodIndex = 0; periodIndex < simulatedPeriods.Count; periodIndex++)
            {
                double forwardPrice = forwardCurve[simulatedPeriods[periodIndex]];
                ReadOnlyMemory<double> simulatedSpotPrices = simResults.SpotPricesForStepIndex(periodIndex);

                (double averageSimSpotPrice, double sampleStanDev) = SampleStandardDeviationAndMean(simulatedSpotPrices.Span);
                double standardError = sampleStanDev / Math.Sqrt(simResults.NumSims);

                Console.WriteLine("forward price: " + forwardPrice);
                Console.WriteLine("average sim price: " + averageSimSpotPrice);
                double error = averageSimSpotPrice - forwardPrice;
                Console.WriteLine("error: " + error);
                Console.WriteLine("standard error: " + standardError);
                Console.WriteLine("num stan devs: " + error / standardError);
                Console.WriteLine();

            }
        }

        private static double Mean(ReadOnlySpan<double> span)
        {
            double sum = 0;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < span.Length; i++)
            {
                sum += span[i];
            }
            return sum / span.Length;
        }

        private static (double Mean, double SampleStanDev) SampleStandardDeviationAndMean(ReadOnlySpan<double> span)
        {
            double mean = Mean(span);
            double variance = 0.0;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < span.Length; i++)
            {
                variance += Math.Pow(span[i] - mean, 2);
            }

            double stanDev = Math.Sqrt(variance / (span.Length - 1));
            return (Mean: mean, SampleStanDev: stanDev);
        }

    }
}