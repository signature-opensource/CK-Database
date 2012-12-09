using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    partial class MutableItem
    {
        class StObjProperty
        {
            static readonly object _unsetValue = typeof( StObjProperty );

            /// <summary>
            /// Null if this property results from a call to IStObjMutableItem.SetStObjPropertyValue 
            /// on the StObj of a Type that does not define the property (either by a property with a [StObjPropertyAttribute] 
            /// nor with a [StObjPropertyAttribute( PropertyName == ..., PropertyType = ...)] on the class itself.
            /// </summary>
            public readonly StObjPropertyInfo InfoOnType;
            public readonly string Name;
            public Type Type { get { return InfoOnType != null ? InfoOnType.Type : typeof( object ); } }
            object _value;

            public StObjProperty( string name, object value )
            {
                Name = name;
                _value = value;
                InfoOnType = null;
            }

            public StObjProperty( StObjPropertyInfo infoOnType )
            {
                Debug.Assert( infoOnType.Type != null );
                InfoOnType = infoOnType;
                Name = infoOnType.Name;
                _value = _unsetValue;
            }

            public bool HasStructuredObjectProperty
            {
                get { return InfoOnType != null && InfoOnType.PropertyInfo != null; }
            }

            public object Value
            {
                get { return _value == _unsetValue ? Type.Missing : _value; }
                set { _value = value; }
            }

            public bool ValueHasBeenSet
            {
                get { return _value != _unsetValue; }
            }

        }

        void SetStObjProperty( string propertyName, object value )
        {
            if( _stObjProperties == null )
            {
                _stObjProperties = new List<StObjProperty>();
                _stObjProperties.Add( new StObjProperty( propertyName, value ) );
            }
            else
            {
                int idx = _stObjProperties.FindIndex( o => o.Name == propertyName );
                if( idx >= 0 ) _stObjProperties[idx].Value = value;
                else _stObjProperties.Add( new StObjProperty( propertyName, value ) );
            }
        }

        void CheckStObjProperties( IActivityLogger logger )
        {
            if( _stObjProperties == null ) return;
            foreach( StObjProperty p in _stObjProperties )
            {
                // Check the Type constraint that could potentially hurt one day.
                bool containerHasSetOrMerged = IsOwnContainer && HandleStObjPropertySource( logger, p, _dContainer, "Container", true );
                if( _generalization != null ) HandleStObjPropertySource( logger, p, _generalization, "Generalization", !containerHasSetOrMerged );
                // If the value is missing (it has never been set or has been explicitely "removed"), we have nothing to do.
                // If the type is not constrained, we have nothing to do.
                object v = p.Value;
                if( v != Type.Missing )
                {
                    bool setIt = p.HasStructuredObjectProperty;
                    if( p.Type != typeof( object ) )
                    {
                        if( v == null )
                        {
                            if( p.Type.IsValueType && !(p.Type.IsGenericType && p.Type.GetGenericTypeDefinition() == typeof( Nullable<> )) )
                            {
                                logger.Error( "StObjProperty '{0}.{1}' has been set to null but its type '{2}' is not nullable.", ToString(), p.Name, p.Type.Name );
                                setIt = false;
                            }
                        }
                        else
                        {
                            if( !p.Type.IsAssignableFrom( v.GetType() ) )
                            {
                                logger.Error( "StObjProperty '{0}.{1}' is of type '{2}', but a value of type '{3}' has been set.", ToString(), p.Name, p.Type.Name, v.GetType() );
                                setIt = false;
                            }
                        }
                    }
                    // Since CheckStObjProperties is called from PrepareDependentItem after having called PrepareDependentItem on Generalization (if any),
                    // we set the property here since we are actually called from top to bottom: the property that will win is the most specialized one if
                    // the property is virtual. If the property does not inherit (which SHOULD be the normal way of implementing a StObj property), then 
                    // we are sure that every "StObj layer" has been updated with its own value.
                    if( setIt ) p.InfoOnType.SetValue( logger, _stObj, v );
                }
            }
        }

        private bool HandleStObjPropertySource( IActivityLogger logger, StObjProperty p, MutableItem source, string sourceName, bool doSetOrMerge )
        {
            StObjProperty c = source.GetStObjProperty( p.Name );
            // Source property is defined somewhere in the source.
            if( c != null )
            {
                // If the property is explicitely defined (Info != null) and its type is not 
                // compatible with our, there is a problem.
                if( c.InfoOnType != null && !p.Type.IsAssignableFrom( c.Type ) )
                {
                    // It is a warning because if actual values work, everything is okay... but one day, it should fail.
                    logger.Warn( "StObjProperty '{0}.{1}' of type '{2}' is not compatible with the one of its {6} ('{3}.{4}' of type '{5}'). Type should be compatible since {6}'s property value will be propagated if no explicit value is set for '{7}.{1}' or if '{3}.{4}' is set with an incompatible value.",
                        ToString(), p.Name, p.Type.Name,
                        _dContainer._objectType.Type.Name, c.Name, c.Type.Name,
                        sourceName,
                        _objectType.Type.Name );
                }
                if( doSetOrMerge )
                {
                    // The source value must have been set and not explicitely "removed" with a Type.Missing value.
                    if( c.Value != Type.Missing )
                    {
                        // We "Set" the value from this source.
                        if( !p.ValueHasBeenSet ) p.Value = c.Value;
                        else if( p.Value is IMergeable )
                        {
                            if( !((IMergeable)p.Value).Merge( c.Value, new SimpleServiceContainer().Add( logger ) ) )
                            {
                                logger.Error( "Unable to merge StObjProperty '{0}.{1}' with value from {2}.", ToString(), p.Value, sourceName );
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        StObjProperty GetStObjProperty( string propertyName )
        {
            if( _stObjProperties != null )
            {
                int idx = _stObjProperties.FindIndex( p => p.Name == propertyName );
                if( idx >= 0 ) return _stObjProperties[idx];
            }
            return GetStObjPropertyFromContainerOrGeneralization( propertyName );
        }

        private StObjProperty GetStObjPropertyFromContainerOrGeneralization( string propertyName )
        {
            StObjProperty result = IsOwnContainer ? _dContainer.GetStObjProperty( propertyName ) : null;
            if( result == null && _generalization != null ) result = _generalization.GetStObjProperty( propertyName );
            return result;
        }

    }
}
