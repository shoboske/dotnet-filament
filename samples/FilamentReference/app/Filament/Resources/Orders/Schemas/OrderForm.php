<?php

namespace App\Filament\Resources\Orders\Schemas;

use App\Enums\OrderStatus;
use Filament\Forms\Components\DatePicker;
use Filament\Forms\Components\Select;
use Filament\Forms\Components\TextInput;
use Filament\Schemas\Schema;

class OrderForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextInput::make('reference')
                    ->required(),
                Select::make('customer_id')
                    ->label('Customer')
                    ->relationship('customer', 'name', modifyQueryUsing: fn ($query) => $query->orderBy('name'))
                    ->required(),
                Select::make('status')
                    ->options(OrderStatus::class)
                    ->required(),
                TextInput::make('total')
                    ->required()
                    ->numeric(),
                DatePicker::make('created_at')
                    ->required(),
            ]);
    }
}
