/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 16.11.2011
 * Time: 15:02
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
	public static class NativeMethods
	{
		[DllImport("ole32.dll")]
        public static extern int CoCreateInstance([In] ref Guid rclsid,
                                                   [In, MarshalAs(UnmanagedType.IUnknown)] Object pUnkOuter,
                                                   [In] uint dwClsContext,
                                                   [In] ref Guid riid,
                                                   [Out, MarshalAs(UnmanagedType.Interface)] out Object ppv);
	}
    /// <summary>
    /// Definition of CLR interface used by COM objects.
    /// </summary>
	[Guid("809c652e-7396-11d2-9771-00a0c9b4d50c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
	public unsafe interface IMetaDataDispenser
    {
        // We need to be able to call OpenScope, which is the 2nd vtable slot.
        // Thus we need this one placeholder here to occupy the first slot..
        void DefineScope_Placeholder();

        //STDMETHOD(OpenScope)(                   // Return code.
        //LPCWSTR     szScope,                // [in] The scope to open.
        //  DWORD       dwOpenFlags,            // [in] Open mode flags.
        //  REFIID      riid,                   // [in] The interface desired.
        //  IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
        int OpenScope([In, MarshalAs(UnmanagedType.LPWStr)] String szScope, [In] Int32 dwOpenFlags, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out Object punk);
		
	    [PreserveSig]
	    void OpenScopeOnMemory(
	        [In] IntPtr pData,
	        [In] uint cbData,
	        [In] CorOpenFlags dwOpenFlags,
	        [In] ref Guid riid,
	        [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);
//        STDMETHOD(OpenScopeOnMemory)(           // Return code.
//        LPCVOID     pData,                  // [in] Location of scope data.
//        ULONG       cbData,                 // [in] Size of the data pointed to by pData.
//        DWORD       dwOpenFlags,            // [in] Open mode flags.
//        REFIID      riid,                   // [in] The interface desired.
//        IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
		int SetOption([In] ref Guid riid,ref object pValue);
//		  STDMETHOD(SetOption)(                   // Return code.
//        REFGUID     optionid,               // [in] GUID for the option to be set.
//        const VARIANT *value) PURE;         // [in] Value to which the option is to be set.
//
//    STDMETHOD(GetOption)(                   // Return code.
//        REFGUID     optionid,               // [in] GUID for the option to be set.
//        VARIANT *pvalue) PURE;              // [out] Value to which the option is currently set.
//
//    STDMETHOD(OpenScopeOnITypeInfo)(        // Return code.
//        ITypeInfo   *pITI,                  // [in] ITypeInfo to open.
//        DWORD       dwOpenFlags,            // [in] Open mode flags.
//        REFIID      riid,                   // [in] The interface desired.
//        IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
//
//    STDMETHOD(GetCORSystemDirectory)(       // Return code.
//       __out_ecount_part_opt(cchBuffer, *pchBuffer)
//         LPWSTR      szBuffer,              // [out] Buffer for the directory name
//         DWORD       cchBuffer,             // [in] Size of the buffer
//         DWORD*      pchBuffer) PURE;       // [OUT] Number of characters returned
//
//    STDMETHOD(FindAssembly)(                // S_OK or error
//        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
//        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
//        LPCWSTR  szGlobalBin,               // [IN] optional - can be NULL
//        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
//        LPCWSTR  szName,                    // [OUT] buffer - to hold name 
//        ULONG    cchName,                   // [IN] the name buffer's size
//        ULONG    *pcName) PURE;             // [OUT] the number of characters returend in the buffer
//
//    STDMETHOD(FindAssemblyModule)(          // S_OK or error
//        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
//        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
//        LPCWSTR  szGlobalBin,               // [IN] optional - can be NULL
//        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
//        LPCWSTR  szModuleName,              // [IN] required - the name of the module
//      __out_ecount_part_opt(cchName, *pcName)
//        LPWSTR   szName,                    // [OUT] buffer - to hold name 
//        ULONG    cchName,                   // [IN]  the name buffer's size
//        ULONG    *pcName) PURE;             // [OUT] the number of characters returend in the buffer

        // Don't need any other methods.
    }
	/// <summary>
	/// Definition of CLR enum used by COM objects.
	/// </summary>
	[Flags]
    public enum CorOpenFlags : uint {
	    ofRead = 0x00000000,     // Open scope for read
	    ofWrite = 0x00000001,     // Open scope for write.
	    ofReadWriteMask = 0x00000001,     // Mask for read/write bit.
	
	    ofCopyMemory = 0x00000002,     // Open scope with memory. Ask metadata to maintain its own copy of memory.
	
	    ofReadOnly = 0x00000010,     // Open scope for read. Will be unable to QI for a IMetadataEmit* interface
	    ofTakeOwnership = 0x00000020,     // The memory was allocated with CoTaskMemAlloc and will be freed by the metadata
	
	    // These are obsolete and are ignored.
	    // ofCacheImage     =   0x00000004,     // EE maps but does not do relocations or verify image
	    // ofManifestMetadata = 0x00000008,     // Open scope on ngen image, return the manifest metadata instead of the IL metadata
	    ofNoTypeLib = 0x00000080,     // Don't OpenScope on a typelib.
	
	    // Internal bits
	    ofReserved1 = 0x00000100,     // Reserved for internal use.
	    ofReserved2 = 0x00000200,     // Reserved for internal use.
	    ofReserved3 = 0x00000400,     // Reserved for internal use.
	    ofReserved = 0xffffff40      // All the reserved bits.
    }
    // Since we're just blindly passing this interface through managed code to the Symbinder, we don't care about actually
    // importing the specific methods.
    // This needs to be public so that we can call Marshal.GetComInterfaceForObject() on it to get the
    // underlying metadata pointer.
    /// <summary>
    /// Definition of CLR struct used by COM objects.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct COR_FIELD_OFFSET
    {
        public uint ridOfField;
        public uint ulOffset;
    }
}
