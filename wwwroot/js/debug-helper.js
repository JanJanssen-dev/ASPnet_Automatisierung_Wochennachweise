// Debug Helper für Development-Umgebung (Optional)
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {

    // Erweiterte Console-Befehle für Testing
    window.testAPI = async function () {
        try {
            const response = await fetch('/api/wochennachweis/test');
            const data = await response.json();
            console.log('🧪 API Test Ergebnis:', data);
        } catch (error) {
            console.error('🧪 API Test Fehler:', error);
        }
    };

    window.testTemplate = async function () {
        try {
            const response = await fetch('/api/wochennachweis/template');
            if (response.ok) {
                const blob = await response.blob();
                console.log('📄 Template erfolgreich geladen:', blob.size, 'Bytes');
            } else {
                console.error('📄 Template Fehler:', response.status);
            }
        } catch (error) {
            console.error('📄 Template Fehler:', error);
        }
    };

    window.testGeneration = async function () {
        const testConfig = {
            umschulungsbeginn: '2024-01-01',
            nachname: 'Test',
            vorname: 'User',
            klasse: 'TEST-2024',
            zeitraeume: [{
                kategorie: 'Umschulung',
                start: '2024-01-01',
                ende: '2024-01-07',
                beschreibung: 'Test Zeitraum'
            }]
        };

        try {
            const response = await fetch('/api/wochennachweis/generate-data', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(testConfig)
            });

            if (response.ok) {
                const data = await response.json();
                console.log('🧪 Test-Generierung erfolgreich:', data);
            } else {
                const error = await response.json();
                console.error('🧪 Test-Generierung Fehler:', error);
            }
        } catch (error) {
            console.error('🧪 Test-Generierung Fehler:', error);
        }
    };

    console.log(`
🔧 Debug-Modus aktiv!
📋 Verfügbare Befehle:
   - testAPI() - API-Verbindung testen
   - testTemplate() - Template-Download testen  
   - testGeneration() - Daten-Generierung testen
    `);
}