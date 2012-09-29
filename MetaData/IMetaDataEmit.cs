/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 24.4.2011
 * Time: 23:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.CompilerServices;
using Debugger.Interop.MetaData;

namespace EnC.MetaData
{

    /// <summary>
    /// Definition of CLR struct used by COM objects.
    /// </summary>
	[StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct COR_SECATTR
    {
    	public uint tkCtor;
    	public byte[] pCustomAttribute;
    	public uint cbCustomAttribute;
    	
    }
//    typedef struct COR_SECATTR {
//	    mdMemberRef     tkCtor;         // Ref to constructor of security attribute.
//	    const void      *pCustomAttribute;  // Blob describing ctor args and field/property values.
//	    ULONG           cbCustomAttribute;  // Length of the above blob.	
//	} COR_SECATTR;
    /// <summary>
    /// Definition of CLR struct used by COM objects.
    /// </summary>
	public enum CorSaveSize
	{
		cssAccurate             = 0x0000,
		cssQuick                = 0x0001,
		cssDiscardTransientCAs  = 0x0002
	}
//typedef enum CorSaveSize
//{
//    cssAccurate             = 0x0000,               // Find exact save size, accurate but slower.
//    cssQuick                = 0x0001,               // Estimate save size, may pad estimate, but faster.
//    cssDiscardTransientCAs  = 0x0002,               // remove all of the CAs of discardable types
//} CorSaveSize;

    /// <summary>
    /// Definition of CLR interface used by COM objects.
    /// </summary>
	[ComImport, InterfaceType((short) 1), Guid("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859"), ComConversionLoss]
    public interface IMetaDataEmit
    {
    	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetModuleProps([In,MarshalAs(UnmanagedType.LPWStr)]string szName);
//    
//    HRESULT SetModuleProps ( 
//        [in]  LPCWSTR     szName
//    ); 
    	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void Save([In, MarshalAs(UnmanagedType.LPWStr)]string szFile,uint dwSaveFlags);
//    
//    HRESULT Save ( 
//        [in]  LPCWSTR     szFile, 
//        [in]  DWORD       dwSaveFlags
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SaveToStream([In, MarshalAs(UnmanagedType.IUnknown)]IStream pIStream,uint dwSaveFlags);
//    
//    HRESULT SaveToStream (   
//        [in]  IStream     *pIStream,
//        [in]  DWORD       dwSaveFlags
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void GetSaveSize(uint fSave,out UInt64 pdwSaveSize);
//    
//    HRESULT GetSaveSize (    
//        [in]  CorSaveSize fSave,
//        [out] DWORD       *pdwSaveSize
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineTypeDef([In, MarshalAs(UnmanagedType.LPWStr)]string szTypeDef,uint dwTypeDefFlags,uint tkExtends,
		                   [In, MarshalAs(UnmanagedType.LPArray)]uint[] rtkImplements,out uint ptd);
//    
//    HRESULT DefineTypeDef ( 
//        [in]  LPCWSTR     szTypeDef, 
//        [in]  DWORD       dwTypeDefFlags, 
//        [in]  mdToken     tkExtends, 
//        [in]  mdToken     rtkImplements[], 
//        [out] mdTypeDef   *ptd
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineNestedType([In, MarshalAs(UnmanagedType.LPWStr)]string szTypeDef,uint dwTypeDefFlags,uint tkExtends,
		                      [In, MarshalAs(UnmanagedType.LPArray)]uint[] rtkImplements,uint tdEncloser,out uint ptd);
//    
//    HRESULT DefineNestedType ( 
//        [in]  LPCWSTR     szTypeDef,
//        [in]  DWORD       dwTypeDefFlags, 
//        [in]  mdToken     tkExtends, 
//        [in]  mdToken     rtkImplements[], 
//        [in]  mdTypeDef   tdEncloser, 
//        [out] mdTypeDef   *ptd
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetHandler([In,MarshalAs(UnmanagedType.IUnknown)]object pUnk);
//    
//    HRESULT SetHandler ( 
//        [in]  IUnknown    *pUnk
//    );		
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineMethod(uint td,[In, MarshalAs(UnmanagedType.LPWStr)]string szName,uint dwMethodFlags,
		                  [In, MarshalAs(UnmanagedType.LPArray)]byte[] pvSigBlob,uint cbSigBlob,uint ulCodeRVA,uint dwImplFlags,out uint pmd);
//    
//    HRESULT DefineMethod (    
//        [in]  mdTypeDef   td, 
//        [in]  LPCWSTR     szName, 
//        [in]  DWORD       dwMethodFlags, 
//        [in]  PCCOR_SIGNATURE pvSigBlob, 
//        [in]  ULONG       cbSigBlob, 
//        [in]  ULONG       ulCodeRVA, 
//        [in]  DWORD       dwImplFlags, 
//        [out] mdMethodDef *pmd
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineMethodImpl([In, MarshalAs(UnmanagedType.U4)]uint td,[In, MarshalAs(UnmanagedType.U4)]uint tkBody,[In, MarshalAs(UnmanagedType.U4)]uint tkDecl);
//    
//    HRESULT DefineMethodImpl ( 
//        [in]  mdTypeDef   td, 
//        [in]  mdToken     tkBody, 
//        [in]  mdToken     tkDecl
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineTypeRefByName(uint tkResolutionScope,[In, MarshalAs(UnmanagedType.LPWStr)]string szName,out uint ptr);
//    
//    HRESULT DefineTypeRefByName ( 
//        [in]  mdToken     tkResolutionScope, 
//        [in]  LPCWSTR     szName, 
//        [out] mdTypeRef   *ptr 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineImportType([In,MarshalAsAttribute(UnmanagedType.IUnknown)]object pAssemImport,
		                      [In, MarshalAs(UnmanagedType.LPArray)]uint[] pbHashValue,uint cbHashValue,[In,MarshalAsAttribute(UnmanagedType.IUnknown)]object pImport,
		                        uint tkParent,[In,MarshalAsAttribute(UnmanagedType.IUnknown)]object pAssemEmit,out uint ptr);
//    
//    HRESULT DefineImportType ( 
//        [in]  IMetaDataAssemblyImport *pAssemImport, 
//        [in]  const void  *pbHashValue, 
//        [in]  ULONG       cbHashValue,  
//        [in]  IMetaDataImport *pImport, 
//        [in]  mdTypeDef   tdImport, 
//        [in]  IMetaDataAssemblyEmit *pAssemEmit, 
//        [out] mdTypeRef   *ptr
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineMemberRef(uint tkImport,[In, MarshalAs(UnmanagedType.LPWStr)]string szName,[In, MarshalAs(UnmanagedType.LPArray)]byte[] pvSigBlob
		                     ,uint cbSigBlob,out uint pmr);
//    
//    HRESULT DefineMemberRef ( 
//        [in]  mdToken     tkImport, 
//        [in]  LPCWSTR     szName, 
//        [in]  PCCOR_SIGNATURE pvSigBlob, 
//        [in]  ULONG       cbSigBlob, 
//        [out] mdMemberRef *pmr 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineImportMember([In,MarshalAsAttribute(UnmanagedType.IUnknown)]object pAssemImport,
		                        [In, MarshalAs(UnmanagedType.LPArray)]uint[] pbHashValue,uint cbHashValue,[In,MarshalAsAttribute(UnmanagedType.IUnknown)]object pAssemEmit,
		                        uint tkParent,out uint pmr);
//    
//    HRESULT DefineImportMember ( 
//        [in]  IMetaDataAssemblyImport *pAssemImport, 
//        [in]  const void  *pbHashValue, 
//        [in]  ULONG       cbHashValue,
//        [in]  IMetaDataImport *pImport, 
//        [in]  mdToken     mbMember, 
//        [in]  IMetaDataAssemblyEmit *pAssemEmit, 
//        [in]  mdToken     tkParent, 
//        [out] mdMemberRef *pmr 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineEvent(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szEvent,
		                 uint dwEventFlags,uint tkEventType,uint mdAddOn,uint mdRemoveOn,
		                 uint mdFire,[In, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rmdOtherMethods,
		                 out uint pmdEvent);
//    
//    HRESULT DefineEvent (    
//        [in]  mdTypeDef   td, 
//        [in]  LPCWSTR     szEvent, 
//        [in]  DWORD       dwEventFlags, 
//        [in]  mdToken     tkEventType, 
//        [in]  mdMethodDef mdAddOn, 
//        [in]  mdMethodDef mdRemoveOn, 
//        [in]  mdMethodDef mdFire, 
//        [in]  mdMethodDef rmdOtherMethods[], 
//        [out] mdEvent     *pmdEvent 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetClassLayout(uint td,uint dwPackSize,[MarshalAs(UnmanagedType.LPArray)]COR_FIELD_OFFSET[] rFieldOffsets,uint ulClassSize);
//    
//    HRESULT SetClassLayout (   
//        [in]  mdTypeDef   td, 
//        [in]  DWORD       dwPackSize, 
//        [in]  COR_FIELD_OFFSET rFieldOffsets[], 
//        [in]  ULONG       ulClassSize 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DeleteClassLayout(uint td);
//    
//    HRESULT DeleteClassLayout (
//        [in]  mdTypeDef   td
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetFieldMarshal(uint tk, [MarshalAs(UnmanagedType.LPArray)]byte[] pvNativeType,uint cbNativeType);
//    
//    HRESULT SetFieldMarshal (    
//        [in]  mdToken     tk, 
//        [in]  PCCOR_SIGNATURE pvNativeType, 
//        [in]  ULONG       cbNativeType 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DeleteFieldMarshal(uint tk);
//    
//    HRESULT DeleteFieldMarshal (
//        [in]  mdToken     tk
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefinePermissionSet(uint tk,uint dwAction,[In, MarshalAs(UnmanagedType.LPArray)]byte[] pvPermission,uint cbPermission,out uint ppm);
//    
//    HRESULT DefinePermissionSet (    
//        [in]  mdToken     tk, 
//        [in]  DWORD       dwAction, 
//        [in]  void const  *pvPermission, 
//        [in]  ULONG       cbPermission, 
//        [out] mdPermission *ppm 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetRVA(uint md,uint ulRVA);
//    
//    HRESULT SetRVA ( 
//        [in]  mdMethodDef md, 
//        [in]  ULONG       ulRVA 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void GetTokenFromSig([In, MarshalAs(UnmanagedType.LPArray)]byte[] pvSig,uint cbSig,out uint pmsig);
//    
//    HRESULT GetTokenFromSig (   
//        [in]  PCCOR_SIGNATURE pvSig, 
//        [in]  ULONG       cbSig, 
//        [out] mdSignature *pmsig 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineModuleRef([In, MarshalAs(UnmanagedType.LPWStr)]string szName,out uint pmur);
//    
//    HRESULT DefineModuleRef (   
//        [in]  LPCWSTR     szName, 
//        [out] mdModuleRef *pmur 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetParent(uint mr,uint tk);
//    
//    HRESULT SetParent ( 
//        [in]  mdMemberRef mr, 
//        [in]  mdToken     tk 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void GetTokenFromTypeSpec([In, MarshalAs(UnmanagedType.LPArray)]byte[] pvSig,uint cbSig,out uint ptypespec);
//    
//    HRESULT GetTokenFromTypeSpec ( 
//        [in]  PCCOR_SIGNATURE pvSig, 
//        [in]  ULONG       cbSig, 
//        [out] mdTypeSpec *ptypespec 
//    ); 
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SaveToMemory([In, MarshalAs(UnmanagedType.LPArray)]byte[] pbData,UInt64 cbData);
//    
//    HRESULT SaveToMemory (   
//        [in]  void        *pbData, 
//        [in]  ULONG       cbData 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineUserString([In, MarshalAs(UnmanagedType.LPWStr)]string szString,uint cchString, out uint pstk);
//    
//    HRESULT DefineUserString ( 
//        [in]  LPCWSTR szString, 
//        [in]  ULONG       cchString, 
//        [out] mdString    *pstk 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DeleteToken(uint tkObj);
//    
//    HRESULT DeleteToken ( 
//        [in]  mdToken     tkObj 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetMethodProps(uint md,uint dwMethodFlags,uint ulCodeRVA,uint dwImplFlags);
//    
//    HRESULT SetMethodProps ( 
//        [in]  mdMethodDef md, 
//        [in]  DWORD       dwMethodFlags,
//        [in]  ULONG       ulCodeRVA, 
//        [in]  DWORD       dwImplFlags 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetTypeDefProps(uint td,uint dwTypeDefFlags,uint tkExtends,[In,MarshalAs(UnmanagedType.SafeArray)]uint[] rtkImplements);
//    
//    HRESULT SetTypeDefProps (
//        [in]  mdTypeDef   td, 
//        [in]  DWORD       dwTypeDefFlags, 
//        [in]  mdToken     tkExtends, 
//        [in]  mdToken     rtkImplements[] 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetEventProps(uint ev,uint dwEventFlags,uint tkEventType,uint mdAddOn,uint mdRemoveOn,uint mdFire,[MarshalAs(UnmanagedType.SafeArray)]uint[] rmdOtherMethods);
//    
//    HRESULT SetEventProps (  
//        [in]  mdEvent     ev, 
//        [in]  DWORD       dwEventFlags, 
//        [in]  mdToken     tkEventType, 
//        [in]  mdMethodDef mdAddOn, 
//        [in]  mdMethodDef mdRemoveOn, 
//        [in]  mdMethodDef mdFire, 
//        [in]  mdMethodDef rmdOtherMethods[] 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetPermissionSetProps(uint tk,uint dwAction,[In,MarshalAs(UnmanagedType.LPArray)]byte[] pvPermission,uint cbPermission,out uint ppm);
//    
//    HRESULT SetPermissionSetProps ( 
//        [in]  mdToken     tk, 
//        [in]  DWORD       dwAction, 
//        [in]  void const  *pvPermission, 
//        [in]  ULONG       cbPermission, 
//        [out] mdPermission *ppm 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefinePinvokeMap(uint tk,uint dwMappingFlags,[In, MarshalAs(UnmanagedType.LPWStr)]string szImportName,uint mrImportDLL);
//    
//    HRESULT DefinePinvokeMap ( 
//        [in]  mdToken     tk, 
//        [in]  DWORD       dwMappingFlags, 
//        [in]  LPCWSTR     szImportName, 
//        [in]  mdModuleRef mrImportDLL 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetPinvokeMap(uint tk,uint dwMappingFlags,[In,MarshalAs(UnmanagedType.LPWStr)]string szImportName,uint mrImportDLL);
//    
//    HRESULT SetPinvokeMap ( 
//        [in]  mdToken     tk, 
//        [in]  DWORD       dwMappingFlags,
//        [in]  LPCWSTR     szImportName, 
//        [in]  mdModuleRef mrImportDLL 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DeletePinvokeMap(uint tk);
//    
//    HRESULT DeletePinvokeMap ( 
//        [in]  mdToken     tk 
//    );
    	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineCustomAttribute(uint tkObj,uint tkType,[In, MarshalAs(UnmanagedType.LPArray)]uint[] pCustomAttribute,uint cbCustomAttribute,out uint pcv);
//    HRESULT DefineCustomAttribute ( 
//        [in]  mdToken     tkObj, 
//        [in]  mdToken     tkType, 
//        [in]  void const  *pCustomAttribute, 
//        [in]  ULONG       cbCustomAttribute, 
//        [out] mdCustomAttribute *pcv 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetCustomAttributeValue(uint pcv,[MarshalAs(UnmanagedType.LPArray)]byte[] pCustomAttribute,uint cbCustomAttribute);
//    
//    HRESULT SetCustomAttributeValue ( 
//        [in]  mdCustomAttribute pcv, 
//        [in]  void const  *pCustomAttribute,  
//        [in]  ULONG       cbCustomAttribute 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void DefineField(uint td,[In, MarshalAs(UnmanagedType.LPWStr)] string szName,
		                 uint dwFieldFlags,[In, MarshalAs(UnmanagedType.LPArray)]byte[] pvSigBlob,uint cbSigBlob,uint dwCPlusTypeFlag,
		                 [In, MarshalAs(UnmanagedType.LPArray)]byte[] pValue,uint cchValue,out uint pmd);
//    
//    HRESULT DefineField ( 
//        [in]  mdTypeDef   td, 
//        [in]  LPCWSTR     szName, 
//        [in]  DWORD       dwFieldFlags, 
//        [in]  PCCOR_SIGNATURE pvSigBlob, 
//        [in]  ULONG       cbSigBlob, 
//        [in]  DWORD       dwCPlusTypeFlag, 
//        [in]  void const  *pValue, 
//        [in]  ULONG       cchValue, 
//        [out] mdFieldDef  *pmd 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineProperty(uint md,[In, MarshalAs(UnmanagedType.LPWStr)]string szProperty,uint dwPropFlags,[In, MarshalAs(UnmanagedType.LPArray)]byte[] pvSig,
		                    uint cbSig,uint dwCPlusTypeFlag,[In, MarshalAs(UnmanagedType.LPArray)]byte[] pValue,uint cchValue,uint mdSetter,uint mdGetter,
		                    [In, MarshalAs(UnmanagedType.LPArray)]uint[] rmdOtherMethods,out uint pmdProp);
//    
//    HRESULT DefineProperty ( 
//        [in]  mdTypeDef   td, 
//        [in]  LPCWSTR     szProperty, 
//        [in]  DWORD       dwPropFlags, 
//        [in]  PCCOR_SIGNATURE pvSig, 
//        [in]  ULONG       cbSig, 
//        [in]  DWORD       dwCPlusTypeFlag, 
//        [in]  void const  *pValue, 
//        [in]  ULONG       cchValue, 
//        [in]  mdMethodDef mdSetter, 
//        [in]  mdMethodDef mdGetter, 
//        [in]  mdMethodDef rmdOtherMethods[], 
//        [out] mdProperty  *pmdProp 
//    );	
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineParam(uint md,uint ulParamSeq,[In, MarshalAs(UnmanagedType.LPWStr)]string szName,uint dwParamFlags,
		                 uint dwCPlusTypeFlag,[In, MarshalAs(UnmanagedType.LPArray)]byte[] pValue,uint cchValue,out uint ppd);
//    HRESULT DefineParam (
//        [in]  mdMethodDef md, 
//        [in]  ULONG       ulParamSeq, 
//        [in]  LPCWSTR     szName, 
//        [in]  DWORD       dwParamFlags, 
//        [in]  DWORD       dwCPlusTypeFlag, 
//        [in]  void const  *pValue,
//        [in]  ULONG       cchValue, 
//        [out] mdParamDef  *ppd 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetFieldProps(uint fd,uint dwFieldFlags,uint dwCPlusTypeFlag,[MarshalAs(UnmanagedType.LPArray)]byte[] pValue,uint cchValue);
//    
//    HRESULT SetFieldProps (  
//        [in]  mdFieldDef  fd, 
//        [in]  DWORD       dwFieldFlags, 
//        [in]  DWORD       dwCPlusTypeFlag, 
//        [in]  void const  *pValue, 
//        [in]  ULONG       cchValue 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetPropertyProps(uint pr,uint dwPropFlags,uint dwCPlusTypeFlag,[In,MarshalAs(UnmanagedType.LPArray)]byte[] pValue,
		                      uint cchValue,uint mdSetter,uint mdGetter,[In,MarshalAs(UnmanagedType.SafeArray)]uint[] rmdOtherMethods);
//    
//    HRESULT SetPropertyProps ( 
//        [in]  mdProperty  pr, 
//        [in]  DWORD       dwPropFlags, 
//        [in]  DWORD       dwCPlusTypeFlag, 
//        [in]  void const  *pValue, 
//        [in]  ULONG       cchValue, 
//        [in]  mdMethodDef mdSetter, 
//        [in]  mdMethodDef mdGetter, 
//        [in]  mdMethodDef rmdOtherMethods[] 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetParamProps(uint pd,[In,MarshalAs(UnmanagedType.LPWStr)]string szName,uint dwParamFlags,
		                   uint dwCPlusTypeFlag,[In,MarshalAs(UnmanagedType.LPArray)]byte[] pValue,uint cchValue);
//    
//    HRESULT SetParamProps ( 
//        [in]  mdParamDef  pd, 
//        [in]  LPCWSTR     szName, 
//        [in]  DWORD       dwParamFlags, 
//        [in]  DWORD       dwCPlusTypeFlag, 
//        [in]  void const  *pValue, 
//        [in]  ULONG       cchValue 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineSecurityAttributeSet(uint tkObj,[In, MarshalAs(UnmanagedType.LPArray)]COR_SECATTR[] rSecAttrs,uint cSecAttrs,out uint pulErrorAttr);
//    
//    HRESULT DefineSecurityAttributeSet ( 
//        [in]  mdToken     tkObj, 
//        [in]  COR_SECATTR rSecAttrs[], 
//        [in]  ULONG       cSecAttrs, 
//        [out] ULONG       *pulErrorAttr 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
    	void ApplyEditAndContinue ([In,MarshalAsAttribute(UnmanagedType.IUnknown)]object pImport);
//	HRESULT ApplyEditAndContinue ( 
//	    [in]  IUnknown    *pImport
//	);
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void TranslateSigWithScope([In,MarshalAs(UnmanagedType.IUnknown)]object pAssemImport,[In,MarshalAs(UnmanagedType.LPArray)]byte[] pbHashValue,
		                           uint cbHashValue,[In,MarshalAs(UnmanagedType.IUnknown)]IMetaDataImport import,[In,MarshalAs(UnmanagedType.LPArray)]byte[] pbSigBlob,
		                           uint cbSigBlob,[In,MarshalAs(UnmanagedType.IUnknown)]object pAssemEmit,[In,MarshalAs(UnmanagedType.IUnknown)]IMetaDataEmit emit,
		                           [In,MarshalAs(UnmanagedType.LPArray)]byte[] pvTranslatedSig,uint cbTranslatedSigMax,out uint pcbTranslatedSig);
//    
//    HRESULT TranslateSigWithScope ( 
//        [in]  IMetaDataAssemblyImport *pAssemImport, 
//        [in]  const void  *pbHashValue, 
//        [in]  ULONG       cbHashValue, 
//        [in]  IMetaDataImport *import, 
//        [in]  PCCOR_SIGNATURE pbSigBlob, 
//        [in]  ULONG       cbSigBlob,
//        [in]  IMetaDataAssemblyEmit *pAssemEmit, 
//        [in]  IMetaDataEmit *emit, 
//        [out] PCOR_SIGNATURE pvTranslatedSig, 
//        [in]  ULONG       cbTranslatedSigMax, 
//        [out] ULONG       *pcbTranslatedSig 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetMethodImplFlags(uint md,uint dwImplFlags);
//    
//    HRESULT SetMethodImplFlags ( 
//        [in]  mdMethodDef md, 
//        [in]  DWORD       dwImplFlags 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetFieldRVA(uint fd,uint ulRVA);
//    
//    HRESULT SetFieldRVA ( 
//        [in]  mdFieldDef  fd, 
//        [in]  ULONG       ulRVA 
//    );		
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void Merge([In, MarshalAs(UnmanagedType.IUnknown)]IMetaDataImport pImport,[In, MarshalAs(UnmanagedType.IUnknown)]object pHostMapToken,
		           [In, MarshalAs(UnmanagedType.IUnknown)]object pHandler);
//    
//    HRESULT Merge ( 
//        [in]  IMetaDataImport *pImport, 
//        [in]  IMapToken   *pHostMapToken, 
//        [in]  IUnknown    *pHandler 
//    );
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void MergeEnd();
//    
//    HRESULT MergeEnd ();
// ----------------------------
//	 IMetaDataEmit2
// ----------------------------

    
    	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineMethodSpec(uint tkParent,[In,MarshalAs(UnmanagedType.LPArray)]byte[] pvSigBlob,uint cbSigBlob,out uint pmi);
//		STDMETHOD(DefineMethodSpec)(
//	        mdToken     tkParent,               // [IN] MethodDef or MemberRef
//	        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
//	        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob    
//	        mdMethodSpec *pmi) PURE;            // [OUT] method instantiation token
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void GetDeltaSaveSize(CorSaveSize fSave,out uint pdwSaveSize);
//	    STDMETHOD(GetDeltaSaveSize)(            // S_OK or error.
//	        CorSaveSize fSave,                  // [IN] cssAccurate or cssQuick.
//	        DWORD       *pdwSaveSize) PURE;     // [OUT] Put the size here.
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SaveDelta([In,MarshalAs(UnmanagedType.LPWStr)]string szFile,uint dwSaveFlags);
//	    STDMETHOD(SaveDelta)(                   // S_OK or error.
//	        LPCWSTR     szFile,                 // [IN] The filename to save to.
//	        DWORD       dwSaveFlags) PURE;      // [IN] Flags for the save.
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SaveDeltaToStream([In,MarshalAs(UnmanagedType.IUnknown)]IStream pIStream,uint dwSaveFlags);
//	    STDMETHOD(SaveDeltaToStream)(           // S_OK or error.
//	        IStream     *pIStream,              // [IN] A writable stream to save to.
//	        DWORD       dwSaveFlags) PURE;      // [IN] Flags for the save.
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SaveDeltaToMemory([In,MarshalAs(UnmanagedType.LPArray)]byte[] pbData,uint cbData);
//	    STDMETHOD(SaveDeltaToMemory)(           // S_OK or error.
//	        void        *pbData,                // [OUT] Location to write data.
//	        ULONG       cbData) PURE;           // [IN] Max size of data buffer.
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void DefineGenericParam(uint tk,uint ulParamSeq,uint dwParamFlags,[In,MarshalAs(UnmanagedType.LPWStr)]string szName,
		                        uint reserved,[In,MarshalAs(UnmanagedType.SafeArray)]uint[] rtkConstraints,out uint pgp);
//	    STDMETHOD(DefineGenericParam)(          // S_OK or error.
//	        mdToken      tk,                    // [IN] TypeDef or MethodDef
//	        ULONG        ulParamSeq,            // [IN] Index of the type parameter
//	        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
//	        LPCWSTR      szname,                // [IN] Name
//	        DWORD        reserved,              // [IN] For future use (e.g. non-type parameters)
//	        mdToken      rtkConstraints[],      // [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
//	        mdGenericParam *pgp) PURE;          // [OUT] Put GenericParam token here
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void SetGenericParamProps(uint gp,uint dwParamFlags,[In,MarshalAs(UnmanagedType.LPWStr)]string szName,uint reserved,
		                          [In,MarshalAs(UnmanagedType.SafeArray)]uint[] rtkConstraints);
//	    STDMETHOD(SetGenericParamProps)(        // S_OK or error.
//	        mdGenericParam gp,                  // [IN] GenericParam
//	        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
//	        LPCWSTR      szName,                // [IN] Optional name
//	        DWORD        reserved,              // [IN] For future use (e.g. non-type parameters)
//	        mdToken      rtkConstraints[]) PURE;// [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
		void ResetENCLog();
//	    STDMETHOD(ResetENCLog)() PURE;          // S_OK or error.

    }
}
