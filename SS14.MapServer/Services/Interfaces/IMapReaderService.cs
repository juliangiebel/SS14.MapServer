namespace SS14.MapServer.Services.Interfaces;

public interface IMapReaderService
{
    Task UpdateMapsFromFS(string path);

}