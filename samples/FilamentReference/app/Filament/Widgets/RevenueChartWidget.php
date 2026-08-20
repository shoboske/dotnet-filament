<?php

namespace App\Filament\Widgets;

use App\Models\Order;
use Carbon\Carbon;
use Filament\Support\RawJs;
use Filament\Widgets\ChartWidget;

class RevenueChartWidget extends ChartWidget
{
    private const DAYS = 14;

    protected ?string $heading = 'Revenue';

    protected ?string $description = 'Last '.self::DAYS.' days';

    protected function getData(): array
    {
        $firstDay = Carbon::today()->subDays(self::DAYS - 1);

        $byDay = Order::where('created_at', '>=', $firstDay)
            ->get(['created_at', 'total'])
            ->groupBy(fn (Order $order) => $order->created_at->toDateString())
            ->map(fn ($group) => $group->sum('total'));

        $labels = [];
        $data = [];

        foreach (range(0, self::DAYS - 1) as $offset) {
            $day = $firstDay->copy()->addDays($offset);
            $labels[] = $day->format('M j');
            $data[] = (float) ($byDay[$day->toDateString()] ?? 0);
        }

        return [
            'datasets' => [
                [
                    'label' => 'Revenue',
                    'data' => $data,
                ],
            ],
            'labels' => $labels,
        ];
    }

    protected function getType(): string
    {
        return 'line';
    }

    protected function getOptions(): array|RawJs|null
    {
        return RawJs::make(<<<'JS'
            {
                scales: {
                    y: {
                        ticks: {
                            callback: (value) => '$' + value,
                        },
                    },
                },
            }
            JS);
    }
}
