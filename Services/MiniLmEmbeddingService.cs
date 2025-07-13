using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ESGPlatform.Services
{
    public class MiniLmEmbeddingService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly MiniLmTokenizer _tokenizer;
        private readonly int _maxLength;

        public MiniLmEmbeddingService(string onnxPath, string vocabPath, int maxLength = 128)
        {
            _session = new InferenceSession(onnxPath);
            _tokenizer = new MiniLmTokenizer(vocabPath) { MaxLength = maxLength };
            _maxLength = maxLength;
        }

        public float[] GetEmbedding(string text)
        {
            var inputIds = _tokenizer.TokenizeToIds(text);
            var attentionMask = inputIds.Select(id => id == 0 ? 0 : 1).ToArray();

            var inputIdsTensor = new DenseTensor<long>(inputIds.Select(i => (long)i).ToArray(), new[] { 1, _maxLength });
            var attentionMaskTensor = new DenseTensor<long>(attentionMask.Select(i => (long)i).ToArray(), new[] { 1, _maxLength });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
            };

            using var results = _session.Run(inputs);
            // Output is usually named "last_hidden_state" or similar
            var output = results.First().AsEnumerable<float>().ToArray();
            // The output shape is [1, maxLength, hiddenSize], we want the [CLS] token (first vector)
            // hiddenSize for MiniLM is 384
            int hiddenSize = output.Length / _maxLength;
            var clsEmbedding = new float[hiddenSize];
            Array.Copy(output, 0, clsEmbedding, 0, hiddenSize);
            return clsEmbedding;
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
} 