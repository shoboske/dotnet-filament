<?php

namespace App\Filament\Widgets;

use App\Models\Order;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;
use Filament\Widgets\TableWidget;

class RecentOrdersWidget extends TableWidget
{
    protected static ?string $heading = 'Recent orders';

    protected int|string|array $columnSpan = 'full';

    protected static ?int $sort = 10;

    public function table(Table $table): Table
    {
        return $table
            ->query(Order::query()->with('customer'))
            ->columns([
                TextColumn::make('created_at')
                    ->date(),
                TextColumn::make('reference'),
                TextColumn::make('status')
                    ->badge(),
                TextColumn::make('total')
                    ->money()
                    ->alignEnd(),
                TextColumn::make('customer.name')
                    ->label('Customer'),
            ])
            ->defaultSort('created_at', 'desc')
            ->paginated([5])
            ->defaultPaginationPageOption(5);
    }
}
