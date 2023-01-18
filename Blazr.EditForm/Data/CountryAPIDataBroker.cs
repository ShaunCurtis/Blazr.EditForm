namespace Blazr.EditForm.Data;

public interface ICountryDataBroker
{
    public ValueTask<DboCountry> GetItemAsync(Guid uid);
    public ValueTask<bool> SaveItemAsync(DboCountry item);
}

public class CountryAPIDataBroker : ICountryDataBroker
{
    // Normally inject the HttpClient 
    public CountryAPIDataBroker() { }

    public async ValueTask<DboCountry> GetItemAsync(Guid uid)
    {
        // Emulate getting record from the API
        await Task.Delay(500);
        return new() { Uid = uid, Name = "United Kingdom", Code = "UK" };
    }

    public async ValueTask<bool> SaveItemAsync(DboCountry item)
    {
        // Emulate saving the record to the API
        await Task.Delay(500);
        return true;
    }
}
