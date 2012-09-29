/*
 * Vytvořeno aplikací SharpDevelop.
 * Uživatel: Honza
 * Datum: 23.2.2012
 * Čas: 15:15
 * 
 * Tento template můžete změnit pomocí Nástroje | Možnosti | Psaní kódu | Upravit standardní hlavičky souborů.
 */
using System;

namespace EnC.MetaData
{
	/// <summary>
	/// Exception with translating metadata.
	/// </summary>
	public class TranslatingException : Exception
	{
		public TranslatingException(string text) : base(text)
		{
		}
	}
}
