using System;
using System.Threading.Tasks;
using CK.SqlServer;
using CK.Core;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res.Poco" ), Versions( "1.0.0" )]
    public abstract class PocoPackage : SqlPackage
    {
        IPocoFactory<IThing> _factory;

        void StObjConstruct( IPocoFactory<IThing> factory )
        {
            _factory = factory;
        }

        [SqlProcedure( "sPocoThingWrite" )]
        public abstract string Write( ISqlCallContext ctx, [ParameterSource]IThing thing );

        [SqlProcedure( "sPocoThingRead" )]
        public abstract IThing ReadFromDatabase( ISqlCallContext ctx );

        [SqlProcedure( "sPocoThingRead" )]
        public abstract Task<IThing> ReadFromDatabaseAsync( ISqlCallContext ctx );

        public IThing Read( ISqlCallContext ctx )
        {
            var r = _factory.Create();
            int i = 0;
            foreach( var p in _factory.PocoClassType.GetProperties() )
            {
                switch( p.Name )
                {
                    case nameof( IThing.Name ): p.SetValue( r, "name" ); break;
                    case nameof( IThing.UniqueId ): p.SetValue( r, Guid.NewGuid() ); break;
                    default: if( p.CanWrite ) p.SetValue( r, ++i ); break;
                }
            }
            return r;
        }
    }
}
