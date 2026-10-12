namespace RentalX.Services.Licensing;

public interface IClientAccessMonitor
{
    /// <summary>Starts periodic checks. Safe to call multiple times.</summary>
    void Start();

    /// <summary>Stops periodic checks.</summary>
    void Stop();

    /// <summary>One-shot check. Returns false if access was revoked (caller should stop work).</summary>
    Task<bool> EnsureAccessAsync(CancellationToken ct = default);
}
