using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false )]
    public class SqlPackageAttribute : Attribute, ISetupNameAttribute, IStObjStructuralConfigurator, IStObjSetupConfigurator
    {
        /// <summary>
        /// Gets or sets the <see cref="SqlDatabase"/> type targeted by the package.
        /// The <see cref="SqlPackageType.Database"/> property is automatically set (see remarks).
        /// </summary>
        /// <remarks>
        /// The type must be a specialization of <see cref="SqlDatabase"/>. 
        /// If it supports <see cref="IAmbiantContract"/>, the property is bound to the corresponding ambiant contract instance. 
        /// </remarks>
        public Type Database { get; set; }

        /// <summary>
        /// Gets or sets the package to which this package belongs.
        /// </summary>
        public Type Package { get; set; }

        /// <summary>
        /// Gets or sets the sql schema to use.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the full name (for the setup process).
        /// Defaults to the <see cref="Type.Name"/> of the package.
        /// </summary>
        public string FullName { get; set; }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
            if( !typeof( SqlPackageType ).IsAssignableFrom( o.ObjectType.BaseType ) )
            {
                logger.Error( "{0}: Attribute SqlPackage must be set only on class that specialize SqlPackageType.", o.ToString() );
            }
            if( Package != null )
            {
                if( o.Container.Type == null ) o.Container.Type = Package;
                else if( o.Container.Type != Package )
                {
                    logger.Error( "{0}: SqlPackage attribute sets Package to be '{1}' but it is already '{2}'.", o.ToString(), Package.Name, o.Container.Type );
                }
            }
            if( Database != null )
            {
                if( !typeof( SqlDatabase ).IsAssignableFrom( Database ) )
                {
                    logger.Error( "{0}: Database type property must reference a type that specializes SqlDatabase.", o.ToString() );
                }
                else
                {
                    var ambiant = o.AllAmbiantProperties.First( a => a.Name == "Database" );
                    ambiant.StObjRequirementBehavior = StObjRequirementBehavior.WarnIfNotStObj;
                    ambiant.Type = Database;
                }
            }
            if( Schema != null ) o.SetPropertyStructuralValue( logger, "SqlTableAttribute", "Schema", Schema );
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            if( data.IsDefaultFullName )
            {
                logger.Info( "SqlPackage class '{0}' uses its own name as its setup FullName.", data.StObj.ObjectType.Name );
                data.FullNameWithoutContext = data.StObj.ObjectType.Name;
            }
        }
    }
}
