using System;

namespace SqlCallDemo.ComplexType;

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
