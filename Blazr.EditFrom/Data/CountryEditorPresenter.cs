namespace Blazr.EditForm.Data;

public class CountryEditorPresenter
{
    private ICountryDataBroker _broker;

    public readonly CountryEditContext Record = new CountryEditContext(new());

    public CountryEditorPresenter(ICountryDataBroker broker)
        => _broker = broker;

    public async ValueTask<bool> GetItemAsync(Guid uid)
    {
        var record = await _broker.GetItemAsync(uid);
        //Logic to check we got a record
        this.Record.Load(record);
        return true;
    }

    public async ValueTask<bool> SaveItemAsync()
    {
        await _broker.SaveItemAsync(this.Record.AsRecord);
        return true;
    }
}
