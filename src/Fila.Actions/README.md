# Fila.Actions

Fila's actions: the things a user can do to a record or a selection of records.

Ships `CreateAction`, `EditAction`, `DeleteAction`, `ReplicateAction`, `RestoreAction`,
`ForceDeleteAction`, their bulk counterparts, and `ActionGroup`. Each can open a form, ask for
confirmation, hide itself per record, and raise a notification when it finishes. Ported from
Filament's actions package.

## Install

```bash
dotnet add package Fila.Actions --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## A custom action

```csharp
private static readonly Action MarkShippedAction = new Action("mark-shipped")
    .Label("Mark shipped")
    .Icon("check-circle")
    .Color("success")
    .RequiresConfirmation()
    .ModalDescription("This marks the order as shipped.")
    .Visible(ctx => ((Order)ctx.Record!).Status is OrderStatus.Pending or OrderStatus.Processing)
    .Handle(async ctx =>
    {
        ((Order)ctx.Record!).Status = OrderStatus.Shipped;
        await ctx.Db.SaveChangesAsync(ctx.Ct);
    })
    .Notify(Notification.Make().Title("Marked as shipped").Success());
```

Then list it alongside the built-in ones:

```csharp
.Actions(BuildEditAction(), MarkShippedAction, BuildReplicateAction(), BuildDeleteAction())
```

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
