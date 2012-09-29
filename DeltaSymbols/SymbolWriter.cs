/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 17.11.2011
 * Time: 12:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using Debugger;
using Debugger.Interop.CorSym;
using Debugger.Interop;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop;
using ICSharpCode.Core;
using EnC;
using EnC.MetaData;
using IStream = System.Runtime.InteropServices.ComTypes.IStream;

namespace EnC.DeltaSymbols{
    /// <summary>
    /// Class used for emitting debugging symbols and translating symbols from one assembly to other.
    /// </summary>
	public class SymbolWriterClass
	{
        /// <summary>
        /// Writer of symbols, that are used to update running assembly.
        /// </summary>
        ISymUnmanagedWriter2 mWriter;
        /// <summary>
        /// Reader of symbols written during compilation of the last version of assembly.
        /// </summary>
        ISymUnmanagedReader mReader;
        /// <summary>
        /// Mememory buffered stream with interface for COM objects.
        /// </summary>
        IStream stream;
        /// <summary>
        /// Parenting instance of EnCManager.
        /// </summary>
		EnCManager manager;
        /// <summary>
        /// Token translator, used for migrating metadata.
        /// </summary>
		ITokenTranslator translator;
        /// <summary>
        /// State enum used for describing state of writer.
        /// </summary>
		public enum WriterState{
			NotIninitialized,
			Building,
			Done
		}
        /// <summary>
        /// Actual writer state.
        /// </summary>
		public WriterState State{
			get; private set;
		}
        /// <summary>
        /// Writer of symbols, that are used to update running assembly.
        /// </summary>
        public ISymUnmanagedWriter2 CorSymWriter{
        	get{return mWriter;}
        }
        /// <summary>
        /// Reader of symbols written during compilation of the last version of assembly.
        /// </summary>
        public ISymUnmanagedReader CorSymNewReader
        {
            get { return mReader; }
        }
		public string DeltaPDBPath{
			get; private set;
		}
        protected static readonly Guid CLSID_CorSymWriter = new Guid("0AE2DEB0-F901-478b-BB9F-881EE8066788");
        public SymbolWriterClass(EnCManager manager,ITokenTranslator transl)
        {
            // Create the writer from the COM catalog
            Type writerType = Type.GetTypeFromCLSID(typeof(CorSymWriter_SxSClass).GUID);
            object comWriterObj = Activator.CreateInstance(writerType);
            Type readerType = Type.GetTypeFromCLSID(typeof(CorSymReader_SxSClass).GUID);
            object comReaderObj = Activator.CreateInstance(readerType);
            mWriter = (ISymUnmanagedWriter2)comWriterObj;
            mReader = (ISymUnmanagedReader)comReaderObj;
            this.manager = manager;
            this.stream = new CorMemStream();
            this.translator = transl;
            State = WriterState.NotIninitialized;
        }
        /// <summary>
        /// Destroys <c>mWriter</c> and <c>mReader</c> if possible.
        /// </summary>
        public void Dispose()
        {
            // If the underlying symbol API supports the Destroy method, then call it.
            ISymUnmanagedDispose disposer = mWriter as ISymUnmanagedDispose;
            if (disposer != null)
                disposer.__Destroy();
            disposer = mReader as ISymUnmanagedDispose;
            if (disposer != null)
                disposer.__Destroy();
            mWriter = null;
            mReader = null;
        }
        /// <summary>
        /// Initialize's instances of <c>ISymUnmanagedReader</c> and <c>ISymUnmanagedWriter</c> interfaces. 
        /// The first is used to read symbols from the new assembly and second for writing part of it with tokens 
        /// relative to the old assembly.
        /// </summary>
        /// <param name="oldEmitter"><c>IMetaDataEmit</c> instance relative to the old running assembly.</param>
        /// <param name="newImporter"><c>IMetaDataImport</c> instance relative to the new background-compiled assembly.</param>
        public void Initialize(IMetaDataEmit oldEmitter,IMetaDataImport newImporter)
        {	
        	DeltaPDBPath = manager.ResourceManager.TemporaryPath + "temp";
			IntPtr pFilename = Marshal.StringToCoTaskMemUni(manager.ResourceManager.NewAssemblyPath);
			IntPtr pFilename2 = Marshal.StringToCoTaskMemUni(DeltaPDBPath + ".exe");
			IntPtr pPath = Marshal.StringToCoTaskMemUni("");
            
            try
            {
                mReader.Initialize(newImporter, pFilename, pPath, null);
                mWriter.Initialize(oldEmitter, pFilename2, stream, 0);
                State = WriterState.Building;
            }
            catch (System.Exception e)
            {
                throw new TranslatingException(DeltaPDBPath + ";" + e.Message) ;
            }
            finally
            {
				Marshal.FreeCoTaskMem(pFilename2);
				Marshal.FreeCoTaskMem(pFilename);
				Marshal.FreeCoTaskMem(pPath);
			}
        }
        /// <summary>
        /// Creates symbols for method identified by <c>oldToken</c> by translating symbols 
        /// from new version of method identified by <c>newToken</c>.
        /// </summary>
        /// <param name="newToken">Identifies new version of method.</param>
        /// <param name="oldToken">Identifies old version of method.</param>
        /// <param name="placeholder">Translation definition for local variables.</param>
        public List<SequencePoint> EmitMethod(uint oldToken,uint newToken,Dictionary<int,int> placeholder=null)
        {
        	if(State != WriterState.Building){
        		throw new TranslatingException("ISym* interfaces were not initializde. (EnC)");
        	}
        	int bla = ((IMetaDataImport)manager.MetadataManager.OldEmitter.CorMetaDataEmit).IsValidToken(oldToken);
        	ISymUnmanagedMethod smMethod = mReader.__GetMethod(newToken);
        	mWriter.__OpenMethod(oldToken);
        	
        	uint seqCount = smMethod.__GetSequencePointCount();
        	
        	uint[] lines = new uint[seqCount];
        	uint[] offsets = new uint[seqCount];
        	uint[] columns = new uint[seqCount];
        	uint[] endLines = new uint[seqCount];
        	uint[] endColumns = new uint[seqCount];
        	ISymUnmanagedDocument[] documents = new ISymUnmanagedDocument[seqCount];
        	ISymUnmanagedDocumentWriter[] docWriters = new ISymUnmanagedDocumentWriter[seqCount];
        	uint cCount;
        	
        	smMethod.__GetSequencePoints(seqCount,out cCount,offsets,documents,lines,columns,endLines,endColumns);
        	for (int i = 0; i < seqCount; i++) {
        		docWriters[i] = DefineDocument(documents[i]);
        		mWriter.__DefineSequencePoints(docWriters[i],1,ref offsets[i],ref lines[i],ref columns[i],
        		                               ref endLines[i],ref endColumns[i]);
        	}
        	mWriter.__SetMethodSourceRange(docWriters[0],lines[0],columns[0],docWriters[seqCount - 1],endLines[seqCount - 1],endColumns[seqCount - 1]);
        	
        	EmitScope(smMethod.__GetRootScope(),placeholder); //
        	
        	try{
        		mWriter.__CloseMethod();
        	} catch(COMException){
        		
        	}
        	
        	return SequencePoint.StoreSequencePoints(offsets,lines,columns,endLines,endColumns);
        
        }
        /// <summary>
        /// Emits scope debugging symbols based on <c>ISymUnmanagedScope</c> insatnce, representing
        /// scope from new assembly.
        /// </summary>
        /// <param name="smScope">Scope from new version of changed assembly.</param>
        /// <param name="placeholder">Placeholder translation for local variables.</param>
        public void EmitScope(ISymUnmanagedScope smScope, Dictionary<int, int> placeholder)
        {
        	if(State != WriterState.Building){
        		throw new TranslatingException("ISym* interfaces were not initialized.");
        	}
        	uint scStartOffset = smScope.__GetStartOffset();
        	uint scEndOffset = smScope.__GetEndOffset();
        	mWriter.OpenScope(scStartOffset);
        	
        	uint localsCount = smScope.__GetLocalCount();
        	if(localsCount > 0){
        		uint read;
        		ISymUnmanagedVariable[] variables = new ISymUnmanagedVariable[localsCount];
        		smScope.__GetLocals(localsCount,out read,variables);
        		for (int i = 0; i < localsCount; i++) {
        			byte[] signature = variables[i].GetSignature();
        			Signature sig = new Signature(signature);
        			sig.Migrate(translator);
        			signature = sig.Compress();
        			
        			string name = variables[i].GetName();
        			uint addr1 = 0;//variables[i].GetAddressField1();
        			uint addr2 = 0;//variables[i].GetAddressField2();
        			uint addr3 = 0;//variables[i].GetAddressField3();
        			uint addrKind = variables[i].GetAddressKind();//variables[i].GetAddressKind();
        			if((variables[i].GetAttributes() & 1) != 1)
        			{
        				addr1 = variables[i].GetAddressField1();
        				addrKind = variables[i].GetAddressKind();
                        if (placeholder != null && placeholder.ContainsKey((int)addr1))
                        {
                            addr1 = (uint)placeholder[(int)addr1];
                        }
        			}
        			uint varStartOffset = scStartOffset;
        			uint varEndOffset = scEndOffset;
        			uint attributes = variables[i].GetAttributes();
        			
        			IntPtr pName = Marshal.StringToCoTaskMemUni(name);
        			IntPtr pSig = Marshal.AllocCoTaskMem(signature.Length);
        			Marshal.Copy(signature,0,pSig,signature.Length);
        			
        			try{
        				mWriter.DefineLocalVariable(pName,attributes,(uint)signature.Length,pSig,addrKind,
        				                            addr1,addr2,addr3,varStartOffset,varEndOffset);
        			} finally {
        				Marshal.FreeCoTaskMem(pSig);
        				Marshal.FreeCoTaskMem(pName);
        			}
        		}
        	}
        	ISymUnmanagedScope[] subScopes = smScope.GetChildren();
        	foreach(ISymUnmanagedScope subScope in subScopes){
        		EmitScope(subScope,placeholder);
        	}
        	mWriter.CloseScope(scEndOffset);
        }
        /// <summary>
        /// Defines copy of <c>document</c> in new symbols file.
        /// </summary>
        /// <param name="document">Document to be copied</param>
        /// <returns>Instance of <c>ISymUnmanagedDocumentWriter</c>, which is used to write <c>document</c> in symbole file.</returns>
        public ISymUnmanagedDocumentWriter DefineDocument(ISymUnmanagedDocument document)
        {
        	if(State != WriterState.Building){
        		throw new TranslatingException("ISym* interfaces were not initializde. (EnC)");
        	}
        	string url = document.GetURL();
        	Guid type = document.GetDocumentType();
        	Guid language = document.GetLanguage();
        	Guid langVendor = document.GetLanguageVendor();
        	IntPtr pUrl = Marshal.StringToCoTaskMemUni(url);
        	try{
        		return mWriter.__DefineDocument(pUrl,ref language,ref langVendor,ref type);
        	} finally {
        		Marshal.FreeCoTaskMem(pUrl);
        	}        	
        }
        /// <summary>
        /// Closes ISymUnmanagedWrite interface and moves state of writer to Done
        /// </summary>
        public void Close()
        {
        	if(State != WriterState.NotIninitialized && State != WriterState.Done){
        		mWriter.Close();
                Dispose();
        		State = WriterState.Done;
        	} 
        }
        /// <summary>
        /// Returns IStream instance used for filling running process's symbole store.
        /// </summary>
        /// <returns>IStream instance used for filling running process's symbole store</returns>
        public IStream GetSymbolsStream()
        {
        	if(State == WriterState.NotIninitialized){
        		throw new TranslatingException("ISym* interfaces are not ready. (EnC)");
        	} else if(State == WriterState.Building){
        		Close();
        	}
			return stream;
        }
        /// <summary>
        /// Gets methods(described by metadata token) sequence points from <c>ISymUnmanagedReader</c>.
        /// </summary>
        /// <param name="reader">Reader for getting debugging symbols.</param>
        /// <param name="mToken">Metadata token of the method.</param>
        /// <returns><c>List</c> of SequencePoint.</returns>
        public static List<SequencePoint> GetMethodSequencePoints(ISymUnmanagedReader reader,uint mToken)
        {        	
        	ISymUnmanagedMethod pMethod = reader.__GetMethod(mToken);
        	uint seqCount = pMethod.__GetSequencePointCount();
        	
        	uint[] lines = new uint[seqCount];
        	uint[] offsets = new uint[seqCount];
        	uint[] columns = new uint[seqCount];
        	uint[] endLines = new uint[seqCount];
        	uint[] endColumns = new uint[seqCount];
        	ISymUnmanagedDocument[] documents = new ISymUnmanagedDocument[seqCount];
        	ISymUnmanagedDocumentWriter[] docWriters = new ISymUnmanagedDocumentWriter[seqCount];
        	uint cCount;
        	
        	pMethod.__GetSequencePoints(seqCount,out cCount,offsets,documents,lines,columns,endLines,endColumns);
        	
        	List<SequencePoint> store = new List<SequencePoint>();
        	for (int i = 0; i < offsets.Length; i++) {
        		SequencePoint point;
        		point.Column = columns[i];
        		point.Offset = offsets[i];
        		point.Line = lines[i];
        		point.EndColumn = endColumns[i];
        		point.EndLine = endLines[i];
        		store.Add(point);
        	}
        	return store;
        }
    }
}