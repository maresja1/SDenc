using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Signatures;
using EnC.MetaData;
using Debugger.Interop.CorSym;

namespace EnC
{
    public class LocalVarDiff
    {
        /// <summary>
        /// Reader of symbols used by running assembly.
        /// </summary>
        ISymUnmanagedReader oldReader;
        /// <summary>
        /// Reader of symbols emitted with new assembly.
        /// </summary>
        ISymUnmanagedReader newReader;
        /// <summary>
        /// Translation from new IL offset to the old one.
        /// </summary>
        SequencePointRemapper remapper;
        /// <summary>
        /// Used for storing old local variable definitions with placeholders.
        /// </summary>
        static Dictionary<uint, List<LocalVarDefinition>> oldLocalVars = new Dictionary<uint, List<LocalVarDefinition>>();
        /// <summary>
        /// Clears cache for old local variable definitions with placeholders.
        /// Can cause bad handling of changes of local variables signatures.
        /// </summary>
        public static void ClearLocalVarCache(){
            oldLocalVars.Clear();
        }
        public LocalVarDiff(ISymUnmanagedReader oldReader, ISymUnmanagedReader newReader, SequencePointRemapper remapper)
        {
            this.remapper = remapper;
            this.oldReader = oldReader;
            this.newReader = newReader;
        }
        /// <summary>
        /// Identifies same variables in new and old. Compute differences between new and old local variable declarations and
        /// uses placeholder in case where it is needed. Placeholders are stored to placeholders container, used to correctly
        /// change IL code of new version of methods.
        /// </summary>
        /// <param name="original">Signature of original version of method.</param>
        /// <param name="newSig">Signature of new version of mehtod.</param>
        /// <param name="placeholders">Container, that holds placeholder translation when method succeded.</param>
        /// <param name="tkOldMethod">Metadata token to the original version of method.</param>
        /// <param name="tkNewMethod">Metadata token to the new version of method.</param>
        /// <param name="translator">Token translator used to translate signatures.</param>
        /// <returns>Signature with placeholders for new version of method in running assembly.</returns>
        public Signature makeSignature(Signature original, Signature newSig, out Dictionary<int, int> placeholders,
            uint tkOldMethod, uint tkNewMethod, ITokenTranslator translator)
        {
            placeholders = new Dictionary<int, int>();
            int varCount = original.Types.Count - 2;
            List<LocalVarDefinition> olds;
            if (!oldLocalVars.TryGetValue(tkOldMethod,out olds))
                olds = getDefinitions(tkOldMethod, oldReader);

            List<LocalVarDefinition> news = getDefinitions(tkNewMethod, newReader);
            olds.ForEach((LocalVarDefinition def) =>
            {
                def.ilStart = remapper.TranslateILOffset(def.ilStart);
                def.ilEnd = remapper.TranslateILOffset(def.ilEnd);
            });
            news.ForEach((LocalVarDefinition def) =>
            {
                if (def.signature != null) {
                    def.signature = Signature.Migrate(def.signature, translator, 0);
                }
            });
            List<int> used = new List<int>();
            for (int i = 0; i < news.Count; i++) {
                Predicate<LocalVarDefinition> lambda = (LocalVarDefinition def) =>
                {
                    return def.Same(news[i]);
                };
                if (!olds.Exists(lambda) || used.Contains(olds.FindIndex(lambda))) {
                    placeholders.Add(i, olds.Count);
                    original.AddSigPart(news[i].signature);
                    olds.Add(news[i]);
                } else {
                    int index = olds.FindIndex(lambda);
                    placeholders.Add(i, index);
                    used.Add(index);
                }
            }
            for (int i = 0; i < olds.Count; i++) {
                if (!placeholders.ContainsValue(i)) {
                    olds[i].removed = true;
                }
            }
            original.Types[1].Code = (uint)olds.Count;
            oldLocalVars[tkOldMethod] = olds;
            return original;
        }
        /// <summary>
        /// Get definitions of local variables in method.
        /// </summary>
        /// <param name="tkMethod">Token of method with desired variables.</param>
        /// <param name="reader">File containing symbols of desired method.</param>
        /// <returns>List of definitions of local variables in method.</returns>
        private List<LocalVarDefinition> getDefinitions(uint tkMethod, ISymUnmanagedReader reader)
        {
            List<LocalVarDefinition> definitions = new List<LocalVarDefinition>();
            int start = 0;
            getDefinitionsForScope(reader.GetMethod(tkMethod).GetRootScope(), ref start, definitions);
            return definitions;
        }
        /// <summary>
        /// Get definitions of local variables in scope.    
        /// </summary>
        /// <param name="scope">Interface refering to desired scope.</param>
        /// <param name="index">Index pointing to the last read variable.</param>
        /// <param name="definitions">List of definitions, where found variable definition are placed.</param>
        private void getDefinitionsForScope(ISymUnmanagedScope scope, ref int index, List<LocalVarDefinition> definitions)
        {
            ISymUnmanagedVariable[] variables = scope.GetLocals();
            foreach (ISymUnmanagedVariable var in variables) {
                LocalVarDefinition def = new LocalVarDefinition();
                def.ilStart = scope.GetStartOffset();
                def.ilEnd = scope.GetEndOffset();
                def.name = var.GetName();
                def.signature = var.GetSignature();
                definitions.Add(def);
                ++index;
            }
            foreach (ISymUnmanagedScope cScope in scope.GetChildren()) {
                getDefinitionsForScope(cScope, ref index, definitions);
            }
        }
    }
    /// <summary>
    /// Used for storing information and comparing LocalVarDefinitions to each other.
    /// </summary>
    class LocalVarDefinition
    {
        public string name;
        public uint ilStart;
        public uint ilEnd;
        public bool removed = false;
        public byte[] signature;
        public bool Same(LocalVarDefinition obj)
        {
            LocalVarDefinition other = obj as LocalVarDefinition;
            return other.name == name && other.ilEnd == ilEnd && other.ilStart == ilStart && ((signature == null && obj.signature == null) ||
                signature.SequenceEqual<byte>(obj.signature)) && !removed;
        }
    }
}
