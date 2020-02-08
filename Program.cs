using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFICompiler
{
    class Program
    {
        private static string Input;

        private static void Main(string[] Args)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("> COMPILING");
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("- Reader");
                if (!ReadFile(Args[0]))
                {
                    Console.WriteLine("The input file could not be read.");
                    return;
                }

                Console.WriteLine("- Tokenizer");
                Tokenizer Tokenizer = new Tokenizer(Input);
                Token[] Tokens = Tokenizer.Run();

                /*
                foreach (Token T in Tokens)
                {
                    Console.WriteLine(T.Type + " " + T.Body);
                }
                */

                Console.WriteLine("- Parser");
                Parser Parser = new Parser(Tokens);
                Block MainBlock = Parser.Run();

                Console.WriteLine("- Generator");
                Generator Generator = new Generator(MainBlock);
                string Output = Generator.Run();

                Console.WriteLine("- Writer");
                if (!WriteFile(Args[1], Output))
                {
                    Console.WriteLine("The output file could not be written.");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("> DONE");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception E)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("> FAILED");
                Console.WriteLine(E.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        private static bool ReadFile(string Path)
        {
            FileStream Stream = new FileStream(Path, FileMode.Open);
            StreamReader Reader = new StreamReader(Stream);

            try
            {
                Input = Reader.ReadToEnd();
            }
            catch
            {
                return false;
            }
            finally
            {
                Reader.Close();
                Stream.Close();
            }

            return true;
        }

        private static bool WriteFile(string Path, string Output)
        {
            FileStream Stream = new FileStream(Path, FileMode.Create);
            StreamWriter Writer = new StreamWriter(Stream);

            try
            {
                Writer.Write(Output);
            }
            catch
            {
                return false;
            }
            finally
            {
                Writer.Flush();
                Writer.Close();
                Stream.Close();
            }

            return true;
        }
    }
}
