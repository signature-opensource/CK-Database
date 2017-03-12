//#region Proprietary License
///*----------------------------------------------------------------------------
//* This file (CK.StObj.Engine\StObj\StObjCollectorResult.FinalAssembly.cs) is part of CK-Database. 
//* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
//*-----------------------------------------------------------------------------*/
//#endregion

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using CK.Core;
//using System.Diagnostics;
//using System.Reflection.Emit;
//using System.Reflection;
//using CK.Reflection;
//using System.Resources;
//using System.Collections;
//using System.IO;
//using Mono.Cecil;
//using Mono.Cecil.Cil;

//namespace CK.Setup
//{
//    public partial class StObjCollectorResult
//    {
//        bool GenerateForOtherPlatforms(string baseDirectory, OtherPlatformSupportConfiguration config)
//        {
//            baseDirectory = FileUtil.NormalizePathSeparator(baseDirectory, false);
//            string p = config.BinFolder;
//            if (!Path.IsPathRooted(p))
//            {
//                p = Path.Combine(baseDirectory, config.BinFolder);
//                while (!Directory.Exists(p))
//                {
//                    baseDirectory = Path.GetDirectoryName(baseDirectory);
//                    if (baseDirectory == null) return false;
//                }
//            }
//            IEnumerable<string> assemblyPaths = GetAssemblyPaths(config.AssemblyNamesToRedirect);
//            var resolver = new DefaultAssemblyResolver();
//            foreach( var aPath in assemblyPaths.Append(baseDirectory).Select( a => Path.GetDirectoryName( a ) ).Distinct() )
//            {
//                resolver.AddSearchDirectory( aPath );
//            }
//            var readerParameters = new ReaderParameters() { AssemblyResolver = resolver };
//            var toChange = AssemblyDefinition.ReadAssembly(_finalAssembly.SaveFilePath, readerParameters);
//            // Loads assemblies for which calls must be redirected and processes their types.
//            foreach (var path in assemblyPaths)
//            {
//                var a = AssemblyDefinition.ReadAssembly(path, readerParameters);
//                foreach (var targetType in a.MainModule.Types)
//                {
//                    var refTarget = toChange.MainModule.ImportReference(targetType);
//                    SwapTypes(toChange.MainModule, targetType.FullName, refTarget);
//                }
//            }
//            var keep = toChange.MainModule
//                        .AssemblyReferences
//                        .Except(config.AssemblyNamesToRemove)
//                        .Where(a => a.Name != "System.Data")
//                        //.GroupBy(a => a.Name)
//                        //.Select(g => g.OrderByDescending(a => a.Version).First())
//                        .ToArray();
//            toChange.MainModule.AssemblyReferences.Clear();
//            foreach (var a in keep) toChange.MainModule.AssemblyReferences.Add(a);
//            toChange.Write( Path.Combine( baseDirectory, _finalAssembly.SaveFileName) );
//            return true;
//        }

//        IEnumerable<string> GetAssemblyPaths(IList<string> assemblyNamesToRedirect)
//        {
//            return new string[] {
//                @"C:\Users\olivi\.nuget\packages\System.Data.Common\4.3.0\lib\netstandard1.2\System.Data.Common.dll",
//                @"C:\Users\olivi\.nuget\packages\System.Data.SqlClient\4.3.0\runtimes\win\lib\netstandard1.3\System.Data.SqlClient.dll",
//                @"C:\Dev\CK-Database\CK-Database\Tests\SqlTransform\SqlTransform.Tests\bin\Debug\netcoreapp1.1\CK.SqlServer.Setup.Model.dll"
//            };
//        }

//        static int SwapTypes(ModuleDefinition module, string search, TypeReference replace)
//        {
//            int changeCount = 0;
//            foreach (TypeDefinition t in module.Types)
//            {
//                foreach (FieldDefinition f in t.Fields)
//                {
//                    if (f.FieldType.FullName == search)
//                    {
//                        f.FieldType = replace;
//                        ++changeCount;
//                    }
//                }
//                foreach (MethodDefinition m in t.Methods)
//                {
//                    if (m.MethodReturnType.ReturnType.FullName == search)
//                    {
//                        m.MethodReturnType.ReturnType = replace;
//                        ++changeCount;
//                    }
//                    foreach (ParameterDefinition p in m.Parameters)
//                    {
//                        if (p.ParameterType.FullName == search)
//                        {
//                            p.ParameterType = replace;
//                            ++changeCount;
//                        }
//                    }
//                    if (m.HasBody)
//                    {
//                        foreach (VariableDefinition v in m.Body.Variables)
//                        {
//                            if (v.VariableType.FullName == search)
//                            {
//                                v.VariableType = replace;
//                                ++changeCount;
//                            }
//                        }
//                        for (int i = 0; i < m.Body.Instructions.Count; i++)
//                        {
//                            var instruction = m.Body.Instructions[i];
//                            TypeReference tRef = instruction.Operand as TypeReference;
//                            if (tRef != null && tRef.FullName == search)
//                            {
//                                var p = m.Body.GetILProcessor();
//                                p.Replace(instruction, p.Create(instruction.OpCode, replace));
//                                ++changeCount;
//                            }
//                            else
//                            {
//                                MethodReference mRef = instruction.Operand as MethodReference;
//                                if (mRef != null && mRef.DeclaringType.FullName == search)
//                                {
//                                    var p = m.Body.GetILProcessor();
//                                    p.Replace(
//                                        instruction,
//                                        p.Create(
//                                            instruction.OpCode,
//                                            module.ImportReference(GetMethod(replace.Resolve(), mRef))));
//                                    ++changeCount;
//                                }
//                                else
//                                {
//                                    FieldReference fRef = instruction.Operand as FieldReference;
//                                    if (fRef != null && fRef.DeclaringType.FullName == search)
//                                    {
//                                        var p = m.Body.GetILProcessor();
//                                        p.Replace(
//                                            instruction,
//                                            p.Create(
//                                                instruction.OpCode,
//                                                module.ImportReference(GetField(replace.Resolve(), fRef))));
//                                        ++changeCount;
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            return changeCount;
//        }

//        static MethodReference GetMethod(TypeDefinition t, MethodReference old)
//        {
//            var newM = t.Methods.FirstOrDefault(m => m.Name == old.Name
//                                                     && m.Parameters.Count == old.Parameters.Count
//                                                     && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(old.Parameters.Select(p => p.ParameterType.FullName)));
//            if (newM == null) throw new CKException($"Method {old.Name}( {string.Join(", ", old.Parameters.Select(p => p.ParameterType.Name))} ) not found on target type.");
//            return newM;
//        }

//        static FieldReference GetField(TypeDefinition t, FieldReference old)
//        {
//            return t.Fields.Single(f => f.Name == old.Name);
//        }


//    }
//}
