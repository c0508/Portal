using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ESGPlatform.Services
{
    public class MiniLmTokenizer
    {
        private readonly Dictionary<string, int> _vocab;
        private readonly string[] _vocabArray;
        private readonly Regex _basicTokenizer = new Regex(@"\w+|[^\w\s]", RegexOptions.Compiled);
        private const string UnknownToken = "[UNK]";
        private const string CLS = "[CLS]";
        private const string SEP = "[SEP]";
        public int MaxLength { get; set; } = 128;

        public MiniLmTokenizer(string vocabPath)
        {
            _vocabArray = File.ReadAllLines(vocabPath);
            _vocab = _vocabArray.Select((tok, idx) => new { tok, idx })
                .ToDictionary(x => x.tok, x => x.idx);
        }

        public int[] TokenizeToIds(string text)
        {
            var tokens = new List<string> { CLS };
            foreach (Match match in _basicTokenizer.Matches(text.ToLower()))
            {
                var word = match.Value;
                if (_vocab.ContainsKey(word))
                {
                    tokens.Add(word);
                }
                else
                {
                    // WordPiece: try to split into subwords
                    var subwords = WordPieceTokenize(word);
                    tokens.AddRange(subwords);
                }
            }
            tokens.Add(SEP);
            // Pad or truncate
            var ids = tokens.Select(t => _vocab.ContainsKey(t) ? _vocab[t] : _vocab[UnknownToken]).ToList();
            if (ids.Count > MaxLength)
                ids = ids.Take(MaxLength).ToList();
            else if (ids.Count < MaxLength)
                ids.AddRange(Enumerable.Repeat(_vocab["[PAD]"], MaxLength - ids.Count));
            return ids.ToArray();
        }

        private List<string> WordPieceTokenize(string word)
        {
            var subwords = new List<string>();
            int start = 0;
            while (start < word.Length)
            {
                int end = word.Length;
                string curSub = null;
                while (start < end)
                {
                    var substr = (start > 0 ? "##" : "") + word.Substring(start, end - start);
                    if (_vocab.ContainsKey(substr))
                    {
                        curSub = substr;
                        break;
                    }
                    end--;
                }
                if (curSub == null)
                {
                    subwords.Add(UnknownToken);
                    break;
                }
                subwords.Add(curSub);
                start += curSub.StartsWith("##") ? curSub.Length - 2 : curSub.Length;
            }
            return subwords;
        }
    }
} 