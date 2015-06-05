using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCallDemo.ComplexType
{
    public class ComplexTypeSimpleWithCtor
    {
        public ComplexTypeSimpleWithCtor( int id, string name, DateTime creationDate )
        {
            Id = id + 100000;
            Name = "From Ctor: " + name;
            CreationDate = creationDate;
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
        public DateTime CreationDate { get; private set; }
    }
}
