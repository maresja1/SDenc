/*
 * Vytvořeno aplikací SharpDevelop.
 * Uživatel: Honza
 * Datum: 10.2.2012
 * Čas: 12:51
 * 
 * Tento template můžete změnit pomocí Nástroje | Možnosti | Psaní kódu | Upravit standardní hlavičky souborů.
 */
using System;
using System.Collections.Generic;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Editor;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Class containing information for converting document offset to 
    /// position described by number of line and column.
	/// </summary>
	public class LineOffsetConverter
	{
        /// <summary>
        /// Starting offset for each line from <c>startLine</c>.
        /// </summary>
		List<int> lineOffsets;
        /// <summary>
        /// Starting line, from which converter can function.
        /// </summary>
		int startLine;
		private LineOffsetConverter()
		{
			lineOffsets = new List<int>();
			startLine = 0;
		}
		public LineOffsetConverter(int pStartLine, List<int> pLineOffsets)
		{
			lineOffsets = pLineOffsets;
			startLine = pStartLine;
		}
        /// <summary>
        /// Converts position(line,column) to offset.
        /// </summary>
        /// <param name="line">Number of line.</param>
        /// <param name="column">Number of column.</param>
        /// <returns>Offset in document text.</returns>
		public int GetOffsetFromPosition(int line, int column)
		{
			if(line < startLine){
				return -1;
			} else {
				return lineOffsets[line - startLine] + column;
			}
		}
        /// <summary>
        /// Converts offset to position(line,column).
        /// </summary>
        /// <param name="offset">Offset in text.</param>
        /// <param name="line">Output - line.</param>
        /// <param name="column">Output - column.</param>
		public void GetPositionFromOffset(int offset,out int line, out int column)
		{
			int i = 0;
			while(lineOffsets[i] < offset){
				i++;
				if(i >= lineOffsets.Count)
					throw new ArgumentException("Line is not in scope");
			}
			i--;
			line = i + startLine;
			column = offset - lineOffsets[i];
		}
        /// <summary>
        /// Builds converter from <c>DomRegion</c> and <c>IDocument</c>.
        /// </summary>
        /// <param name="region">DomRegion</param>
        /// <param name="doc">Document</param>
        /// <returns>Convertor</returns>
		public static LineOffsetConverter BuildConverter(DomRegion region, IDocument doc)
		{
			LineOffsetConverter lineCon = new LineOffsetConverter();
			lineCon.startLine = region.BeginLine;
			int stOffset = doc.PositionToOffset(region.BeginLine,0);
			for(int i = region.BeginLine;i <= region.EndLine; i++){
				lineCon.lineOffsets.Add(doc.PositionToOffset(i,0) - stOffset);
			}
			return lineCon;
		}
	}
}
