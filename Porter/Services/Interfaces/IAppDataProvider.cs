namespace Porter.Services.Interfaces;

public interface IAppDataProvider<T> where T : class, new ()
{
	T Value { get; set; }

	T Load();

	T Load(string path);

	void Save();

	void Save(string path);
}
