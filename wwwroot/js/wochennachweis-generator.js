// Client-seitige Wochennachweis-Generierung - SEITENSPEZIFISCHE VERSION
class ClientWochennachweisGenerator {
    constructor() {
        this.template = null;
        this.isGenerating = false;
        this.isIndexPage = false;
        this.isResultPage = false;
    }

    async initialize() {
        // 🔧 SEITENERKENNUNG
        this.isIndexPage = document.getElementById('wochennachweis-form') !== null;
        this.isResultPage = window.location.pathname.includes('/Generate') || document.querySelector('.table') !== null;

        console.log('🔍 Seitenerkennung:');
        console.log('- Index-Seite:', this.isIndexPage ? '✅' : '❌');
        console.log('- Result-Seite:', this.isResultPage ? '✅' : '❌');

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

        // WICHTIG: Bibliotheken-Prüfung UNVERÄNDERT - NICHT MODIFIZIEREN!
        const missingLibs = [];

        // PizZip prüfen (auch bekannt als JSZip in manchen Versionen)
        if (typeof PizZip === 'undefined' && typeof JSZip === 'undefined') {
            missingLibs.push('PizZip/JSZip');
        }

        // Docxtemplater prüfen (verschiedene globale Namen möglich)
        if (typeof Docxtemplater === 'undefined' &&
            typeof docxtemplater === 'undefined' &&
            typeof window.docxtemplater === 'undefined') {
            missingLibs.push('Docxtemplater');
        }

        // JSZip für Archive prüfen (separat von PizZip)
        if (typeof JSZip === 'undefined') {
            missingLibs.push('JSZip (für Archive)');
        }

        if (missingLibs.length > 0) {
            const errorMessage = `⚠️ Folgende Bibliotheken sind nicht verfügbar: ${missingLibs.join(', ')}.
        <br><small>Das bedeutet nicht zwingend einen Fehler - prüfen Sie die Funktionalität mit dem Test-Button.</small>
        <button class="btn btn-sm btn-primary mt-2" onclick="window.location.reload()">
            <i class="bi bi-arrow-clockwise me-1"></i>Seite neu laden
        </button>`;
            this.showError(errorMessage);
        }

        // Erweiterte Debug-Info
        console.log('📚 Bibliothek-Status:');
        console.log('- PizZip:', typeof PizZip !== 'undefined' ? '✅ Verfügbar' : '❌ Nicht geladen');
        console.log('- Docxtemplater (Global):', typeof Docxtemplater !== 'undefined' ? '✅ Verfügbar' : '❌ Nicht geladen');
        console.log('- docxtemplater (lowercase):', typeof docxtemplater !== 'undefined' ? '✅ Verfügbar' : '❌ Nicht geladen');
        console.log('- JSZip:', typeof JSZip !== 'undefined' ? '✅ Verfügbar' : '❌ Nicht geladen');

        return true;
    }

    // 🔧 NEUE METHODE: Prüfe Dependencies
    checkDependencies() {
        const hasPizZip = typeof PizZip !== 'undefined';
        const hasDocxtemplater = typeof docxtemplater !== 'undefined' || typeof Docxtemplater !== 'undefined';
        const hasJSZip = typeof JSZip !== 'undefined';

        if (!hasPizZip || !hasDocxtemplater || !hasJSZip) {
            this.showError('Benötigte Libraries fehlen. Führen Sie npm run copy-libs aus.');
            return false;
        }
        return true;
    }

    async downloadSingleDocument(woche) {
        try {
            this.showProgress(`📄 Erstelle Dokument für Woche ${woche.nummer || woche.Nummer}...`);

            if (!this.checkDependencies()) return;

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

            // VERBESSERTE DATEINAMENS-LOGIK
            const wocheNummer = woche.nummer || woche.Nummer || 'Unbekannt';
            const monat = this.getMonatFromTemplateData(woche);
            const kategorie = woche.kategorie || woche.Kategorie || 'Sonstige';
            const filename = `Wochennachweis_${String(wocheNummer).padStart(2, '0')}_${monat}_${kategorie}.docx`;

            this.downloadBlob(new Blob([document]), filename);

            this.showSuccess(`✅ Dokument für Woche ${wocheNummer} erstellt!`);
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

        // 🔧 NUR AUF INDEX-SEITE ERLAUBT
        if (!this.isIndexPage) {
            console.warn('⚠️ generateDocuments() nur auf Index-Seite verfügbar');
            return;
        }

        this.isGenerating = true;

        try {
            if (!this.checkDependencies()) return;

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

            // Flexible Property-Zugriffe für verschiedene Formate
            const wochenArray = this.getWochenArray(wochennachweisData);

            for (let i = 0; i < wochenArray.length; i++) {
                const woche = wochenArray[i];
                const wocheNummer = woche.nummer || woche.Nummer || 'Unbekannt';
                this.showProgress(`📝 Erstelle Dokument ${i + 1}/${wochenArray.length} (Woche ${wocheNummer})...`);

                try {
                    const document = await this.createDocument(woche);

                    // VERBESSERTE DATEINAMENS-LOGIK
                    const monat = this.getMonatFromTemplateData(woche);
                    const kategorie = woche.kategorie || woche.Kategorie || 'Sonstige';
                    const filename = `Wochennachweis_${String(wocheNummer).padStart(2, '0')}_${monat}_${kategorie}.docx`;

                    documents.push({
                        name: filename,
                        content: document,
                        woche: woche,
                        kategorie: kategorie // Für Ordnerstruktur
                    });
                } catch (docError) {
                    console.error(`❌ Fehler bei Dokument ${i + 1}:`, docError);
                    this.showError(`Fehler bei Woche ${wocheNummer}: ${docError.message}`);
                }
            }

            if (documents.length === 0) {
                throw new Error('Keine Dokumente konnten erstellt werden');
            }

            // 5. ZIP erstellen und downloaden
            this.showProgress('📦 Erstelle ZIP-Archiv mit Unterordnern...');
            await this.createZipDownloadWithFolders(documents, wochennachweisData);

            this.showSuccess(`✅ ${documents.length} Dokumente erfolgreich erstellt und als ZIP heruntergeladen!`);

            // Zusätzlich Einzeldownload-Buttons anzeigen
            this.showDownloadButtons(documents);

        } catch (error) {
            console.error('❌ Fehler bei der Generierung:', error);
            this.showError('Fehler bei der Generierung: ' + error.message);
        } finally {
            this.isGenerating = false;
        }
    }

    // HILFSMETHODE: Monat aus Template-Daten extrahieren
    getMonatFromTemplateData(woche) {
        const templateData = woche.templateData || woche.TemplateData || {};

        // Prüfe verschiedene mögliche Monat-Felder
        if (templateData.MONAT) return templateData.MONAT;
        if (templateData.monat) return templateData.monat;

        // Fallback: Aus Datum extrahieren
        const datum = woche.montag || woche.Montag;
        if (datum) {
            const monatsnamen = ['', 'Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
                'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
            const datumObj = new Date(datum);
            return monatsnamen[datumObj.getMonth() + 1] || 'Unbekannt';
        }

        return 'Unbekannt';
    }

    // HILFSMETHODE: Flexible Zugriffe auf Wochen-Array
    getWochenArray(wochennachweisData) {
        if (!wochennachweisData) {
            return [];
        }

        // Versuche verschiedene Property-Namen
        return wochennachweisData.wochen ||
            wochennachweisData.Wochen ||
            wochennachweisData.WOCHEN ||
            [];
    }

    getConfigFromForm() {
        // 🔧 NUR AUF INDEX-SEITE VERFÜGBAR
        if (!this.isIndexPage) {
            console.warn('⚠️ getConfigFromForm() nur auf Index-Seite verfügbar');
            return {};
        }

        return {
            umschulungsbeginn: document.getElementById('Umschulungsbeginn')?.value || '',
            umschulungsEnde: document.getElementById('UmschulungsEnde')?.value || '',
            nachname: document.getElementById('Nachname')?.value || '',
            vorname: document.getElementById('Vorname')?.value || '',
            klasse: document.getElementById('Klasse')?.value || '',
            bundesland: document.getElementById('Bundesland')?.value || 'DE-NW',
            zeitraeume: this.getZeitraeume()
        };
    }

    getZeitraeume() {
        // 🔧 NUR AUF INDEX-SEITE VERFÜGBAR
        if (!this.isIndexPage) {
            return [];
        }

        const zeitraeume = [];
        const tableRows = document.querySelectorAll('#zeitraeume-tbody tr[data-index]');

        tableRows.forEach(row => {
            const cells = row.querySelectorAll('td');
            if (cells.length >= 4) {
                // Badge-Text extrahieren
                const kategorieBadge = cells[0].querySelector('.badge');
                const kategorie = kategorieBadge ? kategorieBadge.textContent.trim().replace(/.*\s/, '') : cells[0].textContent.trim();

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

        // Zeiträume sind optional - Fallback wird verwendet
        return true;
    }

    async testDocxtemplater() {
        try {
            this.showProgress('🔧 Teste Docxtemplater...');

            if (!this.checkDependencies()) return false;

            // 1. Bibliotheken prüfen - UNVERÄNDERT
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
                // 3. Docxtemplater verwenden - UNVERÄNDERT
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
            // 1. PizZip-Instanz mit dem Template erstellen - UNVERÄNDERT
            const zip = new PizZip(this.template);

            // 2. Docxtemplater initialisieren - UNVERÄNDERT
            const doc = new docxtemplater();
            doc.loadZip(zip);

            // 3. Optionen setzen - UNVERÄNDERT
            doc.setOptions({
                paragraphLoop: true,
                linebreaks: true,
                delimiters: {
                    start: '{{',
                    end: '}}'
                }
            });

            // 4. Daten setzen - Flexible Property-Zugriffe
            const templateData = wochenData.templateData || wochenData.TemplateData || {};
            doc.setData(templateData);

            // 5. Template rendern - UNVERÄNDERT
            doc.render();

            // 6. Output generieren - UNVERÄNDERT
            const output = doc.getZip().generate({
                type: 'arraybuffer',
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

    // 🔥 NEUE METHODE: ZIP MIT UNTERORDNERN ERSTELLEN
    async createZipDownloadWithFolders(documents, metaData) {
        try {
            if (!this.checkDependencies()) return;

            const zip = new JSZip();

            // Dokumente nach Kategorien sortieren
            const kategorien = {
                'Praktikum': [],
                'Umschulung': [],
                'Sonstige': []
            };

            documents.forEach(doc => {
                const kategorie = doc.kategorie || 'Sonstige';
                if (kategorien[kategorie]) {
                    kategorien[kategorie].push(doc);
                } else {
                    kategorien['Sonstige'].push(doc);
                }
            });

            // Für jede Kategorie einen Ordner erstellen
            Object.keys(kategorien).forEach(kategorie => {
                const docs = kategorien[kategorie];
                if (docs.length > 0) {
                    console.log(`📁 Erstelle Ordner '${kategorie}' mit ${docs.length} Dokumenten`);

                    // Ordner explizit erstellen (wichtig für manche JSZip-Versionen)
                    zip.folder(kategorie);

                    // Dokumente in den entsprechenden Ordner legen
                    docs.forEach(doc => {
                        zip.file(`${kategorie}/${doc.name}`, doc.content);
                    });
                }
            });

            // README.txt in Root des ZIPs
            const nachname = metaData.nachname || metaData.Nachname || 'Unbekannt';
            const vorname = metaData.vorname || metaData.Vorname || 'Unbekannt';
            const klasse = metaData.klasse || metaData.Klasse || 'Unbekannt';

            const readme = `Wochennachweise - ${nachname}, ${vorname}
${'='.repeat(50)}

Erstellt am: ${new Date().toLocaleString('de-DE')}
Anzahl Dokumente: ${documents.length}
Name: ${nachname}, ${vorname}
Klasse: ${klasse}

Ordnerstruktur:
${Object.keys(kategorien).map(kat => {
                const count = kategorien[kat].length;
                return count > 0 ? `📁 ${kat}/ (${count} Dokumente)` : null;
            }).filter(Boolean).join('\n')}

Dokumente nach Kategorien:
${Object.keys(kategorien).map(kat => {
                const docs = kategorien[kat];
                if (docs.length === 0) return null;
                return `\n📂 ${kat}:\n${docs.map(doc => `   📄 ${doc.name}`).join('\n')}`;
            }).filter(Boolean).join('')}

Hinweise:
- Diese Dokumente wurden client-seitig generiert
- Alle Daten wurden lokal in Ihrem Browser verarbeitet
- Keine personenbezogenen Daten wurden an externe Server übertragen
- Bei Fragen zur Verwendung siehe Template-Hilfe im Generator

Generator-Version: ${new Date().getFullYear()}.${new Date().getMonth() + 1}
Erstellt mit: ASP.NET Core Wochennachweis-Generator
`;
            zip.file('README.txt', readme);

            // Zusätzliche Metadaten-Datei
            const metadataJson = {
                erstellt: new Date().toISOString(),
                person: {
                    nachname: nachname,
                    vorname: vorname,
                    klasse: klasse
                },
                statistik: {
                    gesamtDokumente: documents.length,
                    praktikum: kategorien['Praktikum'].length,
                    umschulung: kategorien['Umschulung'].length,
                    sonstige: kategorien['Sonstige'].length
                },
                kategorien: Object.keys(kategorien).reduce((acc, kat) => {
                    if (kategorien[kat].length > 0) {
                        acc[kat] = kategorien[kat].map(doc => ({
                            dateiname: doc.name,
                            wochenNummer: doc.woche.nummer || doc.woche.Nummer
                        }));
                    }
                    return acc;
                }, {})
            };
            zip.file('metadata.json', JSON.stringify(metadataJson, null, 2));

            // ZIP generieren
            console.log('📦 Generiere ZIP-Archiv mit Ordnerstruktur...');
            const zipContent = await zip.generateAsync({
                type: 'blob',
                compression: 'DEFLATE',
                compressionOptions: {
                    level: 6
                }
            });

            // Download starten
            const filename = `Wochennachweise_${nachname}_${new Date().toISOString().split('T')[0]}.zip`;
            this.downloadBlob(zipContent, filename);

            console.log('✅ ZIP-Download mit Ordnerstruktur gestartet:', filename);

        } catch (error) {
            console.error('❌ ZIP-Fehler:', error);
            throw new Error(`ZIP-Erstellung fehlgeschlagen: ${error.message}`);
        }
    }

    downloadBlob(blob, filename) {
        try {
            const link = document.createElement('a');
            const url = URL.createObjectURL(blob);

            link.href = url;
            link.download = filename;
            link.style.display = 'none';

            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            // URL nach kurzer Zeit freigeben
            setTimeout(() => {
                URL.revokeObjectURL(url);
            }, 1000);

            console.log('💾 Download gestartet:', filename);

        } catch (error) {
            console.error('❌ Download-Fehler:', error);
            throw new Error(`Download fehlgeschlagen: ${error.message}`);
        }
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

        // Buttons für jedes Dokument erstellen, sortiert nach Kategorie
        const buttonsDiv = document.createElement('div');
        buttonsDiv.className = 'row';

        // Gruppierung nach Kategorien
        const kategorien = {};
        documents.forEach(doc => {
            const kat = doc.kategorie || 'Sonstige';
            if (!kategorien[kat]) kategorien[kat] = [];
            kategorien[kat].push(doc);
        });

        // Für jede Kategorie eine Spalte
        Object.keys(kategorien).forEach(kategorie => {
            const docs = kategorien[kategorie];
            if (docs.length === 0) return;

            const katCol = document.createElement('div');
            katCol.className = 'col-md-6 mb-3';
            katCol.innerHTML = `<h6><i class="bi bi-${kategorie === 'Praktikum' ? 'building' : 'mortarboard'} me-1"></i>${kategorie} (${docs.length})</h6>`;

            docs.forEach(doc => {
                const wocheNummer = doc.woche.nummer || doc.woche.Nummer || 'X';
                const button = document.createElement('button');
                button.className = 'btn btn-outline-primary btn-sm me-1 mb-1';
                button.innerHTML = `<i class="bi bi-download"></i> Woche ${wocheNummer}`;
                button.onclick = () => this.downloadSingleDocument(doc.woche);
                katCol.appendChild(button);
            });

            buttonsDiv.appendChild(katCol);
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
            setTimeout(() => this.hideProgress(), 10000);
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

// Globale Instanz erstellen - UNVERÄNDERT
const wochennachweisGenerator = new ClientWochennachweisGenerator();

// Bei Seitenload initialisieren - VERBESSERT
document.addEventListener('DOMContentLoaded', function () {
    console.log('🚀 Initialisiere erweiterten Wochennachweis-Generator...');
    wochennachweisGenerator.initialize();

    // Event-Handler für das Generieren-Formular (nur auf Index-Seite)
    const generateForm = document.querySelector('form[action="/Home/Generate"]');
    if (generateForm && wochennachweisGenerator.isIndexPage) {
        generateForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            console.log('📝 Starte Client-seitige Generierung mit ZIP-Unterordnern...');
            await wochennachweisGenerator.generateDocuments();
        });
    }

    // Event-Handler für den Test-Button (alle Seiten)
    const testButton = document.getElementById('test-docxtemplater-button');
    if (testButton) {
        testButton.addEventListener('click', async function () {
            console.log('🔧 Starte Docxtemplater-Test...');
            await wochennachweisGenerator.testDocxtemplater();
        });
    }

    // Debug: Formular-Elemente prüfen (nur auf Index-Seite)
    if (wochennachweisGenerator.isIndexPage) {
        console.log('🔍 Verfügbare Formular-Elemente:');
        ['Umschulungsbeginn', 'UmschulungsEnde', 'Nachname', 'Vorname', 'Klasse', 'Bundesland'].forEach(id => {
            const el = document.getElementById(id);
            console.log(`- ${id}:`, el ? '✅ Gefunden' : '❌ Nicht gefunden');
        });
    } else {
        console.log('📋 Result-Seite: Formular-Elemente nicht erwartet');
    }

    console.log('✅ Erweiterte Generator-Initialisierung abgeschlossen');
});