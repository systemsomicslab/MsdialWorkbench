// Based on https://gist.github.com/riyadparvez/4365600/#file-streamtokenizer-cs , and fixed by Kazuya Ujihara to work 

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NCDK.Common.Util
{
    /// <summary>
    /// The <see cref="StreamTokenizer"/> class takes an input stream and
    /// parses it into "tokens", allowing the tokens to be
    /// Read one at a time. The parsing process is controlled by a table
    /// and a number of flags that can be set to various states. The
    /// stream tokenizer can recognize identifiers, numbers, quoted
    /// strings, and various comment styles.
    /// <para>
    /// Each byte Read from the input stream is regarded as a character
    /// in the range <c>'\u0000'</c> through <c>'\u00FF'</c>.
    /// The character value is used to look up five possible attributes of
    /// the character: <i>white space</i>, <i>alphabetic</i>,
    /// <i>numeric</i>, <i>string quote</i>, and <i>comment character</i>.
    /// Each character can have zero or more of these attributes.
    /// </para>
    /// <para>
    /// In addition, an instance has four flags. These flags indicate:
    /// <list type="bulett">
    /// <item>Whether line terminators are to be returned as tokens or treated as white space that merely separates tokens.</item>
    /// <item>Whether C-style comments are to be recognized and skipped.</item>
    /// <item>Whether C++-style comments are to be recognized and skipped.</item>
    /// <item>Whether the characters of identifiers are converted to lowercase.</item>
    /// </list>
    /// </para>
    /// <para>
    /// A typical application first constructs an instance of this class,
    /// sets up the syntax tables, and then repeatedly loops calling the
    /// <see cref="NextToken"/> method in each iteration of the loop until
    /// it returns the value <see cref="TTypeEOF"/>.
    /// </para>
    /// </summary>
    public class StreamTokenizer : IEnumerable<int>
    {
        /* Only one of these will be non-null */
        private TextReader reader = null;

        /// <summary>
        /// The next character to be considered by the nextToken method.  May also
        /// be NEED_CHAR to indicate that a new character should be Read, or SKIP_LF
        /// to indicate that a new character should be Read and, if it is a '\n'
        /// character, it should be discarded and a second new character should be
        /// Read.
        /// </summary>
        private int peekc = NEED_CHAR;

        private const int NEED_CHAR = Int32.MaxValue;
        private const int SKIP_LF = Int32.MaxValue - 1;

        private bool pushedBack;

        /// <summary> The line number of the last token Read </summary>

        private bool eolIsSignificantP = false;
        private byte[] characterType = new byte[256];
        private const byte CT_WHITESPACE = 1;
        private const byte CT_DIGIT = 2;
        private const byte CT_ALPHA = 4;
        private const byte CT_QUOTE = 8;
        private const byte CT_COMMENT = 16;

        public int LineNumber { get; private set; }

        /// <summary>
        /// After a call to the <see cref="NextToken"/> method, this field
        /// contains the type of the token just Read. For a single character
        /// token, its value is the single character, converted to an integer.
        /// For a quoted string token, its value is the quote character.
        /// Otherwise, its value is one of the following:
        /// <list type="bullet">
        ///     <item><see cref="TTypeWord"/> indicates that the token is a word.</item>
        ///     <item><see cref="TTypeNumber"/> indicates that the token is a number.</item>
        ///     <item><see cref="TTypeEOL"/> indicates that the end of line has been Read.
        ///     The field can only have this value if the <see cref="EolIsSignificant(bool)"/> 
        ///     method has been called with the argument <see langword="true"/>.</item>
        ///     <item><see cref="TTypeEOL"/> indicates that the end of the input stream has been reached.</item>
        /// </list>
        /// The initial value of this field is -4.
        /// </summary>
        public int TType { get; set; } = TTypeNothing;

        /// <summary>
        /// A constant indicating that the end of the stream has been Read.
        /// </summary>
        public const int TTypeEOF = -1;

        /// <summary>
        /// A constant indicating that the end of the line has been Read.
        /// </summary>
        public const int TTypeEOL = '\n';

        /// <summary>
        /// A constant indicating that a number token has been Read.
        /// </summary>
        public const int TTypeNumber = -2;

        /// <summary>
        /// A constant indicating that a word token has been Read.
        /// </summary>
        public const int TTypeWord = -3;

        /// <summary>
        /// A constant indicating that no token has been Read, used for
        /// initializing ttype.  FIXME This could be made public and
        /// made available as the part of the API in a future release.
        /// </summary>
        private const int TTypeNothing = -4;

        /// <summary>
        /// If the current token is a word token, this field contains a
        /// string giving the characters of the word token. When the current
        /// token is a quoted string token, this field contains the body of
        /// the string.
        /// <para>
        /// The current token is a word when the value of the
        /// <see cref="TType"/> field is <see cref="TTypeWord"/>. The current token is
        /// a quoted string token when the value of the <see cref="TType"/> field is
        /// a quote character.
        /// </para>
        /// <para>
        /// The initial value of this field is null.
        /// </para>
        /// </summary>
        /// <seealso cref="QuoteChar(int)"/>
        /// <seealso cref="TTypeWord"/>
        /// <seealso cref="TType"/>
        public string StringValue { get; private set; }

        /// <summary>
        /// If the current token is a number, this field contains the value
        /// of that number. The current token is a number when the value of
        /// the <see cref="TType"/> field is <see cref="TTypeNumber"/>.
        /// </summary>
        /// <value>The initial value of this field is 0.0.</value>
        public double NumberValue { get; private set; }

       /// <summary> Private constructor that initializes everything except the streams. </summary>
        private StreamTokenizer()
        {
            WordChars('a', 'z');
            WordChars('A', 'Z');
            WordChars(128 + 32, 255);
            WhiteSpaceChars(0, ' ');
            CommentChar('/');
            QuoteChar('"');
            QuoteChar('\'');
            ParseNumbers();
            LineNumber = 1;
        }

        /// <summary>
        /// Create a tokenizer that parses the given character stream.
        /// </summary>
        /// <param name="r">a Reader object providing the input stream.</param>
        public StreamTokenizer(TextReader r) : this()
        {
            reader = r ?? throw new ArgumentNullException(nameof(r));
        }

        /// <summary>
        /// Resets this tokenizer's syntax table so that all characters are
        /// "ordinary." See the <see cref="OrdinaryChar(int)"/> method
        /// for more information on a character being ordinary.
        /// </summary>
        public void ResetSyntax()
        {
            Array.Clear(characterType, 0, characterType.Length);
        }

        /// <summary>
        /// Specifies that all characters <i>c</i> in the range
        /// "low &lt;= <i>c</i> &lt;= high"
        /// are word constituents. A word token consists of a word constituent
        /// followed by zero or more word constituents or number constituents.
        /// </summary>
        /// <param name="low">the low end of the range.</param>
        /// <param name="hi">the high end of the range.</param>
        public void WordChars(int low, int hi)
        {
            if (low < 0)
            {
                low = 0;
            }
            if (hi >= characterType.Length)
            {
                hi = characterType.Length - 1;
            }
            while (low <= hi)
            {
                characterType[low++] |= CT_ALPHA;
            }
        }

        /// <summary>
        /// Specifies that all characters <i>c</i> in the range
        /// "low &lt;= <i>c</i> &lt;= high"
        /// are white space characters. White space characters serve only to
        /// separate tokens in the input stream.
        /// <para>
        /// Any other attribute settings for the characters in the specified
        /// range are cleared.
        /// </para>
        /// </summary>
        /// <param name="low">the low end of the range.</param>
        /// <param name="hi">the high end of the range.</param>
        public void WhiteSpaceChars(int low, int hi)
        {
            if (low < 0)
            {
                low = 0;
            }
            if (hi >= characterType.Length)
            {
                hi = characterType.Length - 1;
            }
            while (low <= hi)
            {
                characterType[low++] = CT_WHITESPACE;
            }
        }

        /// <summary>
        /// Specifies that all characters <i>c</i> in the range
        /// "low &lt;= <i>c</i> &lt;= high"
        /// are "ordinary" in this tokenizer. See the
        /// <see cref="OrdinaryChar(int)"/> method for more information on a
        /// character being ordinary.
        /// </summary>
        /// <param name="low">the low end of the range.</param>
        /// <param name="hi">the high end of the range.</param>
        /// <seealso cref="OrdinaryChar(int)"/>
        public void OrdinaryChars(int low, int hi)
        {
            if (low < 0)
            {
                low = 0;
            }
            if (hi >= characterType.Length)
            {
                hi = characterType.Length - 1;
            }
            while (low <= hi)
            {
                characterType[low++] = 0;
            }
        }
        /// <summary>
        /// Specifies that the character argument is "ordinary"
        /// in this tokenizer. It removes any special significance the
        /// character has as a comment character, word component, string
        /// delimiter, white space, or number character. When such a character
        /// is encountered by the parser, the parser treats it as a
        /// single-character token and sets <see cref="TType"/> field to the
        /// character value.
        /// <para>Making a line terminator character "ordinary" may interfere
        /// with the ability of a <see cref="StreamTokenizer"/> to count
        /// lines. The <see cref="LineNumber"/> method may no longer reflect
        /// the presence of such terminator characters in its line count.
        /// </para>
        /// </summary>
        /// <param name="ch">the character.</param>
        public void OrdinaryChar(int ch)
        {
            if (ch >= 0 && ch < characterType.Length)
                characterType[ch] = 0;
        }

        /// <summary>
        /// Specified that the character argument starts a single-line
        /// comment. All characters from the comment character to the end of
        /// the line are ignored by this stream tokenizer.
        /// <para>Any other attribute settings for the specified character are cleared.</para>
        /// </summary>
        /// <param name="ch">the character.</param>
        public void CommentChar(int ch)
        {
            if (ch >= 0 && ch < characterType.Length)
            {
                characterType[ch] = CT_COMMENT;
            }
        }

        /// <summary>
        /// Specifies that matching pairs of this character delimit string
        /// constants in this tokenizer.
        /// <para>
        /// When the <see cref="NextToken"/> method encounters a string
        /// constant, the <see cref="TType"/> field is set to the string
        /// delimiter and the <c>sval</c> field is set to the body of
        /// the string.
        /// </para>
        /// <para>
        /// If a string quote character is encountered, then a string is
        /// recognized, consisting of all characters after (but not including)
        /// the string quote character, up to (but not including) the next
        /// occurrence of that same string quote character, or a line
        /// terminator, or end of file. The usual escape sequences such as
        /// <c>"\n"</c> and <c>"\t"</c> are recognized and
        /// converted to single characters as the string is parsed.
        /// </para>
        /// <para>
        /// Any other attribute settings for the specified character are cleared.
        /// </para>
        /// </summary>
        /// <param name="ch">the character.</param>
        public void QuoteChar(int ch)
        {
            if (ch >= 0 && ch < characterType.Length)
                characterType[ch] = CT_QUOTE;
        }

        /// <summary>
        /// Specifies that numbers should be parsed by this tokenizer. The
        /// syntax table of this tokenizer is modified so that each of the twelve
        /// characters:
        /// <pre>
        ///      0 1 2 3 4 5 6 7 8 9 . -
        /// </pre>
        /// has the "numeric" attribute.
        /// <para>
        /// When the parser encounters a word token that has the format of a
        /// double precision floating-point number, it treats the token as a
        /// number rather than a word, by setting the <see cref="TType"/>
        /// field to the value <see cref="TTypeNumber"/> and putting the numeric
        /// value of the token into the <see cref="NumberValue"/> field.
        /// </para>
        /// </summary>
        public void ParseNumbers()
        {
            for (int i = '0'; i <= '9'; i++)
            {
                characterType[i] |= CT_DIGIT;
            }
            characterType['.'] |= CT_DIGIT;
            characterType['-'] |= CT_DIGIT;
        }

        /// <summary>
        /// Determines whether or not ends of line are treated as tokens.
        /// If the flag argument is true, this tokenizer treats end of lines
        /// as tokens; the <see cref="NextToken"/> method returns
        /// <see cref="TTypeEOL"/> and also sets the <see cref="TType"/> field to
        /// this value when an end of line is Read.
        /// <para>
        /// A line is a sequence of characters ending with either a
        /// carriage-return character (<c>'\r'</c>) or a newline
        /// character (<c>'\n'</c>). In addition, a carriage-return
        /// character followed immediately by a newline character is treated
        /// as a single end-of-line token.
        /// </para>
        /// <para>
        /// If the <paramref name="flag"/> is false, end-of-line characters are 
        /// treated as white space and serve only to separate tokens.
        /// </para>
        /// </summary>
        /// <param name="flag"><see langword="true"/> indicates that end-of-line characters
        /// are separate tokens; <see langword="false"/> indicates that end-of-line characters are white space.</param>
        public void EolIsSignificant(bool flag)
        {
            eolIsSignificantP = flag;
        }

        /// <summary>
        /// Determines whether or not the tokenizer recognizes C-style comments.
        /// If the flag argument is <see langword="true"/>, this stream tokenizer
        /// recognizes C-style comments. All text between successive
        /// occurrences of <c>/*</c> and <c>*/</c> are discarded.
        /// <para>
        /// If the flag argument is <see langword="false"/>, then C-style comments
        /// are not treated specially.</para>
        /// </summary>
        public bool SlashStarComments { get; set; } = false;

        /// <summary>
        /// Determines whether or not the tokenizer recognizes C++-style comments.
        /// If the flag argument is <see langword="true"/>, this stream tokenizer
        /// recognizes C++-style comments. Any occurrence of two consecutive
        /// slash characters (<c>'/'</c>) is treated as the beginning of
        /// a comment that extends to the end of the line.
        /// <para>
        /// If the flag argument is <see langword="false"/>, then C++-style
        /// comments are not treated specially.</para>
        /// </summary>
        public bool SlashSlashComments { get; set; } = false;

        /// <summary>
        /// Determines whether or not word token are automatically lowercased.
        /// If the flag argument is <see langword="true"/>, then the value in the
        /// <see cref="StringValue"/> field is lowercased whenever a word token is
        /// returned (the <see cref="TType"/> field has the
        /// value <see cref="TTypeWord"/> by the <see cref="NextToken"/> method
        /// of this tokenizer.
        /// <para>
        /// If the flag argument is <see langword="false"/>, then the
        /// <see cref="StringValue"/> field is not modified.</para>
        /// </summary>
        public bool LowerCaseMode { get; set; }

        /// <summary>Read the next character</summary>
        private int Read()
        {
            if (reader != null)
            {
                return reader.Read();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Parses the next token from the input stream of this tokenizer.
        /// The type of the next token is returned in the <see cref="TType"/>
        /// field. Additional information about the token may be in the
        /// <see cref="NumberValue"/> field or the <see cref="StringValue"/> field of this
        /// tokenizer.
        /// <para>
        /// Typical clients of this
        /// class first set up the syntax tables and then sit in a loop
        /// calling <see cref="NextToken"/> to parse successive tokens until <see cref="TTypeEOF"/>
        /// is returned.</para>
        /// </summary>
        /// <returns>the value of the <see cref="TType"/> field.</returns>
        public int NextToken()
        {
            if (pushedBack)
            {
                pushedBack = false;
                return TType;
            }
            byte[] ct = characterType;
            StringValue = null;

            int c = peekc;
            if (c < 0)
                c = NEED_CHAR;
            if (c == SKIP_LF)
            {
                c = Read();
                if (c < 0)
                    return TType = TTypeEOF;
                if (c == '\n')
                    c = NEED_CHAR;
            }
            if (c == NEED_CHAR)
            {
                c = Read();
                if (c < 0)
                    return TType = TTypeEOF;
            }
            TType = c;      /* Just to be safe */

            // Set peekc so that the next invocation of nextToken will Read
            // another character unless peekc is reset in this invocation
            peekc = NEED_CHAR;

            int ctype = c < 256 ? ct[c] : CT_ALPHA;
            while ((ctype & CT_WHITESPACE) != 0)
            {
                if (c == '\r')
                {
                    LineNumber++;
                    if (eolIsSignificantP)
                    {
                        peekc = SKIP_LF;
                        return TType = TTypeEOL;
                    }
                    c = Read();
                    if (c == '\n')
                        c = Read();
                }
                else
                {
                    if (c == '\n')
                    {
                        LineNumber++;
                        if (eolIsSignificantP)
                        {
                            return TType = TTypeEOL;
                        }
                    }
                    c = Read();
                }
                if (c < 0)
                    return TType = TTypeEOF;
                ctype = c < 256 ? ct[c] : CT_ALPHA;
            }

            if ((ctype & CT_DIGIT) != 0)
            {
                bool neg = false;
                if (c == '-')
                {
                    c = Read();
                    if (c != '.' && (c < '0' || c > '9'))
                    {
                        peekc = c;
                        return TType = '-';
                    }
                    neg = true;
                }
                double v = 0;
                int decexp = 0;
                int seendot = 0;
                while (true)
                {
                    if (c == '.' && seendot == 0)
                        seendot = 1;
                    else if ('0' <= c && c <= '9')
                    {
                        v = v * 10 + (c - '0');
                        decexp += seendot;
                    }
                    else
                        break;
                    c = Read();
                }
                peekc = c;
                if (decexp != 0)
                {
                    double denom = 10;
                    decexp--;
                    while (decexp > 0)
                    {
                        denom *= 10;
                        decexp--;
                    }
                    /* Do one division of a likely-to-be-more-accurate number */
                    v = v / denom;
                }
                NumberValue = neg ? -v : v;
                return TType = TTypeNumber;
            }

            if ((ctype & CT_ALPHA) != 0)
            {
                var sb = new StringBuilder();
                do
                {
                    sb.Append((char)c);
                    c = Read();
                    ctype = c < 0 ? CT_WHITESPACE : c < 256 ? ct[c] : CT_ALPHA;
                } while ((ctype & (CT_ALPHA | CT_DIGIT)) != 0);
                peekc = c;
                StringValue = sb.ToString();
                if (LowerCaseMode)
                    StringValue = StringValue.ToLowerInvariant();
                return TType = TTypeWord;
            }

            if ((ctype & CT_QUOTE) != 0)
            {
                TType = c;
                var sb = new StringBuilder();
                // Invariants (because \Octal needs a lookahead):
                //   (i)  c contains char value
                //   (ii) d contains the lookahead
                int d = Read();
                while (d >= 0 && d != TType && d != '\n' && d != '\r')
                {
                    if (d == '\\')
                    {
                        c = Read();
                        int first = c;   /* To allow \377, but not \477 */
                        if (c >= '0' && c <= '7')
                        {
                            c = c - '0';
                            int c2 = Read();
                            if ('0' <= c2 && c2 <= '7')
                            {
                                c = (c << 3) + (c2 - '0');
                                c2 = Read();
                                if ('0' <= c2 && c2 <= '7' && first <= '3')
                                {
                                    c = (c << 3) + (c2 - '0');
                                    d = Read();
                                }
                                else
                                    d = c2;
                            }
                            else
                                d = c2;
                        }
                        else
                        {
                            switch (c)
                            {
                                case 'a':
                                    c = 0x7;
                                    break;
                                case 'b':
                                    c = '\b';
                                    break;
                                case 'f':
                                    c = 0xC;
                                    break;
                                case 'n':
                                    c = '\n';
                                    break;
                                case 'r':
                                    c = '\r';
                                    break;
                                case 't':
                                    c = '\t';
                                    break;
                                case 'v':
                                    c = 0xB;
                                    break;
                            }
                            d = Read();
                        }
                    }
                    else
                    {
                        c = d;
                        d = Read();
                    }
                    sb.Append((char)c);
                }

                // If we broke out of the loop because we found a matching quote
                // character then arrange to Read a new character next time
                // around; otherwise, save the character.
                peekc = (d == TType) ? NEED_CHAR : d;

                StringValue = sb.ToString();
                return TType;
            }

            if (c == '/' && (SlashSlashComments || SlashStarComments))
            {
                c = Read();
                if (c == '*' && SlashStarComments)
                {
                    int prevc = 0;
                    while ((c = Read()) != '/' || prevc != '*')
                    {
                        if (c == '\r')
                        {
                            LineNumber++;
                            c = Read();
                            if (c == '\n')
                            {
                                c = Read();
                            }
                        }
                        else
                        {
                            if (c == '\n')
                            {
                                LineNumber++;
                                c = Read();
                            }
                        }
                        if (c < 0)
                            return TType = TTypeEOF;
                        prevc = c;
                    }
                    return NextToken();
                }
                else if (c == '/' && SlashSlashComments)
                {
                    while ((c = Read()) != '\n' && c != '\r' && c >= 0) ;
                    peekc = c;
                    return NextToken();
                }
                else
                {
                    /* Now see if it is still a single line comment */
                    if ((ct['/'] & CT_COMMENT) != 0)
                    {
                        while ((c = Read()) != '\n' && c != '\r' && c >= 0) ;
                        peekc = c;
                        return NextToken();
                    }
                    else
                    {
                        peekc = c;
                        return TType = '/';
                    }
                }
            }

            if ((ctype & CT_COMMENT) != 0)
            {
                while ((c = Read()) != '\n' && c != '\r' && c >= 0) ;
                peekc = c;
                return NextToken();
            }

            return TType = c;
        }

        /// <summary>
        /// Causes the next call to the <see cref="NextToken"/>  method of this
        /// tokenizer to return the current value in the <see cref="TType"/>
        /// field, and not to modify the value in the<see cref="NumberValue"/> or
        /// <see cref="StringValue"/> field.
        /// </summary>
        public void PushBack()
        {
            if (TType != TTypeNothing)   /* No-op if NextToken() not called */
            {
                pushedBack = true;
            }
        }

        /// <summary>
        /// Returns the string representation of the current stream token and
        /// the line number it occurs on.
        /// <para>The precise string returned is unspecified, although the following
        /// example can be considered typical:
        /// <pre>
        ///     Token['a'], line 10
        /// </pre>
        /// </para>
        /// </summary>
        /// <returns>a string representation of the token</returns>
        public override string ToString()
        {
            string ret;
            switch (TType)
            {
                case TTypeEOF:
                    ret = "EOF";
                    break;
                case TTypeEOL:
                    ret = "EOL";
                    break;
                case TTypeWord:
                    ret = StringValue;
                    break;
                case TTypeNumber:
                    ret = "n=" + NumberValue;
                    break;
                case TTypeNothing:
                    ret = "NOTHING";
                    break;
                default:
                    {
                        // ttype is the first character of either a quoted string or
                        // is an ordinary character. ttype can definitely not be less
                        // than 0, since those are reserved values used in the previous
                        // case statements
                        if (TType < 256 &&
                            ((characterType[TType] & CT_QUOTE) != 0))
                        {
                            ret = StringValue;
                            break;
                        }

                        char[] s = new char[3];
                        s[0] = s[2] = '\'';
                        s[1] = (char)TType;
                        ret = new string(s);
                        break;
                    }
            }
            return "Token[" + ret + "], line " + LineNumber;
        }

        public IEnumerator<int> GetEnumerator()
        {
            ResetSyntax();
            while (true)
            {
                int token = NextToken();
                if (token == TTypeEOF)
                {
                    yield break;
                }
                yield return token;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
