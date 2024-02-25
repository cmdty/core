namespace Cmdty.Core.Simulation
{
    public interface IStandardNormalGenerator
    {
        void Generate(double[] randomNormals);
        void Reset();
        bool MatchesDimensions(int numDimensions); // Necessary for stuff like Sobol which has to be set up with specific number of dimensions
        bool Antithetic { get; }
        /// <summary>
        /// True if pseudo-random, false if quasi-random
        /// </summary>
        bool IsRandom { get; }
    }
}