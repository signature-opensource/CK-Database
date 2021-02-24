namespace SqlCallDemo.ComplexType
{
    public class OutputTypeCastWithDefault
    {
        // No cast.
        public int ParamInt { get; set; }

        // No cast
        public short ParamSmallInt { get; set; }

        /// <summary>
        /// Exposing an "int" here is an error.
        /// To support this, we must handle chain of casts: have a look at
        /// ISqlServerExtensions extensions IsTypeCompatible and SafeAssignableCastChain
        /// in CK.SqlServer.Setup.Runtime. This is a TODO.
        /// </summary>
        public byte ParamTinyInt { get; set; }

        public string Result { get; set; }
    }
}
