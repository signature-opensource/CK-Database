namespace SqlCallDemo.PocoSupport
{
    public interface IThingWithAgeAndHeight : IThing
    {
        int Age { get; set; }

        int Height { get; set; }
    }
}
