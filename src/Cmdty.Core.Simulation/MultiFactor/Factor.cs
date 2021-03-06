﻿#region License
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
using System.Collections.Generic;
using System.Linq;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;

namespace Cmdty.Core.Simulation.MultiFactor
{
    public sealed class Factor<T>
        where T : ITimePeriod<T>
    {
        public double MeanReversion { get; }
        public IReadOnlyDictionary<T, double> Volatility { get; }

        public Factor(double meanReversion, IReadOnlyDictionary<T, double> volatility)
        {
            if (meanReversion < 0)
                throw new ArgumentException("Mean reversion must be non-negative.", nameof(meanReversion));
            MeanReversion = meanReversion;
            Volatility = volatility.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    public static class Factor
    {
        public static Factor<T> ForConstVol<T>(double meanReversion, T start, T end, double vol)
            where T : ITimePeriod<T>
        {
            IEnumerable<double> constantVol = start.EnumerateTo(end).Select(period => vol);
            var vols = new TimeSeries<T, double>(start, constantVol);
            return new Factor<T>(meanReversion, vols);
        }
    }
}
