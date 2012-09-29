/*
 * Vytvořeno aplikací SharpDevelop.
 * Uživatel: Honza
 * Datum: 10.2.2012
 * Čas: 21:27
 * 
 * Tento template můžete změnit pomocí Nástroje | Možnosti | Psaní kódu | Upravit standardní hlavičky souborů.
 */
using System;
using System.Collections.Generic;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Stores information about changes in body of a method.
	/// </summary>
	public class BodyChangeHistory
	{
        /// <summary>
        /// List of SourceTextChange representing changes in body.
        /// </summary>
		public List<SourceTextChange> TextChanges{
			get; private set;
		}
        /// <summary>
        /// Convertor from offset in text line and column and visa versa.
        /// </summary>
		public LineOffsetConverter Converter{
			get; private set;
		}
		public BodyChangeHistory(LineOffsetConverter conv)
		{
			TextChanges = new List<SourceTextChange>();
			Converter = conv;
		}
	}
}
