// Initializes the Chart.js canvases _Chart.cshtml/_StatsOverview.cshtml render — the dashboard
// chart widget and each stat's trend sparkline. Loaded after vendor/chart.min.js and before this
// runs on DOMContentLoaded; both containers carry their series as a JSON data attribute rather
// than inline Alpine state, since Fila's dashboard is a single static server render with no
// Livewire-style live updates to wire up (see ChartWidget.cs's doc comment).
//
// Colors are read off computed styles the same way Filament's own chart.js/stat/chart.js do —
// via hidden <span> elements carrying fila.widgets.css's .fi-wi-chart-*-color classes — so the
// chart always matches the CSS tokens exactly instead of duplicating hex values here. Fila has no
// Livewire/Alpine theme store; its light/dark toggle is the plain `fila-theme-changed` DOM event
// _ThemeScript.cshtml dispatches, so that's what triggers a re-color instead of Filament's
// Alpine.effect on Alpine.store('theme').
(function () {
    if (typeof Chart === "undefined") return;

    // packages/widgets/resources/js/components/chart.js's global Chart.defaults override.
    Chart.defaults.plugins.legend.labels.boxWidth = 12;
    Chart.defaults.plugins.legend.position = "bottom";

    function colorOf(container, className) {
        var el = container.querySelector("." + className);
        return el ? getComputedStyle(el).color : undefined;
    }

    function initCharts() {
        document.querySelectorAll("[data-fila-chart]").forEach(function (frame) {
            if (frame.filaChart) return;

            var config = JSON.parse(frame.dataset.filaChart);
            var canvas = frame.querySelector("canvas");
            var backgroundColor = colorOf(frame, "fi-wi-chart-bg-color");
            var borderColor = colorOf(frame, "fi-wi-chart-border-color");
            var textColor = colorOf(frame, "fi-wi-chart-text-color");
            var gridColor = colorOf(frame, "fi-wi-chart-grid-color");

            var formatValue = function (value) {
                return config.valuePrefix + value + config.valueSuffix;
            };

            frame.filaChart = new Chart(canvas, {
                type: "line",
                data: {
                    labels: config.labels,
                    datasets: [{
                        label: config.datasetLabel,
                        data: config.values,
                        borderWidth: 2,
                        borderColor: borderColor,
                        backgroundColor: backgroundColor,
                        pointBackgroundColor: borderColor,
                        pointRadius: 2,
                        pointHitRadius: 4,
                        fill: true,
                        tension: 0.3,
                    }],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: { duration: 0 },
                    color: textColor,
                    scales: {
                        x: {
                            border: { display: false },
                            grid: { display: false },
                            ticks: { color: textColor },
                        },
                        y: {
                            border: { display: false },
                            grid: { color: gridColor },
                            ticks: { color: textColor, callback: formatValue },
                        },
                    },
                    plugins: {
                        legend: { display: true },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    return ctx.dataset.label + ": " + formatValue(ctx.formattedValue);
                                },
                            },
                        },
                    },
                },
            });
        });
    }

    // Filament's packages/widgets/resources/js/components/stats-overview/stat/chart.js, exactly:
    // borderWidth 2, fill 'start', tension 0.5, no points, no axes, no legend, no tooltip.
    function initSparklines() {
        document.querySelectorAll("[data-fila-sparkline]").forEach(function (frame) {
            if (frame.filaChart) return;

            var config = JSON.parse(frame.dataset.filaSparkline);
            var canvas = frame.querySelector("canvas");
            var backgroundColor = colorOf(frame, "fi-wi-stats-overview-stat-chart-bg-color");
            var borderColor = colorOf(frame, "fi-wi-stats-overview-stat-chart-border-color");

            frame.filaChart = new Chart(canvas, {
                type: "line",
                data: {
                    labels: config.values.map(function (_, i) { return i; }),
                    datasets: [{
                        data: config.values,
                        borderWidth: 2,
                        fill: "start",
                        tension: 0.5,
                        backgroundColor: backgroundColor,
                        borderColor: borderColor,
                    }],
                },
                options: {
                    animation: { duration: 0 },
                    elements: { point: { radius: 0 } },
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: { enabled: false },
                    },
                    scales: {
                        x: { display: false },
                        y: { display: false },
                    },
                },
            });
        });
    }

    function recolor(frame, dataColorClass, borderColorClass, extraScaleUpdate) {
        var chart = frame.filaChart;
        if (!chart) return;

        var backgroundColor = colorOf(frame, dataColorClass);
        var borderColor = colorOf(frame, borderColorClass);
        var dataset = chart.data.datasets[0];
        dataset.backgroundColor = backgroundColor;
        dataset.borderColor = borderColor;
        dataset.pointBackgroundColor = borderColor;

        if (extraScaleUpdate) extraScaleUpdate(chart);

        chart.update("none");
    }

    function recolorAll() {
        document.querySelectorAll("[data-fila-chart]").forEach(function (frame) {
            recolor(frame, "fi-wi-chart-bg-color", "fi-wi-chart-border-color", function (chart) {
                var textColor = colorOf(frame, "fi-wi-chart-text-color");
                var gridColor = colorOf(frame, "fi-wi-chart-grid-color");
                chart.options.color = textColor;
                chart.options.scales.x.ticks.color = textColor;
                chart.options.scales.y.ticks.color = textColor;
                chart.options.scales.y.grid.color = gridColor;
            });
        });

        document.querySelectorAll("[data-fila-sparkline]").forEach(function (frame) {
            recolor(frame, "fi-wi-stats-overview-stat-chart-bg-color", "fi-wi-stats-overview-stat-chart-border-color");
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initCharts();
        initSparklines();
    });

    document.addEventListener("fila-theme-changed", recolorAll);
})();
