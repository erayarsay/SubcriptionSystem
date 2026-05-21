function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `lila-toast ${type}`;

    const icon = type === 'success' ? '✅' : '❌';

    toast.innerHTML = `
        <span class="toast-icon">${icon}</span>
        <span class="toast-message">${message}</span>
    `;

    container.appendChild(toast);

    setTimeout(() => toast.classList.add('show'), 100);
    
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 400);
    }, 4000);
}

function showSubDetail(subId) {
    // Burada AJAX ile abonelik ve transaction detaylarını çekip modalda göstereceğiz usta
    Swal.fire({
        title: 'Abonelik Detayları',
        html: `
            <div class="detail-list">
                <p><strong>Alım Tarihi:</strong> 01.05.2026</p>
                <p><strong>Bitiş Tarihi:</strong> 31.05.2026</p>
                <p><strong>Harcanan:</strong> 150 ₺</p>
                <hr>
                <p class="text-lila"><strong>Kalan Süre:</strong> 27 Gün 4 Saat</p>
            </div>
        `,
        confirmButtonColor: '#7048e8'
    });
}