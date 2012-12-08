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

        /// <summary>
        /// Used to expose only the <see cref="Count"/> first items of a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        class ListAmbientProperty : IReadOnlyList<MutableAmbientProperty>
        {
            readonly MutableItem _item;

            public ListAmbientProperty( MutableItem item )
            {
                _item = item;
            }

            public int IndexOf( object item )
            {
                int idx = -1;
                MutableAmbientProperty a = item as MutableAmbientProperty;
                if( a != null 
                    && a.Owner == _item._leafSpecialization
                    && a.AmbientPropertyInfo.Index < _item._objectType.AmbientProperties.Count )
                {
                    idx = a.AmbientPropertyInfo.Index;
                }
                return idx;
            }

            public MutableAmbientProperty this[int index]
            {
                get 
                { 
                    if( index >= _item._objectType.AmbientProperties.Count ) throw new IndexOutOfRangeException(); 
                    return _item._specialization._allAmbientProperties[index]; 
                }
            }

            public bool Contains( object item )
            {
                return IndexOf( item ) >= 0;
            }

            public int Count
            {
                get { return _item._objectType.AmbientProperties.Count; }
            }

            public IEnumerator<MutableAmbientProperty> GetEnumerator()
            {
                return _item._specialization._allAmbientProperties.Take( _item._objectType.AmbientProperties.Count ).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal void ResolvePropertiesOnSpecialization( IActivityLogger logger, StObjCollectorResult result, IStObjValueResolver valueResolver )
        {
            Debug.Assert( _specialization == null && _leafSpecialization == this, "We are on the  ultimate (leaf) Specialization." );
            if( _directPropertiesToSet != null )
            {
                foreach( var k in _directPropertiesToSet )
                {
                    try
                    {
                        if( k.Value != Type.Missing ) k.Key.SetValue( _stObj, k.Value, null );
                    }
                    catch( Exception ex )
                    {
                        logger.Error( ex, "While setting direct property '{1}.{0}'.", k.Key.Name, k.Key.DeclaringType.FullName );
                    }
                }
            }
            foreach( var a in _allAmbientProperties )
            {
                EnsureCachedAmbientProperty( logger, result, valueResolver, a.Type, a.Name, a );
                if( a.Value == Type.Missing )
                {
                    if( valueResolver != null ) valueResolver.ResolveExternalPropertyValue( logger, a );
                }
                object value = a.Value;
                if( value == Type.Missing )
                {
                    if( !a.IsOptional ) logger.Error( "{0}: Unable to resolve non optional.", a.ToString() );
                }
                else
                {
                    // Actual ambient property setting.
                    try
                    {
                        MutableItem resolved = value as MutableItem;
                        // If the property value is a StObj, extracts its actual value.
                        if( resolved != null )
                        {
                            a.AmbientPropertyInfo.PropertyInfo.SetValue( _stObj, resolved.Object, null );

                            MutableItem source = this;
                            AmbientPropertyInfo sourceProp = a.AmbientPropertyInfo;
                            Debug.Assert( sourceProp.Index < source._objectType.AmbientProperties.Count, "This is the way to test if the property is defined at the source level or not." );

                            // Walks up the chain to locate the most abstract compatible slice.
                            {
                                MutableItem genResolved = resolved.Generalization;
                                while( genResolved != null && sourceProp.PropertyType.IsAssignableFrom( genResolved.ObjectType ) )
                                {
                                    resolved = genResolved;
                                    genResolved = genResolved.Generalization;
                                }
                            }
                            if( resolved._trackedAmbientProperties != null ) resolved._trackedAmbientProperties.Add( new TrackedAmbientPropertyInfo( source, sourceProp ) );

                            // Walks up the source chain and adjusts the resolved target accordingly.
                            while( (source = source.Generalization) != null && resolved._needsTrackedAmbientProperties )
                            {
                                bool sourcePropChanged = false;
                                // If source does not define anymore sourceProp. Does it define the property with another type?
                                while( source != null && sourceProp.Index >= source._objectType.AmbientProperties.Count )
                                {
                                    sourcePropChanged = true;
                                    if( (sourceProp = sourceProp.Generalization) == null )
                                    {
                                        // No property defined anymore at this level: we do not have anything more to do.
                                        source = null;
                                    }
                                }
                                if( source == null ) break;
                                Debug.Assert( sourceProp != null );
                                // Walks up the chain to locate the most abstract compatible slice.
                                if( sourcePropChanged )
                                {
                                    MutableItem genResolved = resolved.Generalization;
                                    while( genResolved != null && sourceProp.PropertyType.IsAssignableFrom( genResolved.ObjectType ) )
                                    {
                                        resolved = genResolved;
                                        genResolved = genResolved.Generalization;
                                    }
                                }
                                if( resolved._trackedAmbientProperties != null ) resolved._trackedAmbientProperties.Add( new TrackedAmbientPropertyInfo( source, sourceProp ) );
                            }
                        }
                        else a.AmbientPropertyInfo.PropertyInfo.SetValue( _stObj, value, null );
                    }
                    catch( Exception ex )
                    {
                        logger.Error( ex, "While setting ambient property '{1}.{0}'.", a.Name, a.AmbientPropertyInfo.DeclaringType.FullName );
                    }
                }
            }
        }

        MutableAmbientProperty EnsureCachedAmbientProperty( IActivityLogger logger, StObjCollectorResult result, IStObjValueResolver dependencyResolver, Type propertyType, string name, MutableAmbientProperty alreadySolved = null )
        {
            Debug.Assert( _specialization == null );
            Debug.Assert( _prepareState == PrepareState.PreparedDone || _prepareState == PrepareState.CachingAmbientProperty );
            Debug.Assert( alreadySolved == null || (alreadySolved.Name == name && alreadySolved.Type == propertyType) );

            // Reentrancy is handled by returning null. 
            // The path that lead to such null result is simply ignored. 
            // Only the first entry point in the cycle will cache a new (invalid) MutableAmbientProperty( this, name ) in its cache. Any other call to the same cycle will lead to (and return) this (empty) marker.
            // The only other case where we return null is when the requested propertyType is not compatible with an existing cached property with the same name.
            if( _prepareState == PrepareState.CachingAmbientProperty ) return null;
            _prepareState = PrepareState.CachingAmbientProperty;
            try
            {
                MutableAmbientProperty a;
                if( alreadySolved != null )
                {
                    a = alreadySolved;
                }
                else
                {
                    a = _allAmbientProperties.FirstOrDefault( p => p.Name == name );
                    if( a != null && !propertyType.IsAssignableFrom( a.Type ) )
                    {
                        logger.Warn( "Looking for property named '{0}' of type '{1}': found a candidate on '{2}' but type does not match (it is '{3}'). It is ignored.", name, propertyType.Name, ToString(), a.Type.Name );
                        return null;
                    }
                }
                // Never seen this property: we must find it in our containers.
                if( a == null )
                {
                    MutableItem currentLevel = this;
                    do
                    {
                        if( currentLevel.IsOwnContainer ) a = currentLevel._dContainer._leafSpecialization.EnsureCachedAmbientProperty( logger, result, dependencyResolver, propertyType, name );
                        currentLevel = currentLevel.Generalization;
                    }
                    while( (a == null || a.Value == Type.Missing) && currentLevel != null );
                    if( a == null )
                    {
                        a = new MutableAmbientProperty( this, name );
                    }
                    _allAmbientProperties.Add( a );
                    Debug.Assert( a.IsFinalValue );
                }
                if( a.IsFinalValue ) return a;

                // Property has been explicitely set to Type.Missing in order to inherit from its containers.
                if( a.UseValue && a.Value == Type.Missing )
                {
                    MutableAmbientProperty found = null;
                    MutableItem currentLevel = this;
                    do
                    {
                        if( currentLevel.IsOwnContainer ) found = currentLevel._dContainer._leafSpecialization.EnsureCachedAmbientProperty( logger, result, dependencyResolver, propertyType, name );
                        currentLevel = currentLevel.Generalization;
                    }
                    while( (found == null || found.Value == Type.Missing) && currentLevel != null );
                    a.SetValue( found == null ? Type.Missing : found.Value );
                    return a;
                }

                // Property has been explicitely set or configured for resolution at a given level.
                // Before accepting the value or resolving it, we apply container's inheritance up to this level if it is not the most specialized one.
                if( a.MaxSpecializationDepthSet < _objectType.SpecializationDepth )
                {
                    MutableAmbientProperty found = null;
                    MutableItem currentLevel = this;
                    do
                    {
                        if( currentLevel.IsOwnContainer ) found = currentLevel._dContainer._leafSpecialization.EnsureCachedAmbientProperty( logger, result, dependencyResolver, propertyType, name );
                        currentLevel = currentLevel.Generalization;
                    }
                    while( (found == null || found.Value == Type.Missing) && currentLevel != null && currentLevel._objectType.SpecializationDepth > a.MaxSpecializationDepthSet );
                    if( found != null && found.Value != Type.Missing )
                    {
                        a.SetValue( found.Value );
                        return a;
                    }
                }
                // No value found from containers: we may have to solve it.
                a.SetValue( a.UseValue ? a.Value : a.ResolveToStObj( logger, result, null ) );
                return a;
            }
            finally
            {
                _prepareState = PrepareState.PreparedDone;
            }
        }


    }
}
