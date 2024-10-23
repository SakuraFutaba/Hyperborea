using ECommons.Configuration;

namespace Hyperborea;
public class Config : IEzConfig
{
    public bool DisableInnCheck = false;
    public uint CurrentMount = 0;
    public bool FastTeleport = false;
    public float NoclipSpeed = 0.05f;
    public bool ForcedFlight = false;
    public string GameVersion = "";
    public uint[] OpcodesZoneDown = [425, 867];
    public uint[] OpcodesZoneUp = [606];
}
