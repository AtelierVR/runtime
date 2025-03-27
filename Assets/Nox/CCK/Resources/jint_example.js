import { setData, getData, getDataKeys } from 'api';

function onPrepare() {
    log("Script is being prepared");
    log(JSON.stringify(gameobject));
}

function onStart() {
    log("Script has started");

    // Définir des données
    api.setData("key1", "value1");
    api.setData("key2", "value2");

    // Récupérer des données
    var value1 = api.getData("key1");
    log("Value for key1: " + value1);

    // Récupérer toutes les clés de données
    var keys = api.getDataKeys();
    log("All keys: " + keys.join(", "));
}

function onUpdate() {
    log("Script is updating");
}

function onDestroy() {
    log("Script is being destroyed");
}