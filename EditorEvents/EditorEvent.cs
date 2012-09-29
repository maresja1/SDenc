/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 2.11.2011
 * Time: 14:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Editor;

namespace EnC.EditorEvents
{
	using SourceTextChagneMap = Dictionary<uint,BodyChangeHistory>;

    /// <summary>
    /// Holds events done in editor while stopped at breakpoint.
    /// </summary>
	public class EditorEvent{
        /// <summary>
        /// Table of BodyChangeHistory for changed methods, identified by methods token.
        /// </summary>
		public SourceTextChagneMap sourceTextChanges = new SourceTextChagneMap();
        /// <summary>
        /// List of changes of project structure.
        /// </summary>
		public List<SourceChange> changes = new List<SourceChange>();
		/// <summary>
		/// List of paths of touched files by editting.
		/// </summary>
		public List<string> touched = new List<string>();
	}
    /// <summary>
    /// Stores basic inforamtions about change done in source code.
    /// </summary>
	public struct SourceTextChange{
        /// <summary>
        /// Offset in text(source code) where change taked place.
        /// </summary>
		public int Offset;
        /// <summary>
        /// Length of inserted/removed text.
        /// </summary>
		public int Length;
        /// <summary>
        /// Indicates whether change was inserting or removing text.
        /// </summary>
		public bool Removed;
        /// <summary>
        /// Stores offsets of semicolons and left brackets ending scope.
        /// </summary>
		public int[] Semicolon;
	}
}
