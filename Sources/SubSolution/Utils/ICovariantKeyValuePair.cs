namespace SubSolution.Utils
{
    public interface ICovariantKeyValuePair<out TKey, out TValue>
    {
        public TKey Key { get; }
        public TValue Value { get; }
    }
}