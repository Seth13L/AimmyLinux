using AimmyLinux.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AimmyLinux.Services.Inference;

public sealed class OnnxDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _inputWidth;
    private readonly int _inputHeight;

    public OnnxDetector(string modelPath, int fallbackImageSize)
    {
        var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        _session = new InferenceSession(modelPath, options);
        _inputName = _session.InputMetadata.Keys.First();

        var dimensions = _session.InputMetadata[_inputName].Dimensions;
        _inputHeight = dimensions.Length > 2 && dimensions[2] > 0 ? dimensions[2] : fallbackImageSize;
        _inputWidth = dimensions.Length > 3 && dimensions[3] > 0 ? dimensions[3] : fallbackImageSize;
    }

    public IReadOnlyList<Detection> Detect(Image<Rgba32> frame, float minimumConfidence)
    {
        using var resized = frame.Width == _inputWidth && frame.Height == _inputHeight
            ? frame.Clone()
            : frame.Clone(ctx => ctx.Resize(_inputWidth, _inputHeight));

        var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

        for (int y = 0; y < _inputHeight; y++)
        {
            var pixelRow = resized.GetPixelRowSpan(y);
            for (int x = 0; x < _inputWidth; x++)
            {
                var pixel = pixelRow[x];
                tensor[0, 0, y, x] = pixel.R / 255f;
                tensor[0, 1, y, x] = pixel.G / 255f;
                tensor[0, 2, y, x] = pixel.B / 255f;
            }
        }

        using var input = NamedOnnxValue.CreateFromTensor(_inputName, tensor);
        using var outputs = _session.Run(new[] { input });

        var outputTensor = outputs.First().AsTensor<float>();
        return ParseDetections(
            outputTensor,
            _inputWidth,
            _inputHeight,
            frame.Width,
            frame.Height,
            minimumConfidence);
    }

    private static IReadOnlyList<Detection> ParseDetections(
        Tensor<float> output,
        int modelWidth,
        int modelHeight,
        int frameWidth,
        int frameHeight,
        float minimumConfidence)
    {
        var dims = output.Dimensions.ToArray();
        if (dims.Length != 3)
        {
            return Array.Empty<Detection>();
        }

        var channelsFirst = dims[1] <= 256 && dims[2] > dims[1];
        var boxes = channelsFirst ? dims[2] : dims[1];
        var channels = channelsFirst ? dims[1] : dims[2];

        if (channels < 5 || boxes <= 0)
        {
            return Array.Empty<Detection>();
        }

        var detections = new List<Detection>(Math.Min(boxes, 256));
        var scaleX = frameWidth / (float)modelWidth;
        var scaleY = frameHeight / (float)modelHeight;

        for (int i = 0; i < boxes; i++)
        {
            float cx = channelsFirst ? output[0, 0, i] : output[0, i, 0];
            float cy = channelsFirst ? output[0, 1, i] : output[0, i, 1];
            float w = channelsFirst ? output[0, 2, i] : output[0, i, 2];
            float h = channelsFirst ? output[0, 3, i] : output[0, i, 3];

            // Some models output normalized coordinates in range [0,1].
            if (cx <= 1.5f && cy <= 1.5f && w <= 1.5f && h <= 1.5f)
            {
                cx *= modelWidth;
                cy *= modelHeight;
                w *= modelWidth;
                h *= modelHeight;
            }

            var (confidence, classId) = GetConfidenceAndClass(output, channelsFirst, i, channels);
            if (confidence < minimumConfidence || w <= 0 || h <= 0)
            {
                continue;
            }

            detections.Add(new Detection(
                CenterX: cx * scaleX,
                CenterY: cy * scaleY,
                Width: w * scaleX,
                Height: h * scaleY,
                Confidence: confidence,
                ClassId: classId));
        }

        return detections;
    }

    private static (float Confidence, int ClassId) GetConfidenceAndClass(
        Tensor<float> output,
        bool channelsFirst,
        int boxIndex,
        int channels)
    {
        float Read(int channel)
            => channelsFirst ? output[0, channel, boxIndex] : output[0, boxIndex, channel];

        if (channels == 5)
        {
            return (Math.Clamp(Read(4), 0f, 1f), 0);
        }

        var bestV8Score = -1f;
        var bestV8Class = 0;
        for (int c = 4; c < channels; c++)
        {
            var score = Read(c);
            if (score > bestV8Score)
            {
                bestV8Score = score;
                bestV8Class = c - 4;
            }
        }

        // v5-style confidence fallback: objectness * best class score.
        var objectness = Read(4);
        var bestV5Score = -1f;
        var bestV5Class = 0;
        for (int c = 5; c < channels; c++)
        {
            var score = objectness * Read(c);
            if (score > bestV5Score)
            {
                bestV5Score = score;
                bestV5Class = c - 5;
            }
        }

        if (bestV5Score > bestV8Score)
        {
            return (bestV5Score, bestV5Class);
        }

        return (bestV8Score, bestV8Class);
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
