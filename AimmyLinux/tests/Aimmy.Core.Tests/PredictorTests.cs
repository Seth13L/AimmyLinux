using Aimmy.Core.Config;
using Aimmy.Core.Models;
using Aimmy.Core.Prediction;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class PredictorTests
{
    [Fact]
    public void ShalloePredictor_ProjectsForwardUsingVelocityHistory()
    {
        var config = AimmyConfig.CreateDefault();
        config.Prediction.ShalloeLeadMultiplier = 2.0;

        var predictor = new ShalloePredictor(config);

        predictor.Predict(new Detection(100, 100, 20, 20, 0.9f, 0), DateTime.UtcNow);
        var projected = predictor.Predict(new Detection(110, 100, 20, 20, 0.9f, 0), DateTime.UtcNow.AddMilliseconds(16));

        Assert.True(projected.CenterX > 110);
    }
}
