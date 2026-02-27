using Aimmy.Core.Models;

namespace Aimmy.Core.Prediction;

public interface ITargetPredictor
{
    string Name { get; }
    Detection Predict(Detection current, DateTime timestamp);
    void Reset();
}
