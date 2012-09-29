/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 9.11.2011
 * Time: 9:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace EnC.MetaData
{
	/// <summary>
	/// Empty for now, possible future purpose.
	/// </summary>
	public class MetaDataEmitter
	{
		IMetaDataEmit emitter;
		
		public IMetaDataEmit CorMetaDataEmit{
			get{ return emitter; }
		}
		
		public MetaDataEmitter(IMetaDataEmit emitter){
			this.emitter = emitter;
		}
	}
}
