/*
 * Created by SharpDevelop.
 * User: Honza
 * Date: 11.12.2011
 * Time: 1:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.IO;

using Mono.Cecil;

namespace EnC.MetaData
{
    /// <summary>
    /// Class for compressing and decompressing signatures, with ability to translate tokens in it.
    /// </summary>
    public class Signature
    {
        /// <summary>
        /// Buffer for compressing.
        /// </summary>
        MemoryStream output = null;
        /// <summary>
        /// Holds decompressed signature.
        /// </summary>
        List<uint> signature = new List<uint>();
        /// <summary>
        /// Types with tokens in signature.
        /// </summary>
        List<ElementType> types = new List<ElementType>();
        /// <summary>
        /// Types with tokens in signature.
        /// </summary>
        public List<ElementType> Types { get { return types; } }
        /// <summary>
        /// If there is padding at start of byte array.
        /// </summary>
        int startIndex;

        /// <summary>
        /// Compress this signature to byte array.
        /// </summary>
        /// <returns>Byte array of compressed signature.</returns>
        public byte[] Compress()
        {
            output = new MemoryStream();
            writeSignature();
            output.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[(int)output.Length];
            output.Read(buffer, 0, (int)output.Length);
            output.Dispose();
            return buffer;
        }
        public Signature(byte[] compressed_sig,int startIndex=2)
        {
            MemoryStream input = new MemoryStream(compressed_sig);
            this.startIndex = startIndex;
            readSignature(input);
            input.Dispose();
        }
        /// <summary>
        /// Writes integer in compressed mode to <c>output</c> stream.
        /// </summary>
        /// <param name="data">Input to be compressed.</param>
        private void writeCompressed(uint data)
        {
            if (data <= 0x80) {
                output.WriteByte((byte)(data & 0x7F));
            } else if (data <= 0x3FFF) {
                byte[] sig = BitConverter.GetBytes(data);
                sig[1] = (byte)((sig[1] | 0x80) & 0xBF);
                output.WriteByte(sig[1]);
                output.WriteByte(sig[0]);
            } else {
                byte[] sig = BitConverter.GetBytes(data);
                sig[3] = (byte)((sig[3] | 0xC0) & 0xDF);
                output.WriteByte(sig[3]);
                output.WriteByte(sig[2]);
                output.WriteByte(sig[1]);
                output.WriteByte(sig[0]);
            }
        }
        /// <summary>
        /// Write signature to <c>output</c> stream.
        /// </summary>
        private void writeSignature()
        {
            foreach (ElementType element in types) {
                writeCompressed(element.Code);
                if(element.Operand !=null){
                        writeCompressed(OutputWithToken(element));
                }
            }
        }
        /// <summary>
        /// Reads and decompress signature from stream <c>input</c>.
        /// </summary>
        /// <param name="input">Input stream</param>
        private void readSignature(MemoryStream input)
        {
            int pos = 0;
            while (input.Position < input.Length) {
                uint elIndex = getNextDecompressed(input);
                if (pos >= startIndex) {
                    switch (elIndex) {
                        case 0x11:
                            types.Add(ParseWithToken(elIndex,input));
                            break;
                        case 0x12:
                            types.Add(ParseWithToken(elIndex,input));
                            break;
                        case 0x1f:
                            types.Add(ParseWithToken(elIndex, input));
                            break;
                        case 0x20:
                            types.Add(ParseWithToken(elIndex, input));
                            break;
                        default:
                            signature.Add(elIndex);
                            types.Add(new ElementType(elIndex));
                            break;
                    }
                } else {
                    signature.Add(elIndex);
                    types.Add(new ElementType(elIndex));
                }
                ++pos;
            }
        }
        /// <summary>
        /// Adds part of signature to the end.
        /// </summary>
        /// <param name="compressed_sig">Compressed byte array of signature.</param>
        public void AddSigPart(byte[] compressed_sig)
        {
            int tmp = startIndex;
            startIndex = 0;
            MemoryStream input = new MemoryStream(compressed_sig);
            readSignature(input);
            startIndex = tmp;
            input.Dispose();
        }
        /// <summary>
        /// Parse from stream code and token.
        /// </summary>
        /// <param name="elIndex">Code describing usage of token.</param>
        /// <param name="input">Input stream.</param>
        /// <returns>New instance of ElementType containing token and code.</returns>
        private ElementType ParseWithToken(uint elIndex,MemoryStream input)
        {
            uint oper = getNextDecompressed(input);
            if (oper % 4 == 0) {
                //0x02
                return new ElementType(elIndex, new MetadataToken(TokenType.TypeDef, oper >> 2));
            } else if (oper % 4 == 1) {
                //0x01
                return new ElementType(elIndex, new MetadataToken(TokenType.TypeRef, oper >> 2));
            } else if (oper % 4 == 2) {
                //0x1b
                return new ElementType(elIndex, new MetadataToken(TokenType.TypeSpec, oper >> 2));
            } else {
                throw new TranslatingException("Unexpected signature format.");
            }
        }
        /// <summary>
        /// Converts ElementType to <c>uint</c>.
        /// </summary>
        /// <param name="type">ElementType instance</param>
        /// <returns></returns>
        private uint OutputWithToken(ElementType type)
        {
            MetadataToken? token = type.Operand as MetadataToken?;
            if (token != null) {
                uint tid = 0;
                switch (token.Value.TokenType) {
                    case TokenType.TypeRef:
                        tid = 1;
                        break;
                    case TokenType.TypeDef:
                        tid = 0;
                        break;
                    case TokenType.TypeSpec:
                        tid = 2;
                        break;
                }
                return (token.Value.RID << 2) | tid;
            } else {
                throw new TranslatingException("Signature serialize: Argument error - type has no operand token.");
            }
        }
        /// <summary>
        /// Get next <c>uint</c> by decompressing from <c>input</c> stream.
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <returns>Result of decompression</returns>
        private uint getNextDecompressed(MemoryStream input)
        {
            int b = input.ReadByte();
            if (b == -1)
                return 0;
            byte actual = (byte)b;
            if ((actual >> 7) == 0) {
                return (Convert.ToUInt32(actual % 0x80));
            } else {
                if ((actual >> 6) == 2) {
                    byte[] val = { (byte)input.ReadByte(), (byte)(actual % 0x40), 0, 0, };
                    return (BitConverter.ToUInt32(val, 0));
                } else {
                    byte next_1 = (byte)input.ReadByte();
                    byte next_2 = (byte)input.ReadByte();
                    byte next_3 = (byte)input.ReadByte();
                    byte[] val = {next_3,next_2,
						next_1,(byte)(actual % 0x20) };
                    return (BitConverter.ToUInt32(val, 0));
                }
            }
        }
        /// <summary>
        /// Migrate whole signature to other metadata scope.
        /// </summary>
        /// <param name="translator">Translator used for migration</param>
        public void Migrate(ITokenTranslator translator)
        {
            foreach (ElementType el in this.types) {
                if (el.Operand is MetadataToken) {
                    el.Operand = translator.TranslateToken((MetadataToken)el.Operand);
                }
            }
        }
        /// <summary>
        /// Decompress signature, migrate to other metadata scope and compress again.
        /// </summary>
        /// <param name="oldSignature">Compressed old signature</param>
        /// <param name="translator">Token translator</param>
        /// <param name="withStart">Indicates, whether there is padding at start of signature.</param>
        /// <returns>Compressed migrated signature in byte array</returns>
        public static byte[] Migrate(byte[] oldSignature, ITokenTranslator translator,int startIndex = 2)
        {
            Signature sig = new Signature(oldSignature,startIndex);
            sig.Migrate(translator);
            return sig.Compress();
        }
    }
    /// <summary>
    /// Describe token and its usage in signature.
    /// </summary>
    public class ElementType
    {
        public uint Code { get; set; }
        public object Operand { get; set; }
        public ElementType(uint code, object operand = null)
        {
            this.Code = code;
            this.Operand = operand;
        }
    }
}