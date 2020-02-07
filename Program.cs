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
            if (!ReadFile(Args[0]))
            {
                Console.WriteLine("The input file could not be read.");
                return;
            }

            Tokenizer Tokenizer = new Tokenizer(Input);
            Token[] Tokens = Tokenizer.Run();

            Parser Parser = new Parser(Tokens);
            Block MainBlock = Parser.Run();

            Generator Generator = new Generator(MainBlock);
            string Output = Generator.Run();

            if(!WriteFile(Args[1], Output))
            {
                Console.WriteLine("The output file could not be written.");
                return;
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
