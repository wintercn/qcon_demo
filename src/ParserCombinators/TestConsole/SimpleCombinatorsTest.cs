﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VBF.Compilers.Scanners;

namespace TestConsole
{
    using SimpleCombinators;
    using System.IO;
    using VBF.Compilers;
    using RE = VBF.Compilers.Scanners.RegularExpression;

    class SimpleCombinatorsTest
    {
        private ScannerInfo m_scannerInfo;

        private Token PLUS;
        private Token ASTERISK;
        private Token LEFT_PARENTHESIS;
        private Token RIGHT_PARENTHESIS;
        private Token NUMBER;
        private Token SPACE;

        private void SetUpScanner()
        {
            var lexcion = new Lexicon();

            var lexer = lexcion.Lexer;

            PLUS = lexer.DefineToken(RE.Symbol('+'));
            ASTERISK = lexer.DefineToken(RE.Symbol('*'));
            LEFT_PARENTHESIS = lexer.DefineToken(RE.Symbol('('));
            RIGHT_PARENTHESIS = lexer.DefineToken(RE.Symbol(')'));
            NUMBER = lexer.DefineToken(RE.Range('0', '9').Many1(), "number");
            SPACE = lexer.DefineToken(RE.Symbol(' ').Many1());

            m_scannerInfo = lexcion.CreateScannerInfo();
        }

        private Parse<int> SetUpParser()
        {
            Parse<int> T = null;

            Parse<int> Num = from n in NUMBER.AsTerminal() select Int32.Parse(n);

            //U → ‘[0..9]+’ | ‘(’ T ‘)’  
            Parse<int> U = Grammar.Union(
                Num,
                from lp in LEFT_PARENTHESIS.AsTerminal()
                from exp in T
                from rp in RIGHT_PARENTHESIS.AsTerminal()
                select exp
                );

            //F → U F1
            Parse<IEnumerable<int>> F1 = null;
            F1 = Grammar.Union(
                from op in ASTERISK.AsTerminal()
                from u in U
                from f1 in F1
                select new[] { u }.Concat(f1),
                Grammar.Empty(Enumerable.Empty<int>())
                );

            //F1 → ‘*’ U F1 | ε
            Parse<int> F =
                from u in U
                from f1 in F1
                select f1.Aggregate(u, (a, i) => a * i);

            //T → F T1
            Parse<IEnumerable<int>> T1 = null;
            T1 = Grammar.Union(
                from op in PLUS.AsTerminal()
                from f in F
                from t1 in T1
                select new[] { f }.Concat(t1),
                Grammar.Empty(Enumerable.Empty<int>())
                );

            //T1 → ‘+’ F T1 | ε
            T =
                from f in F
                from t1 in T1
                select t1.Aggregate(f, (a, i) => a + i);

            //E → T$
            Parse<int> E = from t in T
                           from eos in Grammar.Eos()
                           select t;

            return E;
        }

        public void Test(SourceReader sr)
        {
            Console.WriteLine("=============== Simple Parser Combinators ===============");
            SetUpScanner();
            var parse = SetUpParser();

            ForkableScannerBuilder fsb = new ForkableScannerBuilder(m_scannerInfo);
            fsb.SetTriviaTokens(SPACE.Index);


            var scanner = fsb.Create(sr);

            try
            {
                var result = parse(scanner);
                Console.WriteLine("Result: {0}", result.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Parse Errors:");
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
