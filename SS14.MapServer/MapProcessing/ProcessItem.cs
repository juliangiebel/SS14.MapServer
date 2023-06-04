namespace SS14.MapServer.MapProcessing;

public record ProcessItem(string GitRef, IList<string> Maps, Action<MapProcessResult> OnCompletion, string? RepositoryUrl = null);
