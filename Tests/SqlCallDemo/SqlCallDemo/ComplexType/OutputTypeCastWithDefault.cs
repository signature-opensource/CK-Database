namespace SqlCallDemo.ComplexType
{
    public class OutputTypeCastWithDefault
    {
        // No cast.
        public int ParamInt { get; set; }

        // No cast
        public short ParamSmallInt { get; set; }

        // Cast (here it's an int).
        public int ParamTinyInt { get; set; }

        public string Result { get; set; }

    }
}
