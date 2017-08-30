using System.Xml.Linq;

namespace CK.StObj.Runner
{
    /// <summary>
    /// These definitions are included in CKSetup.Core project.
    /// </summary>
    public static partial class Program
    {
        static internal readonly string XmlFileName = "dbSetup-config.xml";
        static internal readonly XName xRunner = XNamespace.None + "Runner";
        static internal readonly XName xLogFiler = XNamespace.None + "LogFilter";
        static internal readonly XName xSetup = XNamespace.None + "Setup";
    }
}
