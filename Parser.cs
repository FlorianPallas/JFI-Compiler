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
        public Block ParentBlock;
        public Parser Parser;
        public int BlockOffset;
        private int CurrentOffset;
        public string EndLabel;

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
            EndLabel = Parser.NewLabel("BLOCK");

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
                                    Token Value1 = BlockTokens[Index + 2];
                                    string Operator = BlockTokens[Index + 3].Body;
                                    Token Value2 = BlockTokens[Index + 4];
                                    Index += 6;
                                    Block SuccessBlock = GetBlock();
                                    Statements.Enqueue(new IfStatement(this, Value1, Value2, Operator, SuccessBlock, null));
                                    break;
                                }
                            case "WHILE":
                                {
                                    Token Value1 = BlockTokens[Index + 2];
                                    string Operator = BlockTokens[Index + 3].Body;
                                    Token Value2 = BlockTokens[Index + 4];
                                    Index += 6;
                                    Block LoopBlock = GetBlock();
                                    Statements.Enqueue(new WhileStatement(this, Value1, Value2, Operator, LoopBlock));
                                    break;
                                }
                            case "PRINT":
                                {
                                    Token Value = BlockTokens[Index + 2];
                                    Index += 5;
                                    Statements.Enqueue(new PrintStatement(this, Value));
                                    break;
                                }
                            case "RETURN":
                                {
                                    Index += 2;
                                    Statements.Enqueue(new ReturnStatement(this));
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
                            Token Value = BlockTokens[Index + 2];
                            Statements.Enqueue(new AssignStatement(this, Identifier, Value));
                            Index += 4;
                        }
                        else if (BlockTokens[Index + 1].Type == TokenType.OPERATOR && BlockTokens[Index + 1].Body == "+=")
                        {
                            Token Value = BlockTokens[Index + 2];
                            Statements.Enqueue(new IncrementStatement(this, Identifier, Value));
                            Index += 4;
                        }
                        else if (BlockTokens[Index + 1].Type == TokenType.OPERATOR && BlockTokens[Index + 1].Body == "-=")
                        {
                            Token Value = BlockTokens[Index + 2];
                            Statements.Enqueue(new DecrementStatement(this, Identifier, Value));
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
                                    Token Value = BlockTokens[Index + 3];
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

            Output += EndLabel + ":";
            Output += "\n";
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

        protected string GetBranchCommand(string Operator)
        {
            switch (Operator)
            {
                case "==": { return "BEQ"; }
                case "!=": { return "BNQ"; }
                case ">": { return "BGT"; }
                case ">=": { return "BGE"; }
                case "<": { return "BLT"; }
                case "<=": { return "BLE"; }
                default: { throw new Exception("Invalid Operator"); }
            }
        }

        protected string GetVariableOrConstant(Token T, string Register)
        {
            string Output = String.Empty;
            if (T.Type == TokenType.CONSTANT)
            {
                Output += "LI " + Register + ", " + T.Body;
            }
            else if (T.Type == TokenType.IDENTIFIER)
            {
                int Offset = CurrentBlock.GetVariableOffset(T.Body);
                Output += "LW " + Register + ", " + Offset + "($sp)";
            }
            else if(T.Type == TokenType.KEYWORD && T.Body.ToUpper() == "READ")
            {
                Output += "LI $v0, 5";
                Output += "\n";
                Output += "SYSCALL";
                Output += "\n";
                Output += "MOVE " + Register + ", $v0";
            }
            else
            {
                throw new Exception("Invalid Argument");
            }
            return Output;
        }
    }

    public class IncrementStatement : Statement
    {
        string Identifier;
        Token Value;

        public IncrementStatement(Block _CurrentBlock, string _Identifier, Token _Value) : base(_CurrentBlock)
        {
            Identifier = _Identifier;
            Value = _Value;
        }

        public override string GenerateCode()
        {
            string Output = String.Empty;
            int Offset = CurrentBlock.GetVariableOffset(Identifier);
            Output += "LW $t0, " + Offset + "($sp)";
            Output += "\n";
            Output += GetVariableOrConstant(Value, "$t1");
            Output += "\n";
            Output += "ADD $t0, $t0, $t1";
            Output += "\n";
            Output += "SW $t0, " + Offset + "($sp)";
            return Output;
        }
    }

    public class DecrementStatement : Statement
    {
        string Identifier;
        Token Value;

        public DecrementStatement(Block _CurrentBlock, string _Identifier, Token _Value) : base(_CurrentBlock)
        {
            Identifier = _Identifier;
            Value = _Value;
        }

        public override string GenerateCode()
        {
            string Output = String.Empty;
            int Offset = CurrentBlock.GetVariableOffset(Identifier);
            Output += "LW $t0, " + Offset + "($sp)";
            Output += "\n";
            Output += GetVariableOrConstant(Value, "$t1");
            Output += "\n";
            Output += "SUB $t0, $t0, $t1";
            Output += "\n";
            Output += "SW $t0, " + Offset + "($sp)";
            return Output;
        }
    }

    public class ReturnStatement : Statement
    {

        public ReturnStatement(Block _CurrentBlock) : base(_CurrentBlock)
        {

        }

        public override string GenerateCode()
        {
            string Output = String.Empty;
            Output += "ADDI $sp, $sp, " + -CurrentBlock.BlockOffset;
            Output += "\n";
            Output += "J " + CurrentBlock.ParentBlock.EndLabel;
            Output += "\n";
            return Output;
        }
    }

    public class PrintStatement : Statement
    {
        Token Value;

        public PrintStatement(Block _CurrentBlock, Token _Value) : base(_CurrentBlock)
        {
            Value = _Value;
        }

        public override string GenerateCode()
        {
            string Output = String.Empty;
            Output += GetVariableOrConstant(Value, "$a0");
            Output += "\n";
            Output += "LI $v0, 1";
            Output += "\n";
            Output += "SYSCALL";
            return Output;
        }
    }

    public class AssignStatement : Statement
    {
        string Identifier;
        Token Value;

        public AssignStatement(Block _CurrentBlock, string _Identifier, Token _Value) : base(_CurrentBlock)
        {
            Identifier = _Identifier;
            Value = _Value;
        }

        public override string GenerateCode()
        {
            string Output = String.Empty;
            int Offset = CurrentBlock.GetVariableOffset(Identifier);

            Output += GetVariableOrConstant(Value, "$t0");
            Output += "\n";
            Output += "SW $t0, " + Offset + "($sp)";
            return Output;
        }
    }

    public class IfStatement : Statement
    {
        Token Value1;
        Token Value2;
        Block SuccessBlock;
        Block FailBlock;
        string Operator;

        public IfStatement(Block _CurrentBlock, Token _Value1, Token _Value2, string _Operator, Block _SuccessBlock, Block _FailBlock) : base(_CurrentBlock)
        {
            Value1 = _Value1;
            Value2 = _Value2;
            SuccessBlock = _SuccessBlock;
            FailBlock = _FailBlock;
            Operator = _Operator;
        }

        public override string GenerateCode()
        {
            string StartLabel = CurrentBlock.Parser.NewLabel("IF");
            string SuccessLabel = CurrentBlock.Parser.NewLabel("IF");
            string EndLabel = CurrentBlock.Parser.NewLabel("IF");

            string Output = String.Empty;
            Output += GetVariableOrConstant(Value1, "$t1");
            Output += "\n";
            Output += GetVariableOrConstant(Value2, "$t2");
            Output += "\n";
            Output += GetBranchCommand(Operator) + " $t1, $t2, " + SuccessLabel;
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

    public class WhileStatement : Statement
    {
        Token Value1;
        Token Value2;
        Block LoopBlock;
        string Operator;

        public WhileStatement(Block _CurrentBlock, Token _Value1, Token _Value2, string _Operator, Block _LoopBlock) : base(_CurrentBlock)
        {
            Value1 = _Value1;
            Value2 = _Value2;
            Operator = _Operator;
            LoopBlock = _LoopBlock;
        }

        public override string GenerateCode()
        {
            string StartLabel = CurrentBlock.Parser.NewLabel("WHILE");
            string BodyLabel = CurrentBlock.Parser.NewLabel("WHILE");
            string EndLabel = CurrentBlock.Parser.NewLabel("WHILE");

            string Output = String.Empty;
            Output += StartLabel + ":";
            Output += "\n";
            Output += GetVariableOrConstant(Value1, "$t1");
            Output += "\n";
            Output += GetVariableOrConstant(Value2, "$t2");
            Output += "\n";
            Output += GetBranchCommand(Operator) + " $t1, $t2, " + BodyLabel;
            Output += "\n";
            Output += "J " + EndLabel;
            Output += "\n";
            Output += BodyLabel + ":";
            Output += "\n";
            Output += LoopBlock.GenerateCode();
            Output += "\n";
            Output += "J " + StartLabel;
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
