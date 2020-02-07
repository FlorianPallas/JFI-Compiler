using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFICompiler
{
    /// <summary>
    /// Generates an AST from the tokens provided by the tokenizer.
    /// </summary>
    public class Parser
    {
        private Token[] Tokens;

        public Parser(Token[] _Tokens)
        {
            Tokens = _Tokens;
            Labels = new Dictionary<string, int>();
        }

        public Block Run()
        {
            Block MainBlock = new Block(Tokens, this, null);
            MainBlock.SetOffsets();
            return MainBlock;
        }

        Dictionary<string, int> Labels;

        public string NewLabel()
        {
            return NewLabel("l");
        }

        public string NewLabel(string Prefix)
        {
            if (!Labels.ContainsKey(Prefix))
            {
                Labels.Add(Prefix, 0);
            }

            string Label = Prefix + Labels[Prefix];
            Labels[Prefix]++;
            return Label;
        }
    }

    public class Block
    {
        private List<Block> ChildBlocks;
        protected Block ParentBlock;
        public Parser Parser;
        public int BlockOffset;
        private int CurrentOffset;

        private Queue<Statement> Statements;
        private Token[] BlockTokens;
        private int Index;

        Dictionary<string, int> IntegerVariables;

        public Block(Token[] _Tokens, Parser _Parser, Block _ParentBlock)
        {
            if(_ParentBlock != null)
            {
                _ParentBlock.ChildBlocks.Add(this);
            }

            Parser = _Parser;
            Statements = new Queue<Statement>();
            ChildBlocks = new List<Block>();
            BlockTokens = _Tokens;
            ParentBlock = _ParentBlock;
            Index = 0;
            BlockOffset = 0;
            CurrentOffset = 0;
            IntegerVariables = new Dictionary<string, int>();

            while (Index < BlockTokens.Length)
            {
                Token T = BlockTokens[Index];
                HandleStatement(T);
            }
        }

        public int SetOffsets()
        {
            int Capacity = 4 * IntegerVariables.Count;
            int OffsetSum = -Capacity;
            foreach(Block C in ChildBlocks)
            {
                C.BlockOffset = OffsetSum;
                OffsetSum -= C.SetOffsets();
            }

            return OffsetSum;
        }

        public void DeclareVariable(string Identifier)
        {
            if (IntegerVariables.ContainsKey(Identifier))
            {
                throw new Exception("Variable already declared.");
            }

            IntegerVariables.Add(Identifier, CurrentOffset);
            CurrentOffset -= 4;
        }

        private void HandleStatement(Token T)
        {
            switch (T.Type)
            {
                case TokenType.KEYWORD:
                    {
                        switch (T.Body.ToUpper())
                        {
                            case "IF":
                                {
                                    int Value1 = int.Parse(BlockTokens[Index + 2].Body);
                                    int Value2 = int.Parse(BlockTokens[Index + 4].Body);
                                    Index += 6;
                                    Block SuccessBlock = GetBlock();
                                    Statements.Enqueue(new IfStatement(this, Value1, Value2, SuccessBlock, null));
                                    break;
                                }
                            default: { throw new Exception("Invalid Keyword!"); }
                        }
                        break;
                    }
                case TokenType.IDENTIFIER:
                    {
                        string Identifier = BlockTokens[Index].Body;
                        if(BlockTokens[Index + 1].Type == TokenType.OPERATOR && BlockTokens[Index + 1].Body == "=")
                        {
                            int Value = int.Parse(BlockTokens[Index + 2].Body);
                            Statements.Enqueue(new AssignStatement(this, Identifier, Value));
                            Index += 4;
                        }
                        break;
                    }
                case TokenType.TYPE:
                    {
                        switch (T.Body.ToUpper())
                        {
                            case "INT":
                                {
                                    string Identifier = BlockTokens[Index + 1].Body;
                                    int Value = 0;
                                    if(BlockTokens[Index + 3].Type == TokenType.CONSTANT)
                                    {
                                        Value = int.Parse(BlockTokens[Index + 3].Body);
                                    }
                                    DeclareVariable(Identifier);
                                    Statements.Enqueue(new AssignStatement(this, Identifier, Value));
                                    Index += 5;
                                    break;
                                }
                            default: { throw new Exception("Invalid Keyword!"); }
                        }
                        break;
                    }
                default: { throw new Exception("Invalid Statement!"); }
            }
        }

        private Block GetBlock()
        {
            Token CurrentToken = BlockTokens[Index];

            if (CurrentToken.Type != TokenType.SYMBOL || CurrentToken.Body != "{")
            {
                throw new Exception("Expected '{'");
            }

            Index++;
            CurrentToken = BlockTokens[Index];

            int LayerCounter = 0;
            List<Token> Tokens = new List<Token>();
            while (Index < BlockTokens.Length)
            {
                Tokens.Add(BlockTokens[Index]);
                if (CurrentToken.Type == TokenType.SYMBOL && CurrentToken.Body == "{")
                {
                    LayerCounter++;
                }

                if (CurrentToken.Type == TokenType.SYMBOL && CurrentToken.Body == "}")
                {
                    LayerCounter--;
                    if (LayerCounter < 0)
                    {
                        break;
                    }
                }

                Index++;
                if (Index >= BlockTokens.Length)
                {
                    throw new Exception("Expected '}'");
                }
                CurrentToken = BlockTokens[Index];
            }

            Tokens.RemoveAt(Tokens.Count - 1);
            Index++;

            return new Block(Tokens.ToArray(), Parser, this);
        }

        private Expression GetExpression()
        {
            return null;
        }

        private Expression GetExpressionParanthesis()
        {
            Token CurrentToken = BlockTokens[Index];

            if(CurrentToken.Type != TokenType.SYMBOL || CurrentToken.Body != "(")
            {
                throw new Exception("Expected '('");
            }

            Index++;
            CurrentToken = BlockTokens[Index];

            int LayerCounter = 0;
            List<Token> ExpressionTokens = new List<Token>();
            while (CurrentToken.Type == TokenType.CONSTANT || CurrentToken.Type == TokenType.IDENTIFIER || CurrentToken.Type == TokenType.OPERATOR || CurrentToken.Type == TokenType.SYMBOL)
            {
                ExpressionTokens.Add(BlockTokens[Index]);

                if (CurrentToken.Type == TokenType.SYMBOL && CurrentToken.Body == "(")
                {
                    LayerCounter++;
                }

                if (CurrentToken.Type == TokenType.SYMBOL && CurrentToken.Body == ")")
                {
                    LayerCounter--;
                    if(LayerCounter < 0)
                    {
                        break;
                    }
                }

                Index++;
                if (Index >= BlockTokens.Length)
                {
                    throw new Exception("Expected ')'");
                }
                CurrentToken = BlockTokens[Index];
            }

            Index = 0;
            while(Index < ExpressionTokens.Count)
            {
                Token T = ExpressionTokens[Index];
                if(T.Type == TokenType.CONSTANT)
                {

                }
            }

            return null;
        }

        public string GenerateCode()
        {
            string Output = String.Empty;

            Output += "ADDI $sp, $sp, " + BlockOffset;
            Output += "\n";

            foreach (Statement S in Statements)
            {
                if(S == null) { continue; }
                Output += S.GenerateCode();
                Output += "\n";
            }

            Output += "ADDI $sp, $sp, " + -BlockOffset;
            Output += "\n";

            return Output;
        }

        internal int GetVariableOffset(string Identifier)
        {
            if (IntegerVariables.ContainsKey(Identifier))
            {
                return IntegerVariables[Identifier];
            }
            else if (ParentBlock != null && ParentBlock.IntegerVariables.ContainsKey(Identifier))
            {
                return ParentBlock.IntegerVariables[Identifier] - BlockOffset;
            }
            else
            {
                throw new Exception("Variable out of scope.");
            }
        }
    }

    public abstract class Statement
    {
        protected Block CurrentBlock;

        public Statement(Block _CurrentBlock)
        {
            CurrentBlock = _CurrentBlock;
        }

        public virtual string GenerateCode()
        {
            return String.Empty;
        }
    }

    public class AssignStatement : Statement
    {
        string Identifier;
        int Value;

        public AssignStatement(Block _CurrentBlock, string _Identifier, int _Value) : base(_CurrentBlock)
        {
            Identifier = _Identifier;
            Value = _Value;
        }

        public override string GenerateCode()
        {
            string Output = String.Empty;
            int Offset = CurrentBlock.GetVariableOffset(Identifier);

            Output += "LI $t1, " + Value;
            Output += "\n";
            Output += "SW $t1, " + Offset + "($sp)";
            return Output;
        }
    }

    public class IfStatement : Statement
    {
        int Value1;
        int Value2;
        Block SuccessBlock;
        Block FailBlock;

        public IfStatement(Block _CurrentBlock, int _Value1, int _Value2, Block _SuccessBlock, Block _FailBlock) : base(_CurrentBlock)
        {
            Value1 = _Value1;
            Value2 = _Value2;
            SuccessBlock = _SuccessBlock;
            FailBlock = _FailBlock;
        }

        public override string GenerateCode()
        {
            string StartLabel = CurrentBlock.Parser.NewLabel("IF");
            string SuccessLabel = CurrentBlock.Parser.NewLabel("IF");
            string EndLabel = CurrentBlock.Parser.NewLabel("IF");

            string Output = String.Empty;
            Output += "LI $t1, " + Value1;
            Output += "\n";
            Output += "LI $t2, " + Value2;
            Output += "\n";
            Output += "BEQ $t1, $t2, " + SuccessLabel;
            Output += "\n";

            if (FailBlock != null)
            {
                Output += FailBlock.GenerateCode();
                Output += "\n";
                
            }
            Output += "J " + EndLabel;
            Output += "\n";

            Output += SuccessLabel + ":";
            Output += "\n";
            Output += SuccessBlock.GenerateCode();
            Output += "\n";
            Output += EndLabel + ":";
            return Output;
        }
    }

    abstract class Expression
    {
        public virtual string GenerateCode()
        {
            return String.Empty;
        }
    }

    abstract class ExpressionLayer
    {
        protected Expression Value1;
        protected Expression Value2;

        public ExpressionLayer(Token _Value, Token _Operator)
        {
            if (_Operator.Body == "+")
            {
                if (_Value.Type == TokenType.CONSTANT)
                {
                    Value1 = new ExpressionConstant(int.Parse(_Value.Body));
                }
                else if (_Value.Type == TokenType.IDENTIFIER)
                {
                    Value1 = new ExpressionVariable(_Value.Body);
                }
                else
                {
                    throw new Exception("Invalid Expression");
                }
            }
        }

        protected virtual string GetOperator()
        {
            return null;
        }
    }

    class E1 : ExpressionLayer
    {
        public E1(Token _Value, Token _Operator) : base(_Value, _Operator) { }

        protected override string GetOperator()
        {
            return "+";
        }
    }

    class E2 : ExpressionLayer
    {
        public E2(Token _Value, Token _Operator) : base(_Value, _Operator) { }

        protected override string GetOperator()
        {
            return "*";
        }
    }

    class E3 : ExpressionLayer
    {
        public E3(Token _Value, Token _Operator) : base(_Value, _Operator) { }

        protected override string GetOperator()
        {
            return null;
        }
    }

    class E4 : ExpressionLayer
    {
        public E4(Token _Value, Token _Operator) : base(_Value, _Operator)
        {

        }
    }

    class ExpressionConstant : Expression
    {
        int Value;

        public ExpressionConstant(int _Value)
        {
            Value = _Value;
        }

        public override string GenerateCode()
        {
            return Value.ToString();
        }
    }

    class ExpressionVariable : Expression
    {
        string Identifier;

        public ExpressionVariable(string _Identifier)
        {
            Identifier = _Identifier;
        }
        public override string GenerateCode()
        {
            return "LOAD " + Identifier;
        }
    }
}
