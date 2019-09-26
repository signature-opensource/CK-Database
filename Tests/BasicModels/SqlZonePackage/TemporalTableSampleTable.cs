using CK.Core;

namespace SqlZonePackage.Zone
{
    [SqlTable( "a Temporal Table Sample", Package = typeof( Package ) )]
    [Versions( "1.0.0" )]
    public class TemporalTableSampleTable : SqlTable
    {
    }
}
