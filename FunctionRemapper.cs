/*
 * Vytvořeno aplikací SharpDevelop.
 * Uživatel: Honza
 * Datum: 9.2.2012
 * Čas: 10:12
 * 
 * Tento template můžete změnit pomocí Nástroje | Možnosti | Psaní kódu | Upravit standardní hlavičky souborů.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Debugger.Interop.CorDebug;
using EnC.EditorEvents;
using EnC.MetaData;
using EnC.DeltaSymbols;

namespace EnC
{
    using Debugger;

    /// <summary>
    /// Used to handle remaping of IP in changed method.
    /// </summary>
	public class FunctionRemapper
	{
        private Dictionary<uint, SequencePointRemapper> remappers;
		private EnCManager manager;

        public FunctionRemapper(EnCManager manager, Dictionary<uint, SequencePointRemapper> remappers)
		{
			this.manager = manager;
            this.remappers = remappers;
		}
        /// <summary>
        /// Finds appropriete SequncePointMap instances and builds SequencePointRemapper, gets new IL offset and call RemapFunction.
        /// </summary>
		public void FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, uint oldILOffset)
		{
			ICorDebugILFrame2 frame = (ICorDebugILFrame2) pThread.GetActiveFrame();
			
			uint nToken = pOldFunction.GetToken();
			
			SequencePointRemapper remapper;
			if(!remappers.TryGetValue(nToken,out remapper)){
                throw new KeyNotFoundException("Methods sequence points not found.");
			}
            frame.__RemapFunction(remapper.TranslateILOffset(oldILOffset));
		}
	}
}
