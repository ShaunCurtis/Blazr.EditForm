namespace Blazr.EditForm.Data;

public class CountryEditorPresenter
{
    private ICountryDataBroker _broker;

    public CountryEditContext Record { get; private set; } = new(new());

    public CountryEditorPresenter(ICountryDataBroker broker)
        => _broker = broker;

    public async ValueTask<bool> GetItemAsync(Guid uid)
    {
        var record = await _broker.GetItemAsync(uid);

        //Logic to check we got a record

        // Create a new record edit context
        this.Record = new(record);
        
        return true;
    }

    public async ValueTask<bool> SaveItemAsync()
    {
        // Get the record to save
        var saveRecord = this.Record.AsRecord;
        await _broker.SaveItemAsync(saveRecord);

        // Create a new record edit context
        this.Record = new(saveRecord);

        return true;
    }

    public void Reset()
        => this.Record.Reset();
}
