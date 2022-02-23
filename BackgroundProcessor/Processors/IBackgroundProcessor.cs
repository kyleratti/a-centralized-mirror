namespace BackgroundProcessor.Processors;

public interface IBackgroundProcessor
{
	public Task Process(CancellationToken cancellationToken);
}