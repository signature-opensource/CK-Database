#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlPackageBase\SqlPackageAttributeImplBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;
using CK.Setup;
using System.Reflection;

namespace CK.SqlServer.Setup
{
    public abstract class SqlPackageAttributeImplBase : IStObjStructuralConfigurator
    {
        readonly SqlPackageAttributeBase _attr;

        protected SqlPackageAttributeImplBase( SqlPackageAttributeBase a )
        {
            _attr = a;
        }

        protected SqlPackageAttributeBase Attribute => _attr;

        void IStObjStructuralConfigurator.Configure( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( !typeof( SqlPackageBase ).IsAssignableFrom( o.ObjectType.GetTypeInfo().BaseType ) )
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
