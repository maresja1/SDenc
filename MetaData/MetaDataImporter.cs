/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 9.11.2011
 * Time: 9:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Debugger.Interop.MetaData;
using ICSharpCode.SharpDevelop.Project;
using Mono.Cecil;

namespace EnC.MetaData
{
	/// <summary>
	/// Extension to IMetaDataImport with auto-converting byte arrays to string etc..
	/// </summary>
	public class MetaDataImporter
	{
        /// <summary>
        /// Holds import interface
        /// </summary>
		IMetaDataImport importer;

        /// <summary>
        /// Holds import interface
        /// </summary>
		public IMetaDataImport CorMetaDataImport{
			get{ return importer; }
		}
		
		public MetaDataImporter(IMetaDataImport pImporter)
		{
			importer = pImporter;
		}		
		public uint FindMethod(string szTypeDef,string szMethod, byte[] signature)
		{
			uint token = FindTypeDef(szTypeDef);
			if(token==0)
				return 0;
			return FindMethod(token,szMethod,signature);
		}
		public uint FindTypeRef(string szTypeRef)
		{
			IntPtr hEnum = new IntPtr();
			uint cDefs,maxcDefs = 10;
			uint[] typeRefs = new uint[maxcDefs];
			
			importer.EnumTypeRefs(ref hEnum,typeRefs,maxcDefs,out cDefs);
			while(cDefs > 0)
			{
				for (int i = 0; i < cDefs; i++) {					
					string name;
					uint resScope;
					GetTypeRefProps(typeRefs[i],out name,out resScope);
					if(name == szTypeRef)
					{
						importer.CloseEnum(hEnum);
						return typeRefs[i];
					}
				}
				importer.EnumTypeRefs(ref hEnum,typeRefs,maxcDefs,out cDefs);
			}
			importer.CloseEnum(hEnum);
			return 0;
		}
		public uint FindMemberRef(string szTypeRef,string szMemberRef)
		{
			uint token = FindTypeRef(szTypeRef);
			if(token==0)
				return 0;
			return FindMemberRef(token,szMemberRef);
		}
		public uint FindMemberRef(uint tkParent,string szMemberRef)
		{
			IntPtr hEnum = new IntPtr();
			uint cDefs,maxcDefs = 10;
			uint[] memberRefs = new uint[maxcDefs];
			
			importer.EnumMemberRefs(ref hEnum,tkParent,memberRefs,maxcDefs,out cDefs);
			while(cDefs > 0)
			{
				for (int i = 0; i < cDefs; i++) {					
					string name;
					uint ptk;
					byte[] sigBlob;
					GetMemberRefProps(memberRefs[i],out name,out ptk,out sigBlob);
					if(name == szMemberRef)
					{
						importer.CloseEnum(hEnum);
						return memberRefs[i];
					}
				}
				importer.EnumMemberRefs(ref hEnum,tkParent,memberRefs,maxcDefs,out cDefs);
			
			}
			importer.CloseEnum(hEnum);
			return 0;
		}
		public uint FindTypeDef(string szTypeDef)
		{
			IntPtr hEnum = new IntPtr();
            uint cDefs, maxcDefs = 10;
			uint[] typeDefs = new uint[maxcDefs];
			
			importer.EnumTypeDefs(ref hEnum,typeDefs,maxcDefs,out cDefs);
			while(cDefs > 0)
			{
				for (int i = 0; i < cDefs; i++) {	
					TypeDefProps props = GetTypeDefProps(typeDefs[i]);
					if(props.tName == szTypeDef)
					{
						importer.CloseEnum(hEnum);
						return typeDefs[i];
					}
				}
				importer.EnumTypeDefs(ref hEnum,typeDefs,maxcDefs,out cDefs);
			}
			return 0;			
		}
        public uint FindMethod(uint tdToken, string szMethod, byte[] signature)
		{
			uint token = 0;
			IntPtr hEnum = new IntPtr();
			uint cDefs,maxcDefs = 10;			
			uint[] methodDefs = new uint[maxcDefs];
			
			importer.EnumMethods(ref hEnum,tdToken,methodDefs,maxcDefs,out cDefs);
			while(cDefs > 0)
			{
				for (int i = 0; i < cDefs; i++) {					
					string name;
					uint flags,rva,td,attr;
					byte[] sigBlob;
					GetMethodProps(methodDefs[i],out name,out td,out attr,out sigBlob,out rva,out flags);
					if(name == szMethod && checkSignature(signature,sigBlob))
					{
						importer.CloseEnum(hEnum);
						return methodDefs[i];
					}
				}
				importer.EnumMethods(ref hEnum,tdToken,methodDefs,maxcDefs,out cDefs);
			}	
			importer.CloseEnum(hEnum);
			return token;			
		}
		private unsafe byte[] pointerToArray(byte* ptr,uint length)
		{
			byte[] array = new byte[length];
			for (int i = 0; i < length; i++) {
				array[i] = *ptr;
				ptr++;
			}
			return array;
		}
		public MethodProps GetMethodProps(uint token)
		{
			MethodProps props = new MethodProps();
			props.mToken = token;
			
			GetMethodProps(token,out props.mName,out props.pClass,out props.pdwAttr,out props.ppvSigBlob,
			               out props.pulCodeRVA,out props.pdwImplFlags);
			
			MetadataToken mToken = new MetadataToken(props.pClass);
			if(mToken.TokenType == TokenType.TypeDef){
				TypeDefProps tdProps = GetTypeDefProps(props.pClass);
				props.parentProps = tdProps;
			}
			return props;
		}
		public unsafe void GetMethodProps(uint mb,out string szMethod, out uint pClass,out uint pdwAttr,out byte[] ppvSigBlob,out uint pulCodeRVA,
		                                  out uint pdwImplFlags)
		{			
			uint pchName,maxcbName = 50,lenSigBlob;
			byte* ptrBlob;
			byte[] buffer = new byte[2*(maxcbName+1)];
			
			importer.GetMethodProps(mb,out pClass,buffer,maxcbName,out pchName,out pdwAttr,out ptrBlob,out lenSigBlob,out pulCodeRVA,out pdwImplFlags);
			if(pchName > maxcbName)
			{
				buffer = new byte[2*(pchName+1)];
				importer.GetMethodProps(mb,out pClass,buffer,pchName,out pchName,out pdwAttr,out ptrBlob,out lenSigBlob,out pulCodeRVA,out pdwImplFlags);
			}
			szMethod = UnicodeEncoding.Unicode.GetString(buffer).Substring(0,(int)pchName-1);
			ppvSigBlob = pointerToArray(ptrBlob,lenSigBlob);
		}
		public unsafe TypeDefProps GetTypeDefProps(uint td)
		{
            TypeDefProps props = new TypeDefProps() ;
			uint pchName,maxcbName = 50;
			byte[] buffer = new byte[2*(maxcbName+1)];
			
				
			importer.GetTypeDefProps(td,buffer,maxcbName,out pchName,out props.flags,out props.extends);
			if(pchName > maxcbName)
			{
				buffer = new byte[2*(pchName+1)];
                importer.GetTypeDefProps(td, buffer, maxcbName, out pchName, out props.flags, out props.extends);
			}
			props.tName = UnicodeEncoding.Unicode.GetString(buffer).Substring(0,(int)pchName-1);
            return props;
		}
		public unsafe void GetTypeRefProps(uint tr,out string szTypeRef,out uint resScope)
		{
			uint pchName,maxcbName = 50;
			byte[] buffer = new byte[2*(maxcbName+1)];
			
			importer.GetTypeRefProps(tr,out resScope,buffer,maxcbName,out pchName);
			if(pchName > maxcbName)
			{
				buffer = new byte[2*(pchName+1)];
				importer.GetTypeRefProps(tr,out resScope,buffer,pchName,out pchName);					
			}
			szTypeRef = UnicodeEncoding.Unicode.GetString(buffer).Substring(0,(int)pchName-1);
		}
		public unsafe void GetMemberRefProps(uint mr, out string szMemberRef,out uint ptk,out byte[] sigBlob)
		{
			uint pchName,maxcbName = 50,lenSigBlob;
			byte* ptrBlob;
			byte[] buffer = new byte[2*(maxcbName+1)];
			
			importer.GetMemberRefProps(mr,out ptk,buffer,maxcbName,out pchName,out ptrBlob,out lenSigBlob);
			if(pchName > maxcbName)
			{
				buffer = new byte[2*(pchName+1)];
				importer.GetMemberRefProps(mr,out ptk,buffer,pchName,out pchName,out ptrBlob,out lenSigBlob);							
			}
			szMemberRef = UnicodeEncoding.Unicode.GetString(buffer).Substring(0,(int)pchName-1);			
			sigBlob = pointerToArray(ptrBlob,lenSigBlob);
		}
		public string GetUserString(uint token)
		{
			uint pcString,maxbString = 50;
			byte[] buffer = new byte[2*(maxbString+1)];
			importer.GetUserString(token,buffer,maxbString,out pcString);
			if(pcString > maxbString){
				buffer = new byte[2*(pcString+1)];
				importer.GetUserString(token,buffer,pcString,out pcString);
			}
			string res = UnicodeEncoding.Unicode.GetString(buffer).Substring(0,(int)pcString);
			return res;
		}
		public unsafe byte[] GetSigantureSA(uint token)
		{
			byte* sig;
			uint len;
			importer.GetSigFromToken(token,out sig,out len);
			return pointerToArray(sig,len);
		}
        private bool checkSignature(byte[] sig1, byte[] sig2)
        {
            if (sig1.Length != sig2.Length)
            {
                return false;
            }
            bool res = true;
            for (int i = 0; i < sig1.Length; i++)
            {
                if (sig1[i] != sig2[i])
                {
                    res = false;
                    break;
                }
            }
            return res;
        }
        public unsafe FieldProps GetFieldProps(uint token)
        {
            FieldProps props;
            uint pchName,maxcbName = 50,lenSigBlob;
			byte* ptrBlob;
			byte[] buffer = new byte[2*(maxcbName+1)];
            IntPtr ppValue;
            uint ppchValue;
            importer.GetFieldProps(token, out props.mClass, buffer, maxcbName, out pchName, out props.pdwAttr,
                out ptrBlob, out lenSigBlob, out props.pdwCPlusTypeFlag, out ppValue, out ppchValue);
			if(pchName > maxcbName)
			{
				buffer = new byte[2*(pchName+1)];
                importer.GetFieldProps(token, out props.mClass, buffer, pchName, out pchName, out props.pdwAttr,
                    out ptrBlob, out lenSigBlob, out props.pdwCPlusTypeFlag, out ppValue, out ppchValue);
			}
			props.fName = UnicodeEncoding.Unicode.GetString(buffer).Substring(0,(int)pchName-1);
            props.sigBlob = pointerToArray(ptrBlob, lenSigBlob);
            props.token = token;
            props.ppValue = read_array(ppValue, ppchValue);
            return props;
        }
        public uint FindField(uint tdToken, string szField, byte[] signature)
        {
            IntPtr hEnum = new IntPtr();
            uint cDefs, maxcDefs = 10;
            uint[] fieldDefs = new uint[maxcDefs];

            importer.EnumFields(ref hEnum, tdToken, fieldDefs, maxcDefs, out cDefs);
            while (cDefs > 0)
            {
                for (int i = 0; i < cDefs; i++)
                {
                    FieldProps props = GetFieldProps(fieldDefs[i]);
                    if (props.fName == szField && checkSignature(signature, props.sigBlob))
                    {
                        importer.CloseEnum(hEnum);
                        return fieldDefs[i];
                    }
                }
                importer.EnumMethods(ref hEnum, tdToken, fieldDefs, maxcDefs, out cDefs);
            }
            importer.CloseEnum(hEnum);
            return 0;
        }
        /// <summary>
        /// Copies buffer from unmanaged style allocated array.
        /// !!! Dealocates given source !!!
        /// </summary>
        /// <param name="sigPointer">Pointer representing array.</param>
        /// <param name="length">Length of bytes in array</param>
        /// <returns>Managed array.</returns>
        private byte[] read_sig(IntPtr sigPointer,uint length)
        {
        	List<byte> signature = new List<byte>();
			for (int i = 0; i < length; i++) {
				signature.Add(Marshal.ReadByte(sigPointer,i));
			}
			Marshal.FreeCoTaskMem(sigPointer);
			return signature.ToArray();
        }
        private byte[] read_array(IntPtr sigPointer, uint length)
        {
            List<byte> signature = new List<byte>();
            for (int i = 0; i < length; i++) {
                signature.Add(Marshal.ReadByte(sigPointer, i));
            }
            return signature.ToArray();
        }
        public byte[] GetTypeSpec(uint token)
        {
			uint len;
			IntPtr ptr;
			importer.GetTypeSpecFromToken(token,out ptr,out len);
			return read_sig(ptr,len);
        }
        public string GetModuleRefProps(uint token)
        {
        	string name;
        	IntPtr pName;
        	uint pchName, maxChName = 50;
        	pName = Marshal.AllocCoTaskMem((int)(2*maxChName + 1));
        	importer.GetModuleRefProps(token,pName,maxChName,out pchName);
        	if(pchName > maxChName){
        		Marshal.FreeCoTaskMem(pName);
        		pName = Marshal.AllocCoTaskMem((int)(2*pchName + 1));
        		importer.GetModuleRefProps(token,pName,pchName,out pchName);
        	}
    		name =  Marshal.PtrToStringUni(pName);
    		Marshal.FreeCoTaskMem(pName);
    		return name;
        }
        public MethodSpecProps GetMethodSpecProps(uint token){
        	MethodSpecProps res;
        	IntPtr buffer;
        	uint length;
        	importer.GetMethodSpecProps(token,out res.tkParent,out buffer,out length);
        	res.signature = read_sig(buffer,length);
        	return res;
        }
	}
    /// <summary>
    /// Struct containing row of MethodSpec table in metadata.
    /// </summary>
	public struct MethodSpecProps{
		public uint tkParent;
		public byte[] signature;
	}
    /// <summary>
    /// Struct containing row of Method table in metadata.
    /// </summary>
	public struct MethodProps{
		public uint mToken;
		public string mName;
		public TypeDefProps parentProps;
		public uint pClass;
		public uint pdwAttr;
		public byte[] ppvSigBlob;
		public uint pulCodeRVA;
		public uint pdwImplFlags;
	}
    /// <summary>
    /// Struct containing row of Field table in metadata.
    /// </summary>
    public struct FieldProps
    {
        public uint token;
        public string fName;
        public uint mClass;
        public uint pdwAttr;
        public byte[] sigBlob;
        public uint pdwCPlusTypeFlag;
        public byte[] ppValue;
    }
    /// <summary>
    /// Struct containing row of TypeDef table in metadata.
    /// </summary>
    public struct TypeDefProps
    {
        public uint token;
        public string tName;
        public uint flags;
        public uint extends;
    }
}