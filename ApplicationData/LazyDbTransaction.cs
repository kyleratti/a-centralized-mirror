using System.Data;

namespace ApplicationData;

public sealed class LazyDbTransaction : IDisposable
{
	private readonly Lazy<IDbTransaction> _lazyTransaction;

	private LazyDbTransaction(Lazy<IDbTransaction> lazyTransaction)
	{
		_lazyTransaction = lazyTransaction;
	}

	public static LazyDbTransaction Create(IDbConnection connection, IsolationLevel isolationLevel)
	{
		var lazyTx = new Lazy<IDbTransaction>(() => connection.CreateTransaction(isolationLevel));

		return new LazyDbTransaction(lazyTx);
	}

	public IDbTransaction Tx => _lazyTransaction.Value;

	/// <inheritdoc />
	public void Dispose()
	{
		if (!_lazyTransaction.IsValueCreated)
			return;

		_lazyTransaction.Value.Dispose();
	}
}
