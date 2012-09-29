/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 15.9.2011
 * Time: 20:59
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using CSharpBinding;
using Debugger;
using Debugger.AddIn.Pads;
using Debugger.AddIn.TreeModel;
using Debugger.Interop.CorDebug;
using EnC;
using EnC.MetaData;
using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Gui.Pads;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Services;
using Microsoft.CSharp;
using Mono.Cecil;
using CompilerError = System.CodeDom.Compiler.CompilerError;
using CompilerErrors = System.CodeDom.Compiler.CompilerErrorCollection;

namespace EnC
{
	/// <summary>
    /// Class offering access to resources needed by EnC and their initiliazation.
    /// Contains calling of build new version of changed assembly.
	/// </summary>
	public class ResourceManager
	{
        /// <summary>
        /// Reference to the EnCManager.
        /// </summary>
		EnCManager encManager;
        /// <summary>
        /// Refernce to interface representing debugged project.
        /// </summary>
		IProject project;
        /// <summary>
        /// Refernce to instance representing debugged process.
        /// </summary>
        Process process;
        /// <summary>
        /// Refernce to instance representing debugged assembly.
        /// </summary>
        Module currentModule = null;
        /// <summary>
        /// Refernce to instance representing last version of new assembly definition.
        /// </summary>
        AssemblyDefinition newAssemblyDef = null;
        /// <summary>
        /// Refernce to instance representing original assembly definition.
        /// </summary>
		AssemblyDefinition oldAssemblyDef = null;
        
		Object buildLock = new Object();
		/// <summary>
		/// Indicates, whether compile was successful.
		/// </summary>
		public bool CompileSuccessful{
			get; private set;
		}
        /// <summary>
        /// Contains reference to the debugged project.
        /// </summary>
		public IProject Project{
			get{ return project; }
		}
        /// <summary>
        /// Contains reference to running process.
        /// </summary>
		public Process Process{
			get{ return process; }
		}
        /// <summary>
        /// Definition of assembly, which was recompiled after last change.
        /// </summary>
		public AssemblyDefinition NewAssembly{
			get{ return newAssemblyDef; }
		}
        /// <summary>
        /// Definition of original assembly, which was compiled before starting debug.
        /// </summary>
		public AssemblyDefinition OldAssembly{
			get{ return oldAssemblyDef; }
		}
        /// <summary>
        /// Module just being debugged.
        /// </summary>
		public Module CurrentModule{
			get{ return currentModule; }
		}
        /// <summary>
        /// Path to the new assembly.
        /// </summary>
		public string NewAssemblyPath{
			get; private set;
		}
        /// <summary>
        /// Name of the new assembly.
        /// </summary>
		public string NewAssemblyName{
			get; private set;
		}
        /// <summary>
        /// Path to temporaty folder of project.
        /// </summary>
		public string TemporaryPath{
			get; private set;
		}
		public ResourceManager(EnCManager manager,Process process)
		{
			this.encManager = manager;
			project = ProjectService.CurrentProject;
			this.process = process;
		}
        /// <summary>
        /// Loads files, which has been changed for compilation.
        /// </summary>
        /// <param name="filesToCompile">Paths to files with changes.</param>
		public void Load(List<string> filesToCompile)
		{
			string oldAssemblyPath;
            oldAssemblyPath = project.OutputAssemblyFullPath;
            TemporaryPath = ((CompilableProject)project).IntermediateOutputFullPath;
			NewAssemblyPath = rebuildAssembly(filesToCompile);
			if(CompileSuccessful)
			{
	        	newAssemblyDef = AssemblyDefinition.ReadAssembly(NewAssemblyPath);
	        	if(oldAssemblyDef == null){
	        		oldAssemblyDef = AssemblyDefinition.ReadAssembly(oldAssemblyPath);
	        	}
			}
		}
        /// <summary>
        /// Loads aasembly definition by Mono.Cecil
        /// </summary>
		public void PreLoad()
		{
			string oldAssemblyPath = project.OutputAssemblyFullPath;
	        oldAssemblyDef = AssemblyDefinition.ReadAssembly(oldAssemblyPath);
		}
        /// <summary>
        /// Assign module to <c>ResourceManager</c>, if it represents the debugged assembly.
        /// </summary>
        /// <param name="module">Module to be assigned.</param>
        /// <returns>True if assigned.</returns>
		public bool TryToCatchModule(Module module)
		{
			if(module.Name == project.AssemblyName + ".exe")
				currentModule = module;
            return (module.Name == project.AssemblyName + ".exe");
		}
        /// <summary>
        /// Rebuilds assembly given the dirty files.
        /// </summary>
        /// <param name="filesToCompile">Dirty files to be recompiled in new assembly.</param>
        /// <returns>Path of the new assembly.</returns>
		private string rebuildAssembly(List<string> filesToCompile)
		{
			if(project.IsStartable){
				NewAssemblyName = project.AssemblyName + "_enc.exe";
			} else {
				NewAssemblyName = project.AssemblyName + "_enc.dll";
			}
			string path = TemporaryPath + NewAssemblyName;
            CompilerErrors errs = CSharpBackgroundCompiler.RecompileWithName(path,filesToCompile);
            
            CompileSuccessful = (errs != null && !errs.HasErrors);
            WorkbenchSingleton.SafeThreadAsyncCall(
				delegate() {
		            if(!CompileSuccessful && errs != null)
		            {
		            	foreach(CompilerError err in errs){
		            		BuildError error = new BuildError(err.FileName,err.Line,err.Column,err.ErrorNumber,err.ErrorText);
		            		error.IsWarning = err.IsWarning;
		            		TaskService.Add(new Task(error));
		        		}
		            } else {
		            	TaskService.Clear();
		            }
				}
			);
            return path;
        }
        /// <summary>
        /// Forbids references to definitions and module, thus giving ability to GC to free them from memory.
        /// </summary>
		public void Unload()
		{
			oldAssemblyDef = null;
			newAssemblyDef = null;
			currentModule = null;
		}
	}
}