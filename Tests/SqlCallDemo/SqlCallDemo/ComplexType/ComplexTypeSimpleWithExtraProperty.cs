using System;

namespace SqlCallDemo.ComplexType
{

    public class ComplexTypeSimpleWithExtraProperty
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreationDate { get; set; }
        public string ExtraProperty { get; set; }
    }
}
