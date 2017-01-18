using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using CK.Core;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res.Poco" ), Versions( "1.0.0" )]
    public abstract class PocoPackage : SqlPackage
    {
        IPocoFactory<IThing> _factory;

        void Construct( IPocoFactory<IThing> factory )
        {
            _factory = factory;
        }

        [SqlProcedure( "sPocoThingWrite" )]
        public abstract string Write( ISqlCallContext ctx, [ParameterSource]IThing thing );

        public IThing Read( ISqlCallContext ctx )
        {
            var r = _factory.Create();
            int i = 0;
            foreach( var p in _factory.PocoClassType.GetProperties() )
            {
                p.SetValue( r, p.Name == nameof(IThing.Name) ? (object)"name" : i++ );
            }
            return r;
        }
    }
}
