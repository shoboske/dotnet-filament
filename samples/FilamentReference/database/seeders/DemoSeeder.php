<?php

namespace Database\Seeders;

use App\Enums\OrderStatus;
use App\Models\Customer;
use App\Models\Order;
use Illuminate\Database\Seeder;

class DemoSeeder extends Seeder
{
    /**
     * Seed the application's database.
     */
    public function run(): void
    {
        if (Customer::withTrashed()->exists()) {
            return;
        }

        $acme = Customer::create(['name' => 'Acme Corp', 'email' => 'buyer@acme.test']);
        $globex = Customer::create(['name' => 'Globex', 'email' => 'ops@globex.test']);
        $initech = Customer::create(['name' => 'Initech', 'email' => 'ap@initech.test']);

        $statuses = [
            OrderStatus::Pending,
            OrderStatus::Processing,
            OrderStatus::Shipped,
            OrderStatus::Delivered,
            OrderStatus::Cancelled,
        ];
        $customers = [$acme, $globex, $initech];

        for ($i = 1; $i <= 42; $i++) {
            Order::create([
                'reference' => 'ORD-'.(1000 + $i),
                'customer_id' => $customers[$i % count($customers)]->id,
                'status' => $statuses[$i % count($statuses)],
                'total' => 19.99 + $i * 7.25,
                'created_at' => now()->subDays($i),
            ]);
        }
    }
}
