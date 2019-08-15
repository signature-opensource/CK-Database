using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implementation of <see cref="SqlPackageAttributeBase"/>.
    /// This is the base class for <see cref="SqlPackageAttributeImpl"/> and <see cref="SqlTableAttributeImpl"/>.
    /// </summary>
    public abstract class SqlPackageAttributeImplBase : IStObjStructuralConfigurator
    {
        readonly SqlPackageAttributeBase _attr;

        /// <summary>
        /// Initializes a new <see cref="SqlPackageAttributeImplBase"/>
        /// </summary>
        /// <param name="a">The attribute.</param>
        protected SqlPackageAttributeImplBase( SqlPackageAttributeBase a )
        {
            _attr = a;
        }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        protected SqlPackageAttributeBase Attribute => _attr;
       
        void IStObjStructuralConfigurator.Configure( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( !typeof( SqlPackage ).IsAssignableFrom( o.ObjectType.BaseType ) )
            {
                monitor.Error( $"{o.ToString()}: Attribute {GetType().Name} must be set only on class that specialize SqlPackageBase." );
            }
            if( Attribute.Package != null )
            {
                if( o.Container.Type == null ) o.Container.Type = Attribute.Package;
                else if( o.Container.Type != Attribute.Package )
                {
                    monitor.Error( $"{o.ToString()}: Attribute {GetType().Name} sets Package to be '{Attribute.Package.Name}' but it is already '{o.Container.Type}'." );
                }
            }
            if( Attribute.Database != null )
            {
                if( !typeof( SqlDatabase ).IsAssignableFrom( Attribute.Database ) )
                {
                    monitor.Error( $"{o.ToString()}: Database type property must reference a type that specializes SqlDatabase." );
                }
                else
                {
                    o.SetAmbiantPropertyConfiguration( monitor, "Database", Attribute.Database, StObjRequirementBehavior.WarnIfNotStObj );
                }
            }
            else o.SetAmbiantPropertyConfiguration( monitor, "Database", typeof(SqlDefaultDatabase), StObjRequirementBehavior.WarnIfNotStObj );
            // ResourceLocation is a StObjProperty.
            o.SetStObjPropertyValue( monitor, "ResourceLocation", new ResourceLocator( Attribute.ResourceType, Attribute.ResourcePath, o.ObjectType ) );
            if( Attribute.Schema != null )
            {
                o.SetAmbiantPropertyValue( monitor, "Schema", Attribute.Schema );
            }
            ConfigureMutableItem( monitor, o );
        }

        /// <summary>
        /// When implemented this method must participate to <see cref="IStObjMutableItem"/> configuration.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="o">Tme mutable item to configure.</param>
        protected abstract void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o );

        /// <summary>
        /// Helper that handle a better name for <see cref="IMutableStObjSetupData.FullNameWithoutContext"/> than
        /// the default one (see <see cref="IStObjSetupDataBase.IsDefaultFullNameWithoutContext"/>).
        /// Name is changed to be based on the schema and the .Net type with an automatic use
        /// of namespace to disambiguate names in inheritance chains.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="data">The data for which full name should be updated.</param>
        /// <param name="loggedObjectTypeName">Name to use while logging.</param>
        /// <returns>True if FullNameWithoutContext has been changed, false otherwise.</returns>
        protected bool SetAutomaticSetupFullNameWithoutContext( IActivityMonitor monitor, IMutableStObjSetupData data, string loggedObjectTypeName )
        {
            if( data.IsDefaultFullNameWithoutContext )
            {
                var p = (SqlPackage)data.StObj.InitialObject;
                var autoName = p.Schema + '.' + data.StObj.ObjectType.Name;
                if( data.IsFullNameWithoutContextAvailable( autoName ) )
                {
                    monitor.Info( $"{loggedObjectTypeName} '{data.StObj.ObjectType.FullName}' uses '{autoName}' as its SetupName." );
                }
                else
                {
                    autoName = FindAvailableFullNameWithoutContext( data, autoName );
                    monitor.Info( $"{loggedObjectTypeName} '{data.StObj.ObjectType.FullName}' has no defined SetupName. It has been automatically computed as '{autoName}'. You may set a [SetupName] attribute on the class to settle it." );
                }
                data.FullNameWithoutContext = autoName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method to resolve the automatic handling of <see cref="IMutableStObjSetupData.FullNameWithoutContext"/>
        /// by <see cref="SetAutomaticSetupFullNameWithoutContext"/>.
        /// </summary>
        /// <param name="data">The StObj data.</param>
        /// <param name="shortestName">The shortest possible name.</param>
        /// <returns>The non-clashing name to use.</returns>
        protected string FindAvailableFullNameWithoutContext( IMutableStObjSetupData data, string shortestName )
        {
            string proposal;
            string className = data.StObj.ObjectType.Name;

            bool shortestNameHasClassName = shortestName.Contains( className );

            if( shortestNameHasClassName )
            {
                className = String.Empty;
            }
            else
            {
                className = '-' + className;
                if( data.IsFullNameWithoutContextAvailable( (proposal = shortestName + className) ) ) return proposal;
            }
            string[] ns = data.StObj.ObjectType.Namespace.Split( '.' );
            int i = ns.Length - 1;
            while( i >= 0 )
            {
                className = '-' + ns[i] + className;
                if( data.IsFullNameWithoutContextAvailable( (proposal = shortestName + className) ) ) return proposal;
            }
            return data.StObj.ObjectType.FullName;
        }


    }
}
