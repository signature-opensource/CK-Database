using System;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Wrapper for keys in Type mapping dictionaries: when wrapped in this class,
    /// the Type is the key of its highest implementation instead of its final concrete class.
    /// This enables the use of one and only one dictionnary for Mappings (Type => Final Type) as well as 
    /// highest implementation association (Ambient object interface => its highest implementation).
    /// </summary>
    internal class AmbientObjecttInterfaceKey
    {
        public readonly Type InterfaceType;

        public AmbientObjecttInterfaceKey( Type ambientObjectInterface )
        {
            Debug.Assert( ambientObjectInterface.IsInterface );
            InterfaceType = ambientObjectInterface;
        }

        public override bool Equals( object obj )
        {
            AmbientObjecttInterfaceKey k = obj as AmbientObjecttInterfaceKey;
            return k != null && k.InterfaceType == InterfaceType;
        }

        public override int GetHashCode()
        {
            return -InterfaceType.GetHashCode();
        }
    }
}
