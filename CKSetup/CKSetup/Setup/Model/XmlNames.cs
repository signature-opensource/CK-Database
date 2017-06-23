using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    static class XmlNames
    {
        static public readonly XName nDB = XNamespace.None + "DB";
        static public readonly XName nComponent = XNamespace.None + "Component";
        static public readonly XName nKind = XNamespace.None + "Kind";
        static public readonly XName nDependencies = XNamespace.None + "Dependencies";
        static public readonly XName nDependency = XNamespace.None + "Dependency";
        static public readonly XName nEmbeddedComponents = XNamespace.None + "EmbeddedComponents";
        static public readonly XName nFiles = XNamespace.None + "Files";
        static public readonly XName nFile = XNamespace.None + "File";
        static public readonly XName nRef = XNamespace.None + "Ref";
        static public readonly XName nTargetFramework = XNamespace.None + "TargetFramework";
        static public readonly XName nName = XNamespace.None + "Name";
        static public readonly XName nVersion = XNamespace.None + "Version";


    }
}
