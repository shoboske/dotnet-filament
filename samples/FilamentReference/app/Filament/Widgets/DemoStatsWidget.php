<?php

namespace App\Filament\Widgets;

use App\Enums\OrderStatus;
use App\Models\Customer;
use App\Models\Order;
use Carbon\Carbon;
use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;

class DemoStatsWidget extends StatsOverviewWidget
{
    private const TREND_DAYS = 14;

    protected function getStats(): array
    {
        $customers = Customer::count();
        $orders = Order::count();
        $revenue = Order::sum('total');
        $shipped = Order::where('status', OrderStatus::Shipped)->count();

        $since = Carbon::today()->subDays(self::TREND_DAYS - 1);
        $recent = Order::where('created_at', '>=', $since)
            ->get(['created_at', 'total']);

        $byDayCount = $recent->groupBy(fn (Order $order) => $order->created_at->toDateString())
            ->map(fn ($group) => $group->count());

        $byDayRevenue = $recent->groupBy(fn (Order $order) => $order->created_at->toDateString())
            ->map(fn ($group) => $group->sum('total'));

        $ordersPerDay = $this->trendSeries($since, $byDayCount);
        $revenuePerDay = $this->trendSeries($since, $byDayRevenue);

        return [
            Stat::make('Customers', (string) $customers)
                ->description('Active accounts'),

            Stat::make('Orders', (string) $orders)
                ->description("{$shipped} shipped")
                ->descriptionIcon('heroicon-m-arrow-trending-up')
                ->descriptionColor('info')
                ->chart($ordersPerDay)
                ->chartColor('info'),

            Stat::make('Revenue', '$'.number_format($revenue, 2))
                ->description('$'.number_format(array_sum($revenuePerDay), 2).' in '.self::TREND_DAYS.' days')
                ->descriptionIcon('heroicon-m-arrow-trending-up')
                ->descriptionColor('success')
                ->chart($revenuePerDay)
                ->chartColor('success'),
        ];
    }

    /**
     * @return array<float>
     */
    private function trendSeries(Carbon $since, \Illuminate\Support\Collection $byDay): array
    {
        return collect(range(0, self::TREND_DAYS - 1))
            ->map(fn (int $offset) => (float) ($byDay[$since->copy()->addDays($offset)->toDateString()] ?? 0))
            ->all();
    }
}
