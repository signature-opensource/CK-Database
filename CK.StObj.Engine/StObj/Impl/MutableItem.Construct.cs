using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;
using System.IO;
using System.Reflection;

namespace CK.Setup
{
    partial class MutableItem
    {
        void AddPreConstructProperty( PropertyInfo p, object o, BuildValueCollector valueCollector )
        {
            if( _preConstruct == null ) _preConstruct = new List<PropertySetter>();
            _preConstruct.Add( new PropertySetter( p, o, valueCollector ) );
        }

        void AddPostBuildProperty( PropertyInfo p, object o, BuildValueCollector valueCollector )
        {
            Debug.Assert( Specialization == null, "Called on leaf only." );
            if( _leafData.PostBuildProperties == null ) _leafData.PostBuildProperties = new List<PropertySetter>();
            _leafData.PostBuildProperties.Add( new PropertySetter( p, o, valueCollector ) );
        }

        internal void CallConstruct( IActivityMonitor monitor, BuildValueCollector valueCollector, IStObjValueResolver valueResolver )
        {
            Debug.Assert( _constructParameterEx != null, "Always allocated." );
            if( _preConstruct != null )
            {
                foreach( var p in _preConstruct )
                {
                    SetPropertyValue( monitor, p );
                }
            }

            if( AmbientTypeInfo.StObjConstruct == null ) return;

            object[] parameters = new object[_constructParameterEx.Count];
            int i = 0;
            foreach( MutableParameter t in _constructParameterEx )
            {
                // We inject our "setup monitor" only if it is exactly the formal parameter: ... , IActivityMonitor monitor, ...
                // This enforces code homogeneity and let room for any other IActivityMonitor injection.
                if( t.IsSetupLogger )
                {
                    t.SetParameterValue( monitor );
                    t.BuilderValueIndex = Int32.MaxValue;
                }
                else
                {
                    MutableItem resolved = null;
                    if( t.Value == System.Type.Missing )
                    {
                        // Parameter reference have already been resolved as dependencies for graph construction since 
                        // no Value has been explicitely set for the parameter.
                        resolved = t.CachedResolvedStObj;
                        if( resolved != null )
                        {
                            Debug.Assert( resolved.InitialObject != System.Type.Missing );
                            t.SetParameterValue( resolved.InitialObject );
                        }
                    }
                    if( valueResolver != null ) valueResolver.ResolveParameterValue( monitor, t );
                    if( t.Value == System.Type.Missing && !t.IsRealParameterOptional )
                    {
                        if( !t.IsOptional )
                        {
                            // By throwing an exception here, we stop the process and avoid the construction 
                            // of an invalid object graph...
                            // This behavior (FailFastOnFailureToResolve) may be an option once. For the moment: log the error.
                            monitor.Fatal().Send( $"{t}: Unable to resolve non optional. Attempting to use a default value to continue the setup process in order to detect other errors." );
                        }
                        t.SetParameterValue( t.Type.GetTypeInfo().IsValueType ? Activator.CreateInstance( t.Type ) : null );
                    }
                    if( resolved != null && t.Value == resolved.InitialObject )
                    {
                        t.BuilderValueIndex = -(resolved.IndexOrdered + 1);
                    }
                    else
                    {
                        t.BuilderValueIndex = valueCollector.RegisterValue( t.Value );
                    }
                }
                parameters[i++] = t.Value;
            }
            AmbientTypeInfo.StObjConstruct.Invoke( _leafData.StructuredObject, parameters );
        }

        internal void SetPostBuildProperties( IActivityMonitor monitor, StObjCollectorResult collector, StObjCollectorContextualResult cachedContext )
        {
            Debug.Assert( Specialization == null, "Called on leaves only." );
            if( _leafData.PostBuildProperties != null )
            {
                foreach( var p in _leafData.PostBuildProperties )
                {
                    SetPropertyValue( monitor, p );
                }
            }
        }

        struct PropertySetter
        {
            public readonly PropertyInfo Property;
            public readonly object Value;
            public readonly int IndexValue;

            public PropertySetter( PropertyInfo p, object o, BuildValueCollector valueCollector )
            {
                Property = p;
                Value = o;
                if( o is MutableItem ) IndexValue = -1;
                else
                {
                    IndexValue = valueCollector.RegisterValue( o );
                }
            }
        }

        void SetPropertyValue( IActivityMonitor monitor, PropertySetter p )
        {
            object o = p.Value;
            MutableItem m = o as MutableItem;
            if( m != null ) o = m.InitialObject;
            try
            {
                p.Property.SetValue( _leafData.StructuredObject, o, null );
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex, "While setting '{1}.{0}'.", p.Property.Name, p.Property.DeclaringType.FullName );
            }
        }

        internal void WritePreConstructProperties( BinaryWriter w )
        {
            WritePropertySetterList( w, _preConstruct );
        }

        internal void WritePostBuildProperties( BinaryWriter w )
        {
            Debug.Assert( Specialization == null, "Called on leaves only." ); 
            WritePropertySetterList( w, _leafData.PostBuildProperties );
        }

        private static void WritePropertySetterList( BinaryWriter w, List<PropertySetter> setters )
        {
            int nb = setters == null ? 0 : setters.Count;
            w.Write( nb );
            if( nb > 0 )
            {
                foreach( var p in setters )
                {
                    w.Write( p.Property.DeclaringType.AssemblyQualifiedName );
                    w.Write( p.Property.Name );
                    if( p.IndexValue == -1 )
                    {
                        w.Write( -(((MutableItem)p.Value).IndexOrdered+1) );
                    }
                    else w.Write( p.IndexValue );
                }
            }
        }

    }
}
