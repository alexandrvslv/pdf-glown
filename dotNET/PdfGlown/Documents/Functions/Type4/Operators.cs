/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections.Generic;

namespace PdfGlown.Documents.Functions.Type4
{
    /// This class provides all the supported operators.
    public static class Operators
    {

        //Arithmetic operators
        public static readonly Operator ABS = new ArithmeticOperators.Abs();
        public static readonly Operator ADD = new ArithmeticOperators.Add();
        public static readonly Operator ATAN = new ArithmeticOperators.Atan();
        public static readonly Operator CEILING = new ArithmeticOperators.Ceiling();
        public static readonly Operator COS = new ArithmeticOperators.Cos();
        public static readonly Operator CVI = new ArithmeticOperators.Cvi();
        public static readonly Operator CVR = new ArithmeticOperators.Cvr();
        public static readonly Operator DIV = new ArithmeticOperators.Div();
        public static readonly Operator EXP = new ArithmeticOperators.Exp();
        public static readonly Operator FLOOR = new ArithmeticOperators.Floor();
        public static readonly Operator IDIV = new ArithmeticOperators.IDiv();
        public static readonly Operator LN = new ArithmeticOperators.Ln();
        public static readonly Operator LOG = new ArithmeticOperators.Log();
        public static readonly Operator MOD = new ArithmeticOperators.Mod();
        public static readonly Operator MUL = new ArithmeticOperators.Mul();
        public static readonly Operator NEG = new ArithmeticOperators.Neg();
        public static readonly Operator ROUND = new ArithmeticOperators.Round();
        public static readonly Operator SIN = new ArithmeticOperators.Sin();
        public static readonly Operator SQRT = new ArithmeticOperators.Sqrt();
        public static readonly Operator SUB = new ArithmeticOperators.Sub();
        public static readonly Operator TRUNCATE = new ArithmeticOperators.Truncate();

        //Relational, boolean and bitwise operators
        public static readonly Operator AND = new BitwiseOperators.And();
        public static readonly Operator BITSHIFT = new BitwiseOperators.Bitshift();
        public static readonly Operator EQ = new RelationalOperators.Eq();
        public static readonly Operator FALSE = new BitwiseOperators.False();
        public static readonly Operator GE = new RelationalOperators.Ge();
        public static readonly Operator GT = new RelationalOperators.Gt();
        public static readonly Operator LE = new RelationalOperators.Le();
        public static readonly Operator LT = new RelationalOperators.Lt();
        public static readonly Operator NE = new RelationalOperators.Ne();
        public static readonly Operator NOT = new BitwiseOperators.Not();
        public static readonly Operator OR = new BitwiseOperators.Or();
        public static readonly Operator TRUE = new BitwiseOperators.True();
        public static readonly Operator XOR = new BitwiseOperators.Xor();

        //Conditional operators
        public static readonly Operator IF = new ConditionalOperators.If();
        public static readonly Operator IFELSE = new ConditionalOperators.IfElse();

        //Stack operators
        public static readonly Operator COPY = new StackOperators.Copy();
        public static readonly Operator DUP = new StackOperators.Dup();
        public static readonly Operator EXCH = new StackOperators.Exch();
        public static readonly Operator INDEX = new StackOperators.Index();
        public static readonly Operator POP = new StackOperators.Pop();
        public static readonly Operator ROLL = new StackOperators.Roll();

        public static readonly Dictionary<string, Operator> operators = new();

        /// <summary>Creates a new Operators object with the default set of operators.</summary>
        static Operators()
        {
            operators.Add("add", ADD);
            operators.Add("abs", ABS);
            operators.Add("atan", ATAN);
            operators.Add("ceiling", CEILING);
            operators.Add("cos", COS);
            operators.Add("cvi", CVI);
            operators.Add("cvr", CVR);
            operators.Add("div", DIV);
            operators.Add("exp", EXP);
            operators.Add("floor", FLOOR);
            operators.Add("idiv", IDIV);
            operators.Add("ln", LN);
            operators.Add("log", LOG);
            operators.Add("mod", MOD);
            operators.Add("mul", MUL);
            operators.Add("neg", NEG);
            operators.Add("round", ROUND);
            operators.Add("sin", SIN);
            operators.Add("sqrt", SQRT);
            operators.Add("sub", SUB);
            operators.Add("truncate", TRUNCATE);

            operators.Add("and", AND);
            operators.Add("bitshift", BITSHIFT);
            operators.Add("eq", EQ);
            operators.Add("false", FALSE);
            operators.Add("ge", GE);
            operators.Add("gt", GT);
            operators.Add("le", LE);
            operators.Add("lt", LT);
            operators.Add("ne", NE);
            operators.Add("not", NOT);
            operators.Add("or", OR);
            operators.Add("true", TRUE);
            operators.Add("xor", XOR);

            operators.Add("if", IF);
            operators.Add("ifelse", IFELSE);

            operators.Add("copy", COPY);
            operators.Add("dup", DUP);
            operators.Add("exch", EXCH);
            operators.Add("index", INDEX);
            operators.Add("pop", POP);
            operators.Add("roll", ROLL);
        }

        /// <summary>Returns the operator for the given operator name.</summary>
        /// <param name="operatorName">the operator name</param>
        /// <returns>the operator (or null if there's no such operator</returns>
        public static Operator GetOperator(string operatorName)
        {
            return operators.TryGetValue(operatorName, out var stdOperator) ? stdOperator : null;
        }

    }
}
