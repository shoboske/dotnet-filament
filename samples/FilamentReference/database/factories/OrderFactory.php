<?php

namespace Database\Factories;

use App\Enums\OrderStatus;
use App\Models\Customer;
use App\Models\Order;
use Illuminate\Database\Eloquent\Factories\Factory;

/**
 * @extends Factory<Order>
 */
class OrderFactory extends Factory
{
    /**
     * Define the model's default state.
     *
     * @return array<string, mixed>
     */
    public function definition(): array
    {
        return [
            'reference' => 'ORD-'.fake()->unique()->numberBetween(1000, 9999),
            'customer_id' => Customer::factory(),
            'status' => fake()->randomElement(OrderStatus::cases()),
            'total' => fake()->randomFloat(2, 10, 500),
            'created_at' => fake()->dateTimeBetween('-60 days'),
        ];
    }
}
