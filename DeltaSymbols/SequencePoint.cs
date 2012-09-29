/*
 * Vytvořeno aplikací SharpDevelop.
 * Uživatel: Honza
 * Datum: 8.2.2012
 * Čas: 22:37
 * 
 * Tento template můžete změnit pomocí Nástroje | Možnosti | Psaní kódu | Upravit standardní hlavičky souborů.
 */
using System;
using System.Collections.Generic;

namespace EnC.DeltaSymbols
{
	/// <summary>
    /// Struct for storing sequence point.
	/// </summary>
	public struct SequencePoint{
		public uint Line;
		public uint Column;
		public uint Offset;
		public uint EndLine;
		public uint EndColumn;
        /// <summary>
        /// Converts arrays of parts of sequence point to <c>List</c> of instances of this structure.
        /// </summary>
		public static List<SequencePoint> StoreSequencePoints(uint[] offsets,uint[] lines,uint[] columns,uint[] endLines,uint[] endColumns)
        {
        	List<SequencePoint> store = new List<SequencePoint>();
        	for (int i = 0; i < offsets.Length; i++) {
        		SequencePoint point;
        		point.Column = columns[i];
        		point.Offset = offsets[i];
        		point.Line = lines[i];
        		point.EndColumn =endColumns[i];
        		point.EndLine = endLines[i];
        		store.Add(point);
        	}
        	return store;
        }
	}
}
