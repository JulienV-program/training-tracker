window.charts = window.charts || {};

// Convertit une valeur décimale en minutes (ex: 5.5) en format "m:ss" (ex: "5:30")
window.formatPace = function (decimalMinutes) {
    if (decimalMinutes == null || isNaN(decimalMinutes)) return '-';
    const minutes = Math.floor(decimalMinutes);
    const seconds = Math.round((decimalMinutes - minutes) * 60);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
};

window.renderStravaChart = function (dataJson, speedLabel, speedUnit) {
    const data = JSON.parse(dataJson);
    const canvas = document.getElementById('stravaChart');
    if (!canvas) return;

    speedLabel = speedLabel || 'Allure (m/s)';
    const isPaceUnit = speedUnit === 'minkm' || speedUnit === 'min100m';

    if (window.charts.strava) {
        window.charts.strava.destroy();
    }

    const ctx = canvas.getContext('2d');

    window.charts.strava = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.timeOffset),
            datasets: [
                {
                    label: 'Fréquence cardiaque (bpm)',
                    data: data.map(d => d.heartRate),
                    yAxisID: 'y',
                    borderColor: 'red',
                    pointRadius: 0,
                    borderWidth: 1.5
                },
                {
                    label: 'Puissance (W)',
                    data: data.map(d => d.watts),
                    yAxisID: 'y1',
                    borderColor: 'orange',
                    pointRadius: 0,
                    borderWidth: 1.5
                },
                {
                    label: 'Altitude (m)',
                    data: data.map(d => d.altitude),
                    yAxisID: 'y2',
                    borderColor: 'green',
                    pointRadius: 0,
                    borderWidth: 1
                },
                {
                    label: speedLabel,
                    data: data.map(d => d.velocity),
                    yAxisID: 'y3',
                    borderColor: 'blue',
                    pointRadius: 0,
                    borderWidth: 1,
                    hidden: true
                },
                {
                    label: 'Température (°C)',
                    data: data.map(d => d.temperature),
                    yAxisID: 'y4',
                    borderColor: 'purple',
                    pointRadius: 0,
                    borderWidth: 1,
                    hidden: true
                },
                {
                    label: 'Cadence (rpm vélo / pas-min course)',
                    data: data.map(d => d.cadence),
                    yAxisID: 'y5',
                    borderColor: 'teal',
                    pointRadius: 0,
                    borderWidth: 1,
                    hidden: true
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            if (context.dataset.yAxisID === 'y3' && isPaceUnit) {
                                return `${context.dataset.label}: ${window.formatPace(context.parsed.y)}`;
                            }
                            return `${context.dataset.label}: ${context.parsed.y}`;
                        }
                    }
                }
            },
            scales: {
                x: { title: { display: true, text: 'Temps (s)' } },
                y: { type: 'linear', position: 'left', title: { display: true, text: 'BPM' } },
                y1: { type: 'linear', position: 'right', title: { display: true, text: 'Watts' }, grid: { drawOnChartArea: false } },
                y2: { type: 'linear', display: false },
                y3: { type: 'linear', display: false },
                y4: { type: 'linear', display: false },
                y5: { type: 'linear', display: false }
            }
        }
    });
};

window.renderSplitsChart = function (dataJson, speedLabel, speedUnit) {
    const data = JSON.parse(dataJson);
    const canvas = document.getElementById('splitsChart');
    if (!canvas) return;

    if (window.charts.splits) {
        window.charts.splits.destroy();
    }

    if (!data || data.length === 0) return;

    speedLabel = speedLabel || 'Vitesse moyenne (m/s)';
    const isPaceUnit = speedUnit === 'minkm' || speedUnit === 'min100m';

    const ctx = canvas.getContext('2d');

    window.charts.splits = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map((d, i) => `Km ${i + 1}`),
            datasets: [
                {
                    type: 'bar',
                    label: speedLabel,
                    data: data.map(d => d.averageSpeed),
                    yAxisID: 'y',
                    backgroundColor: 'rgba(54, 162, 235, 0.6)'
                },
                {
                    type: 'line',
                    label: 'FC moyenne (bpm)',
                    data: data.map(d => d.averageHeartRate),
                    yAxisID: 'y1',
                    borderColor: 'red',
                    pointRadius: 2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            if (context.dataset.yAxisID === 'y' && isPaceUnit) {
                                return `${context.dataset.label}: ${window.formatPace(context.parsed.y)}`;
                            }
                            return `${context.dataset.label}: ${context.parsed.y}`;
                        }
                    }
                }
            },
            scales: {
                y: { type: 'linear', position: 'left', title: { display: true, text: speedLabel } },
                y1: { type: 'linear', position: 'right', title: { display: true, text: 'BPM' }, grid: { drawOnChartArea: false } }
            }
        }
    });
};
