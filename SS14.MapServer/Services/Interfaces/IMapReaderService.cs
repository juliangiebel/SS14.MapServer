namespace SS14.MapServer.Services.Interfaces;

public interface IMapReaderService
{
    Task<IList<Guid>> UpdateMapsFromFs(string path, string gitRef = "master", bool forceTiled = false, CancellationToken cancellationToken = default);

}
