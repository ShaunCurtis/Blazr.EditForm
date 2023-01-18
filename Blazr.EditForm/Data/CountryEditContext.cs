namespace Blazr.EditForm.Data;

public class CountryEditContext
{
    public Guid Uid { get; private set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public DboCountry BaseRecord { get; private set; } = new DboCountry();
    public bool IsDirty => BaseRecord != this.AsRecord;

    public CountryEditContext(DboCountry record) 
        => this.Load(record);

    public void Reset()
        => this.Load(this.BaseRecord);

    public void Load(DboCountry record) 
    {
        this.BaseRecord = record with { };
        this.Uid= record.Uid;
        this.Name= record.Name;
        this.Code= record.Code;
    }

    public DboCountry AsRecord
        => new DboCountry
        {
            Uid= this.Uid,
            Name= this.Name,
            Code= this.Code,
        };
}
