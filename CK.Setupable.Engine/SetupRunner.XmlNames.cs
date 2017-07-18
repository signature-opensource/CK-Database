using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// These definitions are included in CKSetup project.
    /// </summary>
    public static partial class SetupRunner
    {
        static internal readonly string XmlFileName = "dbSetup-config.xml";
        static internal readonly XName xRunner = XNamespace.None + "Runner";
        static internal readonly XName xLogFiler = XNamespace.None + "LogFilter";
        static internal readonly XName xSetup = XNamespace.None + "Setup";
    }
}
