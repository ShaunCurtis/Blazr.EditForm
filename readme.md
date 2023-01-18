Question

>> How do you prevent navigation from an edit form in Blazor when the form data is dirty?

When you read data from a data source such as an API, the data you receive is read only.  Treat it as immutable, using `record` objects rather than `class` objects to represent the data.

This is my demo record.  It's a simple record of the name and registration code for a country.  All the properties are declared as immutable.

```csharp
public record DboCountry
{
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}
```

We can generate a simple dummy data pipeline for this record with get and save async methods that would normally make API calls.

```csharp
public interface ICountryDataBroker
{
    public ValueTask<DboCountry> GetItemAsync(Guid uid);
    public ValueTask<bool> SaveItemAsync(DboCountry item);
}

public class CountryAPIDataBroker
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
```

We need an editable version of `DboCountry`.  This is where using  `record` objects comes into it's own.  Cloning and equality checking is easy.  We save a copy of the original record used to create the record and use this to test state by comparing it against a record we generate dynamically from the current values.  You can add validation to this class or build the necessary fluid validation classes from it.  I've added Fluent Validation to demonstrate,

```csharp
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
```

Next our Presentation layer service.

This holds and manages the data used by the edit form.  The `CountryEditContext` is readonly so can't be replaced during the lifetime of the presenter.  The presenter is a `Transient` service, so it's important not to do anything in it that requires implementing `IDisposable`.

```csharp
public class CountryEditorPresenter
{
    private ICountryDataBroker _broker;

    public readonly CountryEditContext Record = new CountryEditContext(new());

    public CountryEditorPresenter(CountryDataBroker broker)
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
```

The services are registered as follows:

```csharp
builder.Services.AddScoped<ICountryDataBroker, CountryAPIDataBroker>();
builder.Services.AddTransient<CountryEditorPresenter>();
builder.Services.AddTransient<IValidator<CountryEditContext>, CountryValidator>();
```

Finally the edit form.  It's a normal form with the button state controlled by the record edit state.  

The form locking is accomplished by:

1. Wiring the NavigationManager's `RegisterLocationChangingHandler` to a handler that prevents/allows navigation based on the form state.  This prevents intra SPA navigation.
2. Adding a `NavigationLock` component to the form and wiring it up to the form state. This prevents external navigation including using the back button.

```html
@page "/"
@inject CountryEditorPresenter Presenter
@inject NavigationManager NavManager
@implements IDisposable

<PageTitle>Index</PageTitle>
<EditForm EditContext=_editContext>
    <FluentValidationValidator DisableAssemblyScanning="@true" />
    <div class="mb-2">
        <label class="form-label">Country</label>
        <BlazrInputText class="form-control" @bind-Value="this.Presenter.Record.Name"/>
        <ValidationMessage For="() => this.Presenter.Record.Name"/>
    </div>
    <div class="mb-2">
        <label class="form-label">Code</label>
        <BlazrInputText class="form-control" @bind-Value=this.Presenter.Record.Code />
        <ValidationMessage For="() => this.Presenter.Record.Code" />
    </div>
    <div class="mb-2 text-end">
        <button class="btn btn-success" disabled="@(!this.Presenter.Record.IsDirty)" @onclick="this.Save">Save</button>
        <button class="btn btn-danger" disabled="@(!this.Presenter.Record.IsDirty)" @onclick="this.ExitWithoutSave">Exit Without Saving</button>
        <button class="btn btn-dark" disabled="@(this.Presenter.Record.IsDirty)" @onclick="this.Exit">Exit</button>
    </div>
    <div class="mb-2">
        <ValidationSummary />
    </div>
</EditForm>

<NavigationLock ConfirmExternalNavigation="this.Presenter.Record.IsDirty"  />
```
```csharp
@code {
    private EditContext _editContext = default!;
    private IDisposable? _navLockerDispose;

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Presenter.Record);
        await Presenter.GetItemAsync(Guid.NewGuid());
        _navLockerDispose = NavManager.RegisterLocationChangingHandler(this.CheckFromState);
    }

    private ValueTask CheckFromState(LocationChangingContext context)
    {
        if (this.Presenter.Record.IsDirty)
            context.PreventNavigation();

        return ValueTask.CompletedTask;
    }

    private async Task Save()
        => await this.Presenter.SaveItemAsync();

    private Task Exit()
    {
        // Exit to where?
        return Task.CompletedTask;
    }

    private Task ExitWithoutSave()
    {
        this.Presenter.Record.Reset();
        return Task.CompletedTask;
    }

    public void Dispose()
        => _navLockerDispose?.Dispose();
}
```

### For reference 

This is `RazrInputText`:

```html
@namespace Blazr.EditForm
@inherits InputText

<input @attributes="AdditionalAttributes"
       class="@CssClass"
       @bind="CurrentValueAsString"
       @bind:event="oninput" />
```

This is `CountryValidator`:

```csharp
public class CountryValidator : AbstractValidator<CountryEditContext>
{
    public CountryValidator()
    {
        RuleFor(p => p.Name)
        .NotEmpty().WithMessage("You must enter a Name")
        .MaximumLength(50).WithMessage("Name cannot be longer than 50 characters");

        RuleFor(p => p.Code)
        .NotEmpty().WithMessage("You must enter a Code for the Country")
        .MaximumLength(4).WithMessage("A country code is 1, 2, 3 or 4 letters");
    }
}
```

Note that the navigation features used to prevent navigation are new to Net7.0.