using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public abstract class SqlPackageAttributeImplBase : IStObjStructuralConfigurator
    {
        readonly SqlPackageAttributeBase _attr;

        protected SqlPackageAttributeImplBase( SqlPackageAttributeBase a )
        {
            _attr = a;
        }

        protected SqlPackageAttributeBase Attribute { get { return _attr; } }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
            if( !typeof( SqlPackageBase ).IsAssignableFrom( o.ObjectType.BaseType ) )
            {
                logger.Error( "{0}: Attribute {1} must be set only on class that specialize SqlPackageBase.", o.ToString(), GetType().Name );
            }
            if( Attribute.Package != null )
            {
                if( o.Container.Type == null ) o.Container.Type = Attribute.Package;
                else if( o.Container.Type != Attribute.Package )
                {
                    logger.Error( "{0}: Attribute {3} sets Package to be '{1}' but it is already '{2}'.", o.ToString(), Attribute.Package.Name, o.Container.Type, GetType().Name );
                }
            }
            if( Attribute.Database != null )
            {
                if( !typeof( SqlDatabase ).IsAssignableFrom( Attribute.Database ) )
                {
                    logger.Error( "{0}: Database type property must reference a type that specializes SqlDatabase.", o.ToString() );
                }
                else
                {
                    o.SetAmbiantPropertyConfiguration( logger, "Database", null, Attribute.Database, StObjRequirementBehavior.WarnIfNotStObj );
                }
            }
            if( Attribute.ResourceType != null || Attribute.ResourcePath != null )
            {
                // ResourceLocation is a StObjProperty.
                o.SetStObjPropertyValue( logger, "ResourceLocation", new ResourceLocator( Attribute.ResourceType, Attribute.ResourcePath ) ); 
            }
            if( Attribute.Schema != null )
            {
                o.SetAmbiantPropertyValue( logger, "Schema", Attribute.Schema );
            }
            ConfigureMutableItem( logger, o );
        }

        protected virtual void ConfigureMutableItem( IActivityLogger logger, IStObjMutableItem o )
        {
        }

        protected bool SetAutomaticSetupFullNamewithoutContext( IActivityLogger logger, IMutableStObjSetupData data, string loggedObjectTypeName )
        {
            if( data.IsDefaultFullNameWithoutContext )
            {
                var p = (SqlPackageBase)data.StObj.Object;
                var autoName = p.Schema + '.' + data.StObj.ObjectType.Name;
                if( data.IsFullNameWithoutContextAvailable( autoName ) )
                {
                    logger.Info( "{0} '{1}' uses '{2}' as its SetupName.", loggedObjectTypeName, data.StObj.ObjectType.FullName, autoName );
                }
                else
                {
                    autoName = FindAvailableFullNameWithoutContext( data, autoName );
                    logger.Info( "{0} '{1}' has no defined SetupName. It has been automatically computed as '{2}'. You may set a [SetupName] attribute on the class to settle it.", loggedObjectTypeName, data.StObj.ObjectType.FullName, autoName );
                }
                data.FullNameWithoutContext = autoName;
                return true;
            }
            return false;
        }

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
