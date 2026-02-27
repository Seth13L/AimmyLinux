using Aimmy.Core.Config;
using Aimmy.Core.Enums;

namespace Aimmy.Core.Prediction;

public static class PredictorFactory
{
    public static ITargetPredictor Create(AimmyConfig config)
    {
        return config.Prediction.Strategy switch
        {
            PredictionStrategy.Shalloe => new ShalloePredictor(config),
            PredictionStrategy.WiseTheFox => new WiseTheFoxPredictor(config),
            _ => new KalmanPredictor(config)
        };
    }
}
