#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlPackageBase\SqlPackageAttributeImplBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

        void IStObjStructuralConfigurator.Configure( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( !typeof( SqlPackageBase ).IsAssignableFrom( o.ObjectType.BaseType ) )
            {
                monitor.Error().Send( "{0}: Attribute {1} must be set only on class that specialize SqlPackageBase.", o.ToString(), GetType().Name );
            }
            if( Attribute.Package != null )
            {
                if( o.Container.Type == null ) o.Container.Type = Attribute.Package;
                else if( o.Container.Type != Attribute.Package )
                {
                    monitor.Error().Send( "{0}: Attribute {3} sets Package to be '{1}' but it is already '{2}'.", o.ToString(), Attribute.Package.Name, o.Container.Type, GetType().Name );
                }
            }
            if( Attribute.Database != null )
            {
                if( !typeof( SqlDatabase ).IsAssignableFrom( Attribute.Database ) )
                {
                    monitor.Error().Send( "{0}: Database type property must reference a type that specializes SqlDatabase.", o.ToString() );
                }
                else
                {
                    o.SetAmbiantPropertyConfiguration( monitor, "Database", null, Attribute.Database, StObjRequirementBehavior.WarnIfNotStObj );
                }
            }
            else o.SetAmbiantPropertyConfiguration( monitor, "Database", null, typeof(SqlDefaultDatabase), StObjRequirementBehavior.WarnIfNotStObj );
            // ResourceLocation is a StObjProperty.
            o.SetStObjPropertyValue( monitor, "ResourceLocation", new ResourceLocator( Attribute.ResourceType, Attribute.ResourcePath, o.ObjectType ) );
            if( Attribute.Schema != null )
            {
                o.SetAmbiantPropertyValue( monitor, "Schema", Attribute.Schema );
            }
            ConfigureMutableItem( monitor, o );
        }

        protected abstract void ConfigureMutableItem( IActivityMonitor monitor, IStObjMutableItem o );


        protected bool SetAutomaticSetupFullNamewithoutContext( IActivityMonitor monitor, IMutableStObjSetupData data, string loggedObjectTypeName )
        {
            if( data.IsDefaultFullNameWithoutContext )
            {
                var p = (SqlPackageBase)data.StObj.InitialObject;
                var autoName = p.Schema + '.' + data.StObj.ObjectType.Name;
                if( data.IsFullNameWithoutContextAvailable( autoName ) )
                {
                    monitor.Info().Send( "{0} '{1}' uses '{2}' as its SetupName.", loggedObjectTypeName, data.StObj.ObjectType.FullName, autoName );
                }
                else
                {
                    autoName = FindAvailableFullNameWithoutContext( data, autoName );
                    monitor.Info().Send( "{0} '{1}' has no defined SetupName. It has been automatically computed as '{2}'. You may set a [SetupName] attribute on the class to settle it.", loggedObjectTypeName, data.StObj.ObjectType.FullName, autoName );
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
