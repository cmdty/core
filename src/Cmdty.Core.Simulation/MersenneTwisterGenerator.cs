#region License
// Copyright (c) 2020 Jake Fowler
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
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;

namespace Cmdty.Core.Simulation
{
    public sealed class MersenneTwisterGenerator : IStandardNormalGeneratorWithSeed
    {
        // TODO make field RandomSource type and inject in MersenneTwister? Probably not worth it for now.
        private MersenneTwister _randomSource;
        private int _seed;
        private readonly bool? _threadSafe;
        private double[] _antitheticBuffer;
        private bool _returnFromAntitheticBuffer;
        public bool Antithetic { get; }

        public bool IsRandom => true;

        public MersenneTwisterGenerator(int seed, bool threadSafe, bool antithetic)
        {
            _randomSource = new MersenneTwister(seed, threadSafe);
            _seed = seed;
            _threadSafe = threadSafe;
            Antithetic = antithetic;
        }

        public MersenneTwisterGenerator(bool threadSafe, bool antithetic)
        {
            _randomSource = new MersenneTwister(threadSafe);
            _threadSafe = threadSafe;
            Antithetic = antithetic;
        }

        public MersenneTwisterGenerator(int seed, bool antithetic)
        {
            _randomSource = new MersenneTwister(seed);
            Antithetic = antithetic;
        }

        public MersenneTwisterGenerator(bool antithetic)
        {
            _randomSource = new MersenneTwister(RandomSeed.Robust());
            Antithetic = antithetic;
        }

        public void Generate(double[] randomNormals)
        {
            if (Antithetic && _returnFromAntitheticBuffer)
            {
                if (randomNormals.Length != _antitheticBuffer.Length)
                    throw new InvalidOperationException($"Instance is constructed to generate antithetically, " +
                                                        $"so generate must be called with randomNormals of the same size every time.");
                Array.Copy(_antitheticBuffer, randomNormals, randomNormals.Length);
                _returnFromAntitheticBuffer = !_returnFromAntitheticBuffer;
            }
            else
            {
                Normal.Samples(_randomSource, randomNormals, 0, 1);
                if (Antithetic)
                {
                    if (_antitheticBuffer == null)
                        _antitheticBuffer = new double[randomNormals.Length];
                    for (int i = 0; i < randomNormals.Length; i++)
                        _antitheticBuffer[i] = - randomNormals[i];
                    _returnFromAntitheticBuffer = !_returnFromAntitheticBuffer;
                }
            }
        }

        // TODO Generate method which accepts Span<double> as parameter (requires update to Math.NET library)

        public void Reset()
        {
            _randomSource = _threadSafe.HasValue ? new MersenneTwister(_seed, _threadSafe.Value) : new MersenneTwister(_seed);
        }

        public bool MatchesDimensions(int numDimensions)
        {
            return true;
        }

        public void ResetSeed(int seed)
        {
            _randomSource = _threadSafe.HasValue ? new MersenneTwister(seed, _threadSafe.Value) : new MersenneTwister(seed);
            _seed = seed;
        }

        public void ResetRandomSeed()
        {
            _randomSource = new MersenneTwister(RandomSeed.Robust());
        }

    }
}
