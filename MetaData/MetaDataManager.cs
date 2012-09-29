/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 28.4.2011
 * Time: 20:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Debugger.Interop.CorDebug;
using Debugger.Interop.MetaData;
using EnC.EditorEvents;
using EnC.MetaData;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Project;
using Mono.Cecil;

namespace EnC.MetaData
{
    /// <summary>
    /// Transmit metadata structures from one metadata scope to the other. Optimized to use with EnC.
    /// </summary>
	public class MetaDataManager : ITokenTranslator
	{
        /// <summary>
        /// Parenting instance of EnCManager.
        /// </summary>
		EnCManager manager;
		
		/// <summary>
        /// Metadata interface for emitting metadata to running assembly.
		/// </summary>
        MetaDataEmitter oldEmitter;
        /// <summary>
        /// Metadata interface for reading metadata of running assembly.
        /// </summary>
        MetaDataImporter oldImporter;
        /// <summary>
        /// Metadata interface for reading metadata of new assembly.
        /// </summary>
		MetaDataImporter newImporter;
		
        /// <summary>
        /// Token translation cache.
        /// </summary>
		Dictionary<uint, MetadataToken> cache = new Dictionary<uint, MetadataToken>();
		
        /// <summary>
		/// Methods added before IL translating.
		/// </summary>
		Dictionary<uint, MetadataToken> addedMethods = new Dictionary<uint, MetadataToken>();
		
        // Hold dispensers to be able to close opened metadata.

		/// <summary>
		/// Dispenser to new metadata.
		/// </summary>
		IMetaDataDispenser newDispenser;
        /// <summary>
        /// Dispenser to old metadata.
        /// </summary>
		IMetaDataDispenser oldDispenser;
        /// <summary>
        /// Indicates whether interfaces for running assembly were loaded.
        /// </summary>
		bool oldMetadataLoaded = false;
		
		static private Guid importerIID = typeof(IMetaDataImport).GUID;
        static private Guid dispenserClassID = new Guid(0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8); // CLSID_CorMetaDataDispenser
        static private Guid emitterIID = typeof(IMetaDataEmit).GUID;
        static private Guid dispenserIID = typeof(IMetaDataDispenser).GUID;
        static private Guid metaDataSetENC = new Guid("2eee315c-d7db-11d2-9f80-00c04f79a0a3");

        /// <summary>
        /// Metadata interface for emitting metadata of running assembly.
        /// </summary>
        public MetaDataImporter OldImporter{
        	get{ return oldImporter; }
        }
        /// <summary>
        /// Metadata interface for reading metadata of running assembly.
        /// </summary>
        public MetaDataEmitter OldEmitter{
        	get{ return oldEmitter; }
        }
        /// <summary>
        /// Metadata interface for reading metadata of new assembly.
        /// </summary>
        public MetaDataImporter NewImporter{
        	get{ return newImporter; }
        }
        
		public MetaDataManager(EnCManager manager){
			this.manager = manager;
		}
        
        /// <summary>
        /// Updates metadata for both old and new assembly
        /// </summary>
        
        public void Update(){
        	if(!oldMetadataLoaded){
        		getInterfaces(manager.ResourceManager.CurrentModule.CorModule2);
                oldMetadataLoaded = true;
        	} else {
        		oldEmitter.CorMetaDataEmit.ResetENCLog();
        		ClearTokenCache();
        	}
        	newImporter = new MetaDataImporter(createImporter(manager.ResourceManager.NewAssemblyPath));
        }
		/// <summary>
		/// Gets access to metadata of running process.
		/// </summary>
		/// <param name="mod">Debugged module.</param>
		/// <param name="emitter">Emitter access to metadata.</param>
		/// <param name="importer">Importer access to metadata.</param>
		
		private void getInterfaces(ICorDebugModule2 mod)
        {
            Object objEmitter;
            Object objImporter;

            ((ICorDebugModule)mod).__GetMetaDataInterface(ref emitterIID, out objEmitter);
            ((ICorDebugModule)mod).__GetMetaDataInterface(ref importerIID, out objImporter);

            oldEmitter = new MetaDataEmitter(createEmitterCopy((IMetaDataEmit)objEmitter));
            oldImporter = new MetaDataImporter((IMetaDataImport)objEmitter);
        }
		
		/// <summary>
		/// Creates copy of read-only metadata of runnig assembly to enable changes.
		/// </summary>
		/// <param name="roEmitter">Read-only version of metadata.</param>
		/// <returns>Editable copy of metadata.</returns>
		
        private unsafe IMetaDataEmit createEmitterCopy(IMetaDataEmit roEmitter)
        {
            Object objDispenser, objEmitter = null;
            ulong saveSize;
            byte[] metadataBuffer;
            
            roEmitter.GetSaveSize((uint)CorSaveSize.cssAccurate,out saveSize);
            metadataBuffer = new byte[saveSize];
            roEmitter.SaveToMemory(metadataBuffer, saveSize);
            
            int hResult = NativeMethods.CoCreateInstance(ref dispenserClassID, null, 1, ref dispenserIID, out objDispenser);
            if (hResult == 0)
            {
                IMetaDataDispenser dispenser = (IMetaDataDispenser)objDispenser;

                UInt32 fVal = 0x01;
                object val = (object)fVal;
                dispenser.SetOption(ref metaDataSetENC, ref val);
                fixed(byte* buff = metadataBuffer){
                	dispenser.OpenScopeOnMemory(new IntPtr(buff), (uint)metadataBuffer.Length,CorOpenFlags.ofRead, ref emitterIID, out objEmitter);
                }
                oldDispenser = dispenser;
            }
            return (IMetaDataEmit)objEmitter;
        }
        
        /// <summary>
        /// Creaters importer acccess to metadata for new version of assembly.
        /// </summary>
        /// <param name="fileName">URL to the new assembly.</param>
        /// <returns>Importer access to metadata of the new assembly.</returns>
        
        private IMetaDataImport createImporter(string fileName)
        {
        	Object objDispenser, objImporter = null;
            int hResult = NativeMethods.CoCreateInstance(ref dispenserClassID, null, 1, ref dispenserIID, out objDispenser);
            if (hResult == 0)
            {
                IMetaDataDispenser dispenser = (IMetaDataDispenser)objDispenser;
                dispenser.OpenScope(fileName, 0, ref importerIID, out objImporter);
            	newDispenser = dispenser;
            }
            return (IMetaDataImport)objImporter;
        }
        /// <summary>
        /// Lookup TypeDefinition instance in Modules.
        /// </summary>
        /// <param name="asmDef">AssemblyDefinition of running assembly.</param>
        /// <param name="moduleName">The name of module, where TypeDefinition should be.</param>
        /// <param name="FullyQualifiedName">Full name of type.</param>
        /// <returns>TypeDefinition instance.</returns>
		public TypeDefinition FindTypeDefinition(AssemblyDefinition asmDef,string moduleName,string FullyQualifiedName)
		{
			ModuleDefinition targtMod = null;
            
            // Look up module.
           	foreach (ModuleDefinition element in asmDef.Modules) {
            	if(element.Name==moduleName){
            		targtMod = element;
            		break;
            	}
           	}            
            if(targtMod == null){
            	return null;
            }        	
            // Look up class in module.
        	foreach (TypeDefinition element in targtMod.Types) {
            	if(element.FullName == FullyQualifiedName){
        			return element;
            	}
        	}       
            return null;
		}
		/// <summary>
		/// Finds type reference in assembly.
		/// </summary>
		/// <param name="asmDef">Assembly</param>
		/// <param name="scope">Scope</param>
		/// <param name="name">Type name</param>
		/// <returns></returns>
		public TypeReference FindTypeReference(AssemblyDefinition asmDef,string scope,string fullName)
		{
            TypeReference typRef = null;
            
            // Look up module.
           	foreach (ModuleDefinition element in asmDef.Modules) {
            	if(element.TryGetTypeReference(fullName,out typRef)){
            		return typRef;
            	}
           	} 

            return SytemTypeReferenceGetter.GetSystemTypeReference(asmDef,fullName);
		}
		/// <summary>
		/// Finds <c>MethodDefinition</c> corresponding to <c>method</c> given.
		/// </summary>
		/// <param name="asmDef">Assembly to be searched in.</param>
		/// <param name="moduleName">Name of module conating method.</param>
		/// <param name="method">Method description interface.</param>
		/// <returns><c>MethodDefinition</c> corresponding to <c>method</c></returns>
		public MethodDefinition FindMethod(AssemblyDefinition asmDef,string moduleName, IMethod method)
        {
			TypeDefinition typDef = FindTypeDefinition(asmDef,moduleName,method.DeclaringType.FullyQualifiedName);
			
			if(typDef == null)
				return null;
            // Look up method with same signature in class.
        	foreach (MethodDefinition element in typDef.Methods) {
            	if(MemberComparator.SameMethodOverloads(element,method))
            		return element;
        	}
        	return null;
        }
        public FieldDefinition FindField(AssemblyDefinition asmDef, string moduleName, IField field)
        {
            TypeDefinition typDef = FindTypeDefinition(asmDef, moduleName, field.DeclaringType.FullyQualifiedName);

            if (typDef == null)
                return null;
            // Look up method with same signature in class.
            foreach (FieldDefinition element in typDef.Fields) {
                if (element.Name == field.Name)
                    return element;
            }
            return null;
        }
        /// <summary>
        /// Finds <c>PropertyDefinition</c> corresponding to <c>property</c> given.
        /// </summary>
        /// <param name="asmDef">Assembly to be searched in.</param>
        /// <param name="moduleName">Name of module conating method.</param>
        /// <param name="method">Method description interface.</param>
        /// <returns><c>PropertyDefinition</c> corresponding to <c>property</c></returns>
        public PropertyDefinition FindProperty(AssemblyDefinition asmDef, string moduleName, IProperty property)
        {
            TypeDefinition typDef = FindTypeDefinition(asmDef, moduleName, property.DeclaringType.FullyQualifiedName);

            if (typDef == null)
                return null;
            // Look up method with same signature in class.
            foreach (PropertyDefinition element in typDef.Properties) {
                if (MemberComparator.SamePropertyOverloads(element, property))
                    return element;
            }
            return null;
        }
		
		/// <summary>
		/// Registers metadata token to MemberRef table from metadata scope A to metadata scope B.
		/// </summary>
		/// <param name="token">Token in metadata scope A.</param>
		/// <returns>Token in metadata scope B.</returns>
        private MetadataToken registerMemberRef(MetadataToken token)
        {
			string name;
			byte[] sig;
            uint ptk,nToken;
        	
            NewImporter.GetMemberRefProps(token.ToUInt32(),out name,out ptk,out sig);
            MetadataToken scope = TranslateToken(new MetadataToken(ptk));
        	Signature signa = new Signature(sig);
        	signa.Migrate(this);
        	sig = signa.Compress();
        	OldEmitter.CorMetaDataEmit.DefineMemberRef(scope.ToUInt32(),name,sig,(uint)sig.Length,out nToken);
        	return new MetadataToken(nToken);
        }
        
		/// <summary>
		/// Registers metadata token to TypeRef table from metadata scope A to metadata scope B.
		/// </summary>
		/// <param name="token">Token in metadata scope A.</param>
		/// <returns>Token in metadata scope B.</returns>
        private MetadataToken registerTypeRef(MetadataToken token)
        {
			string name;
			uint scope,newToken;
        	
			NewImporter.GetTypeRefProps(token.ToUInt32(),out name,out scope);
			MetadataToken assmToken = TranslateToken(new MetadataToken(scope));
			OldEmitter.CorMetaDataEmit.DefineTypeRefByName(assmToken.ToUInt32(),name,out newToken);
			return new MetadataToken(newToken);
        }
        
		/// <summary>
		/// Registers metadata token to TypeSpec table from metadata scope A to metadata scope B.
		/// </summary>
		/// <param name="token">Token in metadata scope A.</param>
		/// <returns>Token in metadata scope B.</returns>
        private MetadataToken registerTypeSpec(MetadataToken token)
        {
        	uint nToken;
        	byte[] signature = NewImporter.GetTypeSpec(token.ToUInt32());
        	Signature sig = new Signature(signature);
        	sig.Migrate(this);
        	signature = sig.Compress();
			OldEmitter.CorMetaDataEmit.GetTokenFromTypeSpec(signature,(uint)signature.Length,out nToken);
			return new MetadataToken(nToken);
        }
        
		/// <summary>
		/// Registers any metadata token from tables
		/// (MemberRef,UserString,TypeRef,Method,TypeDef,Field,ModuleRef,AssemblyRef)
		/// from metadata scope A to metadata scope B.
		/// </summary>
		/// <param name="token">Token in metadata scope A.(new assembly)</param>
		/// <returns>Token in metadata scope B.(running assembly)</returns>
        public MetadataToken TranslateToken(MetadataToken cecToken)
        {
    		uint token = cecToken.ToUInt32();
            MetadataToken new_token;
            // Look in cache first
            if(cache.TryGetValue(token,out new_token)){
            	return new_token;
            }
    		switch(cecToken.TokenType){
	    		case TokenType.MemberRef:
	    			new_token = registerMemberRef(cecToken);
	    			break;
	    		case TokenType.String:
	    			{
	    				uint nToken;
		    			string str = NewImporter.GetUserString(token);
		    			OldEmitter.CorMetaDataEmit.DefineUserString(str,(uint)str.Length,out nToken);
		    			new_token = new MetadataToken(nToken);
	    			}
	    			break;
	    		case TokenType.TypeRef:
	    			new_token = registerTypeRef(cecToken);
	    			break;    					
                case TokenType.Method:
	    			{
	    				if(addedMethods.TryGetValue(token,out new_token)){
	    					break;
	    				}
	                    string name;uint classTk;byte[] signature;uint attr;uint rva;uint flags;
	                    NewImporter.GetMethodProps(token,out name,out classTk,out attr,out signature,out rva,out flags);
	                    MetadataToken classToken = TranslateToken(new MetadataToken(classTk));
	                    Signature sig = new Signature(signature);
	                    sig.Migrate(this);
	                    signature = sig.Compress();
	                    new_token = new MetadataToken(OldImporter.FindMethod(classToken.ToUInt32(), name, signature));
	    			}
                    break;
                case TokenType.Field:
                    {
	                    FieldProps fProps = NewImporter.GetFieldProps(token);
	                    uint newMClass = TranslateToken(new MetadataToken(fProps.mClass)).ToUInt32();
                        fProps.sigBlob = Signature.Migrate(fProps.sigBlob, this, 1);
	                    //metadata.OldEmitter.CorMetaDataEmit.DefineMemberRef(newMClass,fProps.fName,fProps.sigBlob,(uint)fProps.sigBlob.Length,out new_token);
	                    uint nCorToken = OldImporter.FindField(newMClass, fProps.fName, fProps.sigBlob);
                        if (nCorToken == 0) {
                            OldEmitter.CorMetaDataEmit.DefineField(newMClass,fProps.fName,fProps.pdwAttr,fProps.sigBlob,(uint)fProps.sigBlob.Length,fProps.pdwCPlusTypeFlag,fProps.ppValue,(uint)fProps.ppValue.Length,out nCorToken); 
                        }
                        new_token = new MetadataToken(nCorToken);
                    }
                    break;
                case TokenType.TypeDef:
                    TypeDefProps tProps = NewImporter.GetTypeDefProps(token);
                    new_token = new MetadataToken(OldImporter.FindTypeDef(tProps.tName));
                    break;
        		case TokenType.ModuleRef: 
        			{
        				uint nToken;
        				string name = NewImporter.GetModuleRefProps(token);
        				OldEmitter.CorMetaDataEmit.DefineModuleRef(name,out nToken);
        				new_token = new MetadataToken(nToken);
        			}
        			break;
        		case TokenType.AssemblyRef:
        			{
			        	int index = manager.ResourceManager.NewAssembly.Modules[0].AssemblyReferences.FindIndex(delegate(AssemblyNameReference asmRef){
							return (asmRef.MetadataToken.ToUInt32() == token);
			            });
						string scopeName = manager.ResourceManager.NewAssembly.Modules[0].AssemblyReferences[index].FullName;
						
						index = manager.ResourceManager.OldAssembly.Modules[0].AssemblyReferences.FindIndex(delegate(AssemblyNameReference asmRef){
							return (asmRef.FullName == scopeName);
			            });
						if(index == -1){
							throw new TranslatingException("Assembly reference not found, maybe it wasn't in used in original version");
						} else {
							new_token = manager.ResourceManager.OldAssembly.Modules[0].AssemblyReferences[index].MetadataToken;
						}
        			}
        			break;
        		case TokenType.TypeSpec:
        			new_token = registerTypeSpec(cecToken);
        			break;
        		case TokenType.MethodSpec:
        			uint new_token_cor;
        			MethodSpecProps props = newImporter.GetMethodSpecProps(token);
        			props.tkParent = TranslateToken(new MetadataToken(props.tkParent)).ToUInt32();
        			props.signature = Signature.Migrate(props.signature,this);
        			OldEmitter.CorMetaDataEmit.DefineMethodSpec(props.tkParent,props.signature,(uint)props.signature.Length,out new_token_cor);
        			new_token = new MetadataToken(new_token_cor);
        			break;
                case TokenType.Property:
                default:
                    throw new NotImplementedException();

    		}
            // Add to cache
            cache.Add(token,new_token);
            return new_token;
        }
        
        /// <summary>
        /// Clears token cache.
        /// </summary>
        public void ClearTokenCache()
        {
        	cache = new Dictionary<uint, MetadataToken>();
        }
        /// <summary>
        /// Used to be able to reference methods created in this round from code changed also in the same round. Must not be in cache,
        /// because it can be cleared.
        /// </summary>
        /// <param name="newToken">Token in new assembly.</param>
        /// <param name="registeredToken">Token in running assembly.</param>
        public void RegisterNewMethod(MetadataToken newToken, MetadataToken registeredToken)
        {
        	addedMethods.Add(newToken.ToUInt32(),registeredToken);
        }
	}
}