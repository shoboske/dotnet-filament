<?php

namespace App\Filament\Resources\Orders\Schemas;

use Filament\Infolists\Components\TextEntry;
use Filament\Schemas\Schema;

class OrderInfolist
{
    public static function configure(Schema $schema): Schema
    {
        return $schema
            ->components([
                TextEntry::make('created_at')
                    ->date(),
                TextEntry::make('reference'),
                TextEntry::make('status')
                    ->badge(),
                TextEntry::make('total')
                    ->money(),
                TextEntry::make('customer.name')
                    ->label('Customer'),
            ]);
    }
}
