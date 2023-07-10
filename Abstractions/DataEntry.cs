namespace Abstractions
{
    public class DataEntry
    {
        public enum EntryNames
        {
            X = 1,
            N = 2,
            V = 3,
            Y = 4,
            b = 5,
            r = 6,
            Success = 7,
            ExecutionTime = 8
        }

        public EntryNames Name { get; set; }
        public int Value { get; set; }
    }
}