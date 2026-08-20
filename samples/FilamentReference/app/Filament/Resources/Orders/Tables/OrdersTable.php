<?php

namespace App\Filament\Resources\Orders\Tables;

use App\Enums\OrderStatus;
use App\Models\Order;
use Filament\Actions\Action;
use Filament\Actions\BulkActionGroup;
use Filament\Actions\DeleteAction;
use Filament\Actions\DeleteBulkAction;
use Filament\Actions\EditAction;
use Filament\Actions\ReplicateAction;
use Filament\Actions\ViewAction;
use Filament\Support\Icons\Heroicon;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class OrdersTable
{
    public static function configure(Table $table): Table
    {
        return $table
            ->columns([
                TextColumn::make('created_at')
                    ->date()
                    ->sortable(),
                TextColumn::make('reference')
                    ->searchable()
                    ->sortable(),
                TextColumn::make('status')
                    ->badge(),
                TextColumn::make('total')
                    ->money()
                    ->sortable()
                    ->alignEnd(),
                TextColumn::make('customer.name')
                    ->label('Customer'),
            ])
            ->defaultSort('created_at', 'desc')
            ->filters([
                //
            ])
            ->recordActions([
                ViewAction::make(),
                EditAction::make(),
                self::markShippedAction(),
                ReplicateAction::make(),
                DeleteAction::make(),
            ])
            ->toolbarActions([
                BulkActionGroup::make([
                    DeleteBulkAction::make(),
                ]),
            ])
            ->paginated([25]);
    }

    private static function markShippedAction(): Action
    {
        return Action::make('mark-shipped')
            ->label('Mark shipped')
            ->icon(Heroicon::OutlinedCheckCircle)
            ->color('success')
            ->requiresConfirmation()
            ->modalDescription('This marks the order as shipped.')
            ->visible(fn (Order $record) => in_array($record->status, [OrderStatus::Pending, OrderStatus::Processing]))
            ->action(fn (Order $record) => $record->update(['status' => OrderStatus::Shipped]))
            ->successNotificationTitle('Marked as shipped');
    }
}
