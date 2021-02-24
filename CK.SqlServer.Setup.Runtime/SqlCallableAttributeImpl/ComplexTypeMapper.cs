using CK.CodeGen;
using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CK.SqlServer.Setup
{
    class ComplexTypeMapperModel
    {
        public readonly Type CreatedType;

        class Param
        {
            public readonly string Name;
            public readonly Type Type;
            public readonly int Index;
            public readonly bool IsSimpleType;
            public int InputIndex;
            public int InputIndexTypeMismatch;

            public Param( string name, Type type, int index )
            {
                Name = name;
                Type = type;
                Index = index;
                InputIndex = -1;
                InputIndexTypeMismatch = -1;
                IsSimpleType = SqlCallableAttributeImpl.IsNetTypeMapped( type );
            }

        }

        /// <summary>
        /// This covers a constructor with its multiple parameters or a single property.
        /// </summary>
        class Mapped
        {
            /// <summary>
            /// Gets the Params objects. Only one when this is a MappedProperty.
            /// </summary>
            public readonly IReadOnlyList<Param> Parameters;

            protected Mapped( IReadOnlyList<Param> parameters )
            {
                Parameters = parameters;
            }

            public bool IsInputSatisfied => Parameters.All( p => p.InputIndex != -1 );

            public bool TryMapInput( string sqlName, Func<Type,bool> typeMatcher, int inputIdex )
            {
                bool found = false;
                foreach( var p in Parameters )
                {
                    bool nameMatch = sqlName == null || StringComparer.OrdinalIgnoreCase.Equals( p.Name, sqlName );
                    if( nameMatch )
                    {
                        if( typeMatcher == null || typeMatcher( p.Type ) )
                        {
                            found = true;
                            p.InputIndex = inputIdex;
                        }
                        else
                        {
                            // Name matches but type doesn't.
                            // As of 2021-02-21, we consider this to be an error.
                            p.InputIndexTypeMismatch = inputIdex;
                        }
                    }
                }
                return found;
            }
        }

        class MappedCtor : Mapped
        {
            public readonly ConstructorInfo Ctor;
            public readonly ParameterInfo[] CtorParameters;

            public MappedCtor( ConstructorInfo c, ParameterInfo[] ctorParameters )
                : base( ctorParameters.Select( ( p, i ) => new Param( p.Name, p.ParameterType, i ) ).ToArray() )
            {
                Ctor = c;
                CtorParameters = ctorParameters;
            }
        }

        class MappedProperty : Mapped
        {
            public readonly PropertyInfo Property;

            public Param Param => Parameters[0];

            public MappedProperty( PropertyInfo info )
                : base( new[] { new Param( info.Name, info.PropertyType, 0 ) } )
            {
                Property = info;
            }
        }

        readonly struct UnmappedInput
        {
            public readonly int Index;
            public readonly string Name;
            public readonly string Type;

            public UnmappedInput( int i, string n, string t )
            {
                Index = i;
                Name = n;
                Type = t;
            }
        }

        IReadOnlyList<MappedCtor> _ctors;
        MappedCtor _selectedCtor;
        List<MappedProperty> _props;
        List<UnmappedInput> _unmappedInputs;

        public ComplexTypeMapperModel( Type t )
        {
            CreatedType = t;
            // We sort the longest ctor first.
            _ctors = t.GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                        .Select( c => new MappedCtor( c, c.GetParameters() ) )
                        .OrderByDescending( c => c.CtorParameters.Length )
                        .ToArray();
            _props = t.GetProperties()
                            .Where( p => p.CanWrite && p.GetSetMethod() != null )
                            .Select( p => new MappedProperty( p ) )
                            .ToList();
        }

        /// <summary>
        /// Gets the constructors and properties.
        /// </summary>
        IEnumerable<Mapped> AllMapped => ((IEnumerable<Mapped>)_ctors).Concat( _props );

        /// <summary>
        /// Tries to associate a Sql procedure parameter to one or more of our ctor parameter or property.
        /// Name or typeMatcher can be null (but not both at the same time). 
        /// </summary>
        /// <param name="index">Zero based index of the input. Must be positive.</param>
        /// <param name="sqlName">Name of the input (can be null).</param>
        /// <param name="typeMatcher">
        /// Type matcher for the input (can be null).
        /// When the actual type is known (inputType), it is typically the revert of <see cref="T:Type.IsAssignableFrom"/>, this lambda is fine: t => t.IsAsignableFrom( inputType ).
        /// </param>
        /// <param name="sqlTypeName">Optional string with the type name used for warnings and errors.</param>
        /// <param name="shouldBeMapped">
        /// False to state that the sql input is not required to be mapped to a ctor parameter or a property.
        /// </param>
        /// <returns>True if the input has been mapped at least once.</returns>
        public bool AddInput( int index, string sqlName, Func<Type,bool> typeMatcher, string sqlTypeName, bool shouldBeMapped = true )
        {
            Debug.Assert( index >= 0 );
            Debug.Assert( typeMatcher != null || sqlName != null );
            if( DoAdd( index, sqlName, typeMatcher, AllMapped ) ) return true;
            if( shouldBeMapped )
            {
                if( _unmappedInputs == null ) _unmappedInputs = new List<UnmappedInput>();
                _unmappedInputs.Add( new UnmappedInput( index, sqlName, sqlTypeName ) );
            }
            return false;
        }

        bool DoAdd( int index, string sqlName, Func<Type, bool> typeMatcher, IEnumerable<Mapped> mappings )
        {
            bool found = false;
            foreach( var m in mappings )
            {
                found |= m.TryMapInput( sqlName, typeMatcher, index );
            }
            return found;
        }

        bool ChooseCtor( IActivityMonitor monitor )
        {
            Debug.Assert( _selectedCtor == null );
            foreach( var c in _ctors )
            {
                if( c.IsInputSatisfied )
                {
                    if( _selectedCtor == null ) _selectedCtor = c;
                    else
                    {
                        if( _selectedCtor.Parameters.Count == c.Parameters.Count )
                        {
                            monitor.Error( $"Ambiguous constructors: both '{SqlCallableAttributeImpl.DumpParameters( _selectedCtor.CtorParameters, true )}' and '{SqlCallableAttributeImpl.DumpParameters( c.CtorParameters, true )}' are satisfied." );
                            return false;
                        }
                        Debug.Assert( _selectedCtor.Parameters.Count > c.Parameters.Count );
                        break;
                    }
                }
            }
            if( _selectedCtor == null )
            {
                monitor.Error( "Unable to find a constructor." );
                return false;
            }
            return true;
        }

        public bool CheckValidity( IActivityMonitor monitor, SqlCallableAttributeImpl.SqlParameterHandlerList sqlParams )
        {
            if( !ChooseCtor( monitor ) ) return false;

            // First removes properties that are already handled by the selected constructor, then check that they
            // are not type incompatible.
            Debug.Assert( _selectedCtor != null && _selectedCtor.Parameters.All( p => p.InputIndex >= 0 ) );
            bool typeError = false;
            for( int i = 0; i < _props.Count; ++i )
            {
                var p = _props[i];
                if( _selectedCtor.Parameters.Any( a => a.InputIndex == p.Param.InputIndex ) )
                {
                    var sqlParam = sqlParams.Handlers[p.Param.InputIndex];
                    monitor.Trace( $"Property '{p.Property.DeclaringType.Name}.{p.Param.Name}' assignation is skipped since a parameter constructor is already mapped to '{sqlParam}'." );
                    _props.RemoveAt( i-- );
                }
                else
                {
                    if( p.Param.InputIndexTypeMismatch >= 0 )
                    {
                        var sqlParam = sqlParams.Handlers[p.Param.InputIndexTypeMismatch];
                        typeError = true;
                        monitor.Error( $"Property '{p.Property.DeclaringType.Name}.{p.Param.Name}' cannot be assigned from '{sqlParam}': its type '{p.Property.PropertyType.Name}' is not compatible." );
                    }
                }
            }
            if( typeError ) return false;

            var unmappedProperties = _props.Where( p => !p.IsInputSatisfied ).ToList();
            if( unmappedProperties.Count != 0 &&_unmappedInputs != null )
            {
                using( monitor.OpenWarn( $"There are {unmappedProperties.Count} unmapped property(ies) and {_unmappedInputs.Count} unmapped input(s)." ) )
                {
                    foreach( var p in unmappedProperties )
                    {
                        monitor.Info( $"Property '{p.Property.Name}.{p.Property.PropertyType.Name}' is not mapped." );
                    }
                    foreach( var i in _unmappedInputs )
                    {
                        if( i.Type == null )
                        {
                            monitor.Info( $"Input n°{i.Index} named '{i.Name}' is not mapped." );
                        }
                        else
                        {
                            monitor.Info( $"Input n°{i.Index} named '{i.Name}' of type '{i.Type}' is not mapped." );
                        }
                    }
                }
            }
            return true;
        }

        public string EmitFullInitialization( ICodeWriter b, Func<int, Type, string> getValueGenerator )
        {
            Debug.Assert( _selectedCtor != null && _selectedCtor.IsInputSatisfied );

            var ctorVariableNames = _selectedCtor.Parameters
                                                    .Select( pCtor => getValueGenerator( pCtor.InputIndex, pCtor.Type ) )
                                                    .ToArray();

            b.Append( "var oR = new " ).AppendCSharpName( CreatedType ).Append( "(" )
                .Append( ctorVariableNames )
                .Append( ");" )
                .NewLine();
            foreach( var pProp in _props )
            {
                if( pProp.IsInputSatisfied )
                {
                    var param = pProp.Parameters[0];
                    string varName = getValueGenerator( param.InputIndex, param.Type );
                    b.Append( "oR." ).Append( pProp.Property.Name ).Append( " = " ).Append( varName ).Append( ";" ).NewLine();
                }
            }
            return "oR";
        }

    }

}
