using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFICompiler
{
    /// <summary>
    /// Generates a list of tokens out of the original input string.
    /// </summary>
    class Tokenizer
    {
        private string Input;
        private StringBuilder SB;
        private List<Token> Tokens;
        private bool ParsingConstant;
        private int CurrentLine;

        public Tokenizer(string _Input)
        {
            Input = _Input;
            SB = new StringBuilder();
            Tokens = new List<Token>();

            ParsingConstant = false;
            CurrentLine = 0;
        }

        public Token[] Run()
        {
            try
            {
                for (int I = 0; I < Input.Length; I++)
                {
                    InterpretBuffer(I);
                }
            }
            catch (Exception E)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Syntax Error in Line " + CurrentLine);
                Console.WriteLine(E.Message);
                Console.ForegroundColor = ConsoleColor.White;
                return new Token[0];
            }

            return Tokens.ToArray();
        }

        private void InterpretBuffer(int Index)
        {
            char NewChar = Input[Index];
            string Buffer = SB.ToString();

            if(Buffer.Length > 0 && ((NewChar == '=' && Buffer[0] != '=' && Buffer[0] != '<' && Buffer[0] != '>' && Buffer[0] != '+' && Buffer[0] != '-') || NewChar == ')' || NewChar == ';' || NewChar == '>' || NewChar == '<' || NewChar == '+' || NewChar == '-'))
            {
                if (ParsingConstant || Buffer == "true" || Buffer == "false")
                {
                    Tokens.Add(new Token(TokenType.CONSTANT, Buffer));
                }
                else
                {
                    Tokens.Add(new Token(TokenType.IDENTIFIER, Buffer));
                }
                
                ParsingConstant = false;
                SB.Clear();
            }
            else if (NewChar == '\n')
            {
                CurrentLine++;
                return;
            }
            else if (Char.IsWhiteSpace(NewChar) || NewChar == '\r')
            {
                return;
            }
            else if (Char.IsDigit(NewChar) && !ParsingConstant && Buffer.Length < 1)
            {
                ParsingConstant = true;
                SB.Append(NewChar);
                return;
            }

            SB.Append(NewChar);
            Buffer = SB.ToString();

            if (Buffer == "if" || Buffer == "while" || Buffer == "print" || Buffer == "read" || Buffer == "return")
            {
                Tokens.Add(new Token(TokenType.KEYWORD, Buffer));
            }
            else if (Buffer == "int" || Buffer == "bool")
            {
                Tokens.Add(new Token(TokenType.TYPE, Buffer));
            }
            else if (Buffer == "(" || Buffer == ")" || Buffer == "{" || Buffer == "}" || Buffer == ";")
            {
                Tokens.Add(new Token(TokenType.SYMBOL, Buffer));
            }
            else if (Buffer == "=" || Buffer == "<" || Buffer == ">" || Buffer == "+" || Buffer == "-")
            {
                char NextChar = Input[Index + 1];
                if (NextChar == '=' || NextChar == '<' || NextChar == '>')
                {
                    return;
                }
                else
                {
                    Tokens.Add(new Token(TokenType.OPERATOR, Buffer));
                }
            }
            else if (Buffer == "+" || Buffer == "-" || Buffer == "*" || Buffer == "/" || Buffer == "^" || Buffer == "==" || Buffer == "+=" || Buffer == "-=" || Buffer == "&&" || Buffer == "||" || Buffer == "<=" || Buffer == ">=")
            {
                Tokens.Add(new Token(TokenType.OPERATOR, Buffer));
            }
            else
            {
                if (ParsingConstant)
                {
                    Tokens.Add(new Token(TokenType.CONSTANT, Buffer));
                    ParsingConstant = false;
                }
                else
                {
                    return;
                }
            }

            SB.Clear();
        }
    }

    public enum TokenType
    {
        KEYWORD, TYPE, CONSTANT, IDENTIFIER, OPERATOR, SYMBOL
    }

    public struct Token
    {
        public TokenType Type;
        public string Body;

        public Token(TokenType _Type, string _Body)
        {
            Type = _Type;
            Body = _Body;
        }
    }
}
