/*z
 * Created by SharpDevelop.
 * User: Honza
 * Date: 18.11.2011
 * Time: 10:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace EnC{
    /// <summary>
    /// Implementation of IStream. Used for buffering data from/to COM objects.
    /// </summary>
	public class CorMemStream : MemoryStream, IStream {

		public CorMemStream():base(){
			
		}
        /// <summary>
        /// Load data from some <c>Stream</c>.
        /// </summary>
        /// <param name="stream"><c>Stream</c> source.</param>
		public void CopyFromStream(Stream stream)
		{
			byte[] buffer = new byte[stream.Length];
			stream.Read(buffer,0,buffer.Length);
			this.Write(buffer,0,buffer.Length);
		}
		// convenience method for writing Strings to the stream
        /// <summary>
        /// Writes <c>string</c> to the end of the stream.
        /// </summary>
        /// <param name="s"><c>string</c> to be written.</param>
		public void Write(string s) {
			System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
			byte[] pv = encoding.GetBytes(s);
			Write(pv, 0, pv.GetLength(0));
		}
        /// <summary>
        /// Placeholder for implementing IStream. Not used in current context.
        /// </summary>
		public void Clone(out IStream ppstm) {
			throw new NotImplementedException();
		}
        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="pv">Byte array used for storing bytes from stream.</param>
        /// <param name="cb">Count of bytes to be read.</param>
        /// <param name="pcbRead">Pointer to place, where count of bytes should be stored after reading.</param>
		public void Read(byte[] pv, int cb, System.IntPtr pcbRead) {
			int bytesRead = Read(pv, 0, cb);
			if(pcbRead != IntPtr.Zero) Marshal.WriteInt32(pcbRead, bytesRead);
		}
        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="pv">Byte array containing data for writing data to the stream.</param>
        /// <param name="cb">Count of bytes to written.</param>
        /// <param name="pcbWritten">Pointer for storing count of bytes written to the stream.</param>
		public void Write(byte[] pv, int cb, System.IntPtr pcbWritten) {
			Write(pv, 0, cb);
			if(pcbWritten != IntPtr.Zero) Marshal.WriteInt32(pcbWritten,cb);
		}
		/// <summary>
		/// Seek in the stream.
		/// </summary>
		/// <param name="dlibMove">Offset from origin position.</param>
		/// <param name="dwOrigin">Origin position: 0 - begin, 1 - current, 2 - end.</param>
		/// <param name="plibNewPosition">Pointer to store new position in stream.</param>
		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
		{
			SeekOrigin origin = (SeekOrigin)dwOrigin;
			long pos = base.Seek(dlibMove, origin);
			if(plibNewPosition != IntPtr.Zero) Marshal.WriteInt64(plibNewPosition, pos);
		}
		/// <summary>
		/// Set size of the stream.
		/// </summary>
		/// <param name="libNewSize">New size.</param>
		public void SetSize(long libNewSize)
		{
			this.SetLength(libNewSize);
		}
        /// <summary>
        /// Placeholder for implementing IStream. Not used in current context.
        /// </summary>
		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
		{
		}
        /// <summary>
        /// Placeholder for implementing IStream. Not used in current context.
        /// </summary>		
		public void Commit(int grfCommitFlags)
		{
		}
        /// <summary>
        /// Placeholder for implementing IStream. Not used in current context.
        /// </summary>		
		public void Revert()
		{
		}
        /// <summary>
        /// Placeholder for implementing IStream. Not used in current context.
        /// </summary>		
		public void LockRegion(long libOffset, long cb, int dwLockType)
		{
		}
        /// <summary>
        /// Placeholder for implementing IStream. Not used in current context.
        /// </summary>		
		public void UnlockRegion(long libOffset, long cb, int dwLockType)
		{
		}
	    /// <summary>
	    /// Get description of the stream.
	    /// </summary>
	    /// <param name="pstatstg">Here is result of method when done.</param>
	    /// <param name="grfStatFlag">Not used, just a placeholder.</param>
		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
		{
			pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG();
			pstatstg.grfLocksSupported = 0;
			pstatstg.cbSize = this.Length;
		}
	}
}