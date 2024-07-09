using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlActorPackage.SpecialItemType
{
    [SqlTable( "tFirstSpecial", Package = typeof(Package) )]
    public class FirstSpecialTable : SpecialTableBase
    {
    }
}
