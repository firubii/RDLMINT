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

        static void Main(string[] args)
        {
            string filepath;
            byte[] mintArchive;
            args = new string[] { "-x", @"D:\Game Dumps\Kirby's Return to Dreamland\mint\Archive.bin" };
            if (args.Length > 0)
            {
                if (args[0] == "-x")
                {
                    if (args.Length > 1 && File.Exists(args[1]))
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
                    }
                    else
                    {
                        Console.WriteLine("Error: File does not exist or no file provided");
                    }
                }
                else if (args[0] == "-r")
                {

                }
                else if (args[0] == "-h")
                {
                    Console.WriteLine("Usage: RDLMINT.exe <option>");
                    Console.WriteLine("Options:");
                    Console.WriteLine("    -x <file>:   Extract and decompile a MINT Archive");
                    Console.WriteLine("    -r <folder>: Repack and compile a MINT Archive from a folder");
                    Console.WriteLine("    -h:          Show this message");
                }
            }
            else
            {
                Console.WriteLine("Usage: RDLMINT.exe <option>");
                Console.WriteLine("Options:");
                Console.WriteLine("    -x <file>:   Extract and decompile a MINT Archive");
                Console.WriteLine("    -r <folder>: Repack and compile a MINT Archive from a folder");
                Console.WriteLine("    -h:          Show this message");
            }
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
                scriptDecomp.Add(sdataString);
            }
            //xref decomp
            if (ReverseBytes(BitConverter.ToUInt32(mintScript, (int)xrefStart)) > 0)
            {
                scriptDecomp.Add("    XREF\n    {");
                for (int i = 0; i < ReverseBytes(BitConverter.ToUInt32(mintScript, (int)xrefStart)); i++)
                {
                    if (ReverseBytes(BitConverter.ToUInt32(mintScript, ((int)xrefStart + 4) + (i * 4))) != 0x0)
                    {
                        uint stringOffset = ReverseBytes(BitConverter.ToUInt32(mintScript, ((int)xrefStart + 4) + (i * 4))) + 4;
                        uint stringLength = ReverseBytes(BitConverter.ToUInt32(mintScript, (int)stringOffset - 4));
                        string xrefString = Encoding.UTF8.GetString(mintScript, (int)stringOffset, (int)stringLength);
                        xref.Add(xrefString);
                        scriptDecomp.Add("        " + xrefString);
                    }
                }
                scriptDecomp.Add("    }");
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
                    scriptDecomp.AddRange(new string[] { "        var " + varName });
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
                            //opcodes
                            if (mintScript[b] == 0x01)
                            {
                                scriptDecomp.Add("            mov r" + mintScript[b + 1].ToString("X2") + ", true");
                            }
                            else if (mintScript[b] == 0x02)
                            {
                                scriptDecomp.Add("            mov r" + mintScript[b + 1].ToString("X2") + ", false");
                            }
                            else if (mintScript[b] == 0x03)
                            {
                                scriptDecomp.Add("            ld r" + mintScript[b + 1].ToString("X2") + ", 0x" + ReverseBytes(BitConverter.ToUInt32(sdata.ToArray(), mintScript[b + 3])).ToString("X8"));
                            }
                            else if (mintScript[b] == 0x04)
                            {
                                string sdataString = "";
                                List<byte> stringBytes = new List<byte>();
                                for (int s = mintScript[b + 3]; s < sdata.Count; s++)
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
                                scriptDecomp.Add("            ldstr r" + mintScript[b + 1].ToString("X2") + ", " + sdataString);
                            }
                            else if (mintScript[b] == 0x05)
                            {
                                scriptDecomp.Add("            mov r" + mintScript[b + 1].ToString("X2") + ", " + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x06)
                            {
                                scriptDecomp.Add("            mov r" + mintScript[b + 1].ToString("X2") + ", <res>");
                            }
                            else if (mintScript[b] == 0x07)
                            {
                                scriptDecomp.Add("            argset [" + mintScript[b + 1].ToString("X2") + "] r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x09)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            getstatic r" + mintScript[b + 1].ToString("X2") + ", " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x0A)
                            {
                                scriptDecomp.Add("            getderef r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x0B)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            sizeof r" + mintScript[b + 1].ToString("X2") + ", " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x0C)
                            {
                                scriptDecomp.Add("            putderef r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x0D)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            putstatic r" + mintScript[b + 1].ToString("X2") + ", " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x0E)
                            {
                                scriptDecomp.Add("            addi r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x0F)
                            {
                                scriptDecomp.Add("            subi r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x10)
                            {
                                scriptDecomp.Add("            multi r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x11)
                            {
                                scriptDecomp.Add("            divi r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x12)
                            {
                                scriptDecomp.Add("            modi r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x13)
                            {
                                scriptDecomp.Add("            inci r" + mintScript[b + 1].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x14)
                            {
                                scriptDecomp.Add("            negi r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x16)
                            {
                                scriptDecomp.Add("            addf r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x17)
                            {
                                scriptDecomp.Add("            subf r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x18)
                            {
                                scriptDecomp.Add("            multf r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x19)
                            {
                                scriptDecomp.Add("            divf r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x1C)
                            {
                                scriptDecomp.Add("            negf r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x1D)
                            {
                                scriptDecomp.Add("            intless r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x1E)
                            {
                                scriptDecomp.Add("            intnot r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x1F)
                            {
                                scriptDecomp.Add("            intequal r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x21)
                            {
                                scriptDecomp.Add("            floatless r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x27)
                            {
                                scriptDecomp.Add("            booland r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x28)
                            {
                                scriptDecomp.Add("            boolnot r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x2A)
                            {
                                scriptDecomp.Add("            bitor r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x2D)
                            {
                                scriptDecomp.Add("            not r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x30)
                            {
                                scriptDecomp.Add("            jump " + (int)mintScript[b + 3]);
                            }
                            else if (mintScript[b] == 0x31)
                            {
                                scriptDecomp.Add("            jumpif " + (int)mintScript[b + 3] + ", r" + mintScript[b + 1].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x32)
                            {
                                scriptDecomp.Add("            jumpnot " + (int)mintScript[b + 3] + ", r" + mintScript[b + 1].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x33)
                            {
                                scriptDecomp.Add("            decl " + mintScript[b + 1].ToString("X2") + ", " + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x34)
                            {
                                scriptDecomp.Add("            ret");
                                break;
                            }
                            else if (mintScript[b] == 0x35)
                            {
                                scriptDecomp.Add("            ret r" + mintScript[b + 2].ToString("X2"));
                                break;
                            }
                            else if (mintScript[b] == 0x36)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            call " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x38)
                            {
                                scriptDecomp.Add("            copy r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2") + ", r" + mintScript[b + 3].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x3A)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            new r" + mintScript[b + 1].ToString("X2") + ", " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x3C)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            del r" + mintScript[b + 1].ToString("X2") + ", " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x3D)
                            {
                                uint xrefId = (uint)BitConverter.ToUInt16(new byte[] { mintScript[b + 3], mintScript[b + 2] }, 0);
                                scriptDecomp.Add("            getfield r" + mintScript[b + 1].ToString("X2") + ", " + xref[(int)xrefId]);
                            }
                            else if (mintScript[b] == 0x3E)
                            {
                                scriptDecomp.Add("            mkarray r" + mintScript[b + 1].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x3F)
                            {
                                scriptDecomp.Add("            getindex r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x40)
                            {
                                scriptDecomp.Add("            arraylen r" + mintScript[b + 1].ToString("X2") + ", r" + mintScript[b + 2].ToString("X2"));
                            }
                            else if (mintScript[b] == 0x41)
                            {
                                scriptDecomp.Add("            delarray r" + mintScript[b + 1].ToString("X2"));
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Error: Could not process command 0x" + mintScript[b].ToString("X2") + " at 0x" + b.ToString("X8") + " in script " + fileName);
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
