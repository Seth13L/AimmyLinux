using Aimmy.Core.Config;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IConfigMigrator
{
    bool CanMigrate(string path, string rawContent);
    bool TryMigrate(string path, string rawContent, out AimmyConfig migratedConfig, out string message);
}
