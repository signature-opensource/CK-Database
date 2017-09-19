using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    static class DBXmlNames
    {
        static public readonly XName DB = XNamespace.None + "DB";
        static public readonly XName Component = XNamespace.None + "Component";
        static public readonly XName Kind = XNamespace.None + "Kind";
        static public readonly XName Dependencies = XNamespace.None + "Dependencies";
        static public readonly XName Dependency = XNamespace.None + "Dependency";
        static public readonly XName EmbeddedComponents = XNamespace.None + "EmbeddedComponents";
        static public readonly XName Files = XNamespace.None + "Files";
        static public readonly XName File = XNamespace.None + "File";
        static public readonly XName Ref = XNamespace.None + "Ref";
        static public readonly XName TargetFramework = XNamespace.None + "TargetFramework";
        static public readonly XName Name = XNamespace.None + "Name";
        static public readonly XName Length = XNamespace.None + "Length";
        static public readonly XName SHA1 = XNamespace.None + "SHA1";
        static public readonly XName FileVersion = XNamespace.None + "FileVersion";
        static public readonly XName AssemblyVersion = XNamespace.None + "AssemblyVersion";
        static public readonly XName Version = XNamespace.None + "Version";
        static public readonly XName LocalFileKeys = XNamespace.None + "LocalFileKeys";
        static public readonly XName Key = XNamespace.None + "Key";
        static public readonly XName Missing = XNamespace.None + "Missing";
        static public readonly XName Runtime = XNamespace.None + "Runtime";
    }
}
