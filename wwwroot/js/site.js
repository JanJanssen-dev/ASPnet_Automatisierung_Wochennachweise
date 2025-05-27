// Site-wide JavaScript functionality

document.addEventListener('DOMContentLoaded', function () {
    console.log('🌐 Site.js geladen');

    // Bootstrap tooltips initialisieren falls vorhanden
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Auto-dismiss alerts nach 5 Sekunden
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = new bootstrap.Alert(alert);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 5000);
    });

    // Form validation enhancement
    const forms = document.querySelectorAll('form');
    forms.forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });

    // Loading states für Buttons
    const submitButtons = document.querySelectorAll('button[type="submit"]');
    submitButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            const form = button.closest('form');
            if (form && form.checkValidity()) {
                button.classList.add('btn-loading');
                button.disabled = true;

                // Nach 10 Sekunden wieder aktivieren falls etwas schief geht
                setTimeout(function () {
                    button.classList.remove('btn-loading');
                    button.disabled = false;
                }, 10000);
            }
        });
    });
});

// Utility Funktionen
window.showNotification = function (message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(alertDiv);

    // Auto-remove nach 5 Sekunden
    setTimeout(function () {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
};

// Error handling für globale Fehler
window.addEventListener('error', function (e) {
    console.error('JavaScript Fehler:', e.error);
    if (window.location.hostname === 'localhost') {
        showNotification('JavaScript Fehler: ' + e.message, 'danger');
    }
});

// Debug Funktionen für Development
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    window.debugSite = function () {
        console.log('🔍 Site Debug Info:');
        console.log('- Bootstrap:', typeof bootstrap !== 'undefined' ? '✅' : '❌');
        console.log('- jQuery:', typeof $ !== 'undefined' ? '✅' : '❌');
        console.log('- Current Page:', window.location.pathname);
        console.log('- User Agent:', navigator.userAgent);
    };
}