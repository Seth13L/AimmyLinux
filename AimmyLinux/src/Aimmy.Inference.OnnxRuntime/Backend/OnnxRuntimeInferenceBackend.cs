using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Core.Models;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;

namespace Aimmy.Inference.OnnxRuntime.Backend;

public sealed class OnnxRuntimeInferenceBackend : IInferenceBackend
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _inputWidth;
    private readonly int _inputHeight;
    private readonly Dictionary<int, string> _classes;

    public string Name => "onnxruntime";
    public InferenceRuntimeInfo RuntimeInfo { get; }

    public OnnxRuntimeInferenceBackend(AimmyConfig config)
    {
        if (!File.Exists(config.Model.ModelPath))
        {
            throw new FileNotFoundException("Model file does not exist.", config.Model.ModelPath);
        }

        var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            InterOpNumThreads = 1,
            IntraOpNumThreads = 4
        };

        RuntimeInfo = ConfigureExecutionProvider(options, config.Runtime.GpuMode);

        _session = new InferenceSession(config.Model.ModelPath, options);
        _inputName = _session.InputMetadata.Keys.First();

        var dimensions = _session.InputMetadata[_inputName].Dimensions;
        _inputHeight = dimensions.Length > 2 && dimensions[2] > 0 ? dimensions[2] : config.Model.ImageSize;
        _inputWidth = dimensions.Length > 3 && dimensions[3] > 0 ? dimensions[3] : config.Model.ImageSize;

        _classes = LoadClasses(_session);
    }

    public IReadOnlyList<Detection> Detect(Image<Rgba32> frame, float minimumConfidence)
    {
        using var resized = frame.Width == _inputWidth && frame.Height == _inputHeight
            ? frame.Clone()
            : frame.Clone(ctx => ctx.Resize(_inputWidth, _inputHeight));

        var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

        resized.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < _inputHeight; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < _inputWidth; x++)
                {
                    var pixel = row[x];
                    tensor[0, 0, y, x] = pixel.R / 255f;
                    tensor[0, 1, y, x] = pixel.G / 255f;
                    tensor[0, 2, y, x] = pixel.B / 255f;
                }
            }
        });

        var input = NamedOnnxValue.CreateFromTensor(_inputName, tensor);
        using var outputs = _session.Run(new[] { input });

        var outputTensor = outputs.First().AsTensor<float>();
        return ParseDetections(outputTensor, frame.Width, frame.Height, minimumConfidence);
    }

    public ValueTask DisposeAsync()
    {
        _session.Dispose();
        return ValueTask.CompletedTask;
    }

    private IReadOnlyList<Detection> ParseDetections(Tensor<float> output, int frameWidth, int frameHeight, float minimumConfidence)
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

        var detections = new List<Detection>(Math.Min(256, boxes));
        var scaleX = frameWidth / (float)_inputWidth;
        var scaleY = frameHeight / (float)_inputHeight;

        for (var i = 0; i < boxes; i++)
        {
            float Read(int channel)
                => channelsFirst ? output[0, channel, i] : output[0, i, channel];

            var cx = Read(0);
            var cy = Read(1);
            var width = Read(2);
            var height = Read(3);

            if (cx <= 1.5f && cy <= 1.5f && width <= 1.5f && height <= 1.5f)
            {
                cx *= _inputWidth;
                cy *= _inputHeight;
                width *= _inputWidth;
                height *= _inputHeight;
            }

            var (confidence, classId) = GetConfidenceAndClass(Read, channels);
            if (confidence < minimumConfidence || width <= 0 || height <= 0)
            {
                continue;
            }

            detections.Add(new Detection(
                CenterX: cx * scaleX,
                CenterY: cy * scaleY,
                Width: width * scaleX,
                Height: height * scaleY,
                Confidence: confidence,
                ClassId: classId,
                ClassName: _classes.TryGetValue(classId, out var className) ? className : $"Class_{classId}"));
        }

        return detections;
    }

    private static (float Confidence, int ClassId) GetConfidenceAndClass(Func<int, float> read, int channels)
    {
        if (channels == 5)
        {
            return (Math.Clamp(read(4), 0f, 1f), 0);
        }

        var bestV8Score = -1f;
        var bestV8Class = 0;
        for (var c = 4; c < channels; c++)
        {
            var score = read(c);
            if (score <= bestV8Score)
            {
                continue;
            }

            bestV8Score = score;
            bestV8Class = c - 4;
        }

        var objectness = read(4);
        var bestV5Score = -1f;
        var bestV5Class = 0;
        for (var c = 5; c < channels; c++)
        {
            var score = objectness * read(c);
            if (score <= bestV5Score)
            {
                continue;
            }

            bestV5Score = score;
            bestV5Class = c - 5;
        }

        return bestV5Score > bestV8Score
            ? (bestV5Score, bestV5Class)
            : (bestV8Score, bestV8Class);
    }

    private static InferenceRuntimeInfo ConfigureExecutionProvider(SessionOptions options, GpuExecutionMode preferredMode)
    {
        if (preferredMode == GpuExecutionMode.Cpu)
        {
            return new InferenceRuntimeInfo(GpuExecutionMode.Cpu, "CPU", "CPU mode requested.", false);
        }

        if (preferredMode is GpuExecutionMode.Auto or GpuExecutionMode.Cuda)
        {
            if (TryAppendProvider(options, "AppendExecutionProvider_CUDA"))
            {
                return new InferenceRuntimeInfo(GpuExecutionMode.Cuda, "CUDA", "CUDA provider selected.", false);
            }
        }

        if (preferredMode is GpuExecutionMode.Auto or GpuExecutionMode.Rocm)
        {
            if (TryAppendProvider(options, "AppendExecutionProvider_ROCM"))
            {
                return new InferenceRuntimeInfo(GpuExecutionMode.Rocm, "ROCM", "ROCM provider selected.", false);
            }
        }

        return new InferenceRuntimeInfo(GpuExecutionMode.Cpu, "CPU", "Fell back to CPU because requested GPU provider was unavailable.", true);
    }

    private static bool TryAppendProvider(SessionOptions options, string methodName)
    {
        var method = typeof(SessionOptions).GetMethods()
            .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal) && m.GetParameters().Length == 0);

        if (method is null)
        {
            return false;
        }

        try
        {
            method.Invoke(options, Array.Empty<object>());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<int, string> LoadClasses(InferenceSession session)
    {
        var classes = new Dictionary<int, string>();

        try
        {
            var metadata = session.ModelMetadata;
            if (metadata?.CustomMetadataMap is null || !metadata.CustomMetadataMap.TryGetValue("names", out var namesJson) || string.IsNullOrWhiteSpace(namesJson))
            {
                return classes;
            }

            using var document = JsonDocument.Parse(namesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return classes;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!int.TryParse(property.Name, out var classId))
                {
                    continue;
                }

                var className = property.Value.GetString();
                if (string.IsNullOrWhiteSpace(className))
                {
                    continue;
                }

                classes[classId] = className;
            }
        }
        catch
        {
            // Ignore metadata parsing failures.
        }

        return classes;
    }
}
