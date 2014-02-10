//#region LGPL License
///*----------------------------------------------------------------------------
//* This file (CK.Reflection\ILGeneratorExtension.cs) is part of CiviKey. 
//*  
//* CiviKey is free software: you can redistribute it and/or modify 
//* it under the terms of the GNU Lesser General Public License as published 
//* by the Free Software Foundation, either version 3 of the License, or 
//* (at your option) any later version. 
//*  
//* CiviKey is distributed in the hope that it will be useful, 
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
//* GNU Lesser General Public License for more details. 
//* You should have received a copy of the GNU Lesser General Public License 
//* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
//*  
//* Copyright © 2007-2012, 
//*     Invenietis <http://www.invenietis.com>,
//*     In’Tech INFO <http://www.intechinfo.fr>,
//* All rights reserved. 
//*-----------------------------------------------------------------------------*/
//#endregion

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Reflection.Emit;
//using System.Diagnostics;
//using System.Reflection;

//namespace CK.Reflection
//{
//    /// <summary>
//    /// Provides extension methods on <see cref="ILGenerator"/> class.
//    /// </summary>
//    public static class TypeBuilderExtension
//    {
//        /// <summary>
//        /// Creates constructors that relay calls to public and protected constructors in the base class.
//        /// </summary> 
//        /// <param name="this">This <see cref="TypeBuilder"/>.</param>
//        /// <param name="baseConstructorfilter">Optional predicate used to filter constructors that must be implemented. When null, all public and protected contructors are defined.</param>
//        /// <param name="constructorAttributesFilter">Optional predicate used to filter constructors' attributes. When null, all attributes are redefined.</param>
//        /// <param name="parameterAttributesFilter">Optional predicate used to filter constructors' arguments' attributes. When null, all attributes are redefined.</param>
//        public static void DefinePassThroughConstructors( this TypeBuilder @this,
//                                                            Predicate<ConstructorInfo> baseConstructorfilter = null,
//                                                            Func<ConstructorInfo, CustomAttributeData, bool> constructorAttributesFilter = null,
//                                                            Func<ParameterInfo, CustomAttributeData, bool> parameterAttributesFilter = null )
//        {
//            Type baseType = @this.BaseType;
//            foreach( var baseCtor in baseType.GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) )
//            {
//                if( baseCtor.IsPrivate ) continue;
//                if( baseConstructorfilter != null && !baseConstructorfilter( baseCtor ) ) continue;
//                var parameters = baseCtor.GetParameters();
//                if( parameters.Length == 0 ) @this.DefineDefaultConstructor( baseCtor.Attributes );
//                else
//                {
//                    Type[] parameterTypes = ReflectionHelper.CreateParametersType( parameters );
//                    Type[][] requiredCustomModifiers = parameters.Select( p => p.GetRequiredCustomModifiers() ).ToArray();
//                    Type[][] optionalCustomModifiers = parameters.Select( p => p.GetOptionalCustomModifiers() ).ToArray();

//                    var ctor = @this.DefineConstructor( MethodAttributes.Public, baseCtor.CallingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers );
//                    for( var i = 0; i < parameters.Length; ++i )
//                    {
//                        ParameterInfo parameter = parameters[i];
//                        ParameterBuilder pBuilder = ctor.DefineParameter( i + 1, parameter.Attributes, parameter.Name );
//                        if( (parameter.Attributes & ParameterAttributes.HasDefault) != 0 )
//                        {
//                            pBuilder.SetConstant( parameter.RawDefaultValue );
//                        }
//                        if( parameterAttributesFilter != null )
//                        {
//                            GenerateCustomAttributeBuilder( parameter.GetCustomAttributesData(), pBuilder.SetCustomAttribute, a => parameterAttributesFilter( parameter, a ) );
//                        }
//                        else
//                        {
//                            GenerateCustomAttributeBuilder( parameter.GetCustomAttributesData(), pBuilder.SetCustomAttribute );
//                        }
//                    }
//                    if( constructorAttributesFilter != null )
//                    {
//                        GenerateCustomAttributeBuilder( baseCtor.GetCustomAttributesData(), ctor.SetCustomAttribute, a => constructorAttributesFilter( baseCtor, a ) );
//                    }
//                    else
//                    {
//                        GenerateCustomAttributeBuilder( baseCtor.GetCustomAttributesData(), ctor.SetCustomAttribute );
//                    }
//                    var g = ctor.GetILGenerator();
//                    g.RepushActualParameters( true, parameters.Length + 1 );
//                    g.Emit( OpCodes.Call, baseCtor );
//                    g.Emit( OpCodes.Ret );
//                }
//            }
//        }

//        // In ReflectionHelper.
//        public static void GenerateCustomAttributeBuilder( IEnumerable<CustomAttributeData> customAttributes, Action<CustomAttributeBuilder> collector, Predicate<CustomAttributeData> filter = null )
//        {
//            if( customAttributes == null ) throw new ArgumentNullException( "customAttributes" );
//            if( collector == null ) throw new ArgumentNullException( "collector" );
//            foreach( var attr in customAttributes )
//            {
//                if( filter != null && !filter( attr ) ) continue;
//                var ctorArgs = attr.ConstructorArguments.Select( a => a.Value ).ToArray();
//                var namedPropertyInfos = attr.NamedArguments.Select( a => a.MemberInfo ).OfType<PropertyInfo>().ToArray();
//                var namedPropertyValues = attr.NamedArguments.Where( a => a.MemberInfo is PropertyInfo ).Select( a => a.TypedValue.Value ).ToArray();
//                var namedFieldInfos = attr.NamedArguments.Select( a => a.MemberInfo ).OfType<FieldInfo>().ToArray();
//                var namedFieldValues = attr.NamedArguments.Where( a => a.MemberInfo is FieldInfo ).Select( a => a.TypedValue.Value ).ToArray();
//                collector( new CustomAttributeBuilder( attr.Constructor, ctorArgs, namedPropertyInfos, namedPropertyValues, namedFieldInfos, namedFieldValues ) );
//            }
//        }



//    }
//}

