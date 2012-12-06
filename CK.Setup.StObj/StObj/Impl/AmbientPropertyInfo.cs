using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{

    internal class AmbientPropertyInfo
    {
        readonly PropertyInfo _p;
        int _index;
        readonly bool _isWriteable;
        readonly bool _isValueMergeable;
        readonly bool _isOptionalDefined;
        bool _isOptional;

        internal AmbientPropertyInfo( PropertyInfo p, bool isOptionalDefined, bool isOptional, int definerSpecializationDepth, int index, bool isWriteable, bool isValueMergeable )
        {
            _p = p;
            _isWriteable = isWriteable;
            _isValueMergeable = isValueMergeable;
            _isOptionalDefined = isOptionalDefined;
            _isOptional = isOptional;
            DefinerSpecializationDepth = definerSpecializationDepth;
            Index = index;
            ContextAttribute c = (ContextAttribute)Attribute.GetCustomAttribute( p, typeof( ContextAttribute ), false );
            if( c != null ) Context = c.Context;
        }

        public string Name { get { return _p.Name; } }
        public Type PropertyType { get { return _p.PropertyType; } }
        public Type DeclaringType { get { return _p.DeclaringType; } }
        public PropertyInfo PropertyInfo { get { return _p; } }

        /// <summary>
        /// Link to the ambient property above.
        /// </summary>
        public AmbientPropertyInfo Generalization { get; private set; } 

        /// <summary>
        /// This is settable in order for base class property's context to be used if it is not explicitely defined
        /// by the specialized property.
        /// </summary>
        public string Context { get; private set; }

        /// <summary>
        /// This is settable in order for final AmbientPropertyInfo at a given specialization level to 
        /// record the level of the first ancestor that defined it (regardless of the property type that - because
        /// of covariance support - can change from level to level).
        /// </summary>
        public int DefinerSpecializationDepth { get; private set; }

        /// <summary>
        /// Gets the index of this <see cref="AmbientPropertyInfo"/> inside any <see cref="StObjTypeInfo"/>.<see cref="AmbientProperties"/> into which it appears.
        /// </summary>
        public int Index { get { return _index; } private set { _index = value; } }

        public bool IsOptional { get { return _isOptional; } private set { _isOptional = value; } }
        public bool IsWriteable { get { return _isWriteable; } }
        public bool IsValueMergeable { get { return _isValueMergeable; } }

        /// <summary>
        /// An ambient property must be public or protected in order to be "specialized" either by overriding (for virtual ones)
        /// or by masking ('new' keyword in C#), typically to support covariance return type.
        /// The "Property Covariance" trick can be supported here because ambient properties are conceptually "read only" properties:
        /// they must be settable only to enable the framework (and no one else) to actually set their values.
        /// </summary>
        static public List<AmbientPropertyInfo> CreateAmbientPropertyListForExactType( Type t, int definerSpecializationDepth, IActivityLogger logger, List<StObjPropertyInfo> stObjProperties )
        {
            var properties = t.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where( p => !p.Name.Contains( '.' ) );
            List<AmbientPropertyInfo> result = null;
            foreach( var p in properties )
            {
                Debug.Assert( stObjProperties != null );
                StObjPropertyAttribute stObjAttr = (StObjPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( StObjPropertyAttribute ), false );
                if( stObjAttr != null )
                {
                    string nP = String.IsNullOrEmpty( stObjAttr.PropertyName ) ? p.Name : stObjAttr.PropertyName;
                    Type tP = stObjAttr.PropertyType == null ? p.PropertyType : stObjAttr.PropertyType;
                    if( stObjProperties.Find( sp => sp.Name == nP ) != null )
                    {
                        logger.Error( "StObj property named '{0}' for '{1}' is defined more than once. It should be declared only once.", p.Name, p.DeclaringType.FullName );
                    }
                    else
                    {
                        stObjProperties.Add( new StObjPropertyInfo( nP, tP, p ) );
                    }
                    // Continue to detect Ambient properties. Properties that are both Ambient and StObj must be detected.
                }
                AmbientPropertyAttribute attr = (AmbientPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( AmbientPropertyAttribute ), false );
                if( attr == null ) continue;

                var mSet = p.GetSetMethod( true );
                var mGet = p.GetGetMethod( true );
                bool isValueMergeable = typeof( IMergeable ).IsAssignableFrom( p.PropertyType );
                bool isWriteable = mSet != null && !mSet.IsPrivate;
                if( (!isWriteable && !isValueMergeable) || (mGet == null || mGet.IsPrivate) )
                {
                    // Warning: not a "Property Covariance" compliant property since
                    // specialized classes will not be able to "override" its signature.
                    // Even if it is not the "Property Covariance" that is targeted, a private get or set
                    // implies a "slicing" of the (base) type that defeats the specialization paradigm (with Covariance).
                    logger.Error( "Property '{0}' of '{1}' can not be considered as an Ambient property. An Ambient property must be readable and writeable (no private setter or getter), or implement CK.Core.IMergeable interface. Did youy forget to make it public?", p.Name, p.DeclaringType.FullName );
                }
                else
                {
                    if( !isWriteable && isValueMergeable )
                    {
                        throw new NotImplementedException( "Not Writeable but Mergeable properties are not yet supported (need a IStObjMutableItem.SetPropertyStructuralSetter to initialize it)." );
                    }
                    if( result == null ) result = new List<AmbientPropertyInfo>();
                    Debug.Assert( result.Find( a => a.Name == p.Name ) == null, "No homonym properties in .Net framework." );
                    result.Add( new AmbientPropertyInfo( p, attr.IsOptionalDefined, attr.IsOptional, definerSpecializationDepth, result.Count, isWriteable, isValueMergeable ) );
                }
            }
            return result;
        }

        static public IEnumerable<AmbientPropertyInfo> MergeAboveAmbientProperties( IEnumerable<AmbientPropertyInfo> above, IList<AmbientPropertyInfo> collector, IActivityLogger logger )
        {
            if( collector == null || collector.Count == 0 ) return above ?? ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
            if( above != null )
            {
                // Adds 'above' into 'collector' before returning it.
                int nbFromAbove = 0;
                foreach( AmbientPropertyInfo a in above )
                {
                    AmbientPropertyInfo exists = null;
                    int idxExists = nbFromAbove;
                    while( idxExists < collector.Count && (exists = collector[idxExists]).Name != a.Name ) ++idxExists;
                    if( idxExists == collector.Count )
                    {
                        collector.Insert( nbFromAbove++, a );
                    }
                    else
                    {
                        // Covariance ?
                        if( exists.PropertyType != a.PropertyType && !a.PropertyType.IsAssignableFrom( exists.PropertyType ) )
                        {
                            logger.Error( "Ambient property '{0}.{1}' type is not compatible with base property '{2}.{1}'.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                        }
                        // A required property can not become optional.
                        if( exists.IsOptional && !a.IsOptional )
                        {
                            if( exists._isOptionalDefined )
                            {
                                logger.Error( "Ambient property '{0}.{1}' states that it is optional but base property '{2}.{1}' is required.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                            }
                            exists._isOptional = false;
                        }
                        // Context inheritance (if not defined).
                        if( exists.Context == null )
                        {
                            exists.Context = a.Context;
                        }
                        // Propagates the top first definer level.
                        exists.DefinerSpecializationDepth = a.DefinerSpecializationDepth;
                        // Captures the Generalization.
                        // We keep the fact that this property overrides one above (errors have been logged if conflict/incoherency occur).
                        // We can keep the Generalization but not a reference to the specialization since we are 
                        // not Contextualized here, but only on a pure Type level.
                        exists.Generalization = a;
                        //
                        collector.RemoveAt( idxExists );
                        exists.Index = nbFromAbove;
                        collector.Insert( nbFromAbove++, exists );

                    }
                }
            }
            return collector;
        }

        /// <summary>
        /// Recursive function to collect Ambient Properties on base types 
        /// </summary>
        static public IEnumerable<AmbientPropertyInfo> CreateAllAmbientPropertyList( Type type, int specializationLevel, IActivityLogger logger, List<StObjPropertyInfo> stObjProperties )
        {
            if( type == typeof( object ) ) return null;
            return MergeAboveAmbientProperties( CreateAllAmbientPropertyList( type.BaseType, specializationLevel - 1, logger, stObjProperties ), CreateAmbientPropertyListForExactType( type, specializationLevel, logger, stObjProperties ), logger );
        }

    }

}
