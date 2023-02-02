/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */

namespace NCDK.Beam
{
    /// <summary>
    /// A character buffer with utilities for sequential processing of characters.
    /// </summary>
    // @author John May
    internal sealed class CharBuffer
    {
        /// <summary>Characters stored in a fixed size array.</summary>
        public char[] cs;

        /// <summary>Current position.</summary>
        public int Position { get; set; }

        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="cs">array of characters</param>
        private CharBuffer(char[] cs)
        {
            this.cs = cs;
        }

        /// <summary>
        /// Determine if there are any characters remaining in the buffer. There are
        /// no characters remaining when the position has reached the end of the
        /// array.
        /// </summary>
        /// <returns>the position has reached the end of the array</returns>
        public bool HasRemaining()
        {
            return Position < cs.Length;
        }

        /// <summary>
        /// Access the next character in the buffer and progress the position.
        /// </summary>
        /// <returns>the next character</returns>
        /// <seealso cref="NextChar"/>
        public char Get()
        {
            return cs[Position++];
        }

        /// <summary>
        /// Access the the next character in the buffer without progressing the
        /// position.
        /// </summary>
        /// <returns>the next character</returns>
        /// <seealso cref="Get()"/>
        public char NextChar => cs[Position];

        /// <summary>
        /// Determine if the next character is a digit. The buffer is first checked
        /// to ensure there are characters remaining.
        /// </summary>
        /// <returns>whether there is a 'next' character and it is a digit</returns>.
        public bool NextIsDigit()
        {
            return HasRemaining() && IsDigit(NextChar);
        }

        /// <summary>
        /// Access the next character as a digit, the buffer position will progress.
        /// No check is made that there are characters remaining and the next
        /// character is a digit.
        /// </summary>
        /// <returns>the next character in the buffer as a digit</returns>.
        /// <see cref="NextIsDigit()"/> 
        public int GetAsDigit()
        {
            return ToDigit(Get());
        }

        /// <summary>
        /// Access the next character as a digit, the buffer position does not
        /// progress. No check is made that there are characters remaining and the
        /// next character is a digit.
        /// </summary>
        /// <returns>the next character in the buffer as a digit</returns>.
        /// <see cref="NextIsDigit()"/> 
        public int GetNextAsDigit()
        {
            return ToDigit(NextChar);
        }

        /// <summary>
        /// Determine if the next character is <paramref name="c"/>.
        /// </summary>
        /// <param name="c">test if the next character is</param>
        /// <returns>whether there are characters remaining and the</returns>
        public bool NextIs(char c)
        {
            return HasRemaining() && cs[Position] == c;
        }

        /// <summary>
        /// Progress the buffer if the next character is the same as the provided
        /// character.
        /// </summary>
        /// <param name="c">a character</param>
        /// <returns>whether the buffer progressed and the character matched</returns>
        public bool GetIf(char c)
        {
            if (HasRemaining() && NextIs(c))
            {
                Position++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a sequence of digits from the buffer as a positive integer.  The
        /// buffer is progressed until the end of the number. If the characters do
        /// not represent a number then -1 is returned and the buffer is not
        /// progressed.
        /// </summary>
        /// <returns>the number read</returns>, &lt; 0 if no number read
        public int GetNumber()
        {
            if (!NextIsDigit())
                return -1;
            int num = GetAsDigit();
            while (NextIsDigit())
                num = (num * 10) + GetAsDigit();
            return num;
        }

        /// <summary>
        /// Get a sequence of specified digits from the buffer as a positive integer.
        /// The buffer is progressed until the end of the number. If the characters do
        /// not represent a number then -1 is returned and the buffer is not
        /// progressed.
        /// <param name="nDigits">the number of digits to read</param>
        /// <returns>the number read</returns>, &lt; 0 if no number read
        /// </summary>
        public int GetNumber(int nDigits)
        {
            if (!NextIsDigit())
                return -1;
            int num = GetAsDigit();
            while (--nDigits > 0 && NextIsDigit())
                num = (num * 10) + GetAsDigit();
            return num;
        }

        /// <summary>
        /// Obtain the string of characters 'from' - 'to' the specified indices.
        /// <param name="from">start index</param>
        /// <param name="to">  end index</param>
        /// <returns>the string between the indices</returns>
        /// </summary>
        public string Substr(int from, int to)
        {
            return new string(cs, from, to - from);
        }

        /// <summary>
        /// The number of characters in the buffer.
        /// </summary>
        /// <returns>length of the buffer</returns>
        public int Length => cs.Length;

        /// <summary>
        /// Determine if the specified character 'c' is a digit (0-9).
        /// </summary>
        /// <param name="c">a character</param>
        /// <returns>the character is a digit</returns>
        public static bool IsDigit(char c)
        {
            // Character.IsDigit allows 'any' unicode digit, we don't need that
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Convert the specified character to the corresponding integral digit.
        /// Note, no check is made as to whether the character is actually a digit
        /// which should be performed with <see cref="IsDigit(char)"/>.
        /// </summary>
        /// <param name="c">a character</param>
        /// <returns>the digit for character</returns>
        /// <seealso cref="IsDigit(char)"/>
        public static int ToDigit(char c)
        {
            return c - '0';
        }

        /// <summary>
        /// Create a buffer from a string.
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>new char buffer</returns>
        public static CharBuffer FromString(string str)
        {
            return new CharBuffer(str.ToCharArray());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return new string(cs);
        }
    }
}
