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
            "00", "setTrue", "setFalse", "load", "loadString", "moveRegister", "moveResult", "setArg", "08", "getStatic", "loadDeref", "sizeOf", "storeDeref", "storeStatic", "addi", "subi", "multi", "divi", "modi", "inci", "deci", "negi", "addf", "subf", "multf", "divf", "incf", "decf", "negf", "intLess", "intLessOrEqual", "intEqual", "intNotEqual", "floatLess", "floatLessOrEqual", "floatEqual", "floatNotEqual", "cmpLess", "cmpLessOrEqual", "boolEqual", "boolNotEqual", "bitAnd", "bitOr", "bitXor", "nti", "not", "slli", "slr", "jump", "jumpIfEqual", "jumpIfNotEqual", "declare", "return", "returnVal", "call", "yield", "copy", "zero", "new", "sppshz", "del", "getField", "makeArray", "arrayIndex", "arrayLength", "deleteArray"
        };

        static void Main(string[] args)
        {
            string filepath;
            byte[] mintArchive;
            //args = new string[] { "-rdb", "MINT\\User.Tsuruoka.MintTest.txt", "-f" };
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
                else if (args[0] == "-r" || args[0] == "-rdb")
                {
                    bool debug = false;
                    if (args[0] == "-rdb")
                    {
                        debug = true;
                    }
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
                            byte[] compiledScript = CompileScript(mintScript, filepath.Split('\\').Last().Replace(".txt", ""), debug);
                            if (compiledScript != null)
                            {
                                File.WriteAllBytes(Directory.GetCurrentDirectory() + @"\Compiled\" + filepath.Split('\\').Last().Replace(".txt", ".bin"), compiledScript);
                                Console.WriteLine("Done!");
                            }
                            else
                            {
                                return;
                            }
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
                                byte[] compiledScript = CompileScript(File.ReadAllLines(files[i]), files[i].Split('\\').Last(), debug);
                                if (compiledScript != null)
                                {
                                    scriptNames.Add(files[i].Split('\\').Last().Replace(".txt", ""));
                                    scripts.Add(compiledScript);
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
                            archive.RemoveRange(0x8, 0x4);
                            archive.InsertRange(0x8, BitConverter.GetBytes(ReverseBytes(archive.Count + 0x4)));

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
            Console.WriteLine("    -x <file>:          Extract and decompile a MINT Archive or individual script");
            Console.WriteLine("    -r <folder|file>:   Repack and compile a MINT Archive from a folder or individual script");
            Console.WriteLine("    -rdb <folder|file>: Repack and compile a MINT Archive from a folder or individual script (Debug comment printing)");
            Console.WriteLine("    -h:                 Show this message");
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
            }
            //xref decomp
            uint xrefCount = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)xrefStart));
            List<uint> xrefOffsets = new List<uint>();
            if (xrefCount > 0)
            {
                for (int i = (int)xrefStart + 0x4; i < (int)xrefStart + 0x4 + (xrefCount * 0x4); i+= 0x4)
                {
                    xrefOffsets.Add(ReverseBytes(BitConverter.ToUInt32(mintScript, i)));
                }
                for (int i = 0; i < xrefOffsets.Count; i++)
                {
                    uint stringLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)xrefOffsets[i]));
                    string xrefString = Encoding.UTF8.GetString(mintScript, (int)xrefOffsets[i] + 0x4, (int)stringLength);
                    xref.Add(xrefString);
                }
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
                            line += opcodeNames[mintScript[b]];
                            switch (mintScript[b])
                            {
                                case 0x01:
                                case 0x02:
                                case 0x06:
                                case 0x13:
                                case 0x14:
                                case 0x1A:
                                case 0x1B:
                                case 0x37:
                                case 0x3E:
                                case 0x41:
                                    {
                                        line += $" r{z}";
                                        break;
                                    }
                                case 0x03:
                                    {
                                        line += $" r{z}, 0x{ReverseBytes(BitConverter.ToUInt32(sdata.ToArray(), v)).ToString("X").ToLower()}";
                                        break;
                                    }
                                case 0x04:
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
                                        line += $" r{z}, \"{sdataString}\"";
                                        break;
                                    }
                                case 0x05:
                                case 0x0A:
                                case 0x0C:
                                case 0x15:
                                case 0x1C:
                                case 0x25:
                                case 0x26:
                                case 0x2C:
                                case 0x2D:
                                case 0x3F:
                                case 0x40:
                                    {
                                        line += $" r{z}, r{x}";
                                        break;
                                    }
                                case 0x07:
                                    {
                                        line += $" [{z}] r{x}";
                                        break;
                                    }
                                case 0x09:
                                case 0x0B:
                                case 0x0D:
                                case 0x3A:
                                case 0x3B:
                                case 0x3C:
                                case 0x3D:
                                    {
                                        line += $" r{z}, {xref[v]}";
                                        break;
                                    }
                                case 0x0E:
                                case 0x0F:
                                case 0x10:
                                case 0x11:
                                case 0x12:
                                case 0x16:
                                case 0x17:
                                case 0x18:
                                case 0x19:
                                case 0x1D:
                                case 0x1E:
                                case 0x1F:
                                case 0x20:
                                case 0x21:
                                case 0x22:
                                case 0x23:
                                case 0x24:
                                case 0x27:
                                case 0x28:
                                case 0x29:
                                case 0x2A:
                                case 0x2B:
                                case 0x2E:
                                case 0x2F:
                                case 0x38:
                                case 0x39:
                                    {
                                        line += $" r{z}, r{x}, r{y}";
                                        break;
                                    }
                                case 0x30:
                                    {
                                        line += $" {v}";
                                        break;
                                    }
                                case 0x31:
                                case 0x32:
                                    {
                                        line += $" {v}, r{z}";
                                        break;
                                    }
                                case 0x33:
                                    {
                                        line += $" {z}, {x}";
                                        break;
                                    }
                                case 0x34:
                                    {
                                        break;
                                    }
                                case 0x35:
                                    {
                                        line += $" r{x}";
                                        break;
                                    }
                                case 0x36:
                                    {
                                        line += $" {xref[v]}";
                                        break;
                                    }
                                default:
                                    {
                                        Console.WriteLine($"Error: Unknown command 0x{mintScript[b].ToString("X2")} at 0x{b.ToString("X8")} in script {fileName}\n    Full command: 0x{mintScript[b].ToString("X2")}{mintScript[b + 1].ToString("X2")}{mintScript[b + 2].ToString("X2")}{mintScript[b + 3].ToString("X2")}");
                                        Thread.Sleep(2000);
                                        break;
                                    }
                            }
                            scriptDecomp.Add(line);
                            if (mintScript[b] == 0x34 || mintScript[b] == 0x35)
                            {
                                break;
                            }
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

        public static byte[] CompileScript(string[] mintScript, string filename, bool debug)
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

                //prep script
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string line = mintScript[i];
                    int spaceCount = 0;
                    for (int c = 0; c < line.Length; c++)
                    {
                        if (line[c] == ' ' || line[c] == '	')
                        {
                            spaceCount++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    for (int c = 0; c < line.Length; c++)
                    {
                        if (!debug)
                        {
                            if (line[c] == '/' && line[c + 1] == '#')
                            {
                                for (int t = c; t < line.Length; t++)
                                {
                                    line = line.Remove(t);
                                }
                                break;
                            }
                            if (line[c] == '#')
                            {
                                for (int t = c; t < line.Length; t++)
                                {
                                    line = line.Remove(t);
                                }
                                break;
                            }
                        }
                        else
                        {
                            if (line[c] == '#' && line[c - 1] != '/')
                            {
                                for (int t = c; t < line.Length; t++)
                                {
                                    line = line.Remove(t);
                                }
                                break;
                            }
                        }
                    }
                    line = line.Remove(0, spaceCount);
                    mintScript[i] = line;
                }
                if (debug)
                {
                    int declareLine = 0;
                    int registerCount = 0;
                    bool increasedRegisters = false;
                    for (int i = 0; i < mintScript.Length; i++)
                    {
                        string line = mintScript[i];
                        if (line.StartsWith("declare "))
                        {
                            declareLine = i;
                            increasedRegisters = false;
                        }
                        else if (line.StartsWith("/#"))
                        {
                            if (declareLine != 0)
                            {
                                if (!increasedRegisters)
                                {
                                    string[] parsedLine = mintScript[declareLine].Split(' ');
                                    registerCount = (int.Parse(parsedLine[1].Replace(",", ""), System.Globalization.NumberStyles.HexNumber) + 1);
                                    mintScript[declareLine] = $"{parsedLine[0]} {registerCount.ToString("X2")}, {parsedLine[2]}";
                                    increasedRegisters = true;
                                }
                                line = line.Remove(0, 2);
                                line = line.Insert(0, $"loadString r{(registerCount - 1).ToString("X2")}, \"");
                                line += "\"";
                                mintScript[i] = line;
                                List<string> script = mintScript.ToList();
                                script.Insert(i + 1, $"setArg [00] r{(registerCount - 1).ToString("X2")}");
                                script.Insert(i + 2, "call Mint.Debug.puts(string)");
                                mintScript = script.ToArray();
                                break;
                            }
                        }
                        else
                        {
                            if (line.Contains("/#"))
                            {
                                if (declareLine != 0)
                                {
                                    if (!increasedRegisters)
                                    {
                                        Console.WriteLine(mintScript[declareLine]);
                                        string[] parsedLine = mintScript[declareLine].Split(' ');
                                        registerCount = (int.Parse(parsedLine[1].Replace(",", ""), System.Globalization.NumberStyles.HexNumber) + 1);
                                        mintScript[declareLine] = $"{parsedLine[0]} {registerCount.ToString("X2")}, {parsedLine[2]}";
                                        increasedRegisters = true;
                                    }
                                    string comment = "";
                                    bool readComment = false;
                                    for (int c = 0; c < line.Length; c++)
                                    {
                                        if (line[c] == '/' && line[c + 1] == '#')
                                        {
                                            readComment = true;
                                        }
                                        if (readComment)
                                        {
                                            comment += line[c];
                                        }
                                    }
                                    comment = comment.Remove(0, 2);
                                    for (int c = 0; c < line.Length; c++)
                                    {
                                        if (line[c] == '/' && line[c + 1] == '#')
                                        {
                                            for (int t = c; t < line.Length; t++)
                                            {
                                                line = line.Remove(t);
                                            }
                                            mintScript[i] = line;
                                            break;
                                        }
                                    }
                                    List<string> script = mintScript.ToList();
                                    script.Insert(i, $"loadString r{(registerCount - 1).ToString("X2")}, \"{comment}\"");
                                    script.Insert(i + 1, $"setArg [00] r{(registerCount - 1).ToString("X2")}");
                                    script.Insert(i + 2, "call Mint.Debug.puts(string)");
                                    mintScript = script.ToArray();
                                    break;
                                }
                            }
                        }
                    }
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
                List<byte[]> sdataArrays = new List<byte[]>();
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
                        else if (parsedLine[2].EndsWith("f") || parsedLine[2].Contains("."))
                        {
                            byte[] floatBytes = BitConverter.GetBytes(float.Parse(parsedLine[2].Replace("f", "")));
                            sdata32 = new byte[] { floatBytes[3], floatBytes[2], floatBytes[1], floatBytes[0] };
                        }
                        else
                        {
                            sdata32 = BitConverter.GetBytes(ReverseBytes(uint.Parse(parsedLine[2])));
                        }
                        bool inSdata = false;
                        for (int b = 0; b < sdata.Count; b += 4)
                        {
                            try
                            {
                                if (sdata[b] == sdata32[0] && sdata[b + 1] == sdata32[1] && sdata[b + 2] == sdata32[2] && sdata[b + 3] == sdata32[3])
                                {
                                    sdataOffset = b;
                                    inSdata = true;
                                }
                            }
                            catch { }
                        }
                        if (!inSdata)
                        {
                            sdata.AddRange(sdata32);
                        }
                        mintScript[i] = $"{parsedLine[0]} {parsedLine[1]}, {sdataOffset.ToString("X4")}";
                    }
                }
                if (sdata.Count > 0)
                {
                    while (sdata.Count.ToString("X").Last() != '0' && sdata.Count.ToString("X").Last() != '4' && sdata.Count.ToString("X").Last() != '8' && sdata.Count.ToString("X").Last() != 'C')
                    {
                        sdata.Add(0x00);
                    }
                }


                //xref
                List<string> xref = new List<string>();
                List<uint> xrefNameOffsets = new List<uint>();
                for (int i = 0; i < mintScript.Length; i++)
                {
                    string[] parsedLine = mintScript[i].Split(' ');
                    string line = mintScript[i];
                    string xrefString = line;
                    int index = 0;
                    if (parsedLine[0] == "call")
                    {
                        xrefString = xrefString.Remove(0, 5);
                        xrefString = xrefString.TrimEnd(new char[] { ' ' });
                        if (!xref.Contains(xrefString))
                        {
                            xref.Add(xrefString);
                        }
                        index = xref.IndexOf(xrefString);
                        line = $"{parsedLine[0]} {index.ToString("X4")}";
                    }
                    else if (parsedLine[0] == "new" || parsedLine[0] == "del" || parsedLine[0] == "getField" || parsedLine[0] == "getStatic" || parsedLine[0] == "storeStatic" || parsedLine[0] == "sizeOf" || parsedLine[0] == "sppshz")
                    {
                        xrefString = xrefString.Replace("new r", "").Replace("del r", "").Replace("getField r", "").Replace("getStatic r", "").Replace("storeStatic r", "").Replace("sizeOf r", "").Replace("sppshz r", "");
                        xrefString = xrefString.Remove(0, 4);
                        xrefString = xrefString.TrimEnd(new char[] { ' ' });
                        if (!xref.Contains(xrefString))
                        {
                            xref.Add(xrefString);
                        }
                        index = xref.IndexOf(xrefString);
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
                                if (mintScript[l].StartsWith("int ") || mintScript[l].StartsWith("string ") || mintScript[l].StartsWith("bool ") || mintScript[l].StartsWith("void ") || mintScript[l].StartsWith("float "))
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
                                            case "negf":
                                            case "cmpLess":
                                            case "cmpLessOrEqual":
                                            case "nti":
                                            case "not":
                                            case "declare":
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
                                            case "sppshz":
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
                                                    if (mintScript[l] == "")
                                                    {
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Error: Unknown command \"{classLine[0]}\" at line {l + 1}\nStopping.");
                                                        Thread.Sleep(2000);
                                                        return null;
                                                    }
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
                    uint methodPointerOffset = (uint)scriptBytes.Count;
                    scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    scriptBytes.AddRange(BitConverter.GetBytes(ReverseBytes((uint)classVars[i].Count)));
                    uint varListOffset = (uint)scriptBytes.Count;
                    for (int v = 0; v < classVars[i].Count; v++)
                    {
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    }
                    for (int v = 0; v < classVars[i].Count; v++)
                    {
                        classVarNameOffsets[i].Add((uint)scriptBytes.Count);
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                        classVarTypeOffsets[i].Add((uint)scriptBytes.Count);
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    }
                    for (int v = 0; v < classVars[i].Count; v++)
                    {
                        scriptBytes.RemoveRange((int)varListOffset + (0x4 * v), 0x4);
                        scriptBytes.InsertRange((int)varListOffset + (0x4 * v), BitConverter.GetBytes(ReverseBytes((uint)classVarNameOffsets[i][v])));
                    }
                    scriptBytes.RemoveRange((int)methodPointerOffset, 0x4);
                    scriptBytes.InsertRange((int)methodPointerOffset, BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
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
                    scriptBytes.InsertRange((int)xrefOffsets[i], BitConverter.GetBytes(ReverseBytes((uint)scriptBytes.Count + 0x4)));
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
                        scriptBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
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
