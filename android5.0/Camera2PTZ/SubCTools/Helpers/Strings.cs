namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class Strings
    {
        /// <summary>
        /// Add spaces to a sentence on a capital letter.
        /// </summary>
        /// <param name="text">Sentence to add spaces.</param>
        /// <returns>Space added sentence.</returns>
        public static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (var i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                {
                    newText.Append(' ');
                }

                newText.Append(text[i]);
            }

            return newText.ToString();
        }

        /// <summary> Converts an array of bytes into a formatted string of hex digits (ex: E4 CA B2).</summary>
        /// <param name="data"> The array of bytes to be translated into a string of hex digits. </param>
        /// <returns> Returns a well formatted string of hex digits with spacing. </returns>
        public static string ByteArrayToHexString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);

            foreach (var b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }

            return sb.ToString().ToUpper();
        }

        /// <summary>
        /// Convert a byte in to a hex string.
        /// </summary>
        /// <param name="data">Byte data to convert to hex string.</param>
        /// <returns>Hex string equivilent of byte.</returns>
        public static string ByteToHexString(byte data)
        {
            return ByteArrayToHexString(new[] { data });
        }

        /// <summary>
        /// Clean the string from any characters from [\u0000-\u001F].
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string CleanString(string input)
        {
            return Regex.Replace(input, @"[\u0000-\u001F]", string.Empty);
        }

        /// <summary>
        /// Check to see if a string contains any characters from [\u0000-\u001F].
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool ContainsBadChars(string input)
        {
            if (input == null)
            {
                return true;
            }

            return Regex.Match(input, @"[\u0000-\u001F]+").Success;
        }

        public static string DecodeNewlines(string text)
        {
            return text.Replace("`%", "\n").Replace("`^", ",");
        }

        public static string DisplayToMonitor(string deviceName)
        {
            deviceName = deviceName.ToLower();

            if (deviceName.Contains(@"\\.\display"))
            {
                return deviceName.Replace(@"\\.\display", "Monitor");
            }
            else
            {
                return deviceName;
            }
        }

        public static string EncodeNewlines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\n", "`%").Replace(",", "`^");
        }

        public static bool EndsWith(this StringBuilder sb, string test)
        {
            return EndsWith(sb, test, StringComparison.CurrentCulture);
        }

        public static bool EndsWith(this StringBuilder sb, string test, StringComparison comparison)
        {
            if (sb.Length < test.Length)
            {
                return false;
            }

            var end = sb.ToString(sb.Length - test.Length, test.Length);
            return end.Equals(test, comparison);
        }

        public static string FromLiteral(this string input)
        {
            return input.Replace("\n", @"\n").Replace("\v", @"\v").Replace("\r", @"\r").Replace("\t", @"\t");
        }

        /// <summary>
        /// Get a unique ID.
        /// </summary>
        /// <returns></returns>
        public static string GenerateID()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Returns all characters in a string between two strings.
        /// </summary>
        /// <param name="strSource"> The full string to search.</param>
        /// <param name="strStart"> a char or word to start at.</param>
        /// <param name="strEnd"> a char or word to end at.</param>
        /// <returns>the characters between strStart and strEnd. </returns>
        public static string GetBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }

        public static string HexToAscii(this string hexString)
        {
            try
            {
                var ascii = string.Empty;

                for (var i = 0; i < hexString.Length; i += 2)
                {
                    var hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    var decval = System.Convert.ToUInt32(hs, 16);
                    var character = System.Convert.ToChar(decval);
                    ascii += character;
                }

                return ascii;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return string.Empty;
        }

        /// <summary>
        /// Convert a hex string to a byte array.
        /// </summary>
        /// <param name="hexString">The hex formatted string to convert to a byte array.</param>
        /// <returns>Byte array of hex.</returns>
        public static byte[] HexToByteArray(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            var hexData = new byte[hexString.Length / 2];

            for (var i = 0; i < hexString.Length; i += 2)
            {
                hexData[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return hexData;
        }

        public static string MonitorToDisplay(string deviceName)
        {
            deviceName = deviceName.ToLower();

            if (deviceName.Contains("monitor"))
            {
                return deviceName.Replace("monitor", @"\\.\DISPLAY");
            }
            else
            {
                return deviceName;
            }
        }

        /// <summary>
        /// Get the class name from the view class. Ie. Intevals from SubCTools.Controls.WPF.Views.Intervals.
        /// </summary>
        /// <param name="viewName">Entire view class name.</param>
        /// <returns>Last word in the class.</returns>
        public static string ParseViewName(string viewName)
        {
            var name = viewName.ToString().Split('.');
            return name[name.Length - 1];
        }

        /// <summary>
        /// Convert a string of ASCII to hex equivilent.
        /// </summary>
        /// <param name="str">ASCII string.</param>
        /// <returns>Hex values of ASCII characters.</returns>
        public static string StringToHex(string str)
        {
            var hexString = "";

            foreach (var c in str)
            {
                hexString += string.Format("{0:X2}", Convert.ToInt32(c)) + " ";
            }

            return hexString;
        }

        /// <summary>
        /// Convert a string of ASCII to hex equivilent.
        /// </summary>
        /// <param name="str">ASCII string.</param>
        /// <returns>Hex values of ASCII characters.</returns>
        public static string ToHex(this string input)
        {
            return StringToHex(input);
        }

        /// <summary>
        /// Converts a single byte to its hex representation IE 0b_0101_1011 -> 5B.
        /// </summary>
        /// <param name="data">The <see cref="byte"/> to convert.</param>
        /// <returns>A hex representation of the byte.</returns>
        public static string ToHexString(this byte data)
        {
            var hex = new StringBuilder(2);
            hex.AppendFormat("{0:X2}", data);
            return hex.ToString();
        }

        public static string ToLiteral(this string input)
        {
            return input.Replace(@"\n", "\n")
.Replace(@"\v", "\v")
.Replace(@"\r", "\r")
.Replace(@"\t", "\t");
        }

        /// <summary>
        /// Inserts a linebreak into the text after a number of characters.
        /// </summary>
        /// <param name="toWrap"> the string to be line wrapped.</param>
        /// <param name="charCount"> the number of characters per line.</param>
        /// <returns>
        /// wrapped string.
        /// </returns>
        public static string WrapText(string toWrap, int charCount)
        {
            if (toWrap.Length <= charCount)
            {
                return toWrap;
            }

            var lines = toWrap.Length / charCount;
            var wrapped = string.Empty;

            for (var i = 0; i <= lines; i++)
            {
                wrapped +=
                    i < lines ? toWrap.Substring(charCount * i, charCount) + System.Environment.NewLine : toWrap.Substring(charCount * i);
            }

            return wrapped;
        }

        public static class Acronym
        {
            private static readonly Dictionary<int, string> characterMap = new Dictionary<int, string>()
            {
                { 199, "C" },
                { 231, "c" },
                { 252, "u" },
                { 251, "u" },
                { 250, "u" },
                { 249, "u" },
                { 233, "e" },
                { 234, "e" },
                { 235, "e" },
                { 232, "e" },
                { 226, "a" },
                { 228, "a" },
                { 224, "a" },
                { 229, "a" },
                { 225, "a" },
                { 239, "i" },
                { 238, "i" },
                { 236, "i" },
                { 237, "i" },
                { 196, "A" },
                { 197, "A" },
                { 201, "E" },
                { 230, "ae" },
                { 198, "Ae" },
                { 244, "o" },
                { 246, "o" },
                { 242, "o" },
                { 243, "o" },
                { 220, "U" },
                { 255, "Y" },
                { 214, "O" },
                { 241, "n" },
                { 209, "N" }
            };

            private static readonly int desiredKeyLength = 2;

            private static readonly List<string> ignoredWords = new List<string> { "THE", "A", "AN", "AS", "AND", "OF", "OR" };
            private static readonly int maxKeyLength = 5;

            public static string Generate(string b)
            {
                if (string.IsNullOrEmpty(b))
                {
                    return "";
                }

                b = b.Trim();

                var a = new List<string>();
                var temp = string.Empty;
                for (int d = 0, f = b.Length; d < f; d++)
                {
                    a.Add(characterMap.TryGetValue(b.ToCharArray()[d], out temp) ? temp : b.ToCharArray()[d].ToString());
                }

                b = string.Join("", a);
                var h = new List<string>();
                foreach (var k in System.Text.RegularExpressions.Regex.Split(b, "\\s+"))
                {
                    if (!string.IsNullOrEmpty(k))
                    {
                        var hold = System.Text.RegularExpressions.Regex.Replace(k, "[^A-Za-z]", "").ToUpper();
                        if (hold != "") { h.Add(hold); }
                    }
                }

                if (GetTotalLength(h) > desiredKeyLength)
                {
                    h = RemoveIgnoredWords(h);
                }

                string c;
                if (h.Count == 0)
                {
                    c = "";
                }
                else
                {
                    if (h.Count == 1)
                    {
                        var g = h[0];
                        if (g.Length > desiredKeyLength)
                        {
                            c = GetFirstSyllable(g);
                        }
                        else
                        {
                            c = g;
                        }
                    }
                    else
                    {
                        c = CreateAcronym(h);
                    }
                }

                if (c.Length > maxKeyLength)
                {
                    c = c.Substring(0, maxKeyLength);
                }

                return c;
            }

            private static string CreateAcronym(List<string> b)
            {
                var a = string.Empty;
                foreach (var word in b)
                {
                    if (word != "")
                    {
                        a += word.ToCharArray()[0];
                    }
                }

                return a;
            }

            private static string GetFirstSyllable(string c)
            {
                var b = false;
                for (var a = 0; a < c.Length; a++)
                {
                    if (IsVowelOrY(c.ToCharArray()[a]))
                    {
                        b = true;
                    }
                    else
                    {
                        if (b)
                        {
                            return c.Substring(0, a + 1);
                        }
                    }
                }

                return c;
            }

            private static int GetTotalLength(List<string> words)
            {
                return string.Join("", words).Length;
            }

            private static bool IsVowelOrY(char a)
            {
                return "aeiouyAEIOUY".IndexOf(a) >= 0;
            }

            private static List<string> RemoveIgnoredWords(List<string> words)
            {
                for (var i = 0; i < words.Count; i++)
                {
                    if (ignoredWords.Contains(words[i]))
                    {
                        words.Remove(words[i]);
                        i--;
                    }
                }

                return words;
            }
        }
    }
}