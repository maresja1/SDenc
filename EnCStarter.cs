/*
 * Created by Jan Mares
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.CSharp;
using Debugger.Interop.CorDebug;
using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Gui.Pads;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Services;
using Mono.Cecil;
using Debugger;
using EnC.MetaData;
using EnC;

namespace EnC
{
	unsafe public class EnCStarter : AbstractCommand
	{
        Dictionary<Process, EnCManager> managers = new Dictionary<Process, EnCManager>();
		public EnCStarter()
		{
			DebuggerService.DebugStarting += debuggerStarting;
			DebuggerService.DebugStarted += debuggerStarted;
		}
		public override void Run()
		{
			
        }
		private void debuggerStarting(object sender,EventArgs e)
		{
            WindowsDebugger winDebugger = DebuggerService.CurrentDebugger as WindowsDebugger;
            winDebugger.DebuggerCore.Processes.Added += processAdded;
            winDebugger.DebuggerCore.Processes.Removed += processRemoved;
		}
		private void debuggerStarted(object sender,EventArgs e)
		{
            WindowsDebugger winDebugger = DebuggerService.CurrentDebugger as WindowsDebugger;
            winDebugger.DebuggerCore.Processes.Added -= processAdded;
            winDebugger.DebuggerCore.Processes.Removed -= processRemoved;
        }
        private void deubggerStop(object sender, EventArgs e)
        {
            WindowsDebugger winDebugger = DebuggerService.CurrentDebugger as WindowsDebugger;
            foreach (KeyValuePair<Process,EnCManager> item in managers) {
                item.Value.StopEnC();
            }
        }
		private void processAdded(object sender, CollectionItemEventArgs<Process> e)
		{
			if(Process.DebugMode == DebugModeFlag.Enc){
				managers.Add(e.Item,new EnCManager(e.Item));
			}
		}
		private void processRemoved(object sender, CollectionItemEventArgs<Process> e)
		{
			EnCManager manager;
			if(managers.TryGetValue(e.Item,out manager)){
			   	manager.StopEnC();
			}
			managers.Remove(e.Item);
		}
	}
}
