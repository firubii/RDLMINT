using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RDLMINT
{
    class Program
    {
        public static List<string> opcodeNames = new List<string>()
        {
            "00", "setTrue", "setFalse", "load", "loadString", "moveRegister", "moveResult", "setArg", "08", "getStatic", "loadDeref", "sizeOf", "storeDeref", "storeStatic", "addi", "subi", "multi", "divi", "modi", "inci", "deci", "negi", "addf", "subf", "multif", "divf", "incf", "decf", "negf", "intLess", "intLessOrEqual", "intEqual", "intNotEqual", "floatLess", "floatLessOrEqual", "floatEqual", "floatNotEqual", "cmpLess", "cmpLessOrEqual", "boolEqual", "boolNotEqual", "bitAnd", "bitOr", "bitXor", "nti", "not", "slli", "slr", "jump", "jumpIfEqual", "jumpIfNotEqual", "declare", "return", "returnVal", "call", "yield", "copy", "zero", "new", "sppshz", "del", "getField", "makeArray", "arrayIndex", "arrayLength", "deleteArray"
        };

        static void Main(string[] args)
        {
            string filepath;
            byte[] mintArchive;
            //args = new string[] { "-r", "MINT\\User.Tsuruoka.MintTest.txt", "-f" };
            //args = new string[] { "-x", "Archive.bin" };
            if (args.Length > 0)
            {
                if (args[0] == "-x")
                {
                    if (args.Length > 1 && File.Exists(args[1]))
                    {
                        if (args.Contains("-f"))
                        {
                            filepath = args[1];
                            byte[] mintScript = File.ReadAllBytes(filepath);
                            if (Encoding.UTF8.GetString(mintScript, 0, 4) == "XBIN")
                            {
                                if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\MINT\"))
                                {
                                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\MINT\");
                                }
                                Console.WriteLine("Decompiling script " + filepath);
                                DecompileScript(mintScript, filepath.Split('\\').Last().Replace(".bin", ""));
                            }
                            else
                            {
                                Console.WriteLine("Error: Provided file is not a MINT script");
                            }
                        }
                        else
                        {
                            filepath = args[1];
                            mintArchive = File.ReadAllBytes(filepath);
                            if (Encoding.UTF8.GetString(mintArchive, 0, 4) == "XBIN")
                            {
                                if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\MINT\"))
                                {
                                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\MINT\");
                                }
                                uint scriptCount = ReverseBytes(BitConverter.ToUInt32(mintArchive, 0x10));
                                Console.WriteLine("Found " + scriptCount + " scripts");
                                for (int i = 0; i < scriptCount; i++)
                                {
                                    uint fileOffset = ReverseBytes(BitConverter.ToUInt32(mintArchive, 0x14 + (i * 0x8) + 0x4));
                                    uint stringOffset = ReverseBytes(BitConverter.ToUInt32(mintArchive, 0x14 + (i * 0x8))) + 0x4;
                                    uint stringLength = ReverseBytes(BitConverter.ToUInt32(mintArchive, (int)(stringOffset - 0x4)));
                                    string scriptName = Encoding.UTF8.GetString(mintArchive, (int)stringOffset, (int)stringLength);
                                    Console.WriteLine("Dumping script " + scriptName);
                                    List<byte> script = new List<byte>();
                                    uint scriptLength = ReverseBytes(BitConverter.ToUInt32(mintArchive, (int)fileOffset + 0x8));
                                    script.AddRange(mintArchive.Skip((int)fileOffset).Take((int)scriptLength));
                                    File.WriteAllBytes(Directory.GetCurrentDirectory() + @"\MINT\" + scriptName + ".bin", script.ToArray());
                                    DecompileScript(script.ToArray(), scriptName);
                                }
                                Console.WriteLine("Finished dumping scripts");
                            }
                            else
                            {
                                Console.WriteLine("Error: Provided file is not a MINT archive");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: File does not exist or no file provided");
                    }
                }
                else if (args[0] == "-r")
                {
                    if (args.Contains("-f"))
                    {
                        filepath = args[1];
                        string[] mintScript = File.ReadAllLines(filepath);
                        if (mintScript[0].StartsWith("script"))
                        {
                            if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\Compiled\"))
                            {
                                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Compiled\");
                            }
                            Console.WriteLine("Compiling script " + filepath);
                            File.WriteAllBytes(Directory.GetCurrentDirectory() + @"\Compiled\" + filepath.Split('\\').Last().Replace(".txt",".bin"), CompileScript(mintScript, filepath.Split('\\').Last().Replace(".txt", "")));
                            Console.WriteLine("Done!");
                        }
                        else
                        {
                            Console.WriteLine("Error: Provided file is not a MINT script");
                        }
                    }
                    else
                    {
                        filepath = args[1];
                        string[] files = Directory.GetFiles(filepath, "*.txt");
                        if (files.Length > 0)
                        {
                            List<byte> archive = new List<byte>()
                            {
                                0x58, 0x42, 0x49, 0x4E, 0x12, 0x34, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFD, 0xE9
                            };
                            List<byte[]> scripts = new List<byte[]>();
                            List<uint> scriptOffsets = new List<uint>();
                            List<string> scriptNames = new List<string>();
                            List<uint> nameOffsets = new List<uint>();
                            for (int i = 0; i < files.Length; i++)
                            {
                                if (CompileScript(File.ReadAllLines(files[i]), files[i].Split('\\').Last()) != null)
                                {
                                    scriptNames.Add(files[i].Split('\\').Last().Replace(".txt", ""));
                                    scripts.Add(CompileScript(File.ReadAllLines(files[i]), files[i].Split('\\').Last()));
                                }
                                else
                                {
                                    return;
                                }
                            }
                            archive.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scripts.Count)));
                            for (int i = 0; i < scripts.Count; i++)
                            {
                                archive.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                            }
                            for (int i = 0; i < scripts.Count; i++)
                            {
                                scriptOffsets.Add((uint)archive.Count);
                                archive.AddRange(scripts[i]);
                            }
                            for (int i = 0; i < scripts.Count; i++)
                            {
                                nameOffsets.Add((uint)archive.Count);
                                archive.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scriptNames[i].Length)));
                                archive.AddRange(Encoding.UTF8.GetBytes(scriptNames[i]));
                                archive.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                                while ((archive.Count).ToString("X").Last() != '0' && (archive.Count).ToString("X").Last() != '4' && (archive.Count).ToString("X").Last() != '8' && (archive.Count).ToString("X").Last() != 'C')
                                {
                                    archive.Add(0x00);
                                }
                            }
                            for (int i = 0; i < scripts.Count; i++)
                            {
                                archive.RemoveRange(0x14 + (i * 0x8), 0x4);
                                archive.InsertRange(0x14 + (i * 0x8), BitConverter.GetBytes(ReverseBytes(nameOffsets[i])));
                                archive.RemoveRange(0x18 + (i * 0x8), 0x4);
                                archive.InsertRange(0x18 + (i * 0x8), BitConverter.GetBytes(ReverseBytes(scriptOffsets[i])));
                            }
                            File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\compiled_Archive.bin", archive.ToArray());
                            Console.WriteLine($"Finished packing folder {filepath} to {Directory.GetCurrentDirectory() + "\\compiled_Archive.bin"}");
                        }
                    }
                }
                else if (args[0] == "-h")
                {
                    Help();
                }
            }
            else
            {
                Help();
            }
        }

        public static void Help()
        {
            Console.WriteLine("Usage: RDLMINT.exe <action> [options]");
            Console.WriteLine("Actions:");
            Console.WriteLine("    -x <file>:        Extract and decompile a MINT Archive or individual script");
            Console.WriteLine("    -r <folder|file>: Repack and compile a MINT Archive from a folder or individual script");
            Console.WriteLine("    -h:               Show this message");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("    -f:   Decompiles or compiles a single provided MINT script");
        }

        public static void DecompileScript(byte[] mintScript, string fileName)
        {
            uint xrefStart = ReverseBytes(BitConverter.ToUInt32(mintScript, 0x1C));
            List<byte> sdata = new List<byte>();
            List<string> xref = new List<string>();
            List<string> scriptDecomp = new List<string>()
            {
                "script " + fileName,
                "{",
            };
            //sdata decomp
            uint sdataSize = ReverseBytes(BitConverter.ToUInt32(mintScript, 0x28));
            if (sdataSize != 0)
            {
                sdata.AddRange(mintScript.Skip(0x2C).Take((int)sdataSize));
                string sdataString = "    SDATA { ";
                for (int i = 0; i < sdata.Count; i++)
                {
                    sdataString += "0x" + sdata[i].ToString("X2");
                    if (i != sdata.Count - 1)
                    {
                        sdataString += ", ";
                    }
                }
                sdataString.Remove(sdataString.Length - 2, 2);
                sdataString += " }";
                //scriptDecomp.Add(sdataString);
            }
            //xref decomp
            if (ReverseBytes(BitConverter.ToUInt32(mintScript, (int)xrefStart)) > 0)
            {
                //scriptDecomp.Add("    XREF\n    {");
                for (int i = 0; i < ReverseBytes(BitConverter.ToUInt32(mintScript, (int)xrefStart)); i++)
                {
                    if (ReverseBytes(BitConverter.ToUInt32(mintScript, ((int)xrefStart + 4) + (i * 4))) != 0x0)
                    {
                        uint stringOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, ((int)xrefStart + 4) + (i * 4))) + 4;
                        uint stringLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)stringOffset - 4));
                        string xrefString = Encoding.UTF8.GetString(mintScript, (int)stringOffset, (int)stringLength);
                        xref.Add(xrefString);
                        //scriptDecomp.Add("        " + xrefString);
                    }
                }
                //scriptDecomp.Add("    }");
            }
            //class & method decomp
            uint classStartOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, 0x20)) + 0x4;
            uint classCount = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)classStartOffset - 0x4));
            for (int c = 0; c < classCount; c++)
            {
                //class name
                uint classOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)classStartOffset + (c * 0x4)));
                uint classNameOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)classOffset));
                uint classNameLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)classNameOffset));
                string className = Encoding.UTF8.GetString(mintScript, (int)classNameOffset + 0x4, (int)classNameLength);
                scriptDecomp.AddRange(new string[] { "    class " + className, "    {" });

                //read variables
                uint varListStart = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)classOffset + 0x4));
                uint varCount = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)varListStart));
                for (int i = 0; i < varCount; i++)
                {
                    uint varOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)varListStart + 0x4 + (i * 0x4)));
                    uint varNameOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)varOffset));
                    uint varNameLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)varNameOffset));
                    string varName = Encoding.UTF8.GetString(mintScript, (int)varNameOffset + 0x4, (int)varNameLength);
                    
                    uint varTypeNameOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)varOffset + 0x4));
                    uint varTypeNameLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)varTypeNameOffset));
                    string varTypeName = Encoding.UTF8.GetString(mintScript, (int)varTypeNameOffset + 0x4, (int)varTypeNameLength);
                    scriptDecomp.AddRange(new string[] { "        " + varTypeName + " " + varName });
                }

                //read methods
                uint methodListStart = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)classOffset + 0x8));
                uint methodCount = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)methodListStart));
                for (int i = 0; i < methodCount; i++)
                {
                    uint methodOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)methodListStart + 0x4 + (i * 0x4)));
                    uint methodNameOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)methodOffset));
                    uint methodNameLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)methodNameOffset));
                    string methodName = Encoding.UTF8.GetString(mintScript, (int)methodNameOffset + 0x4, (int)methodNameLength);
                    scriptDecomp.AddRange(new string[] { "        " + methodName, "        {" });
                    //data
                    uint methodDataOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)methodOffset + 0x4));
                    for (uint b = methodDataOffset; b < mintScript.Length; b += 0x4)
                    {
                        try
                        {
                            string z = mintScript[b + 1].ToString("X2");
                            string x = mintScript[b + 2].ToString("X2");
                            string y = mintScript[b + 3].ToString("X2");
                            short v = BitConverter.ToInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                            string line = "            ";
                            //opcodes
                            if (mintScript[b] == 0x01)
                            {
                                line += $"setTrue r{z}";
                            }
                            else if (mintScript[b] == 0x02)
                            {
                                line += $"setFalse r{z}";
                            }
                            else if (mintScript[b] == 0x03)
                            {
                                line += $"load r{z}, 0x{ReverseBytes(BitConverter.ToUInt32(sdata.ToArray(), v)).ToString("X")}";
                            }
                            else if (mintScript[b] == 0x04)
                            {
                                string sdataString = "";
                                List<byte> stringBytes = new List<byte>();
                                for (short s = v; s < sdata.Count; s++)
                                {
                                    if (sdata[s] != 0x00)
                                    {
                                        stringBytes.Add(sdata[s]);
                                    }
                                    else
                                    {
                                        sdataString = Encoding.UTF8.GetString(stringBytes.ToArray());
                                        break;
                                    }
                                }
                                line += $"loadString r{z}, \"{sdataString}\"";
                            }
                            else if (mintScript[b] == 0x05)
                            {
                                line += $"moveRegister r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x06)
                            {
                                line += $"moveResult r{z}";
                            }
                            else if (mintScript[b] == 0x07)
                            {
                                line += $"setArg [{z}] r{x}";
                            }
                            else if (mintScript[b] == 0x09)
                            {
                                line += $"getStatic r{z}, {xref[v]}";
                            }
                            else if (mintScript[b] == 0x0A)
                            {
                                line += $"loadDeref r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x0B)
                            {
                                line += $"sizeOf r{z}, {xref[v]}";
                            }
                            else if (mintScript[b] == 0x0C)
                            {
                                line += $"storeDeref r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x0D)
                            {
                                line += $"storeStatic r{z}, {xref[v]}";
                            }
                            else if (mintScript[b] == 0x0E)
                            {
                                line += $"addi r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x0F)
                            {
                                line += $"subi r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x10)
                            {
                                line += $"multi r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x11)
                            {
                                line += $"divi r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x12)
                            {
                                line += $"modi r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x13)
                            {
                                line += $"inci r{z}";
                            }
                            else if (mintScript[b] == 0x14)
                            {
                                line += $"deci r{z}";
                            }
                            else if (mintScript[b] == 0x15)
                            {
                                line += $"negi r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x16)
                            {
                                line += $"addf r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x17)
                            {
                                line += $"subf r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x18)
                            {
                                line += $"multf r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x19)
                            {
                                line += $"divf r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x1A)
                            {
                                line += $"incf r{z}";
                            }
                            else if (mintScript[b] == 0x1B)
                            {
                                line += $"decf r{z}";
                            }
                            else if (mintScript[b] == 0x1C)
                            {
                                line += $"negf r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x1D)
                            {
                                line += $"intLess r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x1E)
                            {
                                line += $"intLessOrEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x1F)
                            {
                                line += $"intEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x20)
                            {
                                line += $"intNotEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x21)
                            {
                                line += $"floatLess r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x22)
                            {
                                line += $"floatLessOrEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x23)
                            {
                                line += $"floatEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x24)
                            {
                                line += $"floatNotEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x25)
                            {
                                line += $"cmpLess r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x25)
                            {
                                line += $"cmpLessOrEqual r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x27)
                            {
                                line += $"boolEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x28)
                            {
                                line += $"boolNotEqual r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x29)
                            {
                                line += $"bitAnd r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x2A)
                            {
                                line += $"bitOr r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x2B)
                            {
                                line += $"bitXor r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x2C)
                            {
                                line += $"nti r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x2D)
                            {
                                line += $"not r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x2E)
                            {
                                line += $"slli r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x2F)
                            {
                                line += $"slr r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x30)
                            {
                                line += $"jump {v}";
                            }
                            else if (mintScript[b] == 0x31)
                            {
                                line += $"jumpIfEqual {v}, r{z}";
                            }
                            else if (mintScript[b] == 0x32)
                            {
                                line += $"jumpIfNotEqual {v}, r{z}";
                            }
                            else if (mintScript[b] == 0x33)
                            {
                                line += $"declare {z}, {x}";
                            }
                            else if (mintScript[b] == 0x34)
                            {
                                line += $"return";
                                scriptDecomp.Add(line);
                                break;
                            }
                            else if (mintScript[b] == 0x35)
                            {
                                line += $"returnVal r{x}";
                                scriptDecomp.Add(line);
                                break;
                            }
                            else if (mintScript[b] == 0x36)
                            {
                                line += $"call {xref[v]}";
                            }
                            else if (mintScript[b] == 0x37)
                            {
                                line += $"yield r{z}";
                            }
                            else if (mintScript[b] == 0x38)
                            {
                                line += $"copy r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x39)
                            {
                                line += $"zero r{z}, r{x}, r{y}";
                            }
                            else if (mintScript[b] == 0x3A)
                            {
                                line += $"new r{z}, r{xref[v]}";
                            }
                            else if (mintScript[b] == 0x3B)
                            {
                                line += $"sppshz r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x3C)
                            {
                                line += $"del r{z}, {xref[v]}";
                            }
                            else if (mintScript[b] == 0x3D)
                            {
                                line += $"getField r{z}, {xref[v]}";
                            }
                            else if (mintScript[b] == 0x3E)
                            {
                                line += $"makeArray r{z}";
                            }
                            else if (mintScript[b] == 0x3F)
                            {
                                line += $"arrayIndex r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x40)
                            {
                                line += $"arrayLength r{z}, r{x}";
                            }
                            else if (mintScript[b] == 0x41)
                            {
                                line += $"deleteArray r{z}";
                            }
                            scriptDecomp.Add(line);
                        }
                        catch
                        {
                            Console.WriteLine($"Error: Could not process command 0x{mintScript[b].ToString("X2")} at 0x{b.ToString("X8")} in script {fileName}\n    Full command: 0x{mintScript[b].ToString("X2")}{mintScript[b + 1].ToString("X2")}{mintScript[b + 2].ToString("X2")}{mintScript[b + 3].ToString("X2")}");
                            Thread.Sleep(2000);
                        }
                    }
                    scriptDecomp.Add("        }");
                }
                scriptDecomp.Add("    }");
            }
            scriptDecomp.Add("}");
            File.WriteAllLines(Directory.GetCurrentDirectory() + @"\MINT\" + fileName + ".txt", scriptDecomp.ToArray());
        }

        public static byte[] CompileScript(string[] mintScript, string filename)
        {
            if (mintScript[0].Split(' ')[0] == "script")
            {
                List<byte> scriptBytes = new List<byte>()
                {
                    0x58, 0x42, 0x49, 0x4E, 0x12, 0x34, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0xA4,
                    0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };
                string scriptName = mintScript[0].Split(' ')[1];
                Console.WriteLine("Compiling " + scriptName);

                //cmd prep

                //remove indentions
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string line = mintScript[i];
                    int spaceCount = 0;
                    for (int c = 0; c < line.Length; c++)
                    {
                        if (line[c] == ' ')
                        {
                            spaceCount++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    line = line.Remove(0, spaceCount);
                    mintScript[i] = line;
                }

                //sdata
                List<byte> sdata = new List<byte>();
                int sdataLoadsCount = 0;
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string[] parsedLine = mintScript[i].Replace(",", "").Split(' ');
                    if (parsedLine[0] == "load" || parsedLine[0] == "loadString")
                    {
                        sdataLoadsCount++;
                    }
                }
                int sdataLoadCurrent = 0;
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string[] parsedLine = mintScript[i].Replace(",", "").Split(' ');
                    string line = mintScript[i];
                    string sdataString = "";
                    int sdataOffset = sdata.Count;
                    if (parsedLine[0] == "loadString")
                    {
                        sdataLoadCurrent++;
                        bool readingString = false;
                        for (int c = 0; c < line.Length; c++)
                        {
                            if (line[c] == '\"')
                            {
                                if (!readingString)
                                {
                                    readingString = true;
                                }
                                else
                                {
                                    readingString = false;
                                    break;
                                }
                            }
                            else if (readingString)
                            {
                                sdataString += line[c];
                            }
                        }
                        sdata.AddRange(Encoding.UTF8.GetBytes(sdataString));
                        sdata.Add(0x00);
                        if (sdataLoadCurrent != sdataLoadsCount)
                        {
                            sdata.Add(0xFF);
                        }
                        mintScript[i] = $"{parsedLine[0]} {parsedLine[1]}, {sdataOffset.ToString("X4")}";
                    }
                    else if (parsedLine[0] == "load")
                    {
                        sdataLoadCurrent++;
                        byte[] sdata32 = { };
                        if (parsedLine[2].StartsWith("0x"))
                        {
                            sdata32 = BitConverter.GetBytes(ReverseBytes(uint.Parse(parsedLine[2].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber)));
                        }
                        else if (parsedLine[2].EndsWith("f") || parsedLine[2].EndsWith(".0"))
                        {
                            byte[] floatBytes = BitConverter.GetBytes(float.Parse(parsedLine[2].Replace("f", "")));
                            sdata32 = new byte[] { floatBytes[3], floatBytes[2], floatBytes[1], floatBytes[0] };
                        }
                        else
                        {
                            sdata32 = BitConverter.GetBytes(ReverseBytes(uint.Parse(parsedLine[2])));
                        }
                        sdata.AddRange(sdata32);
                        mintScript[i] = $"{parsedLine[0]} {parsedLine[1]}, {sdataOffset.ToString("X4")}";
                    }
                }
                if (sdata.Count > 0)
                {
                    while ((sdata.Count - 1).ToString("X").Last() != '0' && (sdata.Count - 1).ToString("X").Last() != '4' && (sdata.Count - 1).ToString("X").Last() != '8' && (sdata.Count - 1).ToString("X").Last() != 'C')
                    {
                        sdata.RemoveAt(sdata.Count - 1);
                    }
                }


                //xref
                List<string> xref = new List<string>();
                List<uint> xrefNameOffsets = new List<uint>();
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string[] parsedLine = mintScript[i].Replace(",", "").Split(' ');
                    string line = mintScript[i];
                    int index = 0;
                    if (parsedLine[0] == "call")
                    {
                        if (!xref.Contains(parsedLine[1]))
                        {
                            xref.Add(parsedLine[1]);
                        }
                        index = xref.IndexOf(parsedLine[1]);
                        line = $"{parsedLine[0]} {index.ToString("X4")}";
                    }
                    else if (parsedLine[0] == "new" || parsedLine[0] == "del" || parsedLine[0] == "getField" || parsedLine[0] == "getStatic" || parsedLine[0] == "storeStatic" || parsedLine[0] == "sizeOf")
                    {
                        if (!xref.Contains(parsedLine[2]))
                        {
                            xref.Add(parsedLine[2]);
                        }
                        index = xref.IndexOf(parsedLine[2]);
                        line = $"{parsedLine[0]} {parsedLine[1]}, {index.ToString("X4")}";
                    }
                    mintScript[i] = line;
                }

                List<string> classNames = new List<string>();
                List<List<byte[]>> classMethods = new List<List<byte[]>>();
                List<List<string>> classMethodNames = new List<List<string>>();
                List<List<uint>> classMethodNameOffsets = new List<List<uint>>();
                List<List<string>> classVars = new List<List<string>>();
                List<List<uint>> classVarNameOffsets = new List<List<uint>>();
                List<List<uint>> classVarTypeOffsets = new List<List<uint>>();
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string[] parsedLine = mintScript[i].Replace(",", "").Split(' ');
                    if (parsedLine[0] == "class")
                    {
                        List<string> variables = new List<string>();
                        List<string> methodNames = new List<string>();
                        List<byte[]> methods = new List<byte[]>();
                        List<byte> method = new List<byte>();
                        //Console.WriteLine($"Reading Class {parsedLine[1]}");
                        classNames.Add(parsedLine[1]);
                        bool readingMethod = false;
                        for (int l = i; l < mintScript.Length; l++)
                        {
                            string[] classLine = mintScript[l].Replace(",", "").Split(' ');
                            if (!readingMethod)
                            {
                                if (mintScript[l].StartsWith("int") || mintScript[l].StartsWith("string") || mintScript[l].StartsWith("bool") || mintScript[l].StartsWith("void"))
                                {
                                    if (mintScript[l].EndsWith("{") || mintScript[l + 1].EndsWith("{"))
                                    {
                                        //Console.WriteLine($"Reading Method {mintScript[l]}");
                                        methodNames.Add(mintScript[l]);
                                        readingMethod = true;
                                        method = new List<byte>();
                                    }
                                    else
                                    {
                                        //Console.WriteLine($"Reading Variable {mintScript[l]}");
                                        variables.Add(mintScript[l]);
                                    }
                                }
                                else if (mintScript[l] == "}")
                                {
                                    //Console.WriteLine($"Finished reading Class");
                                    break;
                                }
                            }
                            else
                            {
                                if (mintScript[l] != "{" && mintScript[l] != "}")
                                {
                                    try
                                    {
                                        string outputLog = "";
                                        switch (classLine[0])
                                        {
                                            case "return":
                                                {
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(0xFF);
                                                    method.Add(0x00);
                                                    method.Add(0xFF);
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + "FF00FF";
                                                    break;
                                                }
                                            case "setTrue":
                                            case "setFalse":
                                            case "moveResult":
                                            case "inci":
                                            case "deci":
                                            case "incf":
                                            case "decf":
                                            case "yield":
                                            case "makeArray":
                                            case "deleteArray":
                                                {
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.Add(0xFF);
                                                    method.Add(0xFF);
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + "FFFF";
                                                    break;
                                                }
                                            case "returnVal":
                                                {
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(0xFF);
                                                    method.Add(byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.Add(0xFF);
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + "FF" + byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + "FF";
                                                    break;
                                                }
                                            case "moveRegister":
                                            case "setArg":
                                            case "loadDeref":
                                            case "storeDeref":
                                            case "negi":
                                            case "cmpLess":
                                            case "cmpLessOrEqual":
                                            case "nti":
                                            case "not":
                                            case "declare":
                                            case "sppshz":
                                            case "arrayIndex":
                                            case "arrayLength":
                                                {
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(byte.Parse(classLine[1].Replace("r", "").Replace("[", "").Replace("]", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.Add(byte.Parse(classLine[2].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.Add(0xFF);
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + byte.Parse(classLine[1].Replace("r", "").Replace("[", "").Replace("]", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + byte.Parse(classLine[2].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + "FF";
                                                    break;
                                                }
                                            case "addi":
                                            case "subi":
                                            case "multi":
                                            case "divi":
                                            case "modi":
                                            case "addf":
                                            case "subf":
                                            case "multf":
                                            case "divf":
                                            case "negf":
                                            case "intLess":
                                            case "intLessOrEqual":
                                            case "intEqual":
                                            case "intNotEqual":
                                            case "floatLess":
                                            case "floatLessOrEqual":
                                            case "floatEqual":
                                            case "floatNotEqual":
                                            case "boolEqual":
                                            case "boolNotEqual":
                                            case "bitAnd":
                                            case "bitOr":
                                            case "bitXor":
                                            case "slli":
                                            case "slr":
                                            case "copy":
                                            case "zero":
                                                {
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.Add(byte.Parse(classLine[2].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.Add(byte.Parse(classLine[3].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + byte.Parse(classLine[2].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + byte.Parse(classLine[3].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2");
                                                    break;
                                                }
                                            case "call":
                                                {
                                                    byte[] v = BitConverter.GetBytes(ushort.Parse(classLine[1], System.Globalization.NumberStyles.HexNumber));
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(0xFF);
                                                    method.AddRange(new byte[] { v[1], v[0] });
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + "FF" + v[1].ToString("X2") + v[0].ToString("X2");
                                                    break;
                                                }
                                            case "load":
                                            case "loadString":
                                            case "getStatic":
                                            case "sizeOf":
                                            case "storeStatic":
                                            case "new":
                                            case "del":
                                            case "getField":
                                                {
                                                    byte[] v = BitConverter.GetBytes(ushort.Parse(classLine[2], System.Globalization.NumberStyles.HexNumber));
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.AddRange(new byte[] { v[1], v[0] });
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + byte.Parse(classLine[1].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + v[1].ToString("X2") + v[0].ToString("X2");
                                                    break;
                                                }
                                            case "jump":
                                                {
                                                    byte[] v = BitConverter.GetBytes(short.Parse(classLine[1]));
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(0xFF);
                                                    method.AddRange(new byte[] { v[1], v[0] });
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + "FF" + v[1].ToString("X2") + v[0].ToString("X2");
                                                    break;
                                                }
                                            case "jumpIfEqual":
                                            case "jumpIfNotEqual":
                                                {
                                                    byte[] v = BitConverter.GetBytes(short.Parse(classLine[1]));
                                                    method.Add((byte)opcodeNames.IndexOf(classLine[0]));
                                                    method.Add(byte.Parse(classLine[2].Replace("r", ""), System.Globalization.NumberStyles.HexNumber));
                                                    method.AddRange(new byte[] { v[1], v[0] });
                                                    outputLog = opcodeNames.IndexOf(classLine[0]).ToString("X2") + byte.Parse(classLine[2].Replace("r", ""), System.Globalization.NumberStyles.HexNumber).ToString("X2") + v[1].ToString("X2") + v[0].ToString("X2");
                                                    break;
                                                }
                                            default:
                                                {
                                                    Console.WriteLine($"Error: Unknown command \"{classLine[0]}\" at line {l + 1}\nStopping.");
                                                    Thread.Sleep(2000);
                                                    return null;
                                                }
                                        }
                                        //Console.WriteLine(outputLog);
                                    }
                                    catch
                                    {
                                        Console.WriteLine($"Error: Could not compile command at line {l + 1}\nStopping.");
                                        Console.WriteLine($"Line: {mintScript[l]}");
                                        Thread.Sleep(2000);
                                        return null;
                                    }
                                }
                                else if (mintScript[l] == "}")
                                {
                                    readingMethod = false;
                                    methods.Add(method.ToArray());
                                }
                            }
                        }
                        classMethods.Add(methods);
                        classMethodNames.Add(methodNames);
                        classVars.Add(variables);
                    }
                }
                scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)sdata.Count)));
                scriptBytes.AddRange(sdata);

                while ((scriptBytes.Count - 1).ToString("X").Last() != 'F')
                {
                    scriptBytes.Add(0x00);
                }

                scriptBytes.RemoveRange(0x1C, 0x4);
                scriptBytes.InsertRange(0x1C, BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                
                scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)xref.Count)));
                List<uint> xrefOffsets = new List<uint>();
                for (int i = 0; i < xref.Count; i++)
                {
                    xrefOffsets.Add((uint)scriptBytes.Count);
                    scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                }

                scriptBytes.RemoveRange(0x20, 0x4);
                scriptBytes.InsertRange(0x20, BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));

                scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes(classNames.Count)));
                
                List<uint> classOffsets = new List<uint>();
                List<uint> classNameOffsets = new List<uint>();
                List<uint> methodOffsets = new List<uint>();
                List<uint> methodNameOffsets = new List<uint>();
                List<uint> varOffsets = new List<uint>();
                List<uint> varNameOffsets = new List<uint>();
                List<uint> varTypeOffsets = new List<uint>();
                for (int i = 0; i < classNames.Count; i++)
                {
                    classOffsets.Add((uint)scriptBytes.Count);
                    scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                }
                for (int i = 0; i < classNames.Count; i++)
                {
                    classVarNameOffsets.Add(new List<uint>());
                    classVarTypeOffsets.Add(new List<uint>());
                    classMethodNameOffsets.Add(new List<uint>());
                    scriptBytes.RemoveRange((int)classOffsets[i], 0x4);
                    scriptBytes.InsertRange((int)classOffsets[i], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                    classNameOffsets.Add((uint)scriptBytes.Count);
                    scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x8)));
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + (0x8 * (uint)classVars[i].Count))));
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)classVars[i].Count)));
                    for (int v = 0; v < classVars[i].Count; v++)
                    {
                        scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4 + (0x4 * ((uint)v + 1)))));
                    }
                    for (int v = 0; v < classVars[i].Count; v++)
                    {
                        classVarNameOffsets[i].Add((uint)scriptBytes.Count);
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                        classVarTypeOffsets[i].Add((uint)scriptBytes.Count);
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    }
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)classMethods[i].Count)));
                    for (int m = 0; m < classMethods[i].Count; m++)
                    {
                        methodOffsets.Add((uint)scriptBytes.Count);
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    }
                    for (int m = 0; m < classMethods[i].Count; m++)
                    {
                        scriptBytes.RemoveRange((int)methodOffsets[m], 0x4);
                        scriptBytes.InsertRange((int)methodOffsets[m], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                        classMethodNameOffsets[i].Add((uint)scriptBytes.Count);
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                        scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                        scriptBytes.AddRange(classMethods[i][m]);
                    }
                    methodOffsets = new List<uint>();
                }

                scriptBytes.RemoveRange(0x14, 0x4);
                scriptBytes.InsertRange(0x14, BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)scriptName.Length)));
                scriptBytes.AddRange(Encoding.UTF8.GetBytes(scriptName));
                scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                while ((scriptBytes.Count).ToString("X").Last() != '0' && (scriptBytes.Count).ToString("X").Last() != '4' && (scriptBytes.Count).ToString("X").Last() != '8' && (scriptBytes.Count).ToString("X").Last() != 'C')
                {
                    scriptBytes.Add(0x00);
                }

                for (int i = 0; i < xref.Count; i++)
                {
                    scriptBytes.RemoveRange((int)xrefOffsets[i], 0x4);
                    scriptBytes.InsertRange((int)xrefOffsets[i], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count)));
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)xref[i].Length)));
                    scriptBytes.AddRange(Encoding.UTF8.GetBytes(xref[i]));
                    scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    while ((scriptBytes.Count).ToString("X").Last() != '0' && (scriptBytes.Count).ToString("X").Last() != '4' && (scriptBytes.Count).ToString("X").Last() != '8' && (scriptBytes.Count).ToString("X").Last() != 'C')
                    {
                        scriptBytes.Add(0x00);
                    }
                }

                for (int i = 0; i < classNames.Count; i++)
                {
                    scriptBytes.RemoveRange((int)classNameOffsets[i], 0x4);
                    scriptBytes.InsertRange((int)classNameOffsets[i], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)classNames[i].Length)));
                    scriptBytes.AddRange(Encoding.UTF8.GetBytes(classNames[i]));
                    scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    while ((scriptBytes.Count).ToString("X").Last() != '0' && (scriptBytes.Count).ToString("X").Last() != '4' && (scriptBytes.Count).ToString("X").Last() != '8' && (scriptBytes.Count).ToString("X").Last() != 'C')
                    {
                        scriptBytes.Add(0x00);
                    }
                    for (int v = 0; v < classVars[i].Count; v++)
                    {
                        string type = classVars[i][v].Split(' ')[0];
                        string name = classVars[i][v].Split(' ')[1];
                        scriptBytes.RemoveRange((int)classVarNameOffsets[i][v], 0x4);
                        scriptBytes.InsertRange((int)classVarNameOffsets[i][v], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                        scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)name.Length)));
                        scriptBytes.AddRange(Encoding.UTF8.GetBytes(name));
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

                        while ((scriptBytes.Count).ToString("X").Last() != '0' && (scriptBytes.Count).ToString("X").Last() != '4' && (scriptBytes.Count).ToString("X").Last() != '8' && (scriptBytes.Count).ToString("X").Last() != 'C')
                        {
                            scriptBytes.Add(0x00);
                        }

                        scriptBytes.RemoveRange((int)classVarTypeOffsets[i][v], 0x4);
                        scriptBytes.InsertRange((int)classVarTypeOffsets[i][v], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                        scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)type.Length)));
                        scriptBytes.AddRange(Encoding.UTF8.GetBytes(type));
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

                        while ((scriptBytes.Count).ToString("X").Last() != '0' && (scriptBytes.Count).ToString("X").Last() != '4' && (scriptBytes.Count).ToString("X").Last() != '8' && (scriptBytes.Count).ToString("X").Last() != 'C')
                        {
                            scriptBytes.Add(0x00);
                        }
                    }
                    for (int m = 0; m < classMethodNames[i].Count; m++)
                    {
                        scriptBytes.RemoveRange((int)classMethodNameOffsets[i][m], 0x4);
                        scriptBytes.InsertRange((int)classMethodNameOffsets[i][m], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                        scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes(classMethodNames[i][m].Length)));
                        scriptBytes.AddRange(Encoding.UTF8.GetBytes(classMethodNames[i][m]));
                        while ((scriptBytes.Count).ToString("X").Last() != '0' && (scriptBytes.Count).ToString("X").Last() != '4' && (scriptBytes.Count).ToString("X").Last() != '8' && (scriptBytes.Count).ToString("X").Last() != 'C')
                        {
                            scriptBytes.Add(0x00);
                        }
                    }
                }

                scriptBytes.RemoveRange(0x8, 0x4);
                scriptBytes.InsertRange(0x8, BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
                
                //Console.WriteLine($"Finished processing script {filename}");
                return scriptBytes.ToArray();
            }
            else
            {
                Console.WriteLine($"Error: {filename} is not a valid MINT script!");
                Thread.Sleep(2000);
                return null;
            }
        }

        //Converts byte order between endianness
        static uint ReverseBytes(uint val)
        {
            return (val & 0x000000FF) << 24 |
                    (val & 0x0000FF00) << 8 |
                    (val & 0x00FF0000) >> 8 |
                    ((uint)(val & 0xFF000000)) >> 24;
        }
        static int ReverseBytes(int val)
        {
            return (val & 0x000000FF) << 24 |
                    (val & 0x0000FF00) << 8 |
                    (val & 0x00FF0000) >> 8 |
                    ((int)(val & 0xFF000000)) >> 24;
        }
        //Reads a float in reverse-endianness
        static float ReadSingle(byte[] data, int offset)
        {
            byte tmp = data[offset];
            data[offset] = data[offset + 3];
            data[offset + 3] = tmp;
            tmp = data[offset + 1];
            data[offset + 1] = data[offset + 2];
            data[offset + 2] = tmp;
            return BitConverter.ToSingle(data, offset);
        }
    }
}
