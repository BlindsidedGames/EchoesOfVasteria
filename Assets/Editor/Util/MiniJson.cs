using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// Minimal JSON parser/emitter for Editor utilities.
// Produces Dictionary<string, object>, List<object>, double, string, bool, or null.
namespace TimelessEchoes.Editor.Util
{
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return new Parser(json).ParseValue();
        }

        private sealed class Parser
        {
            private readonly string json;
            private int index;

            public Parser(string json)
            {
                this.json = json;
                index = 0;
                SkipWhitespace();
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length) return null;
                char c = json[index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': return ParseTrue();
                    case 'f': return ParseFalse();
                    case 'n': return ParseNull();
                    default: return ParseNumber();
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                // '{'
                index++;
                SkipWhitespace();
                if (index < json.Length && json[index] == '}') { index++; return dict; }

                while (index < json.Length)
                {
                    SkipWhitespace();
                    string key = ParseString();
                    SkipWhitespace();
                    if (index >= json.Length || json[index] != ':') return dict;
                    index++; // ':'
                    SkipWhitespace();
                    var value = ParseValue();
                    dict[key] = value;
                    SkipWhitespace();
                    if (index >= json.Length) break;
                    char c = json[index++];
                    if (c == '}') break;
                    if (c != ',') break;
                }
                return dict;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                // '['
                index++;
                SkipWhitespace();
                if (index < json.Length && json[index] == ']') { index++; return list; }
                while (index < json.Length)
                {
                    var value = ParseValue();
                    list.Add(value);
                    SkipWhitespace();
                    if (index >= json.Length) break;
                    char c = json[index++];
                    if (c == ']') break;
                    if (c != ',') break;
                }
                return list;
            }

            private string ParseString()
            {
                if (index >= json.Length || json[index] != '"') return string.Empty;
                index++; // '"'
                var sb = new StringBuilder();
                while (index < json.Length)
                {
                    char c = json[index++];
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        if (index >= json.Length) break;
                        char esc = json[index++];
                        switch (esc)
                        {
                            case '\\': sb.Append('\\'); break;
                            case '"': sb.Append('"'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (index + 3 < json.Length)
                                {
                                    string hex = json.Substring(index, 4);
                                    if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code))
                                        sb.Append((char)code);
                                    index += 4;
                                }
                                break;
                        }
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            private object ParseNumber()
            {
                int start = index;
                while (index < json.Length && "-+0123456789.eE".IndexOf(json[index]) >= 0) index++;
                var s = json.Substring(start, index - start);
                if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    return d;
                return 0d;
            }

            private object ParseTrue() { if (Match("true")) return true; return false; }
            private object ParseFalse() { if (Match("false")) return false; return true; }
            private object ParseNull() { if (Match("null")) return null; return null; }

            private bool Match(string s)
            {
                if (index + s.Length > json.Length) return false;
                for (int i = 0; i < s.Length; i++) if (json[index + i] != s[i]) return false;
                index += s.Length;
                return true;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length)
                {
                    char c = json[index];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') index++;
                    else break;
                }
            }
        }
    }
}

