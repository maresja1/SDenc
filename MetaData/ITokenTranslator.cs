/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 11.12.2011
 * Time: 2:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

using Mono.Cecil;
namespace EnC.MetaData
{
	/// <summary>
	/// Interface for translating one token to the other token. Used for 
    /// translating tokens from one metadata scope to the other.
	/// </summary>
	public interface ITokenTranslator
	{
		MetadataToken TranslateToken(MetadataToken cecToken);
	}
}
