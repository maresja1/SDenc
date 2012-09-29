/*
 * Vytvořeno aplikací SharpDevelop.
 * Uživatel: Honza
 * Datum: 10.2.2012
 * Čas: 21:55
 * 
 * Tento template můžete změnit pomocí Nástroje | Možnosti | Psaní kódu | Upravit standardní hlavičky souborů.
 */
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EnC.DeltaSymbols;
using EnC.EditorEvents;

namespace EnC
{
	/// <summary>
	/// Class which build changes in IL sequence points for a method based on what was changed in source code.
	/// </summary>
	public class SequencePointRemapper
	{
        /// <summary>
        /// Array representing movement of sequence points.
        /// </summary>
        int[] diffSequencePoints;
        /// <summary>
        /// List representing original sequence points.
        /// </summary>
        List<SequencePoint> originalSequencePoints;
        /// <summary>
        /// List representing sequence points of new version of method.
        /// </summary>
        List<SequencePoint> newSequencePoints;

        public SequencePointRemapper(List<SequencePoint> origSeqPoints, List<SequencePoint> newSeqPoints, BodyChangeHistory history)
		{
            originalSequencePoints = origSeqPoints;
            diffSequencePoints = new int[origSeqPoints.Count];
            newSequencePoints = newSeqPoints;
            if(history != null)
			    findChanges(history);
		}
        /// <summary>
        /// Build array with sequence points movement using <c>BodyChangeHistory</c> instance,
        /// which is storing informations collected from editor events.
        /// </summary>
        /// <param name="history"><c>BodyChangeHistory</c> instance storing informations collected from editor events.</param>
		private void findChanges(BodyChangeHistory history)
		{
			for (int i = 0; i < history.TextChanges.Count; i++) {
				if(history.TextChanges[i].Semicolon != null){
					int oldStartOff = oldOffset(history.TextChanges[i].Offset,i,history.TextChanges);
					int line,column;

                    history.Converter.GetPositionFromOffset(oldStartOff, out line, out column);
					if(history.TextChanges[i].Removed){
                        diffSequencePoints[findSeqPoint((uint)line, (uint)column)] -= history.TextChanges[i].Semicolon.Length;
                    } else {
                        diffSequencePoints[findSeqPoint((uint)line, (uint)column)] += history.TextChanges[i].Semicolon.Length;
					}
				}
			}
		}
        /// <summary>
        /// Finds old IL offset for new sequence point.
        /// </summary>
        /// <param name="offset">Offset in source code for new sequence point.</param>
        /// <param name="index">Index of sequence point in array of methods sequence points.</param>
        /// <param name="changes">Changes in source code, used for computing old offset.</param>
        /// <returns>Old IL offset.</returns>
		private int oldOffset(int offset,int index,List<SourceTextChange> changes)
		 {
			for (int i = index - 1; i >= 0; i--) {
				SourceTextChange element = changes[i];
				if(element.Offset < offset){
					if(element.Removed){
						offset += element.Length;
					} else {
						offset -= element.Length;					
					}
				}
			}			
			return offset;
		}
        /// <summary>
        /// Finds sequence point for given position.
        /// </summary>
        /// <param name="line">Line describing position.</param>
        /// <param name="column">Column describing position.</param>
        /// <returns>Index of found sequence point.</returns>
		private int findSeqPoint(uint line, uint column)
		{
			List<SequencePoint> points = originalSequencePoints;
			int i = 0;
			for (; i < points.Count; i++) {
				SequencePoint actual = points[i];
				if((actual.Line >= line && actual.Column > column) || (actual.Line > line)){
					return i;
				}
			}
			return i;
		}
        /// <summary>
        /// Translate IL offset from old version of sequence point to the new one.
        /// </summary>
        /// <param name="ilOffset">Old IL offset.</param>
        /// <returns>New IL offset</returns>
        public uint TranslateILOffset(uint ilOffset)
        {
            int seqPointI = seqPointForIlOffset(ilOffset);
            bool end = seqPointI == -1;
            int newIndex = getNewIndexForOriginal((end ? originalSequencePoints.Count - 1 : seqPointI));
            return (end ? newSequencePoints[newIndex].Offset  + 1 : newSequencePoints[newIndex].Offset);
        }
        /// <summary>
        /// Find index of sequence point in original sequence points determined by IL offset.
        /// </summary>
        /// <param name="ilOffset">IL offset of desired sequence point.</param>
        /// <returns>Index of desired sequence point in array of methods sequence points.</returns>
        private int seqPointForIlOffset(uint ilOffset)
        {
            for (int i = 0; i < originalSequencePoints.Count; i++) {
        		if (originalSequencePoints[i].Offset >= ilOffset) {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// Find index of original sequence point in array of new sequence points.
        /// </summary>
        /// <param name="origIndex">Index of sequence point in array of original sequence points.</param>
        /// <returns>Index in array of new sequence points.</returns>
        private int getNewIndexForOriginal(int origIndex)
        {
            int newIndex = origIndex;
            for (int i = 0; i <= origIndex; i++) {
                newIndex += diffSequencePoints[i];
            }
            if (newIndex < 0) {
                return 0;
            } else if (newIndex >= newSequencePoints.Count) {
                return newSequencePoints.Count - 1;
            } else {
                return newIndex;
            }
        }
        /// <summary>
        /// Translate line and column from old version of sequence point to the new one.
        /// </summary>
        /// <param name="begLine">Beginning line</param>
        /// <param name="begColumn">Beginning column</param>
        /// <param name="endLine">Ending line</param>
        /// <param name="endColumn">Ending column</param>
        public void TranslateLineColumn(ref uint begLine, ref uint begColumn,ref uint endLine, ref uint endColumn)
        {
            int index = findSeqPoint(begLine, begColumn);
            int newIndex = getNewIndexForOriginal(index);
            begLine = newSequencePoints[newIndex].Line;
            begColumn = newSequencePoints[newIndex].Column;
            endLine = newSequencePoints[newIndex].EndLine;
            endColumn = newSequencePoints[newIndex].EndColumn;
        }
	}
}
