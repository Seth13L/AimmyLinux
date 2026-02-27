using Aimmy.Core.Config;
using Aimmy.Core.Models;
using Aimmy.Linux.App.Services.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class RuntimeDataCollectorTests
{
    [Fact]
    public void CollectFrame_ReturnsWithoutSaving_WhenCollectionDisabled()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var settings = BuildSettings(tempRoot, collect: false, autoLabel: false);
            var collector = new RuntimeDataCollector(settings, baseDirectory: tempRoot);
            using var frame = new Image<Rgba32>(64, 64);

            var result = collector.CollectFrame(frame, null, constantTrackingEnabled: false);

            Assert.False(result.Saved);
            Assert.Null(result.ImagePath);
            Assert.Null(result.LabelPath);
            Assert.False(Directory.Exists(Path.Combine(tempRoot, "images")));
            Assert.False(Directory.Exists(Path.Combine(tempRoot, "labels")));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void CollectFrame_SavesImage_WhenCollectionEnabledAndAutoLabelDisabled()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var settings = BuildSettings(tempRoot, collect: true, autoLabel: false);
            var collector = new RuntimeDataCollector(settings, baseDirectory: tempRoot);
            using var frame = new Image<Rgba32>(64, 64);

            var result = collector.CollectFrame(frame, null, constantTrackingEnabled: false);

            Assert.True(result.Saved);
            Assert.NotNull(result.ImagePath);
            Assert.True(File.Exists(result.ImagePath));
            Assert.Null(result.LabelPath);
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void CollectFrame_SavesImageAndYoloLabel_WhenAutoLabelEnabledAndDetectionAvailable()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var settings = BuildSettings(tempRoot, collect: true, autoLabel: true);
            var collector = new RuntimeDataCollector(settings, baseDirectory: tempRoot);
            using var frame = new Image<Rgba32>(64, 32);
            var detection = new Detection(32, 16, 20, 10, 0.9f, 3, "enemy");

            var result = collector.CollectFrame(frame, detection, constantTrackingEnabled: false);

            Assert.True(result.Saved);
            Assert.NotNull(result.ImagePath);
            Assert.NotNull(result.LabelPath);
            Assert.True(File.Exists(result.ImagePath));
            Assert.True(File.Exists(result.LabelPath));
            var label = File.ReadAllText(result.LabelPath);
            Assert.Equal("3 0.500000 0.500000 0.312500 0.312500", label);
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void CollectFrame_SkipsWhenConstantTrackingAndAutoLabelDisabled()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var settings = BuildSettings(tempRoot, collect: true, autoLabel: false);
            var collector = new RuntimeDataCollector(settings, baseDirectory: tempRoot);
            using var frame = new Image<Rgba32>(64, 64);
            var detection = new Detection(20, 20, 10, 10, 0.7f, 0, "enemy");

            var result = collector.CollectFrame(frame, detection, constantTrackingEnabled: true);

            Assert.False(result.Saved);
            Assert.False(Directory.Exists(Path.Combine(tempRoot, "images")));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void CollectFrame_EnforcesCooldownBetweenSaves()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var settings = BuildSettings(tempRoot, collect: true, autoLabel: false);
            var collector = new RuntimeDataCollector(settings, utcNowProvider: () => now, baseDirectory: tempRoot);
            using var frame = new Image<Rgba32>(64, 64);

            var first = collector.CollectFrame(frame, null, constantTrackingEnabled: false);
            now = now.AddMilliseconds(100);
            var second = collector.CollectFrame(frame, null, constantTrackingEnabled: false);
            now = now.AddMilliseconds(600);
            var third = collector.CollectFrame(frame, null, constantTrackingEnabled: false);

            Assert.True(first.Saved);
            Assert.False(second.Saved);
            Assert.True(third.Saved);

            var images = Directory.GetFiles(Path.Combine(tempRoot, "images"), "*.jpg");
            Assert.Equal(2, images.Length);
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    private static DataCollectionSettings BuildSettings(string root, bool collect, bool autoLabel)
    {
        return new DataCollectionSettings
        {
            CollectDataWhilePlaying = collect,
            AutoLabelData = autoLabel,
            ImagesDirectory = Path.Combine(root, "images"),
            LabelsDirectory = Path.Combine(root, "labels")
        };
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aimmy-runtime-data-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CleanupTempRoot(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch
        {
            // Ignore test cleanup failures.
        }
    }
}
