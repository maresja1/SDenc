﻿/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 12.12.2011
 * Time: 10:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using EnC.EditorEvents;
using EnC.MetaData;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Dom;
using Mono.Cecil;
using EnC.DeltaSymbols;

namespace EnC
{
    /// <summary>
    /// Class preparing delta structures for EnC changes.
    /// </summary>
    public class DeltaBuilder
    {
        /// <summary>
        /// Buffer containing IL code stream.
        /// </summary>
        public List<byte> dIL { get; private set; }
        /// <summary>
        /// Sequence point for new methods, used later when editting them. Indetified by methods metadata token.
        /// </summary>
        public Dictionary<uint, List<SequencePoint>> NewMethodsSequencePoints { get; private set; }
        /// <summary>
        /// Table of <see cref="SequencePointRemapper">SequencePointRemapper</see> identified by original methods metadata token.
        /// </summary>
        public Dictionary<uint, SequencePointRemapper> SequencePointRemappers { get; private set; }
        /// <summary>
        /// Table of method definitions for methods added earlier by EnC. Used when editting them later.
        /// </summary>
        public Dictionary<uint, MethodDefinition> newMethods = new Dictionary<uint, MethodDefinition>();
        /// <summary>
        /// Reference to instance of <see cref="SymbolWriterClass">SymbolWriterClass</see> used for
        /// emitting and translating symbols of changed methods.
        /// </summary>
        public DeltaSymbols.SymbolWriterClass SymbolWriter
        {
            get;
            set;
        }
        /// <summary>
        /// Parenting instance of <see cref="EnCManager">EnCManager</see>.
        /// </summary>
        public EnCManager Manager
        {
            get;
            private set;
        }
        public DeltaBuilder(EnCManager manager)
        {
            NewMethodsSequencePoints = new Dictionary<uint, List<SequencePoint>>();
            SequencePointRemappers = new Dictionary<uint, SequencePointRemapper>();
            this.Manager = manager;
            dIL = new List<byte>();
        }

        /// <summary>
        /// Get parsed MetaData delta.
        /// </summary>
        /// <returns>MetaData parsed as byte array.</returns>
        public byte[] getDeltaMetadata()
        {
            uint size;
            Manager.MetadataManager.OldEmitter.CorMetaDataEmit.GetDeltaSaveSize(0, out size);
            byte[] dMeta = new byte[size];

            Manager.MetadataManager.OldEmitter.CorMetaDataEmit.SaveDeltaToMemory(dMeta, size);
            return dMeta;
        }
        /// <summary>
        /// Get IL code generated by steps in DeltaBuilder.
        /// </summary>
        /// <returns>IL code in byte array.</returns>
        public byte[] getDeltaIL()
        {
            dIL.InsertRange(0, BitConverter.GetBytes((uint)dIL.Count));
            return dIL.ToArray();
        }
        /// <summary>
        /// Make generic change to running assembly using EnC.
        /// </summary>
        /// <param name="change">Description of change.</param>
        public void MakeChange(SourceChange change)
        {
            switch (change.MemberKind) {
                case SourceChangeMember.Method:
                    if (change.MemberAction == SourceChangeAction.BodyChanged) {
                        ChangeMethod((IMethod)change.OldEntity);
                    } else if (change.MemberAction == SourceChangeAction.Created &&
                              change.NewEntity.IsPrivate && !change.NewEntity.IsVirtual) {
                        AddMethod((IMethod)change.NewEntity);
                    } else {
                        throw new TranslatingException("This kind of change is not supported by this version of EnC.");
                    }
                    break;
                case SourceChangeMember.Property:
                    if (change.MemberAction == SourceChangeAction.BodyChanged) {
                        ChangeProperty((IProperty)change.OldEntity, ((BodyChange)change).isGetter);
                    } else {
                        throw new TranslatingException("This kind of change is not supported by this version of EnC.");
                    }
                    break;
                case SourceChangeMember.Field:
                    if (change.MemberAction == SourceChangeAction.Created) {
                        addField((IField)change.NewEntity);
                    } else {
                        throw new TranslatingException("This kind of change is not supported by this version of EnC.");
                    }
                    break;
                default:
                    throw new TranslatingException("This kind of change is not supported by this version of EnC.");
            }
        }
        /// <summary>
        /// Changes method IL and method metadata.
        /// </summary>
        /// <param name="method">Method to be changed by EnC.</param>
        public void ChangeMethod(IMethod method)
        {
            // Method RVA is offset to start of byte buffer
            uint RVA = (uint)dIL.Count + 4;

            MethodDefinition targtMet, targtMetOld;

            // Find appropriete new version of method in new assembly.
            targtMet = Manager.MetadataManager.FindMethod(Manager.ResourceManager.NewAssembly,
                                                          Manager.ResourceManager.NewAssemblyName, method);
            if (targtMet == null) throw new TranslatingException("Method " + method.ToString() + " could not be found in emitted module");

            // Find appropriete original version of method in running assembly.
            targtMetOld = Manager.MetadataManager.FindMethod(Manager.ResourceManager.OldAssembly,
                                                             Manager.ResourceManager.CurrentModule.Name, method);
            if (targtMetOld == null) throw new TranslatingException("Method " + method.ToString() + " could not be found in debugged module");

            // Get SequencePointRemapper for method (needed to diff local definitions)
            SequencePointRemapper remapper = genSequencePointRemapper(targtMetOld, targtMet);
            // Translate tokens in methods IL code.
            MethodTranslator translator = new MethodTranslator(this);

            Dictionary<int, int> placeholder;
            MethodDescriptor log = translator.TranslateMethod(targtMet, targtMetOld, remapper, out placeholder);
            log.newRva = RVA;

            // Set method RVA.
            Manager.MetadataManager.OldEmitter.CorMetaDataEmit.SetRVA(targtMetOld.MetadataToken.ToUInt32(), RVA);

            // Store methods IL to buffer
            dIL.AddRange(log.codeIL);
            if (SymbolWriter != null) {
                SymbolWriter.EmitMethod(log.destToken, log.srcToken, placeholder);
            }
        }
        /// <summary>
        /// Creates <see cref="SequencePointRemapper">SequencePointRemapper</see> - sequence point difference tool for 
        /// specified method used in debug state refreshing and local variables definition.
        /// </summary>
        /// <param name="methodDef">Method to which generating takes place.</param>
        /// <returns>Instance of <see cref="SequencePointRemapper">SequencePointRemapper</see>.</returns>
        private SequencePointRemapper genSequencePointRemapper(MethodDefinition methodDefOld, MethodDefinition methodDefNew)
        {
            uint token = methodDefOld.MetadataToken.ToUInt32();

            List<SequencePoint> oldSeqs = SymbolWriterClass.GetMethodSequencePoints(
                Manager.ResourceManager.CurrentModule.SymReader, methodDefOld.MetadataToken.ToUInt32());

            List<SequencePoint> newSeqs = SymbolWriterClass.GetMethodSequencePoints(
                SymbolWriter.CorSymNewReader, methodDefNew.MetadataToken.ToUInt32());

            SequencePointRemapper remapper = new SequencePointRemapper(oldSeqs, newSeqs, Manager.ActualEditEvent.sourceTextChanges[token]);
            SequencePointRemappers.Add(token, remapper);

            return remapper;
        }
        /// <summary>
        /// Change IL code and metadata for property.
        /// </summary>
        /// <param name="property">Interaface with info abour property.</param>
        /// <param name="isGetter">Determines whether it is getter method for property.</param>
        public void ChangeProperty(IProperty property, bool isGetter)
        {
            // Method RVA is offset to start of byte buffer
            uint RVA = (uint)dIL.Count + 4;

            PropertyDefinition targtProp, targtPropOld;
            MethodDefinition targtMet, targtMetOld;

            // Find appropriete new version of method in new assembly.
            targtProp = Manager.MetadataManager.FindProperty(Manager.ResourceManager.NewAssembly,
                                                          Manager.ResourceManager.NewAssemblyName, property);
            if (targtProp == null) new TranslatingException("Property " + property.ToString() + " could not be found in emitted module");

            // Find appropriete original version of method in running assembly.
            targtPropOld = Manager.MetadataManager.FindProperty(Manager.ResourceManager.OldAssembly,
                                                             Manager.ResourceManager.CurrentModule.Name, property);
            if (targtPropOld == null) new TranslatingException("Property " + property.ToString() + " could not be found in debugged module");

            targtMet = (isGetter ? targtProp.GetMethod : targtProp.SetMethod);
            targtMetOld = (isGetter ? targtPropOld.GetMethod : targtPropOld.SetMethod);

            SequencePointRemapper remapper = genSequencePointRemapper(targtMetOld, targtMet);
            // Translate tokens in methods IL code.
            MethodTranslator translator = new MethodTranslator(this);
            Dictionary<int, int> placeholder;
            MethodDescriptor log = translator.TranslateMethod(targtMet, targtMetOld, remapper, out placeholder);
            log.newRva = RVA;

            // Set method RVA.
            Manager.MetadataManager.OldEmitter.CorMetaDataEmit.SetRVA(targtMetOld.MetadataToken.ToUInt32(), RVA);

            // Store methods IL to buffer
            dIL.AddRange(log.codeIL);
            if (SymbolWriter != null) {
                SymbolWriter.EmitMethod(log.destToken, log.srcToken, placeholder);
            }
        }
        /// <summary>
        /// Adds new method to IL code stream and metadata.
        /// </summary>
        /// <param name="method">Interface describing method.</param>
        public void AddMethod(IMethod method)
        {
            uint RVA = (uint)dIL.Count + 4;

            MethodDefinition targtMet, emittedMehtod;

            targtMet = Manager.MetadataManager.FindMethod(Manager.ResourceManager.NewAssembly,
                                                          Manager.ResourceManager.NewAssemblyName, method);
            if (targtMet == null) throw new TranslatingException("Method " + method.ToString() + " could not be found in emitted module");

            MethodTranslator translator = new MethodTranslator(this);
            MethodDescriptor log = translator.TranslateNewMethod(targtMet, out emittedMehtod);

            Manager.MetadataManager.OldEmitter.CorMetaDataEmit.SetRVA(log.destToken, RVA);

            TypeDefinition typeDef = Manager.MetadataManager.FindTypeDefinition(Manager.ResourceManager.OldAssembly,
                                                                                Manager.ResourceManager.CurrentModule.Name, method.DeclaringType.FullyQualifiedName);
            if (typeDef != null)
                typeDef.Methods.Add(emittedMehtod);

            Manager.ResourceManager.CurrentModule.RemoveFromLoadedTypes(targtMet.DeclaringType.FullName);

            dIL.AddRange(log.codeIL);
            if (SymbolWriter != null) {
                List<SequencePoint> points = SymbolWriter.EmitMethod(log.destToken, log.srcToken);
                NewMethodsSequencePoints.Add(targtMet.MetadataToken.ToUInt32(), points);
            }
        }
        /// <summary>
        /// Add method to metadata, but not to IL code stream. Used for upcoming search for metadata token.
        /// </summary>
        /// <param name="method">Interface describing method.</param>
        public void PreAddMethod(IMethod method)
        {
            MethodDefinition targtMet;

            targtMet = Manager.MetadataManager.FindMethod(Manager.ResourceManager.NewAssembly,
                                                          Manager.ResourceManager.NewAssemblyName, method);
            if (targtMet == null) throw new TranslatingException("Method " + method.ToString() + " could not be found in emitted module");

            MethodTranslator translator = new MethodTranslator(this);
            MethodDescriptor log = translator.TranslateMethodWithoutBody(targtMet);
            Manager.MetadataManager.RegisterNewMethod(targtMet.MetadataToken, new MetadataToken(log.destToken));

            newMethods.Add(targtMet.MetadataToken.ToUInt32(), targtMet);
        }
        private void addField(IField field)
        {
            FieldDefinition fDef =  Manager.MetadataManager.FindField(Manager.ResourceManager.NewAssembly,
                                                Manager.ResourceManager.NewAssemblyName, field);
            if (fDef == null) throw new TranslatingException("Field " + field.ToString() + " could not be found in emitted module");
            
            TypeDefinition tDef =  Manager.MetadataManager.FindTypeDefinition(Manager.ResourceManager.NewAssembly,
                                                Manager.ResourceManager.NewAssemblyName, field.DeclaringType.FullyQualifiedName);

            if (tDef == null) throw new TranslatingException("Type " + field.DeclaringType.FullyQualifiedName + " could not be found in emitted module");

            MetadataToken res = Manager.MetadataManager.TranslateToken(fDef.MetadataToken);
        }
    }
}