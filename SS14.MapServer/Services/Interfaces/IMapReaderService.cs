namespace SS14.MapServer.Services.Interfaces;

public interface IMapReaderService
{
    Task<IList<Guid>> UpdateMapsFromFs(string path, CancellationToken cancellationToken = default);

}
