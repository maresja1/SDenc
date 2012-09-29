/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 17.9.2011
 * Time: 11:21
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using EnC;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop;
using ICSharpCode.Core;
using EnC.DeltaSymbols;
using Mono.Cecil;

namespace EnC.EditorEvents
{
	/// <summary>
	/// Listens to events in editor and create EditorEvent when asked.
	/// </summary>
	public class EditorEventCreator
	{
        /// <summary>
        /// <c>List</c>, that gives ability to recount line number.
        /// </summary>
		private List<int> removedLines = new List<int>();
		/// <summary>
		/// Indicates, whether user has done any change to the source code at all.
		/// </summary>
		private bool changeMade = false;
        /// <summary>
        /// Indicates, whether instance listens to the editors events.
        /// </summary>
		private bool attached = false;
        /// <summary>
        /// Stores filename of just changing file.
        /// </summary>
		private string changingFilename;
        /// <summary>
        /// Indicates, whether user has done any change to the source code at all.
        /// </summary>
		public bool UpToDate{
			get{
				return !this.changeMade;
			}
		}
        /// <summary>
        /// Stores informations about events done to the source code from last call of <see cref="Reset"/>. 
        /// </summary>
		private EditorEvent actualEvent;
        /// <summary>
        /// Parenting instance of EnCManager.
        /// </summary>
		private EnC.EnCManager manager;
		
		public EditorEventCreator(EnC.EnCManager manager)
		{
			this.manager = manager;
			actualEvent = new EditorEvent();
			lastProjectContent = new ProjectContentCopy(ParserService.CurrentProjectContent);
		}
		/// <summary>
        /// Holds copy of project contain from the last stop or last calling <see cref="Reset"/>.
		/// </summary>
		private ProjectContentCopy lastProjectContent;
			
		/// <summary>
		/// Reset state of editor and makes copy of actual project content.
		/// </summary>
		public void Reset()
		{
			changeMade = false;
			actualEvent = new EditorEvent();
			lastProjectContent = new ProjectContentCopy(ParserService.CurrentProjectContent);
			int count = lastProjectContent.Classes.Count;
		}
		/// <summary>
        /// Add changes from <c>actualEvent.changes</c> to <c>changes</c> without duplicates.
		/// </summary>
		/// <param name="changes">List where changes should be added.</param>
		private void FilterBodyChanges(ref List<SourceChange> changes)
		{
			foreach(SourceChange change in actualEvent.changes){
				if(!changes.Exists(delegate(SourceChange sChange){
                    return bodyChangeEquals(change, sChange.NewEntity);
				})){
					changes.Add(change);
				}
			}
		}
		/// <summary>
        /// Get changes made to project from last calling of <see cref="Reset"/>.
		/// </summary>
		/// <returns>Instance of EditorEvent containing changes done and line recounter</returns>
		public EditorEvent GetChanges()
		{
			List<SourceChange> changes = lastProjectContent.DiffTo(ParserService.CurrentProjectContent);
			FilterBodyChanges(ref changes);
			actualEvent.changes = changes;
			return actualEvent;
		}
		/// <summary>
		/// Attach EditorEventCreator to actual text editor.
		/// </summary>
		public void Attach()
		{
			if(!attached){
				attached = true;
				WorkbenchSingleton.WorkbenchCreated += this.workbenchCreated;
				
				if(WorkbenchSingleton.Workbench != null){
					WorkbenchSingleton.Workbench.ActiveViewContentChanged += this.activeViewContentChanged;
					
					if(WorkbenchSingleton.Workbench.ActiveViewContent != null){
						ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
						if(provider != null)
							provider.TextEditor.Document.Changing += this.changingEvent;
					}
				}
			}
		}
		/// <summary>
		/// Detach EditorEventCreator from text editor.
		/// </summary>
		public void Detach()
		{
			attached = false;
			WorkbenchSingleton.WorkbenchCreated -= this.workbenchCreated;
			
			if(WorkbenchSingleton.Workbench != null){
				WorkbenchSingleton.Workbench.ActiveViewContentChanged -= this.activeViewContentChanged;
				
				if(WorkbenchSingleton.Workbench.ActiveViewContent != null){
					ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
					provider.TextEditor.Document.Changing -= this.changingEvent;
				}
			}
		}
        /// <summary>
        /// Event callback for change in documents text.
        /// </summary>
        /// <param name="src">Document, that is being changed.</param>
        /// <param name="arg">Additional info about changes in text.</param>
		private void changingEvent(object src, TextChangeEventArgs arg)
		{
			IDocument doc = (IDocument) src;
			
			ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			changingFilename = provider.TextEditor.FileName;
			
			if(DebuggerService.IsDebuggerStarted){
				if(arg.InsertedText.Length > 0 || arg.RemovedText.Length > 0){
					if(!actualEvent.touched.Exists(changingFilename.Equals)){
						actualEvent.touched.Add(changingFilename);
					}
					changeMade = true;
                    uint token;
					BodyChange change = findBodyChanges(arg,doc);
                    if (change != null && lastProjectContent.Exist(change.NewEntity)) {
                        if (!actualEvent.changes.Exists(delegate(SourceChange sChange) { return bodyChangeEquals(sChange, change.NewEntity); })) {
                            actualEvent.changes.Add(change);
                        }
                    } else {
                        return;
                    }
                    if (getMethodToken(change, out token)) {
                        BodyChangeHistory hist;
                        if (!actualEvent.sourceTextChanges.TryGetValue(token, out hist)) {
                            hist = new BodyChangeHistory(LineOffsetConverter.BuildConverter(change.NewEntity.BodyRegion, doc));
                            actualEvent.sourceTextChanges.Add(token, hist);
                        }
                        int bOffset = doc.PositionToOffset(change.NewEntity.BodyRegion.BeginLine, 0);

                        SourceTextChange sTchange;
                        sTchange.Offset = arg.Offset - bOffset;

                        if (arg.InsertionLength > 0) {
                            sTchange.Length = arg.InsertionLength;
                            sTchange.Removed = false;
                            sTchange.Semicolon = semicolonFind(arg.InsertedText);
                            hist.TextChanges.Add(sTchange);
                        }
                        if (arg.RemovalLength > 0) {
                            sTchange.Length = arg.RemovalLength;
                            sTchange.Removed = true;
                            sTchange.Semicolon = semicolonFind(arg.RemovedText);
                            hist.TextChanges.Add(sTchange);
                        }
                    }
				}
			}
		}
        /// <summary>
        /// Build offsets of semicolons and scope brackets from inserted/removed text. 
        /// </summary>
        /// <param name="text">Inserted/removed text.</param>
        /// <returns>Offset of delimiters if any, otherwise null.</returns>
		private int[] semicolonFind(string text)
		{
			List<int> results = new List<int>();
			for (int i = 0; i < text.Length; i++) {
				if(";{}".Contains(text[i])){
					results.Add(i);
				}
			}
			return (results.Count > 0 ? results.ToArray() : null);
		}
        /// <summary>
        /// Registers event for changing documents texts after change of actual document.
        /// </summary>
		private void activeViewContentChanged(object src, EventArgs args)
		{
			ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			if(provider != null){
				changingFilename = provider.TextEditor.FileName;
				provider.TextEditor.Document.Changing -= this.changingEvent;
				provider.TextEditor.Document.Changing += this.changingEvent;
			}
		}
        /// <summary>
        /// Register event for changing actual document.
        /// </summary>
		private void workbenchCreated(object src, EventArgs args)
		{
			WorkbenchSingleton.Workbench.ActiveViewContentChanged += this.activeViewContentChanged;
		}
        /// <summary>
        /// Find first change in project structure. Determines which method is changed etc. .
        /// </summary>
        /// <param name="change">Additional info about change in source code text.</param>
        /// <param name="doc">Document, that was changed.</param>
        /// <returns>Instance of BodyChange, containing info about change in structure.</returns>
		private BodyChange findBodyChanges(TextChangeEventArgs change, IDocument doc)
		{
			IDocumentLine line = doc.GetLineForOffset(change.Offset);
			
			int row = line.LineNumber;
			int column = change.Offset - line.Offset;
			
			bool found = false;
            BodyChange entity = null;
			
			foreach(IClass classEl in ParserService.CurrentProjectContent.Classes)
			{
				if (classEl is CompoundClass)
				{
					CompoundClass compClass  = classEl as CompoundClass;
					foreach (IClass compPart in compClass.Parts)
					{
                        if (compPart.BodyRegion.IsInside(row, column) && FileUtility.IsEqualFileName(classEl.CompilationUnit.FileName, changingFilename))
						{
							entity = findBodyChangesClass(compPart, row, column);
							if(entity != null){
								found = true;
								break;
							}
						}
					}
					if(found)
						break;
				}
				if (classEl.BodyRegion.IsInside(row, column) && FileUtility.IsEqualFileName(classEl.CompilationUnit.FileName,changingFilename))
				{
					entity = findBodyChangesClass(classEl, row, column);
					if(entity != null){
						found = true;
						break;
					}
				}
			}
			return entity;
		}
        /// <summary>
        /// Tries to find one change in particular class, that changed document contains.
        /// </summary>
        /// <param name="classEl">Class where change might have been done.</param>
        /// <param name="row">Row of start of text change.</param>
        /// <param name="column">Column of start of text change.</param>
        /// <returns>Instance of BodyChange, containing info about change in structure.</returns>
		private BodyChange findBodyChangesClass(IClass classEl,int row,int column){
			foreach(IMethod methodEl in classEl.Methods){
				if(methodEl.BodyRegion.IsInside(row,column)){
                    return new BodyChange(methodEl, methodEl, SourceChangeMember.Method, SourceChangeAction.BodyChanged, false);
				}
			}
			foreach(IProperty propEl in classEl.Properties){
				if(propEl.BodyRegion.IsInside(row,column)){
                    if (propEl.GetterRegion.IsInside(row, column)) {
                        return new BodyChange(propEl, propEl, SourceChangeMember.Property, SourceChangeAction.BodyChanged, true);
                    } else if (propEl.SetterRegion.IsInside(row, column)) {
                        return new BodyChange(propEl, propEl, SourceChangeMember.Property, SourceChangeAction.BodyChanged, true);
                    }
				}
			}
			return null;
		}
        /// <summary>
        /// Determines if <c>change</c> represent body change of specified <c>method</c>.
        /// </summary>
		private bool bodyChangeEquals(SourceChange change,IMethod method)
		{
			return (change.OldEntity == change.NewEntity && change.NewEntity is IMethod &&
			        MemberComparator.SameMethodOverloads((IMethod)change.NewEntity,method));
		}
        /// <summary>
        /// Determines if <c>change</c> represent body change of specified <c>property</c>.
        /// </summary>
		private bool bodyChangeEquals(SourceChange change,IProperty property)
		{
			return (change.OldEntity == change.NewEntity && change.NewEntity is IProperty &&
			        MemberComparator.SameProperties((IProperty)change.NewEntity,property));
		}
        /// <summary>
        /// Determines if <c>change</c> represent body change of specified <c>entity</c>.
        /// </summary>
        private bool bodyChangeEquals(SourceChange change, IEntity entity)
        {
            // It should always fall to overloads above.
            if (entity is IProperty)
                return bodyChangeEquals(change, (IProperty)entity);
            else if (entity is IMethod)
                return bodyChangeEquals(change, (IMethod)entity);
            throw new ArgumentException();
        }
        /// <summary>
        /// Tries to find token for specified BodyChange.
        /// </summary>
        /// <param name="change">Change, for which is token to be found.</param>
        /// <param name="token">Here is stored token of BodyChange.</param>
        /// <returns>True if token found.</returns>
		private bool getMethodToken(BodyChange change, out uint token)
		{
            if (change.MemberKind == SourceChangeMember.Method) {
                MethodDefinition def = manager.MetadataManager.FindMethod(manager.ResourceManager.OldAssembly,
                                          manager.ResourceManager.CurrentModule.Name,(IMethod)change.NewEntity);
                if (def != null) {
                    token = def.MetadataToken.ToUInt32();
                    return true;
                }
            } else {
                
                PropertyDefinition defProp = manager.MetadataManager.FindProperty(manager.ResourceManager.OldAssembly,
                                          manager.ResourceManager.CurrentModule.Name, (IProperty)change.NewEntity);
                MethodDefinition def = (change.isGetter ? defProp.GetMethod : defProp.SetMethod);
                if (def != null) {
                    token = def.MetadataToken.ToUInt32();
                    return true;
                }
            }
            token = 0;
            return false;
		}
	}
}