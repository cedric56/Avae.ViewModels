# Avae.ViewModels

Lightweight **ViewModel-first navigation** for the [Avae](https://github.com/cedric56/Avae.Abstractions) stack.

The core package is built on `Microsoft.Extensions.DependencyInjection` and has no UI-framework dependency. It can be used from Avalonia, MAUI, Blazor, or another host that maps view-models to views.

> **Status:** preview (`1.0.0-preview.1`) · **Target framework:** `net11.0` · **AOT-compatible**

## Features

- ViewModel-first navigation with DI-based view resolution
- Back/forward navigation history with a bounded history size
- Navigation lifecycle hooks and navigation guards
- Independent keyed navigation regions for multi-pane UIs
- View/view-model construction with optional parameters
- Modal/dialog result flow through `ICloseableViewModel<TResult>`
- Optional CommunityToolkit.Mvvm source set
- Optional ReactiveUI source set
- Designed with trimming/AOT scenarios in mind

## Requirements

- .NET 11 SDK
- `Microsoft.Extensions.DependencyInjection`

The core package does not require a UI framework.

## Install

```xml
<PackageReference Include="Avae.ViewModels" Version="1.0.0-preview.1" />
```

### Optional CommunityToolkit.Mvvm support

Enable the `CommunityToolkit` feature in the consuming project:

```xml
<PropertyGroup>
  <AvaeFeatures>;CommunityToolkit;</AvaeFeatures>
</PropertyGroup>
```

This adds the CommunityToolkit-based source set and its `CommunityToolkit.Mvvm` dependency.

### Optional ReactiveUI support

Enable the `ReactiveUI` feature:

```xml
<PropertyGroup>
  <AvaeFeatures>;ReactiveUI;</AvaeFeatures>
</PropertyGroup>
```

This adds the ReactiveUI-based source set and its `ReactiveUI` / `ReactiveUI.SourceGenerators` dependencies.

Both features can be enabled when needed:

```xml
<PropertyGroup>
  <AvaeFeatures>;CommunityToolkit;ReactiveUI;</AvaeFeatures>
</PropertyGroup>
```

## Core concepts

| Type | Role |
|------|------|
| `IRouter` | Navigation history, `GoTo`, `BackAsync`, `ForwardAsync`, and lifecycle transitions |
| `IViewFor` | UI-independent view abstraction; exposes the current view-model through `Context` |
| `INavigable` | Optional navigation guard and lifecycle callbacks |
| `NavigableContext` | Parameters supplied while creating/navigating to a view or view-model |
| `ViewModelViewMap` | Maps navigation keys to view types |
| `NavigableView` | Menu/sidebar entry containing display metadata and a target view-model type |
| `NavigableViewModelBase` | Shell view-model with navigable items and current view |
| `ICloseableViewModel<TResult>` | Modal/dialog result contract |

Navigation is **view-model first**: the application navigates to a view-model type or instance, and the framework resolves the corresponding view through DI.

## Quick start

### 1. Register pages

```csharp
using Avae.ViewModels;
using Microsoft.Extensions.DependencyInjection;

services.RegisterNavigation();

services.Register<HomeView, HomeViewModel>();
services.Register<SettingsView, SettingsViewModel>();

// Optional lifetimes and a custom view key.
services.RegisterWithLifetime<DetailView, DetailViewModel>(
    viewModelLifetime: ServiceLifetime.Transient,
    viewLifetime: ServiceLifetime.Transient,
    viewKey: "detail");
```

`Register<TView, TViewModel>()` registers both the view and view-model as singletons.

`RegisterWithLifetime<TView, TViewModel>()` lets you choose the view-model and view lifetimes. Its default lifetime for both is `Transient`.

Unless explicitly overridden, the navigation key is the view-model type name, for example `DetailViewModel`.

### 2. Navigate

```csharp
var router = sp.GetRequiredService<IRouter>();

// Create the view-model through DI.
var (view, vm) = await router.GoTo<HomeViewModel>();

// Navigate using an existing instance.
await router.GoTo(existingVm);

// Navigate using a runtime type.
await router.GoToType(typeof(SettingsViewModel));

// Pass construction/navigation parameters.
await router.GoTo<DetailViewModel>(
    context: NavigableContext.Create()
        .WithViewModelParameters(("id", 42))
        .WithViewParameters(("title", "Detail")));
```

A custom key can be supplied when the registered view-model key is not its type name:

```csharp
await router.GoTo<DetailViewModel>(key: "detail");
```

### 3. Navigate through history

```csharp
if (router.CanGoBack)
    await router.BackAsync();

if (router.CanGoForward)
    await router.ForwardAsync();

```

The default history limit is 20 entries.

## Navigation lifecycle

Implement `INavigable` when a view-model needs to participate in navigation:

```csharp
public partial class HomeViewModel : INavigable
{
    public Task OnNavigatedTo(NavigableContext context)
    {
        // Read context.ViewModelParameters / context.Parameters as needed.
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom(NavigableContext context)
        => Task.CompletedTask;

    public Task<bool> CanNavigateAsync()
        => Task.FromResult(true); // false cancels leaving the current item
}
```

`CanNavigateAsync()` is a guard for **leaving the current navigation item**. `OnNavigatedFrom` and `OnNavigatedTo` are invoked during transitions; views can implement `INavigable` as well, so both the view-model and view can receive lifecycle callbacks.

## Multiple navigation regions

Use keyed navigation regions when independent parts of the UI need independent navigation histories:

```csharp
services.AddNavigationRegion("main");
services.AddNavigationRegion("side");

var main = sp.GetRegion("main");
var side = sp.GetRegion("side");

await main.GoTo<HomeViewModel>();
await side.GoTo<TocViewModel>();
```

Each call to `AddNavigationRegion` creates a keyed singleton router with its own history.

If an application needs two independent panes, do not resolve the same unkeyed `Router` singleton for both panes: that would intentionally share one navigation history.

## Shell pattern

`NavigableViewModelBase` can be used for a shell that exposes a collection of navigation entries and the current view:

```csharp
public partial class MainViewModel(IRouter router)
    : NavigableViewModelBase(router)
{
    protected override ObservableCollection<NavigableView> GetNavigables() =>
    [
        new NavigableView<HomeViewModel>("Home", "fa-house"),
        new NavigableView<SettingsViewModel>("Settings", "fa-gear"),
    ];
}
```

Typical bindings are:

- `Navigables` → menu `ItemsSource`
- `SelectedNavigable` → selected navigation item
- `CurrentView` → content host

A shell does not have to inherit from `NavigableViewModelBase`; a regular view-model can use `Router` directly.

## Modals

Modal flows use `ICloseableViewModel<TResult>` and `IModalFor<TViewModel, TResult>`:

```csharp
var result = await sp.ShowModalAsync<EditPersonViewModel, bool?>(
    NavigableContext.Create()
        .WithViewModelParameters(person));
```

The corresponding modal view must implement `IModalFor<TViewModel, TResult>` and provide `ShowModalAsync()`.

## Optional source sets

The package can include source sets from the consuming application rather than imposing those frameworks on the core package.

### CommunityToolkit.Mvvm

With:

```xml
<AvaeFeatures>;CommunityToolkit;</AvaeFeatures>
```

the package includes the sources under `Community/`, such as CommunityToolkit-based observable/navigable view-model helpers.

### ReactiveUI

With:

```xml
<AvaeFeatures>;ReactiveUI;</AvaeFeatures>
```

the package includes the sources under `ReactiveUI/`, including ReactiveUI-backed `NavigableViewModel` and closeable view-model helpers.

## Design notes

- **DI-centric:** views and view-models are created through registered keyed factories.
- **Host-agnostic:** `IViewFor` only requires a `Context` property; UI-specific adapters can implement it in the host application.
- **Multiple regions:** independent keyed `Router` instances provide separate navigation histories without requiring a separate regions container.
- **AOT/trimming:** registration and resolution APIs use `DynamicallyAccessedMembers` annotations where required for constructor discovery.
- **Core dependency surface:** the core package depends on `Microsoft.Extensions.DependencyInjection`; CommunityToolkit and ReactiveUI support are opt-in.

## Project layout

```text
Navigation/Router.cs
Extensions.cs
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

Community/                 # optional: AvaeFeatures=CommunityToolkit
ReactiveUI/                # optional: AvaeFeatures=ReactiveUI
build/Avae.ViewModels.targets
```

## Related projects

- [Avae.Abstractions](https://github.com/cedric56/Avae.Abstractions) — Avae abstractions and samples
- [Avae.Services](https://github.com/cedric56/Avae.Services) — dialogs and notification contracts
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — optional MVVM helpers
- [ReactiveUI](https://www.reactiveui.net/) — optional reactive MVVM support

## License

MIT — see [LICENSE.txt](LICENSE.txt).
