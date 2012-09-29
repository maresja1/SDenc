/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 5.9.2011
 * Time: 20:21
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Debugger;
using Debugger.Interop.CorDebug;
using EnC.DeltaSymbols;
using EnC.EditorEvents;
using EnC.MetaData;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Services;
using Mono.Cecil;
using AppDomain = Debugger.AppDomain;

namespace EnC
{
    using SequencePointMap = Dictionary<uint, List<SequencePoint>>;
    using ICSharpCode.SharpDevelop.Bookmarks;
    using ICSharpCode.SharpDevelop.Editor;
    /// <summary>
    /// Class that takes care of applying changes to the code of running module by EnC method.
    /// Directs ResourceManager and MetadaManager to translate IL code and build Metadata structures.
    /// Designed to live as long as the debugger is running. Its creation takes place in EnCStarter, 
    /// which represents command started after SharpDevelop is started.
    /// </summary>
    public class EnCManager : IEnCHook
    {
        static bool debugMode = true;
        ResourceManager resource;
        MetaDataManager metadata;
        EditorEventCreator eventCreator;
        EditorEvent lastEvent;
        FunctionRemapper funcRemap = null;
        Process process;

        /// <summary>
        /// Contains reference to ResourceManager of debugged project.
        /// </summary>
        public ResourceManager ResourceManager
        {
            get { return resource; }
        }
        /// <summary>
        /// Contains reference to MetadataManager of debugged project.
        /// </summary>
        public MetaDataManager MetadataManager
        {
            get { return metadata; }
        }
        /// <summary>
        /// Contains reference to EditorEvent of debugged project.
        /// </summary>
        public EditorEvent ActualEditEvent
        {
            get { return lastEvent; }
        }
        public EnCManager(Process process)
        {
            resource = new ResourceManager(this, process);
            resource.PreLoad();
            process.Modules.Added += this.ProcessModuleAdded;
            metadata = new MetaDataManager(this);
            eventCreator = new EditorEventCreator(this);
            process.EnCHook = this;
            this.process = process;
            LocalVarDiff.ClearLocalVarCache();
        }
        /// <summary>
        /// Callback for adding any module to debugged process.
        /// </summary>
        /// <param name="sender">Paramters for callback, not used here.</param>
        /// <param name="args">Arguments with description of module.</param>
        public void ProcessModuleAdded(object sender, CollectionItemEventArgs<Module> args)
        {
            if (resource.TryToCatchModule(args.Item))
                eventCreator.Attach();
        }
        /// <summary>
        /// Stops EnC funcionality by stopping eventCreator and detaching callbacks
        /// </summary>
        public void StopEnC()
        {
            eventCreator.Detach();
            process.EnCHook = null;
            process.Modules.Added -= this.ProcessModuleAdded;
        }
        /// <summary>
        /// Called by debugger to inform of successful rempap. Recreates steppers and reset the cache.
        /// </summary>
        /// <param name="pAppDomain">AppDomain, that contains method.</param>
        /// <param name="pThread">Thread in which is method running.</param>
        /// <param name="pFunction">New version of function.</param>
        public void FunctionRemapComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
        {
            foreach (StackFrame frame in resource.Process.SelectedThread.Callstack) {
                frame.RecreateSteppers();
                frame.AppDomain.ResetCache();
                frame.MethodInfo.ResetLocalCache();
            }
            resource.Process.ClearExpressionCache();
        }
        /// <summary>
        /// Is called by debugger to handle RemapOpportunity event.
        /// </summary>
        /// <param name="pAppDomain">AppDomain, that contains method.</param>
        /// <param name="pThread">Thread in which is method running.</param>
        /// <param name="pOldFunction">Old version of function.</param>
        /// <param name="pNewFunction">New version of function.</param>
        /// <param name="oldILOffset">IL offset of IP in old version.</param>
        public void FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
        {
            if (funcRemap != null) {
                funcRemap.FunctionRemapOpportunity(pAppDomain, pThread, pOldFunction, pNewFunction, oldILOffset);
            }
        }
        /// <summary>
        /// Is called by debugger before continueing in debug.
        /// </summary>
        /// <returns>Returns true if succeed.</returns>
        public bool BeforeContinue()
        {
            if (!eventCreator.UpToDate) {
                lastEvent = eventCreator.GetChanges();
                if (!this.patchWithEnC(lastEvent)) {
                    MessageService.ShowError("EnC was not to able to propagate changes. See Errors for detials.");
                    WindowsDebugger winDebugger = DebuggerService.CurrentDebugger as WindowsDebugger;
                    winDebugger.Stop();
                    return false;
                }
                eventCreator.Reset();
            }
            return true;
        }
        /// <summary>
        /// This methods is called after trying to continue with editted code.
        /// It gets a result of searching changes in code and applies them to
        /// running process if it is possible.
        /// <param name="eventLog">Contains changes done to code during pause.</param>
        /// </summary>
        private bool patchWithEnC(EditorEvent eventLog)
        {
            // Build new version of assembly.        	
            resource.Load(eventLog.touched);

            // Exit when errors in compiling
            if (!resource.CompileSuccessful) {
                return false;
            }
            metadata.Update();

            byte[] dmeta = null, dil = null;

            DeltaBuilder dBuilder = new DeltaBuilder(this);

            // Initialize symbol emitter.
            SymbolWriterClass dWriter = new SymbolWriterClass(this, metadata);
            dWriter.Initialize(metadata.OldEmitter.CorMetaDataEmit, metadata.NewImporter.CorMetaDataImport);

            dBuilder.SymbolWriter = dWriter;

            // Process changes and tries to make delta metadata and IL code.
            // If changes not valid exits.
            try {
                if (!processChanges(eventLog, dBuilder)) {
                    return false;
                }

                process.AppDomains.ForEach(delegate(AppDomain app) { app.ResetCache(); });
                regenMovedSymbols(dWriter, lastEvent);
                // Update symbols emitted during changes.
                updateSymbolStore(dWriter);
                dWriter.Close();
                // Recieve deltas and applies them to running module
                dil = dBuilder.getDeltaIL();
                dmeta = dBuilder.getDeltaMetadata();
                resource.CurrentModule.ApplyChanges(dmeta, dil);
                // Log output if in debug mode
                if (debugMode) {
                    StreamWriter writer = new StreamWriter(resource.TemporaryPath + "enc_log_il");
                    writer.BaseStream.Write(dil, 0, dil.Length);
                    writer.Flush();
                    writer.Close();

                    writer = new StreamWriter(resource.TemporaryPath + "enc_log_meta");
                    writer.BaseStream.Write(dmeta, 0, dmeta.Length);
                    writer.Flush();
                    writer.Close();
                }
                funcRemap = new FunctionRemapper(this, dBuilder.SequencePointRemappers);

                updateBreakpoints();
                return true;
            } catch (TranslatingException e) {
                dWriter.Close();
                TaskService.Clear();
                WorkbenchSingleton.SafeThreadAsyncCall(
                   delegate()
                   {
                       TaskService.Clear();
                       TaskService.Add(new Task(null, e.Message, 0, 0, TaskType.Error));
                   }
               );
                return false;
            }
        }
        /// <summary>
        /// Loads symbols generated by compiler when comopiling new version of assembly 
        /// to the debugged module ISymUnamagedReader
        /// </summary>
        private bool processChanges(EditorEvent edEvent, DeltaBuilder dBuilder)
        {
            List<BuildError> errors = SourceChangeValidator.GetErrors(edEvent.changes);

            if (errors.Count > 0) {
                showErrors(errors);
                return false;
            } else {
                // register new elements in metadata
                foreach (SourceChange change in edEvent.changes) {
                    if (change.MemberAction == SourceChangeAction.Created && change.MemberKind == SourceChangeMember.Method) {
                        dBuilder.PreAddMethod((IMethod)change.NewEntity);
                    }
                }
                foreach (SourceChange change in edEvent.changes) {
                    dBuilder.MakeChange(change);
                }
                return true;
            }
        }
        /// <summary>
        /// Enlist errors in ErrorList in SharpDevelop
        /// </summary>
        /// <param name="errors">List of errors</param>
        private void showErrors(List<BuildError> errors)
        {
            WorkbenchSingleton.SafeThreadAsyncCall(
                delegate()
                {
                    TaskService.Clear();
                    foreach (BuildError error in errors) {
                        TaskService.Add(new Task(error));
                    }
                }
            );
        }
        /// <summary>
        /// Regenerate source symbols for methods moved by editting others.
        /// </summary>
        /// <param name="emitter">Symbol emitting interface.</param>
        /// <param name="edEvent">EditorEvent containing changed files.</param>
        private void regenMovedSymbols(SymbolWriterClass emitter, EditorEvent edEvent)
        {
            foreach (IClass cClass in ParserService.CurrentProjectContent.Classes) {
                regenMovedSymbolsClass(cClass,edEvent,emitter);
            }
        }
        private void regenMovedSymbolsClass(IClass cClass, EditorEvent edEvent, SymbolWriterClass emitter)
        {
            Predicate<string> del = delegate(string name){
                return FileUtility.IsEqualFileName(name, cClass.CompilationUnit.FileName);
            };

            if (edEvent.touched.Exists(del)) {
                if (cClass is CompoundClass) {
                    foreach(IClass tClass in ((CompoundClass)cClass).Parts){
                        regenMovedSymbolsClass(tClass, edEvent, emitter);
                    }
                } else {
                    foreach(IMethod method in cClass.Methods) {
                        if(!edEvent.changes.Exists(delegate(SourceChange change){
                            return change.NewEntity is IMethod && MemberComparator.SameMethodOverloads(method, (IMethod)change.NewEntity);
                        })){
                            regenMovedSymbolsMethod(method, emitter);
                        }
                    }
                    foreach (IClass tClass in cClass.InnerClasses) {
                        regenMovedSymbolsClass(tClass, edEvent, emitter);
                    }
                    foreach (IProperty tProp in cClass.Properties) {
                        if(!edEvent.changes.Exists(delegate(SourceChange change){
                            return change.NewEntity is IProperty && MemberComparator.SameProperties(tProp, (IProperty)change.NewEntity);
                        })){
                            regenMovedSymbolsProperty(tProp, emitter);
                        }
                    }
                }
            }
        }
        private void regenMovedSymbolsMethod(IMethod method, SymbolWriterClass emitter)
        {
            MethodDefinition targtMet, targtMetOld;

            // Find appropriete new version of method in new assembly.
            targtMet = MetadataManager.FindMethod(ResourceManager.NewAssembly,
                                                          ResourceManager.NewAssemblyName, method);
            if (targtMet == null) throw new TranslatingException("Method "+method.ToString()+" could not be found in debugged module");

            // Find appropriete original version of method in running assembly.
            targtMetOld = MetadataManager.FindMethod(ResourceManager.OldAssembly,
                                                             ResourceManager.CurrentModule.Name, method);
            if (targtMetOld == null) throw new TranslatingException("Method " + method.ToString() + " could not be found in emitted module");
            
            emitter.EmitMethod(targtMetOld.MetadataToken.ToUInt32(), targtMet.MetadataToken.ToUInt32());
        }
        private void regenMovedSymbolsProperty(IProperty property, SymbolWriterClass emitter)
        {
            PropertyDefinition targtProp, targtPropOld;
            MethodDefinition targtMetSet, targtMetOldSet, targtMetGet, targtMetOldGet;

            // Find appropriete new version of method in new assembly.
            targtProp = MetadataManager.FindProperty(ResourceManager.NewAssembly,
                                                          ResourceManager.NewAssemblyName, property);
            if (targtProp == null) throw new TranslatingException("Property " + property.ToString() + " could not be found in debugged module");

            // Find appropriete original version of method in running assembly.
            targtPropOld = MetadataManager.FindProperty(ResourceManager.OldAssembly,
                                                             ResourceManager.CurrentModule.Name, property);
            if (targtPropOld == null) throw new TranslatingException("Property "+property.ToString()+" could not be found in emitted module");

            targtMetGet = targtProp.GetMethod;
            targtMetSet = targtProp.SetMethod;
            targtMetOldGet = targtPropOld.GetMethod;
            targtMetOldSet = targtPropOld.SetMethod;
            try {
                if (targtMetGet != null)
                    emitter.EmitMethod(targtMetOldGet.MetadataToken.ToUInt32(), targtMetGet.MetadataToken.ToUInt32());
                if (targtMetSet != null)
                    emitter.EmitMethod(targtMetOldSet.MetadataToken.ToUInt32(), targtMetSet.MetadataToken.ToUInt32());
            } catch (COMException e) {

            }
        }
        /// <summary>
        /// Updates symbol store.
        /// </summary>
        /// <param name="writer">Class containing emitted symbols.</param>
        private void updateSymbolStore(SymbolWriterClass writer)
        {
            string name = writer.DeltaPDBPath + ".pdb";
            try {
                resource.CurrentModule.SymReader.__UpdateSymbolStore(name, writer.GetSymbolsStream());
            } catch (COMException e) {
                switch ((ulong)e.ErrorCode) {
                    case 0x806D0002:
                        throw new TranslatingException("PDB - in use.");
                    case 0x806D0003:
                        throw new TranslatingException("PDB - Out of memory exception.");
                    case 0x806D0004:
                        throw new TranslatingException("PDB - File system error.");
                    case 0x806D0005:
                        throw new TranslatingException("PDB - not found.");
                }
            }

        }
        /// <summary>
        /// Recreates breakpoints after change of source code.
        /// </summary>
        private void updateBreakpoints()
        {
            BreakpointBookmark[] markers = new BreakpointBookmark[DebuggerService.Breakpoints.Count];
            DebuggerService.Breakpoints.CopyTo(markers, 0);
            foreach (BreakpointBookmark item in markers) {
                ITextEditor editor = findEditor(item);
                if (editor != null) {
                    DebuggerService.ToggleBreakpointAt(editor, item.LineNumber, typeof(BreakpointBookmark));
                    DebuggerService.ToggleBreakpointAt(editor, item.LineNumber, typeof(BreakpointBookmark));
                }
            }
        }
        /// <summary>
        /// Find TextEditor for bookmark.
        /// </summary>
        /// <param name="mark">Bookmark</param>
        /// <returns>TextEditor</returns>
        private ITextEditor findEditor(BreakpointBookmark mark)
        {
            foreach (IViewContent cont in WorkbenchSingleton.Workbench.ViewContentCollection) {
                foreach (OpenedFile file in cont.Files) {
                    if (FileUtility.IsEqualFileName(file.FileName, mark.FileName) && cont is ITextEditorProvider) {
                        return ((ITextEditorProvider)cont).TextEditor;
                    }
                }
            }
            return null;
        }
    }
}
