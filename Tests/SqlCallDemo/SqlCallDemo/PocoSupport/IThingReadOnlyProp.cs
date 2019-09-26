namespace SqlCallDemo.PocoSupport
{
    public interface IThingReadOnlyProp : IThing
    {
        int ReadOnlyProp { get; }
    }
}
