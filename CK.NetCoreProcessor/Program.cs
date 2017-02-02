using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.NetCoreProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var change = @"C:\Dev\CK-Database\CK-Database\Tests\SqlTransform\SqlTransform.Tests\bin\Debug\net451\win7-x64\Transform.Tests.Generated.dll";

            var target1 = @"C:\Users\olivi\.nuget\packages\System.Data.Common\4.3.0\ref\netstandard1.2\System.Data.Common.dll";
            var target2 = @"C:\Users\olivi\.nuget\packages\System.Data.SqlClient\4.3.0\runtimes\win\lib\netstandard1.3\System.Data.SqlClient.dll";

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(@"C:\Users\olivi\.nuget\packages\System.Data.SqlClient\4.3.0\runtimes\win\lib\netstandard1.3\");
            resolver.AddSearchDirectory(@"C:\Users\olivi\.nuget\packages\System.Data.Common\4.3.0\ref\netstandard1.2\");
            var readerParameters = new ReaderParameters() { AssemblyResolver = resolver };
            var toChange = AssemblyDefinition.ReadAssembly(change, readerParameters);

            var systemData = AssemblyDefinition.ReadAssembly(target1, readerParameters);
            var systemDataClient = AssemblyDefinition.ReadAssembly(target2, readerParameters);

            foreach ( var targetType in systemData.MainModule.Types )
            {
                var refTarget = toChange.MainModule.Import(targetType);
                SwapTypes(toChange.MainModule, targetType.FullName, refTarget);
            }

            foreach (var targetType in systemDataClient.MainModule.Types)
            {
                var refTarget = toChange.MainModule.Import(targetType);
                SwapTypes(toChange.MainModule, targetType.FullName, refTarget);
            }
            var r1 = toChange.MainModule.AssemblyReferences.Single(a => a.Name == "System.Data");
            toChange.MainModule.AssemblyReferences.Remove(r1);

            toChange.Write(@"C:\Dev\CK-Database\CK-Database\Tests\SqlTransform\SqlTransform.Tests\bin\Debug\netcoreapp1.1\Transform.Tests.Generated.dll");               
        }

        static int SwapTypes(ModuleDefinition module, string search, TypeReference replace)
        {
            int changeCount = 0;
            foreach (TypeDefinition t in module.Types)
            {
                foreach (FieldDefinition f in t.Fields)
                {
                    if (f.FieldType.FullName == search)
                    {
                        f.FieldType = replace;
                        ++changeCount;
                    }
                }
                foreach (MethodDefinition m in t.Methods)
                {
                    if (m.MethodReturnType.ReturnType.FullName == search)
                    {
                        m.MethodReturnType.ReturnType = replace;
                        ++changeCount;
                    }
                    foreach (ParameterDefinition p in m.Parameters)
                    {
                        if (p.ParameterType.FullName == search)
                        {
                            p.ParameterType = replace;
                            ++changeCount;
                        }
                    }
                    if (m.HasBody)
                    {
                        foreach (VariableDefinition v in m.Body.Variables)
                        {
                            if (v.VariableType.FullName == search)
                            {
                                v.VariableType = replace;
                                ++changeCount;
                            }
                        }
                        for (int i = 0; i < m.Body.Instructions.Count; i++)
                        {
                            var instruction = m.Body.Instructions[i];
                            TypeReference tRef = instruction.Operand as TypeReference;
                            if (tRef != null && tRef.FullName == search)
                            {
                                var p = m.Body.GetILProcessor();
                                p.Replace(instruction, p.Create(instruction.OpCode, replace));
                                ++changeCount;
                            }
                            else
                            {
                                MethodReference mRef = instruction.Operand as MethodReference;
                                if (mRef != null && mRef.DeclaringType.FullName == search)
                                {
                                    var p = m.Body.GetILProcessor();
                                    p.Replace(
                                        instruction,
                                        p.Create(
                                            instruction.OpCode,
                                            module.Import(GetMethod(replace.Resolve(), mRef))));
                                    ++changeCount;
                                }
                                else
                                {
                                    FieldReference fRef = instruction.Operand as FieldReference;
                                    if (fRef != null && fRef.DeclaringType.FullName == search)
                                    {
                                        var p = m.Body.GetILProcessor();
                                        p.Replace(
                                            instruction,
                                            p.Create(
                                                instruction.OpCode,
                                                module.Import(GetField(replace.Resolve(), fRef))));
                                        ++changeCount;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return changeCount;
        }

        static MethodReference GetMethod(TypeDefinition t, MethodReference old)
        {
            var newM = t.Methods.FirstOrDefault(m => m.Name == old.Name
                                                     && m.Parameters.Count == old.Parameters.Count
                                                     && m.Parameters.Select( p => p.ParameterType.FullName ).SequenceEqual(old.Parameters.Select(p => p.ParameterType.FullName)) );
            if (newM == null) throw new Exception( $"Method {old.Name}( {string.Join( ", ", old.Parameters.Select( p => p.ParameterType.Name ) )} ) not found on target type." );
            return newM;
        }

        static FieldReference GetField(TypeDefinition t, FieldReference old)
        {
            return t.Fields.Single(f => f.Name == old.Name);
        }

    }
}