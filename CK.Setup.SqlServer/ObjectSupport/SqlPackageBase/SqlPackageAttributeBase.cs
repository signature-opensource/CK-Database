using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false )]
    public class SqlPackageAttributeBase : Attribute, IStObjStructuralConfigurator
    {
        /// <summary>
        /// Gets or sets the <see cref="SqlDatabase"/> type targeted by the package. Let it to null to use the ambient one.
        /// The <see cref="SqlPackage.Database"/> property is automatically set (see remarks).
        /// </summary>
        /// <remarks>
        /// The type must be a specialization of <see cref="SqlDatabase"/>. 
        /// If it supports <see cref="IAmbientContract"/>, the property is bound to the corresponding ambient contract instance. 
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
        /// Gets or sets the Resource path to use for the <see cref="ResourceLocator"/>. 
        /// </summary>
        public string ResourcePath { get; set; }

        /// <summary>
        /// Gets or sets the Resource Type to use for the <see cref="ResourceLocator"/>.
        /// When null (the default that should rarely be changed), the decorated type is used.
        /// </summary>
        public Type ResourceType { get; set; }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
            if( !typeof( SqlPackageBase ).IsAssignableFrom( o.ObjectType.BaseType ) )
            {
                logger.Error( "{0}: Attribute {1} must be set only on class that specialize SqlPackageBase.", o.ToString(), GetType().Name );
            }
            if( Package != null )
            {
                if( o.Container.Type == null ) o.Container.Type = Package;
                else if( o.Container.Type != Package )
                {
                    logger.Error( "{0}: Attribute {3} sets Package to be '{1}' but it is already '{2}'.", o.ToString(), Package.Name, o.Container.Type, GetType().Name );
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
                    var ambient = o.AllAmbientProperties.First( a => a.Name == "Database" );
                    ambient.StObjRequirementBehavior = StObjRequirementBehavior.WarnIfNotStObj;
                    ambient.Type = Database;
                }
            }
            if( ResourceType != null || ResourcePath != null )
            {
                o.SetPropertyStructuralValue( logger, "SqlPackageAttributeBase", "ResourceLocation", new ResourceLocator( ResourceType, ResourcePath ) ); 
            }
            if( Schema != null )
            {
                o.SetPropertyStructuralValue( logger, "SqlPackageAttributeBase", "Schema", Schema );
            }
            ConfigureMutableItem( logger, o );
        }

        protected virtual void ConfigureMutableItem( IActivityLogger logger, IStObjMutableItem o )
        {
        }
    }
}
