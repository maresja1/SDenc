/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 14.11.2011
 * Time: 22:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using EnC.MetaData;
using ICSharpCode.SharpDevelop.Dom;
using Mono.Cecil;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Used to compare method and property overloads.
	/// </summary>
	public static class MemberComparator
	{
        /// <summary>
        /// Returns true if <c>method1</c> and <c>method2</c> are same method overloads.
        /// </summary>
		public static bool SameMethodOverloads(IMethod method1, IMethod method2)
		{
			if( method1.Namespace == method2.Namespace && 
			    method1.DeclaringType.FullyQualifiedName == method2.DeclaringType.FullyQualifiedName &&
			    method1.Name == method2.Name && method1.Parameters.Count == method2.Parameters.Count)
			{	
				bool paramsSame = true;
				for (int i = 0; i < method1.Parameters.Count; i++) {
					paramsSame &= method1.Parameters[i].ReturnType.FullyQualifiedName == method2.Parameters[i].ReturnType.FullyQualifiedName;
					paramsSame &= (method1.Parameters[i].Modifiers.CompareTo(method2.Parameters[i].Modifiers) == 0);
					if(!paramsSame)
						return false;
				}
				if(paramsSame){
					return true;
				}
			}
			return false;
		}
        /// <summary>
        /// Returns true if <c>property1</c> and <c>property2</c> are same properties.
        /// </summary>
		public static bool SameProperties(IProperty property1, IProperty property2)
		{
			return (property1.Namespace == property2.Namespace && property1.Name == property2.Name);
		}
        /// <summary>
        /// Returns true if <c>methodDef</c> and <c>method</c> are same method overloads.
        /// </summary>
		public static bool SameMethodOverloads(MethodDefinition methodDef,IMethod method)
		{
			if( methodDef.DeclaringType.FullName == method.DeclaringType.FullyQualifiedName &&
			   (methodDef.Name == method.Name || methodDef.IsConstructor && method.IsConstructor) && 
			   methodDef.Parameters.Count == method.Parameters.Count)
			{	
				bool paramsSame = true;
				for (int i = 0; i < methodDef.Parameters.Count; i++) {
                    string name = methodDef.Parameters[i].ParameterType.Name;
                    if (methodDef.Parameters[i].ParameterType.IsByReference || methodDef.Parameters[i].IsOut)
                        name = name.Substring(0, name.Length - 1);
                    paramsSame &= methodDef.Parameters[i].IsOut == method.Parameters[i].IsOut;
                    paramsSame &= (methodDef.Parameters[i].ParameterType.IsByReference && !methodDef.Parameters[i].IsOut) == method.Parameters[i].IsRef;
                    paramsSame &= SameTypes(methodDef.Parameters[i].ParameterType, method.Parameters[i].ReturnType,name);
					if(!paramsSame)
						return false;
				}
				if(paramsSame){
					return true;
				}
			}
			return false;
		}
    /// <summary>
    /// Returns true if <c>type1</c> and <c>type2</c> represents same type.
    /// </summary>
        public static bool SameTypes(TypeReference type1, IReturnType type2, string type1Name = null)
        {
            if (type1Name == null) {
                type1Name = type1.Name;
            }
            if (type1.IsArray)
                type1Name = type1Name.Substring(0, type1Name.Length - 2);
            if (type1.IsGenericInstance) {
                type1Name = type1Name.Substring(0, type1Name.Length - 2);
            }
            bool paramsSame = true;
            if (type2.IsConstructedReturnType  &&
                    ((ConstructedReturnType)type2).TypeArgumentCount ==
                    ((GenericInstanceType)type1).GenericArguments.Count) {
                for (int j = 0; j < ((ConstructedReturnType)type2).TypeArgumentCount; j++){
                    paramsSame &= SameTypes(((GenericInstanceType)type1).GenericArguments[j],((ConstructedReturnType)type2).TypeArguments[j]);
                }
            } else if(type1.IsGenericInstance)
                return false;
            // Get array element if array return type
            paramsSame &= type1.IsArray == type2.IsArrayReturnType;
            if (type2 is ArrayReturnType) {
                type2 = ((ArrayReturnType)type2).ArrayElementType;
            }
            paramsSame &= type1Name == type2.Name;
            paramsSame &= (type1.Namespace == "" ? type1.DeclaringType.FullName : type1.Namespace) == type2.Namespace;
            return paramsSame;
        }
        /// <summary>
        /// Returns true if <c>propDef</c> and <c>property</c> are same properties.
        /// </summary>
        public static bool SamePropertyOverloads(PropertyDefinition propDef, IProperty property)
        {
            if (propDef.DeclaringType.FullName == property.DeclaringType.FullyQualifiedName &&
               (propDef.Name == property.Name) &&
               propDef.Parameters.Count == property.Parameters.Count) {
                bool paramsSame = true;
                for (int i = 0; i < propDef.Parameters.Count; i++) {
                    string name = propDef.Parameters[i].ParameterType.Name;
                    if (propDef.Parameters[i].ParameterType.IsByReference || propDef.Parameters[i].IsOut)
                        name = name.Substring(0, name.Length - 1);
                    paramsSame &= name == property.Parameters[i].ReturnType.Name;
                    paramsSame &= propDef.Parameters[i].ParameterType.Namespace == property.Parameters[i].ReturnType.Namespace;
                    paramsSame &= propDef.Parameters[i].IsOut == property.Parameters[i].IsOut;
                    paramsSame &= (propDef.Parameters[i].ParameterType.IsByReference && !propDef.Parameters[i].IsOut) == property.Parameters[i].IsRef;
                    paramsSame &= propDef.Parameters[i].ParameterType.IsDefinition == property.Parameters[i].ReturnType.IsArrayReturnType;

                    if (!paramsSame)
                        return false;
                }
                if (paramsSame) {
                    return true;
                }
            }
            return false;
        }
	}
}