using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFICompiler
{
    /// <summary>
    /// Generates MIPS Code from the AST provided by the parser.
    /// </summary>
    public class Generator
    {
        private Block MainBlock;

        public Generator(Block _MainBlock)
        {
            MainBlock = _MainBlock;
        }

        public string Run()
        {
            return MainBlock.GenerateCode();
        }
    }
}
