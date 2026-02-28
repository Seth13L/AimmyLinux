using Aimmy.Core.Config;
using Aimmy.Core.Enums;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class PredictionSettingsViewModel
{
    public IReadOnlyList<string> StrategyOptions { get; } = Enum.GetNames<PredictionStrategy>();

    public bool Enabled { get; set; }
    public string Strategy { get; set; } = PredictionStrategy.Kalman.ToString();
    public bool EmaSmoothingEnabled { get; set; }
    public double EmaSmoothingAmount { get; set; }
    public double KalmanLeadTime { get; set; }
    public double WiseTheFoxLeadTime { get; set; }
    public double ShalloeLeadMultiplier { get; set; }

    public void Load(AimmyConfig config)
    {
        Enabled = config.Prediction.Enabled;
        Strategy = config.Prediction.Strategy.ToString();
        EmaSmoothingEnabled = config.Prediction.EmaSmoothingEnabled;
        EmaSmoothingAmount = config.Prediction.EmaSmoothingAmount;
        KalmanLeadTime = config.Prediction.KalmanLeadTime;
        WiseTheFoxLeadTime = config.Prediction.WiseTheFoxLeadTime;
        ShalloeLeadMultiplier = config.Prediction.ShalloeLeadMultiplier;
    }

    public void Apply(AimmyConfig config)
    {
        config.Prediction.Enabled = Enabled;
        if (Enum.TryParse<PredictionStrategy>(Strategy, true, out var strategy))
        {
            config.Prediction.Strategy = strategy;
        }

        config.Prediction.EmaSmoothingEnabled = EmaSmoothingEnabled;
        config.Prediction.EmaSmoothingAmount = EmaSmoothingAmount;
        config.Prediction.KalmanLeadTime = KalmanLeadTime;
        config.Prediction.WiseTheFoxLeadTime = WiseTheFoxLeadTime;
        config.Prediction.ShalloeLeadMultiplier = ShalloeLeadMultiplier;
    }
}
