namespace NCDK
{
    public sealed class IntractableException
        : CDKException
    {
        public IntractableException()
        {
        }

        public IntractableException(string message)
            : base(message)
        {
        }

        public IntractableException(long t)
            : this("Operation", t)
        {
        }

        public IntractableException(string desc, long t)
            : this(desc + " did not finish after " + t + " ms.")
        {
        }

        public IntractableException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
