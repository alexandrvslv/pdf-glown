/*
 * https://github.com/apache/pdfbox
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
using System;
using SkiaSharp;
using System.Diagnostics;
using System.Linq;
using PdfGlown.Util.Collections;

namespace PdfGlown.Documents.Contents.Fonts.Type1
{
    /// <summary>
    /// This class represents and renders a Type 1 CharString.
    /// @author Villu Ruusmann
    /// @author John Hewson
    /// </summary>
    public class Type1CharString
    {
        private IType1CharStringReader font;
        private readonly string fontName;
        private readonly string glyphName;
        private SKPath path = null;
        private int width = 0;
        private SKPoint leftSideBearing;
        private SKPoint current;
        private bool isFlex = false;
        private readonly List<SKPoint> flexPoints = new();
        private readonly List<object> type1Sequence = new();
        private int commandCount;

        /// <summary>Constructs a new Type1CharString object.</summary>
        /// <param name="font">Parent Type 1 CharString font.</param>
        /// <param name="fontName">Name of the font.</param>
        /// <param name="glyphName">Name of the glyph.</param>
        /// <param name="sequence">Type 1 char string sequence</param>
        public Type1CharString(IType1CharStringReader font, string fontName, string glyphName, List<object> sequence)
            : this(font, fontName, glyphName)
        {
            type1Sequence.AddRange(sequence);
        }

        /// <summary>Constructor for use in subclasses.</summary>
        /// <param name="font">Parent Type 1 CharString font.</param>
        /// <param name="fontName">Name of the font.</param>
        /// <param name="glyphName">Name of the glyph.</param>
        protected Type1CharString(IType1CharStringReader font, string fontName, string glyphName)
        {
            this.font = font;
            this.fontName = fontName;
            this.glyphName = glyphName;
            this.current = new SKPoint(0, 0);
        }

        // todo: NEW name (or CID as hex)
        public string Name
        {
            get => glyphName;
        }

        public string FontName
        {
            get => fontName;
        }

        /// <summary>Returns the bounds of the renderer path.</summary>
        public SKRect Bounds
        {
            get => Path.Bounds;
        }

        /// <summary>Returns the advance width of the glyph.</summary>
        public int Width
        {
            get
            {
                if (path == null)
                {
                    Render();
                }

                return width;
            }
        }

        /// <summary>Returns the path of the character.</summary>
        public SKPath Path
        {
            get => path ?? Render();
        }

        /// <summary>Renders the Type 1 char string sequence to a GeneralPath.</summary>
        /// <returns>path</returns>
        private SKPath Render()
        {
            path = new SKPath() { FillType = SKPathFillType.EvenOdd };
            leftSideBearing = new SKPoint(0, 0);
            width = 0;
            var numbers = new List<float>();
            foreach (var obj in type1Sequence)
            {
                if (obj is CharStringCommand command)
                {
                    HandleType1Command(numbers, command);
                }
                else
                {
                    numbers.Add(System.Convert.ToSingle(obj));
                }
            }
            return path;
        }


        private void HandleType1Command(List<float> numbers, CharStringCommand command)
        {
            commandCount++;
            var type1KeyWord = command.Type1KeyWord;
            if (type1KeyWord == null)
            {
                // indicates an invalid charstring
                Debug.WriteLine($"warn: Unknown charstring command: {command.Type2KeyWord} in glyph {glyphName} of font {fontName}");
                numbers.Clear();
                return;
            }
            switch (type1KeyWord)
            {
                case Type1KeyWord.RMOVETO:
                    if (numbers.Count >= 2)
                    {
                        if (isFlex)
                        {
                            flexPoints.Add(new SKPoint(numbers[0], numbers[1]));
                        }
                        else
                        {
                            RmoveTo(numbers[0], numbers[1]);
                        }
                    }
                    break;
                case Type1KeyWord.VMOVETO:
                    if (numbers.Count > 0)
                    {
                        if (isFlex)
                        {
                            // not in the Type 1 spec, but exists in some fonts
                            flexPoints.Add(new SKPoint(0f, numbers[0]));
                        }
                        else
                        {
                            RmoveTo(0, numbers[0]);
                        }
                    }
                    break;
                case Type1KeyWord.HMOVETO:
                    if (numbers.Count > 0)
                    {
                        if (isFlex)
                        {
                            // not in the Type 1 spec, but exists in some fonts
                            flexPoints.Add(new SKPoint(numbers[0], 0f));
                        }
                        else
                        {
                            RmoveTo(numbers[0], 0);
                        }
                    }
                    break;
                case Type1KeyWord.RLINETO:
                    if (numbers.Count >= 2)
                    {
                        RLineTo(numbers[0], numbers[1]);
                    }
                    break;
                case Type1KeyWord.HLINETO:
                    if (numbers.Count > 0)
                    {
                        RLineTo(numbers[0], 0);
                    }
                    break;
                case Type1KeyWord.VLINETO:
                    if (numbers.Count > 0)
                    {
                        RLineTo(0, numbers[0]);
                    }
                    break;
                case Type1KeyWord.RRCURVETO:
                    if (numbers.Count >= 6)
                    {
                        RrCurveTo(numbers[0], numbers[1], numbers[2],
                                numbers[3], numbers[4], numbers[5]);
                    }
                    break;
                case Type1KeyWord.CLOSEPATH:
                    CloseCharString1Path();
                    break;
                case Type1KeyWord.SBW:
                    if (numbers.Count >= 3)
                    {
                        leftSideBearing = new SKPoint(numbers[0], numbers[1]);
                        width = (int)numbers[2];
                        current = leftSideBearing;
                    }
                    break;
                case Type1KeyWord.HSBW:
                    if (numbers.Count >= 2)
                    {
                        leftSideBearing = new SKPoint(numbers[0], 0);
                        width = (int)numbers[1];
                        current = leftSideBearing;
                    }
                    break;
                case Type1KeyWord.VHCURVETO:
                    if (numbers.Count >= 4)
                    {
                        RrCurveTo(0, numbers[0], numbers[1], numbers[2], numbers[3], 0);
                    }
                    break;
                case Type1KeyWord.HVCURVETO:
                    if (numbers.Count >= 4)
                    {
                        RrCurveTo(numbers[0], 0, numbers[1], numbers[2], 0, numbers[3]);
                    }
                    break;
                case Type1KeyWord.SEAC:
                    if (numbers.Count >= 5)
                    {
                        Seac(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4]);
                    }
                    break;
                case Type1KeyWord.SETCURRENTPOINT:
                    if (numbers.Count >= 2)
                    {
                        SetCurrentPoint(numbers[0], numbers[1]);
                    }
                    break;
                case Type1KeyWord.CALLOTHERSUBR:
                    if (numbers.Count > 0)
                    {
                        CallOtherSubr((int)numbers[0]);
                    }
                    break;
                case Type1KeyWord.DIV:
                    if (numbers.Count >= 2)
                    {
                        float b = numbers.RemoveAtValue(numbers.Count - 1);
                        float a = numbers.RemoveAtValue(numbers.Count - 1);

                        numbers.Add(a / b);
                        return;
                    }
                    break;
                case Type1KeyWord.HSTEM:
                case Type1KeyWord.VSTEM:
                case Type1KeyWord.HSTEM3:
                case Type1KeyWord.VSTEM3:
                case Type1KeyWord.DOTSECTION:
                    // ignore hints
                    break;
                case Type1KeyWord.ENDCHAR:
                    // end
                    break;
                case Type1KeyWord.RET:
                case Type1KeyWord.CALLSUBR:
                    // indicates an invalid charstring
                    Debug.WriteLine($"warn: Unexpected charstring command: {command} in glyph {glyphName} of font {fontName}");
                    break;
                default:
                    // indicates an invalid charstring
                    Debug.WriteLine($"warn: Unknown charstring command: {command} in glyph {glyphName} of font {fontName}");
                    break;
            }
            numbers.Clear();
        }

        /// <summary>
        /// Sets the current absolute point without performing a moveto.
        /// Used only with results from callothersubr
        /// </summary>
        private void SetCurrentPoint(float x, float y)
        {
            current = new SKPoint(x, y);
        }

        /// <summary>Flex (via OtherSubrs)</summary>
        /// <param name="num">OtherSubrs entry number</param>
        private void CallOtherSubr(int num)
        {
            if (num == 0)
            {
                // end flex
                isFlex = false;

                if (flexPoints.Count < 7)
                {
                    Debug.WriteLine($"warn: flex without moveTo in font {fontName}, glyph {glyphName}, command {commandCount}");
                    return;
                }

                // reference point is relative to start point
                SKPoint reference = flexPoints[0];
                reference = new SKPoint(current.X + reference.X,
                                      current.Y + reference.Y);
                flexPoints[0] = reference;

                // first point is relative to reference point
                SKPoint first = flexPoints[1];
                first = new SKPoint(reference.X + first.X, reference.Y + first.Y);
                // make the first point relative to the start point
                first = new SKPoint(first.X - current.X, first.Y - current.Y);
                flexPoints[1] = first;

                var p1 = flexPoints[1];
                var p2 = flexPoints[2];
                var p3 = flexPoints[3];
                RrCurveTo(p1.X, p1.Y,
                          p2.X, p2.Y,
                          p3.X, p3.Y);

                var p4 = flexPoints[4];
                var p5 = flexPoints[5];
                var p6 = flexPoints[6];
                RrCurveTo(p4.X, p4.Y,
                          p5.X, p5.Y,
                          p6.X, p6.Y);

                flexPoints.Clear();
            }
            else if (num == 1)
            {
                // begin flex
                isFlex = true;
            }
            else
            {
                Debug.WriteLine($"warn: Invalid callothersubr parameter: {num}");
            }
        }

        /// <summary>Relative moveto.</summary>
        private void RmoveTo(float dx, float dy)
        {
            float x = (float)current.X + dx;
            float y = (float)current.Y + dy;
            path.MoveTo(x, y);
            current = new SKPoint(x, y);
        }

        /// <summary>Relative lineto.</summary>
        private void RLineTo(float dx, float dy)
        {
            float x = (float)current.X + dx;
            float y = (float)current.Y + dy;
            if (path.PointCount == 0)
            {
                Debug.WriteLine($"warn: rlineTo without initial moveTo in font {fontName}, glyph {glyphName}");
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
            current = new SKPoint(x, y);
        }

        /// <summary>Relative curveto.</summary>
        private void RrCurveTo(float dx1, float dy1, float dx2, float dy2, float dx3, float dy3)
        {
            float x1 = (float)current.X + dx1;
            float y1 = (float)current.Y + dy1;
            float x2 = x1 + dx2;
            float y2 = y1 + dy2;
            float x3 = x2 + dx3;
            float y3 = y2 + dy3;
            if (path.PointCount == 0)
            {
                Debug.WriteLine($"warn: rrcurveTo without initial moveTo in font {fontName}, glyph {glyphName}");
                path.MoveTo(x3, y3);
            }

            {
                path.CubicTo(x1, y1, x2, y2, x3, y3);
            }
            current = new SKPoint(x3, y3);
        }

        /// <summary>Close path.</summary>
        private void CloseCharString1Path()
        {
            if (path.PointCount == 0)
            {
                Debug.WriteLine($"warn: closepath without initial moveTo in font {fontName}, glyph {glyphName}");
            }
            else
            {
                path.Close();
            }
            path.MoveTo(current.X, current.Y);
        }

        /// <summary>Standard Encoding Accented Character
        /// Makes an accented character from two other characters.</summary>        
        private void Seac(float asb, float adx, float ady, float bchar, float achar)
        {
            // base character
            string baseName = StandardEncoding.Instance.GetName((int)bchar);
            try
            {
                var baseString = font.GetType1CharString(baseName);
                path.AddPath(baseString.Path, SKPathAddMode.Append);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"warn: invalid seac character in glyph {glyphName} of font {fontName} {e}");
            }
            // accent character
            string accentName = StandardEncoding.Instance.GetName((int)achar);
            try
            {
                if (accentName == glyphName)
                {
                    // PDFBOX-5339: avoid ArrayIndexOutOfBoundsException 
                    // reproducable with poc file crash-4698e0dc7833a3f959d06707e01d03cda52a83f4
                    Debug.WriteLine("warn: Path for " + baseName + " and for accent " + accentName + " are same, ignored");
                    return;
                }
                var accent = font.GetType1CharString(accentName);
                var accentPath = accent.Path;

                var at = SKMatrix.CreateTranslation(leftSideBearing.X + adx - asb, leftSideBearing.Y + ady);
#if NET9_0_OR_GREATER
                path.AddPath(accentPath, in at, SKPathAddMode.Append);
#else
                path.AddPath(accentPath, ref at, SKPathAddMode.Append);
#endif
            }
            catch (Exception e)
            {
                Debug.WriteLine($"warn: invalid seac character in glyph {glyphName} of font {fontName} {e}");
            }
        }

        /// <summary>Add a command to the type1 sequence.</summary>
        /// <param name="numbers">the parameters of the command to be added</param>
        /// <param name="command">the command to be added</param>
        protected void AddCommand(List<float> numbers, CharStringCommand command)
        {
            type1Sequence.AddRange(numbers.Cast<object>());
            type1Sequence.Add(command);
        }

        protected bool IsSequenceEmpty
        {
            get => type1Sequence.Count == 0;
        }

        protected object LastSequenceEntry
        {
            get => !IsSequenceEmpty ? type1Sequence[type1Sequence.Count - 1] : null;
        }

        override public string ToString()
        {
            return string.Join("\n", type1Sequence.Select(p => p.ToString().Replace("|", "\n").Replace(",", " ")));
        }
    }
}
