import console from 'console';
import { get } from 'http';
import { setTimeout } from 'scheduler';

export let exports = {
    result: null,
};

async function fetch() {
    let delay = 900 * 1000;

    try {
        let res = await get('https://ipinfo.io/json');

        if (!res.ok()) 
            throw new Error("Fetching localisation not available.");

        let data = await res.body.json();

        res = await get('https://api.open-meteo.com/v1/forecast', {
            query: {
                latitude: data.loc.split(',')[0],
                longitude: data.loc.split(',')[1],
                current: 'temperature_2m'
            }
        });

        if (!res.ok()) 
            throw new Error("Fetching weather not available.");

        data = await res.body.json();
        
        if (exports?.result)
            exports.result.text = `${data.current.temperature_2m}${data.current_units.temperature_2m}`;
        
        console.log(`Weather: ${data.current.temperature_2m}${data.current_units.temperature_2m}`);

        const time = new Date(data.current.time + 'Z').getTime();
        const interval = data.current.interval * 1000;
        const next = time + interval;
        const now = Date.now();

        delay = Math.max(1000, (next - now) + 5000);

    } catch (err) {
        console.error('Network error:', err.message || err);
    } finally {
        setTimeout(fetch, delay);
    }
}

export async function onAwake() {
    fetch();
}