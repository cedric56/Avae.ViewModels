# Avae.ViewModels

Lightweight **ViewModel-first navigation** for the [Avae](https://github.com/cedric56/Avae.Abstractions) stack.

Built on `Microsoft.Extensions.DependencyInjection` only (no UI framework dependency in the core package). Works with Avalonia, MAUI, Blazor, or any host that can resolve views from a map.

> **Status:** preview (`1.0.0-preview.1`) · **TFM:** `net11.0` · AOT-friendly

---

## Install

```xml
<PackageReference Include="Avae.ViewModels" Version="1.0.0-preview.1" />
```

Optional CommunityToolkit-based base types:

```xml
<PropertyGroup>
  <AvaeFeatures>;CommunityToolkit;</AvaeFeatures>
</PropertyGroup>
```

(`build/Avae.ViewModels.targets` compiles `Community/**` and can pull `CommunityToolkit.Mvvm`.)

---

## Concepts

| Type | Role |
|------|------|
| **`Router`** | History stack, `GoTo` / `BackAsync` / `ForwardAsync`, lifecycle hooks |
| **`IViewFor`** | View abstraction; holds `Context` (the view model) |
| **`INavigable`** | Optional `CanNavigateAsync`, `OnNavigatedTo` / `OnNavigatedFrom` |
| **`NavigableContext`** | Parameters for view / view-model construction |
| **`ViewModelViewMap`** | Maps a key (default: view-model type name) → view type |
| **`NavigableView`** | Menu/sidebar entry (display name, icon, target VM type) |
| **`NavigableViewModelBase`** | Shell VM: list of navigables + current view |
| **`ICloseableViewModel<T>`** | Modal / dialog result flow |

Navigation is **view-model first**: you navigate to a VM type (or instance); the framework resolves the matching view from DI.

---

## Quick start

### 1. Register pages

```csharp
using Avae.ViewModels;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton<Router>();

services.Register<HomeView, HomeViewModel>();
services.Register<SettingsView, SettingsViewModel>();

// Optional lifetimes + custom key
services.RegisterWithLifetime<DetailView, DetailViewModel>(
    viewModelLifetime: ServiceLifetime.Transient,
    viewLifetime: ServiceLifetime.Transient,
    key: "detail");
```

Default key = `typeof(TViewModel).Name` (must match what `Router.GoTo` uses).

### 2. Navigate

```csharp
var router = sp.GetRequiredService<Router>();

// By type (creates VM via DI)
var (view, vm) = await router.GoTo<HomeViewModel>();

// Existing instance
await router.GoTo(existingVm);

// By runtime type
await router.GoToType(typeof(SettingsViewModel));

// With parameters
await router.GoTo<DetailViewModel>(context: NavigableContext.Create()
    .WithViewModelParameters(("id", 42))
    .WithViewParameters(("title", "Detail")));
```

### 3. History

```csharp
if (router.CanGoBack)
    await router.BackAsync();

if (router.CanGoForward)
    await router.ForwardAsync();

router.EraseHistory();
```

`INavigable.CanNavigateAsync()` can cancel leave/enter. History size is capped (default 20 entries).

---

## Multiple regions (Prism-like zones)

Each region is a **keyed** `Router` singleton (independent history):

```csharp
services.AddNavigationRegion("main");
services.AddNavigationRegion("side");

var main = sp.GetRegion("main");
var side = sp.GetRegion("side");

await main.GoTo<HomeViewModel>();
await side.GoTo<TocViewModel>();
```

Do **not** register a single unkeyed `Router` if you need two panes — both would share one history.

---

## Shell pattern (`NavigableViewModelBase`)

```csharp
public partial class MainViewModel(Router router) : NavigableViewModelBase(router)
{
    public override ObservableCollection<NavigableView> Navigables { get; } =
    [
        new NavigableView<HomeViewModel>("Home", "fa-house"),
        new NavigableView<SettingsViewModel>("Settings", "fa-gear"),
    ];
}
```

Bind UI:

- `Navigables` → menu `ItemsSource`
- `SelectedNavigable` → selection (drives `Router.GoTo`)
- `CurrentView` → content host

You can also use a plain `ObservableObject` + `Router` without inheriting the base class.

---

## Modals

```csharp
var result = await sp.ShowModalAsync<EditPersonViewModel, bool?>(
    NavigableContext.Create().WithViewModelParameters(person));
```

Requires:

- View model : `ICloseableViewModel<TResult>`
- View : `IModalFor<TViewModel, TResult>` with `ShowModalAsync()`

---

## Lifecycle (`INavigable`)

```csharp
public partial class HomeViewModel : INavigable
{
    public Task OnNavigatedTo(NavigableContext context)
    {
        // read context.ViewModelParameters / Parameters
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom(NavigableContext context) => Task.CompletedTask;

    public Task<bool> CanNavigateAsync() => Task.FromResult(true); // false = cancel
}
```

Views can implement `INavigable` as well; both VM and view receive the callbacks.

---

## CommunityToolkit optional sources

With feature `CommunityToolkit`, types under `Community/` (e.g. `NavigableViewModel` using `[RelayCommand]` / `ObservableObject`) are compiled into the consumer.

Core package stays free of a hard dependency on CommunityToolkit.Mvvm.

---

## Design notes

- **DI-centric**: views and VMs created through keyed factories + `ActivatorUtilities`-style resolution via registered factories.
- **AOT / trimming**: public APIs use `DynamicallyAccessedMembers` on register/get paths.
- **No regions container** like Prism: multiple `Router` instances (keyed) cover multi-pane navigation.
- **Host-agnostic**: `IViewFor` is implemented by Avalonia `UserControl`, MAUI pages, Blazor wrappers, etc. in host packages.

---

## Project layout

```
Router.cs
Extensions.cs              # Register*, GetViewModel, regions, modals
ViewModelViewMap.cs
Bases/
  NavigableViewModelBase.cs
  RouterViewModelBase.cs
  NavigableView.cs
  NavigableContext.cs
  …
Interfaces/
  INavigable.cs
  IViewFor.cs
  IModalFor.cs
  …
Community/                 # optional (feature flag)
build/Avae.ViewModels.targets
```

---

## License

MIT — see [LICENSE.txt](LICENSE.txt).

---

## Related

- [Avae.Abstractions](https://github.com/cedric56/Avae.Abstractions) — samples (Avalonia, MAUI, Blazor)
- [Avae.Services](https://github.com/cedric56/Avae.Services) — dialogs, notifications contracts
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — optional base types
