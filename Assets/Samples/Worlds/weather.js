import console from 'console';
import { get } from 'http';

export let exports = {
    result: null,
};

async function fetch() {
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
    } catch (err) {
        console.error('Network error:', err.message || err);
    }
}

export async function onAwake() {
    fetch();
}
