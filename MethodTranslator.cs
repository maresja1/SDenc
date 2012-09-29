/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 29.4.2011
 * Time: 11:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq.Expressions;
using Debugger;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop;
using ICSharpCode.Core;
using EnC.MetaData;
using EnC.DeltaSymbols;
using FieldProps = EnC.MetaData.FieldProps;

namespace EnC
{
    /// <summary>
    /// Emits IL of changed or added methods.
    /// </summary>
    public class MethodTranslator
    {
        /// <summary>
        /// Buffer used for temporary storing of byte streams.
        /// </summary>
        List<byte> buffer = new List<byte>();
        /// <summary>
        /// Manager handling metadata token translation.
        /// </summary>
        MetaDataManager metadata;
        /// <summary>
        /// List of load local variable directly instructions 
        /// </summary>
        static byte[] ldInstructions = { 0x06, 0x07, 0x08, 0x09 };
        /// <summary>
        /// List of store local variable directly instruction 
        /// </summary>
        static byte[] stInstructions = { 0x0A, 0x0B, 0x0C, 0x0D };
        /// <summary>
        /// List of store local variable by operand, one byte length 
        /// </summary>
        static byte[] locVarOthers = { 0x11, 0x12, 0x13 };
        /// <summary>
        /// List of store local variable by operand, two bytes length 
        /// </summary>
        static byte[] locVarOthersLong = { 0x0C, 0x0D, 0x0E };
        /// <summary>
        /// Reference for parenting <see cref="DeltaBuilder">DeltaBuilder</see>
        /// </summary>
        public DeltaBuilder Builder
        {
            get;
            private set;
        }

        public MethodTranslator(DeltaBuilder builder)
        {
            Builder = builder;
            metadata = Builder.Manager.MetadataManager;
        }
        /// <summary>
        /// Translate method IL to other metadata scope, with respect to original
        /// method specified by <c>originMethod</c>. 
        /// (Registers local structures validly in sense of EnC, used for changing methods bodies by EnC)
        /// </summary>
        /// <param name="newMethod">Definition of the new version of the method.</param>
        /// <param name="originMethod">Definition of the last version of the method.</param>
        /// <param name="remapper">IL offset remapper, used to build local variable changes.</param>
        /// <param name="placeholder">Product of method, represents translation from old variable set to the new one.</param>
        /// <returns>Descriptor of the new version of method in IL.</returns>
        public MethodDescriptor TranslateMethod(MethodDefinition newMethod, MethodDefinition originMethod,
            SequencePointRemapper remapper, out Dictionary<int, int> placeholder)
        {
            MethodDescriptor log;

            log.destToken = originMethod.MetadataToken.ToUInt32();
            log.srcToken = newMethod.MetadataToken.ToUInt32();

            // build IL header

            log.localVarsToken = buildILHeader(newMethod, originMethod, remapper, out placeholder);

            // Add instructions

            processBody(newMethod, placeholder);

            log.newRva = 0;
            log.codeIL = buffer.ToArray();
            buffer.Clear();

            return log;
        }

        /// <summary>
        /// Translate new method to other metadata scope.
        /// </summary>
        /// <param name="methodDef"><c>MethodDefintion</c> of method to be translated.</param>
        /// <param name="newMethodDef"><c>MethodDefintion</c> of the result. This instance is used only as information holder and 
        /// does not contain IL code of new method.</param>
        /// <returns>Description of translated method.</returns>
        public MethodDescriptor TranslateNewMethod(MethodDefinition methodDef, out MethodDefinition newMethodDef)
        {
            MethodDescriptor log;
            newMethodDef = new MethodDefinition(methodDef.Name, methodDef.Attributes,
                                                metadata.FindTypeReference(Builder.Manager.ResourceManager.OldAssembly, methodDef.ReturnType.Scope.Name, methodDef.ReturnType.FullName));

            log.srcToken = methodDef.MetadataToken.ToUInt32();

            // build IL header

            log.localVarsToken = buildILHeader(methodDef);

            newMethodDef.Body.LocalVarToken = new MetadataToken(log.localVarsToken);
            copyParameters(methodDef, newMethodDef);
            // Add instructions

            processBody(methodDef);

            // Emit method to metadata
            string methodName;
            uint pClass, pdwAttr, pdwImplFlags, RVA;
            byte[] signature;

            metadata.NewImporter.GetMethodProps(methodDef.MetadataToken.ToUInt32(), out methodName,
                out pClass, out pdwAttr, out signature, out RVA, out pdwImplFlags);

            MetadataToken token = metadata.TranslateToken(new MetadataToken(pClass));

            signature = Signature.Migrate(signature, metadata);

            metadata.OldEmitter.CorMetaDataEmit.DefineMethod(pClass, methodName, pdwAttr, signature,
                (uint)signature.Length, 0, pdwImplFlags, out log.destToken);

            newMethodDef.MetadataToken = new MetadataToken(log.destToken);

            log.newRva = 0;
            log.codeIL = buffer.ToArray();
            buffer.Clear();

            return log;
        }
        /// <summary>
        /// Use only to add definition to method from new assembly to debugged assembly, 
        /// when body will be inserted later. For example, when you need Metadata token, but
        /// you cannot have a body. (Circle dependancy in graph of dependant methods translated)
        /// </summary>
        /// <param name="methodDef">Definiton of method in new assembly.</param>
        /// <returns>Descriptor with metadata token.</returns>
        public MethodDescriptor TranslateMethodWithoutBody(MethodDefinition methodDef)
        {
            MethodDescriptor log = new MethodDescriptor();

            log.srcToken = methodDef.MetadataToken.ToUInt32();

            string methodName;
            uint pClass, pdwAttr, pdwImplFlags, RVA;
            byte[] signature;

            metadata.NewImporter.GetMethodProps(methodDef.MetadataToken.ToUInt32(), out methodName,
                out pClass, out pdwAttr, out signature, out RVA, out pdwImplFlags);

            MetadataToken token = metadata.TranslateToken(new MetadataToken(pClass));

            signature = Signature.Migrate(signature, metadata);

            metadata.OldEmitter.CorMetaDataEmit.DefineMethod(pClass, methodName, pdwAttr, signature,
                (uint)signature.Length, 0, pdwImplFlags, out log.destToken);

            return log;
        }
        /// <summary>
        /// Go through all instructions and translates instruction with operand.
        /// </summary>
        /// <param name="methodDef">Definition of method to be translated with body.</param>
        /// <param name="placeholder">Placeholder container fot translation of local variables, if needed.</param>
        private void processBody(MethodDefinition methodDef, Dictionary<int, int> placeholder = null)
        {
            foreach (Instruction element in methodDef.Body.Instructions) {
                processInstruction(element, placeholder);
                processOperand(element.Operand);
            }
        }
        /// <summary>
        /// Processes instruction - add its byte representation and if instruction addresses any local variable,
        /// translates its index using placeholder.
        /// </summary>
        /// <param name="instruction">Instruction to be processed.</param>
        /// <param name="placeholder">Placeholder, if any, to translate variables indexes.</param>
        private void processInstruction(Instruction instruction, Dictionary<int, int> placeholder = null)
        {
            if (instruction.OpCode.Op1 == 0xFE) {
                buffer.Add(instruction.OpCode.Op1);
                if (placeholder != null)
                    translateLocalVarForPlaceholdersHigh(instruction, placeholder);
                buffer.Add(instruction.OpCode.Op2);
            } else {
                if (placeholder == null || !translateLocalVarForPlaceholdersLow(instruction, placeholder))
                    buffer.Add(instruction.OpCode.Op2);

            }
        }
        /// <summary>
        /// Translate variable index for instruction stored in two bytes.
        /// </summary>
        /// <param name="instruction">Instruction to be processed.</param>
        /// <param name="placeholder">Placeholder, if any, to translate variables indexes.</param>
        private void translateLocalVarForPlaceholdersHigh(Instruction instruction, Dictionary<int, int> placeholder)
        {
            byte bInstruction = instruction.OpCode.Op2;
            if (((IList<byte>)locVarOthersLong).Contains(bInstruction)) {
                if (placeholder.ContainsKey((UInt16)instruction.Operand))
                    instruction.Operand = (UInt16)placeholder[(UInt16)instruction.Operand];
            }
        }
        /// <summary>
        /// Translate variable index for instruction stored in one byte.
        /// </summary>
        /// <param name="instruction">Instruction to be processed.</param>
        /// <param name="placeholder">Placeholder, if any, to translate variables indexes.</param>
        private bool translateLocalVarForPlaceholdersLow(Instruction instruction, Dictionary<int, int> placeholder)
        {
            byte bInstruction = instruction.OpCode.Op2;
            if (((IList<byte>)locVarOthers).Contains(bInstruction)) {
                int index = Array.IndexOf<byte>(locVarOthers, bInstruction);
                if (placeholder.ContainsKey((byte)instruction.Operand) && placeholder[(byte)instruction.Operand] < 4 
                    && bInstruction == 0x11) {

                    buffer.Add(ldInstructions[placeholder[(byte)instruction.Operand]]);
                    instruction.Operand = null;
                    return true;
                } else if (placeholder.ContainsKey((byte)instruction.Operand) && placeholder[(byte)instruction.Operand] < 4 
                    && bInstruction == 0x13) {

                    buffer.Add(stInstructions[placeholder[(byte)instruction.Operand]]);
                    instruction.Operand = null;
                    return true;
                } else if (placeholder.ContainsKey((byte)instruction.Operand) && placeholder[(byte)instruction.Operand] < 255) {

                    instruction.Operand = (byte)placeholder[(byte)instruction.Operand];
                    buffer.Add(instruction.OpCode.Op2);
                    return true;
                } else if (placeholder.ContainsKey((byte)instruction.Operand)) {

                    instruction.Operand = (UInt16)placeholder[(UInt16)instruction.Operand];
                    buffer.Add(0xFE);
                    buffer.Add(locVarOthersLong[index]);
                    return true;
                }
            } else if (((IList<byte>)stInstructions).Contains(bInstruction)) {
                int index = Array.IndexOf<byte>(stInstructions, bInstruction);
                if (placeholder.ContainsKey(index)) {
                    if (placeholder[index] > 3) {
                        buffer.Add(locVarOthers[2]);
                        instruction.Operand = (byte)placeholder[index];
                        return true;
                    } else {
                        buffer.Add(stInstructions[placeholder[index]]);
                        return true;
                    }
                }
            } else if (((IList<byte>)ldInstructions).Contains(bInstruction)) {
                int index = Array.IndexOf<byte>(ldInstructions, bInstruction);
                if (placeholder.ContainsKey(index)) {
                    if (placeholder[index] > 3) {
                        buffer.Add(locVarOthers[0]);
                        instruction.Operand = (byte)placeholder[3];
                        return true;
                    } else {
                        buffer.Add(ldInstructions[placeholder[index]]);
                        return true;
                    }
                }
            }
            return false;
        }
        //        +-----------------+ 0
        //        |	  delta header  |    ----->> size of the rest of IL in bytes
        //        +-----------------+ 4
        //        |    IL header    |
        //		  +-----------------+
        //        |				    |
        //		  |	    IL code     |
        //		  |				    |
        //		  +-----------------+        	

        /// <summary>
        /// Builds header of changing method.
        /// </summary>
        /// <param name="newMethod">MethodDefinition of new version of changing method.</param>
        /// <param name="originMethod">MethoDefinition of original version of changing method.</param>
        /// <param name="tkMethod">Metadata token pointing to place of method in running version of metadata.</param>
        /// <param name="remapper">IL offset remapper, used to build local variable changes.</param>
        /// <param name="placeholder">Product of method, represents translation from old variable set to the new one.</param>
        /// <returns>Metadata token pointing to lolcalVars signature, if not present, 0 is given.</returns>
        private uint buildILHeader(MethodDefinition newMethod, MethodDefinition originMethod,
            SequencePointRemapper remapper, out Dictionary<int, int> placeholder)
        {

            uint local_var_old, token;
            uint local_var = newMethod.Body.LocalVarToken.ToUInt32();
            uint code_size = (uint)newMethod.Body.CodeSize;
            uint max_stack_size = (uint)newMethod.Body.MaxStackSize;
            bool old_tiny = originMethod.Body.IsTiny;
            bool tiny = newMethod.Body.IsTiny;
            placeholder = null;

            if (old_tiny && !tiny) {
                // when new method is fat and the old tiny
                byte[] sig = metadata.NewImporter.GetSigantureSA(local_var);
                Signature signa = new Signature(sig);
                signa.Migrate(metadata);
                byte[] sig2 = signa.Compress();
                metadata.OldEmitter.CorMetaDataEmit.GetTokenFromSig(sig2, (uint)sig2.Length, out token);
                tiny = false;
            } else if (!old_tiny && tiny) {
                // when new method is tiny and old fat
                local_var_old = originMethod.Body.LocalVarToken.ToUInt32();
                local_var = local_var_old;
                max_stack_size = 8;
                tiny = false;
            } else if (!old_tiny && !tiny) {
                // both methods are fat
                Signature sigNew = new Signature(metadata.NewImporter.GetSigantureSA(local_var));
                Signature sigOld = new Signature(metadata.OldImporter.GetSigantureSA(originMethod.Body.LocalVarToken.ToUInt32()));
                sigNew.Migrate(metadata);

                // Instead of unused old local variables use placeholders
                LocalVarDiff varDiff = new LocalVarDiff(Builder.Manager.ResourceManager.CurrentModule.SymReader,
                    Builder.SymbolWriter.CorSymNewReader, remapper);
                
                byte[] sig = varDiff.makeSignature(sigOld, sigNew, out placeholder, originMethod.MetadataToken.ToUInt32(),
                    newMethod.MetadataToken.ToUInt32(),metadata).Compress();

                metadata.OldEmitter.CorMetaDataEmit.GetTokenFromSig(sig, (uint)sig.Length, out local_var);
            }

            //there is no need to change IL and MetaData when both methods are tiny

            if (!tiny) {
                uint header_flags = 0x3013;

                buffer.AddRange(BitConverter.GetBytes((ushort)header_flags));//BitConverter.GetBytes((ushort)header_flags));//.uintToByteArray(header_flags,2));
                buffer.AddRange(BitConverter.GetBytes((ushort)max_stack_size));//Util.uintToByteArray(max_stack_size,2));
                buffer.AddRange(BitConverter.GetBytes(code_size));//.uintToByteArray(code_size));
                buffer.AddRange(BitConverter.GetBytes(local_var));//.uintToByteArray(local_var));

                originMethod.Body.LocalVarToken = new MetadataToken(local_var);
                return local_var;
            } else {
                buffer.Add((byte)((code_size << 2) | 0x2));

                return 0;
            }
        }
        /// <summary>
        /// Build IL header for new method.
        /// </summary>
        /// <param name="methodDef">Definition of new method.</param>
        /// <returns>Metadata token to local variables signature.</returns>
        private uint buildILHeader(MethodDefinition methodDef)
        {
            uint local_var = methodDef.Body.LocalVarToken.ToUInt32();
            uint code_size = (uint)methodDef.Body.CodeSize;
            uint max_stack_size = (uint)methodDef.Body.MaxStackSize;
            bool tiny = methodDef.Body.IsTiny;



            if (!tiny) {
                uint header_flags = 0x3013;

                byte[] sig = metadata.NewImporter.GetSigantureSA(local_var);
                Signature signa = new Signature(sig);
                signa.Migrate(metadata);
                byte[] sig2 = signa.Compress();
                metadata.OldEmitter.CorMetaDataEmit.GetTokenFromSig(sig2, (uint)sig2.Length, out local_var);

                buffer.AddRange(BitConverter.GetBytes((ushort)header_flags));
                buffer.AddRange(BitConverter.GetBytes((ushort)max_stack_size));//Util.uintToByteArray(max_stack_size,2));
                buffer.AddRange(BitConverter.GetBytes(code_size));//.uintToByteArray(code_size));
                buffer.AddRange(BitConverter.GetBytes(local_var));//.uintToByteArray(local_var));

                return local_var;
            } else {
                buffer.Add((byte)((code_size << 2) | 0x2));

                return 0;
            }
        }
        /// <summary>
        /// Stores byte representation of operand to the IL stream. If neccessary, 
        /// translate metadata token to original metadata scope.
        /// </summary>
        /// <param name="operand">Operand, that is being processed.</param>
        private void processOperand(Object operand)
        {
            if (operand is Mono.Cecil.Cil.Operand) {

                //operand with token
                MetadataToken token = metadata.TranslateToken(((Mono.Cecil.Cil.Operand)operand).Token);
                buffer.AddRange(BitConverter.GetBytes(token.ToUInt32()));//uintToByteArray(token.ToUInt32()));

            } else if (operand != null) {

                // operand as value

                if (operand is SByte) {
                    System.SByte oper = (SByte)operand;
                    byte a;
                    unchecked {
                        a = (byte)((IConvertible)oper).ToSByte(null);
                    }
                    buffer.Add(a);
                } else if (operand is Int32) {
                    Int32 oper = (Int32)operand;
                    buffer.AddRange(BitConverter.GetBytes(((IConvertible)oper).ToInt32(null)));
                } else if (operand is Int64) {
                    Int64 oper = (Int64)operand;
                    buffer.AddRange(BitConverter.GetBytes(((IConvertible)oper).ToInt64(null)));
                } else if (operand is Double) {
                    byte[] arr = BitConverter.GetBytes(((IConvertible)operand).ToDouble(null));
                    buffer.AddRange(arr);
                } else if (operand is Single) {
                    byte[] arr = BitConverter.GetBytes(((IConvertible)operand).ToSingle(null));
                    buffer.AddRange(arr);
                } else if (operand is UInt16) {
                    byte[] arr = BitConverter.GetBytes(((IConvertible)operand).ToUInt16(null));
                    buffer.AddRange(arr);
                } else if (operand is byte) {
                    buffer.Add((byte)operand);
                } else if (operand is Int32[]) {
                    Int32[] oper = (Int32[])operand;
                    buffer.AddRange(BitConverter.GetBytes(((IConvertible)oper.Length).ToInt32(null)));
                    foreach (Int32 a in oper) {
                        byte[] arr = BitConverter.GetBytes(((IConvertible)a).ToInt32(null));
                        buffer.AddRange(arr);
                    }
                } else {
                    throw new NotImplementedException();
                }
            }
        }
        /// <summary>
        /// Copy parameter definition from the source to the target.
        /// </summary>
        /// <param name="source">Source of parameters.</param>
        /// <param name="target">Target for parameters.</param>
        private void copyParameters(MethodDefinition source, MethodDefinition target)
        {
            foreach (ParameterDefinition element in source.Parameters) {
                target.Parameters.Add(new ParameterDefinition(element.Name, element.Attributes,
                                                                metadata.FindTypeReference(Builder.Manager.ResourceManager.OldAssembly,
                                                                element.ParameterType.Scope.Name, element.ParameterType.Name)));
            }
        }

    }
    /// <summary>
    /// Structure descripting method translation and its resulting IL code.
    /// </summary>
    public struct MethodDescriptor
    {
        public uint destToken;
        public uint srcToken;
        public uint localVarsToken;
        public uint newRva;
        public byte[] codeIL;
    }
}
