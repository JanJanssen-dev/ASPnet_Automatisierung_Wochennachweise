// Client-seitige Wochennachweis-Generierung
class ClientWochennachweisGenerator {
    constructor() {
        this.template = null;
        this.isGenerating = false;
    }

    async initialize() {
        try {
            // Template beim Laden der Seite vorab herunterladen
            const response = await fetch('/api/wochennachweis/template');
            if (response.ok) {
                this.template = await response.arrayBuffer();
                console.log('✅ Template erfolgreich geladen');
            } else {
                console.warn('⚠️ Template konnte nicht vorab geladen werden');
            }
        } catch (error) {
            console.error('❌ Fehler beim Vorab-Laden des Templates:', error);
        }

        // Test-API-Call
        try {
            const testResponse = await fetch('/api/wochennachweis/test');
            const testData = await testResponse.json();
            console.log('🔧 API Test:', testData);
        } catch (error) {
            console.error('❌ API Test fehlgeschlagen:', error);
        }
    }

    async generateDocuments() {
        if (this.isGenerating) {
            console.warn('⚠️ Generierung läuft bereits...');
            return;
        }

        this.isGenerating = true;

        try {
            // 1. Konfigurationsdaten sammeln
            this.showProgress('📋 Sammle Konfigurationsdaten...');
            const configData = this.getConfigFromForm();

            if (!this.validateConfig(configData)) {
                this.hideProgress();
                return;
            }

            // 2. Daten vom Server generieren lassen
            this.showProgress('🔄 Generiere Wochendaten...');
            const dataResponse = await fetch('/api/wochennachweis/generate-data', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(configData)
            });

            if (!dataResponse.ok) {
                const errorData = await dataResponse.json();
                throw new Error(errorData.message || 'Fehler beim Generieren der Daten');
            }

            const wochennachweisData = await dataResponse.json();
            console.log('📊 Wochendaten erhalten:', wochennachweisData);

            // 3. Template laden falls noch nicht vorhanden
            if (!this.template) {
                this.showProgress('📄 Lade Word-Template...');
                const templateResponse = await fetch('/api/wochennachweis/template');
                if (!templateResponse.ok) {
                    throw new Error('Template konnte nicht geladen werden');
                }
                this.template = await templateResponse.arrayBuffer();
            }

            // 4. Dokumente erstellen
            this.showProgress('🔨 Erstelle Word-Dokumente...');
            const documents = [];

            for (let i = 0; i < wochennachweisData.wochen.length; i++) {
                const woche = wochennachweisData.wochen[i];
                this.showProgress(`📝 Erstelle Dokument ${i + 1}/${wochennachweisData.wochen.length} (Woche ${woche.nummer})...`);

                try {
                    const document = await this.createDocument(woche);
                    documents.push({
                        name: `Wochennachweis_Woche_${String(woche.nummer).padStart(2, '0')}_${woche.kategorie}.docx`,
                        content: document,
                        woche: woche
                    });
                } catch (docError) {
                    console.error(`❌ Fehler bei Dokument ${i + 1}:`, docError);
                    this.showError(`Fehler bei Woche ${woche.nummer}: ${docError.message}`);
                }
            }

            if (documents.length === 0) {
                throw new Error('Keine Dokumente konnten erstellt werden');
            }

            // 5. ZIP erstellen und downloaden
            this.showProgress('📦 Erstelle ZIP-Archiv...');
            await this.createZipDownload(documents, wochennachweisData);

            this.showSuccess(`✅ ${documents.length} Dokumente erfolgreich erstellt!`);

            // Zusätzlich Einzeldownload-Buttons anzeigen
            this.showDownloadButtons(documents);

        } catch (error) {
            console.error('❌ Fehler bei der Generierung:', error);
            this.showError('Fehler bei der Generierung: ' + error.message);
        } finally {
            this.isGenerating = false;
        }
    }

    getConfigFromForm() {
        return {
            umschulungsbeginn: document.getElementById('Umschulungsbeginn')?.value || '',
            nachname: document.getElementById('Nachname')?.value || '',
            vorname: document.getElementById('Vorname')?.value || '',
            klasse: document.getElementById('Klasse')?.value || '',
            zeitraeume: this.getZeitraeume()
        };
    }

    getZeitraeume() {
        const zeitraeume = [];
        const tableRows = document.querySelectorAll('table tbody tr');

        tableRows.forEach(row => {
            const cells = row.querySelectorAll('td');
            if (cells.length >= 4) {
                // Badge-Text extrahieren
                const kategorieBadge = cells[0].querySelector('.badge');
                const kategorie = kategorieBadge ? kategorieBadge.textContent.trim() : cells[0].textContent.trim();

                zeitraeume.push({
                    kategorie: kategorie,
                    start: this.parseGermanDate(cells[1].textContent.trim()),
                    ende: this.parseGermanDate(cells[2].textContent.trim()),
                    beschreibung: cells[3].textContent.trim()
                });
            }
        });

        return zeitraeume;
    }

    parseGermanDate(dateString) {
        // Format: "DD.MM.YYYY" zu ISO-Format
        const parts = dateString.split('.');
        if (parts.length === 3) {
            return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
        }
        return dateString;
    }

    validateConfig(config) {
        if (!config.nachname || !config.vorname || !config.klasse) {
            this.showError('Bitte alle Pflichtfelder ausfüllen!');
            return false;
        }

        if (!config.zeitraeume || config.zeitraeume.length === 0) {
            this.showError('Bitte mindestens einen Zeitraum hinzufügen!');
            return false;
        }

        return true;
    }

    async createDocument(wochenData) {
        try {
            // PizZip und Docxtemplater verwenden
            const zip = new PizZip(this.template);
            const doc = new Docxtemplater(zip, {
                paragraphLoop: true,
                linebreaks: true,
                delimiters: {
                    start: '{{',
                    end: '}}'
                }
            });

            // Template-Daten setzen
            doc.render(wochenData.templateData);

            // Dokument als ArrayBuffer generieren
            const output = doc.getZip().generate({
                type: 'arraybuffer',
                compression: 'DEFLATE',
            });

            return output;
        } catch (error) {
            console.error('❌ Docxtemplater Fehler:', error);
            if (error.properties) {
                console.error('Docxtemplater Details:', error.properties);
            }
            throw new Error(`Dokumenterstellung fehlgeschlagen: ${error.message}`);
        }
    }

    async createZipDownload(documents, metaData) {
        try {
            const zip = new JSZip();

            // Alle Dokumente zum ZIP hinzufügen
            documents.forEach(doc => {
                zip.file(doc.name, doc.content);
            });

            // ZIP generieren
            const zipContent = await zip.generateAsync({
                type: 'blob',
                compression: 'DEFLATE',
                compressionOptions: {
                    level: 6
                }
            });

            // Download starten
            const filename = `Wochennachweise_${metaData.nachname}_${new Date().toISOString().split('T')[0]}.zip`;
            this.downloadBlob(zipContent, filename);

        } catch (error) {
            throw new Error(`ZIP-Erstellung fehlgeschlagen: ${error.message}`);
        }
    }

    downloadBlob(blob, filename) {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }

    showDownloadButtons(documents) {
        // Container für Download-Buttons erstellen falls nicht vorhanden
        let container = document.getElementById('download-buttons');
        if (!container) {
            container = document.createElement('div');
            container.id = 'download-buttons';
            container.className = 'mt-4';
            container.innerHTML = '<h5>📁 Einzelne Dokumente herunterladen:</h5>';

            const mainContainer = document.querySelector('.container');
            if (mainContainer) {
                mainContainer.appendChild(container);
            }
        }

        // Buttons für jedes Dokument erstellen
        const buttonsDiv = document.createElement('div');
        buttonsDiv.className = 'row';

        documents.forEach(doc => {
            const buttonCol = document.createElement('div');
            buttonCol.className = 'col-md-3 mb-2';

            const button = document.createElement('button');
            button.className = 'btn btn-outline-primary btn-sm w-100';
            button.innerHTML = `<i class="bi bi-download"></i> Woche ${doc.woche.nummer}`;
            button.onclick = () => this.downloadSingleDocument(doc.woche);

            buttonCol.appendChild(button);
            buttonsDiv.appendChild(buttonCol);
        });

        container.appendChild(buttonsDiv);
    }

    showProgress(message) {
        let progressDiv = document.getElementById('generation-progress');
        if (!progressDiv) {
            progressDiv = document.createElement('div');
            progressDiv.id = 'generation-progress';
            progressDiv.className = 'alert alert-info sticky-top';
            progressDiv.style.zIndex = '1050';
            const container = document.querySelector('.container');
            if (container) {
                container.prepend(progressDiv);
            }
        }

        progressDiv.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="spinner-border spinner-border-sm me-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <span id="progress-message">${message}</span>
            </div>
        `;
    }

    showSuccess(message) {
        let progressDiv = document.getElementById('generation-progress');
        if (progressDiv) {
            progressDiv.className = 'alert alert-success sticky-top';
            progressDiv.innerHTML = `
                <div class="d-flex align-items-center">
                    <i class="bi bi-check-circle me-2"></i>
                    <span>${message}</span>
                </div>
            `;
            setTimeout(() => this.hideProgress(), 5000);
        }
    }

    showError(message) {
        let progressDiv = document.getElementById('generation-progress');
        if (!progressDiv) {
            progressDiv = document.createElement('div');
            progressDiv.id = 'generation-progress';
            const container = document.querySelector('.container');
            if (container) {
                container.prepend(progressDiv);
            }
        }

        progressDiv.className = 'alert alert-danger sticky-top';
        progressDiv.style.zIndex = '1050';
        progressDiv.innerHTML = `
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    <span>${message}</span>
                </div>
                <button type="button" class="btn-close" onclick="wochennachweisGenerator.hideProgress()"></button>
            </div>
        `;
    }

    hideProgress() {
        const progressDiv = document.getElementById('generation-progress');
        if (progressDiv) {
            progressDiv.remove();
        }
    }
}

// Globale Instanz erstellen
const wochennachweisGenerator = new ClientWochennachweisGenerator();

// Bei Seitenload initialisieren
document.addEventListener('DOMContentLoaded', function () {
    console.log('🚀 Initialisiere Wochennachweis-Generator...');
    wochennachweisGenerator.initialize();

    // Event-Handler für das Generieren-Formular
    const generateForm = document.querySelector('form[asp-action="Generate"]');
    if (generateForm) {
        generateForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            console.log('📝 Starte Client-seitige Generierung...');
            await wochennachweisGenerator.generateDocuments();
        });
    }

    // Debug: Formular-Elemente prüfen
    console.log('🔍 Verfügbare Formular-Elemente:');
    console.log('- Umschulungsbeginn:', document.getElementById('Umschulungsbeginn'));
    console.log('- Nachname:', document.getElementById('Nachname'));
    console.log('- Vorname:', document.getElementById('Vorname'));
    console.log('- Klasse:', document.getElementById('Klasse'));
});