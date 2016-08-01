#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlPackageBase\SqlPackageBaseItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Diagnostics;
using System.Linq;
using CK.Core;
using CK.Setup;
using Yodii.Script;

namespace CK.SqlServer.Setup
{
    public class SqlPackageBaseItem : StObjDynamicPackageItem
    {
        /// <summary>
        /// Initializes a new <see cref="SqlPackageBaseItem"/> bound to a StObj.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Structured Object data that contains the <see cref="Object"/>.</param>
        public SqlPackageBaseItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Context = data.StObj.Context.Context;
            SqlPackageBase p = ActualObject;
            if( p.Database != null ) Location = p.Database.Name;
            ResourceLocation = (ResourceLocator)data.StObj.GetStObjProperty( "ResourceLocation" );
            // By default, a Sql package always has a an associated Model package.
            // If HasModel is not defined (ie. GetStObjProperty returned Type.Missing) or not a boolean or true, we do it.
            // Only HasModel = false will prevent us to associated a model.
            object hasModel = data.StObj.GetStObjProperty( "HasModel" );
            if( !(hasModel is bool) || (bool)hasModel ) EnsureModel();
            
            Debug.Assert( typeof( SqlPackageBaseItemDriver ).IsAssignableFrom( data.DriverType ) );
            Name = data.FullNameWithoutContext;
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlPackageBase"/>.
        /// </summary>
        public new SqlPackageBase ActualObject => (SqlPackageBase)base.ActualObject;

        /// <summary>
        /// Gets or sets a <see cref="ResourceLocation"/> that locates the resources associated 
        /// to this package.
        /// </summary>
        public ResourceLocator ResourceLocation { get; set; }

        protected override object StartDependencySort()
        {
            if( ObjectsPackage != null )
            {
                ObjectsPackage.Groups.AddRange( Groups.OfType<SqlDatabaseItem>() );
            }
            return base.StartDependencySort();
        }

        public static string ProcessY4Template( 
            IActivityMonitor monitor, 
            SetupItemDriver driver,
            ISetupItem setupItem,
            object model,
            string fileName, 
            string text )
        {
            using( monitor.OpenInfo().Send( $"Evaluating template '{fileName}'." ) )
            {
                GlobalContext c = new GlobalContext();
                if( driver != null ) c.Register( "Driver", driver );
                if( setupItem != null ) c.Register( "SetupItem", setupItem );
                if( model == null )
                {
                    var sO = setupItem as ISetupObjectItem;
                    if( sO != null ) model = sO.ActualObject;
                }
                if( model != null ) c.Register( "Model", model );
                TemplateEngine e = new TemplateEngine( c );
                var r = e.Process( text );
                if( r.ErrorMessage != null )
                {
                    using( monitor.OpenError().Send( r.ErrorMessage ) )
                    {
                        monitor.Trace().Send( text );
                    }
                    return null;
                }
                text = r.Text;
            }
            return text;
        }
    }
}
