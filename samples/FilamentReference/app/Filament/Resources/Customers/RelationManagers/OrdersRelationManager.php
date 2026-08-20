<?php

namespace App\Filament\Resources\Customers\RelationManagers;

use Filament\Resources\RelationManagers\RelationManager;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;

class OrdersRelationManager extends RelationManager
{
    protected static string $relationship = 'orders';

    public function table(Table $table): Table
    {
        return $table
            ->recordTitleAttribute('reference')
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
            ])
            ->defaultSort('created_at', 'desc')
            ->paginated([10])
            ->headerActions([])
            ->recordActions([])
            ->toolbarActions([]);
    }
}
