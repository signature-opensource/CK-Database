using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlTableItem : SqlPackageBaseItem
    {
        public SqlTableItem( SqlTable package )
            : base( "ObjTable", typeof( SqlTableSetupDriver ), package )
        {
            EnsureModel();
        }

        public SqlTableItem( IActivityLogger logger, IStObjSetupData data )
            : base( logger, data )
        {
            Name = data.FullNameWithoutContext;
            EnsureModel();
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlTable"/>.
        /// </summary>
        public new SqlTable Object
        { 
            get { return (SqlTable)base.Object; } 
        }

    }
}
