// Client-seitige Wochennachweis-Generierung
class ClientWochennachweisGenerator {
    constructor() {
        this.template = null;
        this.isGenerating = false;
    }

    // In der initialize-Methode der ClientWochennachweisGenerator-Klasse
    async initialize() {
        try {
            // Template beim Laden der Seite vorab herunterladen
            const response = await fetch('/api/wochennachweis/template');
            if (response.ok) {
                this.template = await response.arrayBuffer();
                console.log('✅ Template erfolgreich geladen');
            } else {
                console.warn('⚠️ Template konnte nicht geladen werden:', await response.text());
            }
        } catch (error) {
            console.error('❌ Fehler beim Vorab-Laden des Templates:', error);
        }

        // Verbesserte Bibliotheken-Prüfung
        const missingLibs = [];
        if (typeof PizZip === 'undefined') missingLibs.push('PizZip');
        if (typeof Docxtemplater === 'undefined' && typeof docxtemplater === 'undefined') missingLibs.push('Docxtemplater');
        if (typeof JSZip === 'undefined') missingLibs.push('JSZip');

        if (missingLibs.length > 0) {
            const errorMessage = `Folgende benötigte Bibliotheken konnten nicht geladen werden: ${missingLibs.join(', ')}.
            <button class="btn btn-sm btn-primary mt-2" onclick="window.location.reload()">
                <i class="bi bi-arrow-clockwise me-1"></i>Seite neu laden
            </button>`;
            this.showError(errorMessage);
            return false;
        }

        return true;
    }

    async downloadSingleDocument(woche) {
        try {
            this.showProgress(`📄 Erstelle Dokument für Woche ${woche.nummer}...`);

            // Wenn das Template noch nicht geladen ist
            if (!this.template) {
                this.showProgress('📄 Lade Word-Template...');
                const templateResponse = await fetch('/api/wochennachweis/template');
                if (!templateResponse.ok) {
                    throw new Error('Template konnte nicht geladen werden');
                }
                this.template = await templateResponse.arrayBuffer();
            }

            // Dokument erstellen
            const document = await this.createDocument(woche);

            // Dateinamen erstellen und herunterladen
            const filename = `Wochennachweis_Woche_${String(woche.nummer).padStart(2, '0')}_${woche.kategorie}.docx`;
            this.downloadBlob(document, filename);

            this.showSuccess(`✅ Dokument für Woche ${woche.nummer} erstellt!`);
        } catch (error) {
            console.error('❌ Fehler beim Erstellen des Dokuments:', error);
            this.showError(`Fehler: ${error.message}`);
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
    async testDocxtemplater() {
        try {
            this.showProgress('🔧 Teste Docxtemplater...');

            // 1. Bibliotheken prüfen
            if (typeof PizZip === 'undefined') {
                this.showError('PizZip-Bibliothek nicht geladen!');
                return false;
            }
            if (typeof docxtemplater === 'undefined') {
                this.showError('Docxtemplater-Bibliothek nicht geladen!');
                return false;
            }

            // 2. Vollständigeres Test-Dokument erstellen
            const zip = new PizZip();

            // Minimale Word-Dateistruktur
            zip.file("word/document.xml",
                `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                <w:body>
                    <w:p>
                        <w:r>
                            <w:t>{{name}}</w:t>
                        </w:r>
                    </w:p>
                </w:body>
            </w:document>`
            );

            zip.file("[Content_Types].xml",
                `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                <Default Extension="xml" ContentType="application/xml"/>
                <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            </Types>`
            );

            zip.file("_rels/.rels",
                `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
            </Relationships>`
            );

            zip.file("word/_rels/document.xml.rels",
                `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            </Relationships>`
            );

            try {
                // 3. Docxtemplater verwenden
                const doc = new docxtemplater();
                doc.loadZip(zip);
                doc.setOptions({
                    paragraphLoop: true,
                    linebreaks: true,
                    delimiters: { start: '{{', end: '}}' }
                });

                // Testdaten setzen
                doc.setData({
                    name: 'Docxtemplater Test erfolgreich! Aktuelle Zeit: ' + new Date().toLocaleTimeString()
                });

                // Rendern
                doc.render();

                // Als Blob generieren
                const out = doc.getZip().generate({ type: 'blob' });

                // Erfolgsmeldung und Download
                this.showSuccess('✅ Docxtemplater funktioniert! Das Test-Dokument wird heruntergeladen.');
                this.downloadBlob(out, 'docxtemplater-test.docx');
                return true;
            } catch (innerError) {
                console.error('Fehler beim Erstellen des Test-Dokuments:', innerError);
                this.showError(`Fehler im Docxtemplater: ${innerError.message}`);
                return false;
            }
        } catch (error) {
            console.error('Docxtemplater Test fehlgeschlagen:', error);
            this.showError(`Test fehlgeschlagen: ${error.message}`);
            return false;
        }
    }


    async createDocument(wochenData) {
        try {
            // 1. PizZip-Instanz mit dem Template erstellen
            const zip = new PizZip(this.template);

            // 2. Docxtemplater initialisieren
            const doc = new docxtemplater();
            doc.loadZip(zip);

            // 3. Optionen setzen - WICHTIG: überprüfe deine Template-Delimiters!
            doc.setOptions({
                paragraphLoop: true,
                linebreaks: true,
                delimiters: {
                    start: '{{',  // Stellen Sie sicher, dass diese mit Ihrem Template übereinstimmen
                    end: '}}'
                }
            });

            // 4. Daten setzen
            doc.setData(wochenData.templateData);

            // 5. Template rendern
            doc.render();

            // 6. Output generieren
            const output = doc.getZip().generate({
                type: 'arraybuffer', // Wichtig für den Download
                mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                compression: 'DEFLATE'
            });

            return output;
        } catch (error) {
            console.error('❌ Docxtemplater Fehler:', error);

            // Detaillierte Fehlerinformationen zeigen
            if (error.properties && error.properties.errors) {
                error.properties.errors.forEach(err => {
                    console.log('Fehler bei Tag:', err.properties.tag);
                    console.log('Kontext:', err.properties.context);
                    console.log('Datei:', err.properties.file);
                    console.log('Erklärung:', err.properties.explanation);
                });
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
    // NEUER CODE: Event-Handler für den Test-Button
    const testButton = document.getElementById('test-docxtemplater-button');
    if (testButton) {
        testButton.addEventListener('click', async function () {
            console.log('🔧 Starte Docxtemplater-Test...');
            await wochennachweisGenerator.testDocxtemplater();
        });
    }

    // Debug: Formular-Elemente prüfen
    console.log('🔍 Verfügbare Formular-Elemente:');
    console.log('- Umschulungsbeginn:', document.getElementById('Umschulungsbeginn'));
    console.log('- Nachname:', document.getElementById('Nachname'));
    console.log('- Vorname:', document.getElementById('Vorname'));
    console.log('- Klasse:', document.getElementById('Klasse'));
});