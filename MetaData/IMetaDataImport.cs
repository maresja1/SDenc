/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 24.4.2011
 * Time: 22:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.CompilerServices;

namespace EnC.MetaData
{
    /// <summary>
    /// Definition of CLR interface used by COM objects.
    /// </summary>
    [ComImport, InterfaceType((short) 1), Guid("7DAC8207-D3AE-4C75-9B67-92801A497D44"), ComConversionLoss]
    public unsafe interface IMetaDataImport
    {
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void CloseEnum(IntPtr hEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void CountEnum(IntPtr hEnum, out uint pulCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void ResetEnum(IntPtr hEnum, uint ulPos);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumTypeDefs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rTypeDefs, uint cMax, out uint pcTypeDefs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumInterfaceImpls(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rImpls, uint cMax, out uint pcImpls);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumTypeRefs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rTypeRefs, uint cMax, out uint pcTypeRefs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindTypeDefByName([In, MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, uint tkEnclosingClass, out uint ptd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetScopeProps(IntPtr szName, uint cchName, out uint pchName, out Guid pmvid);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetModuleFromScope(out uint pmd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetTypeDefProps(uint td,[MarshalAs(UnmanagedType.LPArray)]byte[] szTypeDef, uint cchTypeDef, out uint pchTypeDef, out uint pdwTypeDefFlags, out uint ptkExtends);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetInterfaceImplProps(uint iiImpl, out uint pClass, out uint ptkIface);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetTypeRefProps(uint tr, out uint ptkResolutionScope,[MarshalAs(UnmanagedType.LPArray)]byte[] szName, uint cchName, out uint pchName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppIScope, out uint ptd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMembers(ref IntPtr phEnum, uint cl, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMembers, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMembersWithName(ref IntPtr phEnum, uint cl, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMembers, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethods(ref IntPtr phEnum, uint cl, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethods, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethodsWithName(ref IntPtr phEnum, uint cl, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethods, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumFields(ref IntPtr phEnum, uint cl, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rFields, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumFieldsWithName(ref IntPtr phEnum, uint cl, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rFields, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumParams(ref IntPtr phEnum, uint mb, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rParams, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMemberRefs(ref IntPtr phEnum, uint tkParent, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMemberRefs, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethodImpls(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethodBody, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethodDecl, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumPermissionSets(ref IntPtr phEnum, uint tk, uint dwActions, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rPermission, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindMember(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindMethod(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindField(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindMemberRef(uint td, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, uint cbSigBlob, out uint pmr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMethodProps(uint mb, out uint pClass, [MarshalAs(UnmanagedType.LPArray)]byte[] szMethod, uint cchMethod, out uint pchMethod, out uint pdwAttr,out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMemberRefProps(uint mr, out uint ptk, [MarshalAs(UnmanagedType.LPArray)]byte[] szMember, uint cchMember, out uint pchMember, out byte* ppvSigBlob, out uint pbSig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumProperties(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rProperties, uint cMax, out uint pcProperties);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumEvents(ref IntPtr phEnum, uint td, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rEvents, uint cMax, out uint pcEvents);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetEventProps(uint ev, out uint pClass, IntPtr  szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumMethodSemantics(ref IntPtr phEnum, uint mb, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rEventProp, uint cMax, out uint pcEventProp);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMethodSemantics(uint mb, uint tkEventProp, out uint pdwSemanticsFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetClassLayout(uint td, out uint pdwPackSize, [Out] COR_FIELD_OFFSET[] rFieldOffset, uint cMax, out uint pcFieldOffset, out uint pulClassSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetFieldMarshal(uint tk, out IntPtr ppvNativeType, out uint pcbNativeType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission, out uint pcbPermission);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetSigFromToken(uint mdSig, out byte* ppvSig, out uint pcbSig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetModuleRefProps(uint mur, IntPtr szName, uint cchName, out uint pchName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumModuleRefs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rModuleRefs, uint cMax, out uint pcModuleRefs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig, out uint pcbSig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetNameFromToken(uint tk, IntPtr pszUtf8NamePtr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumUnresolvedMethods(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rMethods, uint cMax, out uint pcTokens);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetUserString(uint stk, [MarshalAs(UnmanagedType.LPArray)]byte[] szString, uint cchString, out uint pchString);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetPinvokeMap(uint tk, out uint pdwMappingFlags, IntPtr szImportName, uint cchImportName, out uint pchImportName, out uint pmrImportDLL);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumSignatures(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rSignatures, uint cMax, out uint pcSignatures);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumTypeSpecs(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rTypeSpecs, uint cMax, out uint pcTypeSpecs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumUserStrings(ref IntPtr phEnum, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rStrings, uint cMax, out uint pcStrings);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetParamForMethodIndex(uint md, uint ulParamSeq, out uint ppd);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumCustomAttributes(ref IntPtr phEnum, uint tk, uint tkType, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rCustomAttributes, uint cMax, out uint pcCustomAttributes);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob, out uint pcbSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void FindTypeRef(uint tkResolutionScope, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, out uint ptr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetFieldProps(uint mb, out uint pClass, [MarshalAs(UnmanagedType.LPArray)]byte[] szName, uint cchField, out uint pchField, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetPropertyProps(uint prop, out uint pClass, IntPtr szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetParamProps(uint tk, out uint pmd, out uint pulSequence, IntPtr szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetCustomAttributeByName(uint tkObj, [In, MarshalAs(UnmanagedType.LPWStr)] string szName, out IntPtr ppData, out uint pcbData);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsValidToken(uint tk);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetNestedClassProps(uint tdNestedClass, out uint ptdEnclosingClass);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void GetNativeCallConvFromSig([In] IntPtr pvSig, uint cbSig, out uint pCallConv);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void IsGlobal(uint pd, out int pbGlobal);
        
        // IMetaDataImport2
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EnumGenericParams(ref IntPtr hEnum, uint tk, [Out, MarshalAsAttribute(UnmanagedType.LPArray)] uint[] rGenericParams, uint cMax, out uint pcGenericParams);    
        void GetGenericParamProps();
        void GetMethodSpecProps(uint tkMethodsSpec,out uint tkParent,out IntPtr signatue,out uint sigLength);
        void EnumGenericParamConstraints();
        void GetGenericParamConstraintProps();
        void GetPEKind();
        void GetVersionString();
        void EnumMethodSpecs();
    }
}