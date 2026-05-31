namespace DroneSimulator
{
    public sealed class AlgorithmResult
    {
        public int Score { get; }
        public int DestroyedWeeds { get; }
        public int InitialWeeds { get; }

        public AlgorithmResult(int score, int destroyedWeeds, int initialWeeds)
        {
            Score = score;
            DestroyedWeeds = destroyedWeeds;
            InitialWeeds = initialWeeds;
        }
    }
}
