using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    public enum ComponentExportType
    {
        /// <summary>
        /// No export.
        /// </summary>
        None,

        /// <summary>
        /// Only component description must be exported.
        /// </summary>
        Description,

        /// <summary>
        /// Component description and files must be exported.
        /// </summary>
        DescriptionAndFiles
    }
}
