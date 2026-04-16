// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.Services;

using System.IO;
using System.Text.Json;

public interface IAppSettings
{
    string GrpcEndpoint { get; set; }
    void Save();
}

public sealed class AppSettings : IAppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OPCGateway",
        "admin-settings.json");

    private SettingsData _data;

    public AppSettings()
    {
        _data = Load();
    }

    public string GrpcEndpoint
    {
        get => _data.GrpcEndpoint;
        set
        {
            _data = _data with { GrpcEndpoint = value };
            Save();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath,
            JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static SettingsData Load()
    {
        if (!File.Exists(SettingsPath))
            return new SettingsData();

        try
        {
            return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsPath))
                ?? new SettingsData();
        }
        catch
        {
            return new SettingsData();
        }
    }

    private record SettingsData(
        string GrpcEndpoint = "http://localhost:5002");
}
