using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace EnC.MetaData
{
    /// <summary>
    /// Used for getting system types definitions from mscorlib
    /// </summary>
    static class SytemTypeReferenceGetter
    {
        public static TypeReference GetSystemTypeReference(AssemblyDefinition asmDef, string fullName)
        {
            switch (fullName) {
                case "System.Boolean":
                    return asmDef.MainModule.TypeSystem.Boolean;
                case "System.Byte":
                    return asmDef.MainModule.TypeSystem.Byte;
                case "System.Double":
                    return asmDef.MainModule.TypeSystem.Double;
                case "System.Char":
                    return asmDef.MainModule.TypeSystem.Char;
                case "System.Int16":
                    return asmDef.MainModule.TypeSystem.Int16;
                case "System.Int32":
                    return asmDef.MainModule.TypeSystem.Int32;
                case "System.Int64":
                    return asmDef.MainModule.TypeSystem.Int64;
                case "System.IntPtr":
                    return asmDef.MainModule.TypeSystem.IntPtr;
                case "System.Object":
                    return asmDef.MainModule.TypeSystem.Object;
                case "System.SByte":
                    return asmDef.MainModule.TypeSystem.SByte;
                case "System.Single":
                    return asmDef.MainModule.TypeSystem.Single;
                case "System.String":
                    return asmDef.MainModule.TypeSystem.String;
                case "System.UInt16":
                    return asmDef.MainModule.TypeSystem.UInt16;
                case "System.UInt32":
                    return asmDef.MainModule.TypeSystem.UInt32;
                case "System.UInt64":
                    return asmDef.MainModule.TypeSystem.UInt64;
                case "System.UIntPtr":
                    return asmDef.MainModule.TypeSystem.UIntPtr;
                case "System.Void":
                    return asmDef.MainModule.TypeSystem.Void;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
