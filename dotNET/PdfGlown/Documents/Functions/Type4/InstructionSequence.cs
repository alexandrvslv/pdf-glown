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
using PdfGlown.Bytes;
using PdfGlown.Objects;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Functions.Type4
{

    /// <summary>Represents an instruction sequence, a combination of values, operands and nested procedures.</summary>
    public class InstructionSequence : PdfSimpleObject<List<PdfDirectObject>>
    {

        private readonly List<PdfDirectObject> instructions = new();

        /// <summary>Add a name (ex. an operator)</summary>
        /// <param name="name">the name</param>
        public void AddName(string name)
        {
            instructions.Add(PdfName.Get(name));
        }

        /// <summary>Adds an int value.</summary>
        /// <param name="value">the value</param>
        public void AddInteger(int value)
        {
            instructions.Add(PdfInteger.Get(value));
        }

        /// <summary>Adds a real value.
        /// </summary>
        /// <param name="value">the value</param>
        public void AddReal(float value)
        {
            instructions.Add(PdfReal.Get(value));
        }

        /// <summary>Adds a bool value.</summary>
        /// <param name="value">the value</param>
        public void AddBoolean(bool value)
        {
            instructions.Add(PdfBoolean.Get(value));
        }

        /// <summary>Adds a proc (sub-sequence of instructions).</summary>
        /// <param name="child">the child proc</param>
        public void AddProc(InstructionSequence child)
        {
            instructions.Add(child);
        }

        /// <summary>Executes the instruction sequence.</summary>
        /// <param name="context">the execution context</param>
        /// <exception cref="NotSupportedException"></exception>
        public void Execute(ExecutionContext context)
        {
            var stack = context.Stack;
            foreach (PdfDirectObject o in instructions)
            {
                if (o is IPdfString pdfString)
                {
                    string name = pdfString.StringValue;
                    var cmd = Operators.GetOperator(name);
                    if (cmd != null)
                    {
                        cmd.Execute(context);
                    }
                    else
                    {
                        throw new NotSupportedException("Unknown operator or name: " + name);
                    }
                }
                else
                {
                    stack.Push(o);
                }
            }

            //Handles top-level procs that simply need to be executed
            while (stack.Count > 0 && stack.Peek() is InstructionSequence)
            {
                var nested = (InstructionSequence)stack.Pop();
                nested.Execute(context);
            }
        }

        public override int CompareTo(PdfDirectObject obj)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            throw new NotImplementedException();
        }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data)
        {
            throw new NotImplementedException();
        }
    }
}
