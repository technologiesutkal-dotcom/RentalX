using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RentalX.Services.Licensing;

public sealed class MachineIdService : IMachineIdService
{
    private readonly string _storePath;

    public MachineIdService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RentalX");
        Directory.CreateDirectory(folder);
        _storePath = Path.Combine(folder, "machine.id");
    }

    public string GetMachineId()
    {
        if (File.Exists(_storePath))
        {
            var existing = File.ReadAllText(_storePath).Trim();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }
        }

        var raw = $"{Environment.MachineName}|{Environment.UserName}|{Environment.ProcessorCount}|{Guid.NewGuid():N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var id = Convert.ToHexString(hash)[..32];
        File.WriteAllText(_storePath, id);
        return id;
    }

    public string GetMachineName() => Environment.MachineName;
}
