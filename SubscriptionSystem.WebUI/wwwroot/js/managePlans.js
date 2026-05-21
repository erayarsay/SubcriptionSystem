function openPlanModal() {
    const modal = document.getElementById('planModal');
    if(modal) {
        modal.style.display = 'flex';
    }
}

function closePlanModal() {
    const modal = document.getElementById('planModal');
    if(modal) {
        modal.style.display = 'none';
    }
}

// Modal dışına tıklandığında kapatma
window.onclick = function(event) {
    const modal = document.getElementById('planModal');
    if (event.target == modal) {
        closePlanModal();
    }
}

window.editPlan = function (id, title, subTitle, price, duration, desc, isPopular, orderIndex) {
    // Modal Başlığı
    document.querySelector('.modal-header h3').innerText = "Paketi Düzenle";
    
    const form = document.querySelector('#planModal form');
    form.action = "/Home/UpdatePlan";

    // ID kontrolü ve ataması
    let idInput = form.querySelector('input[name="Id"]');
    if (!idInput) {
        idInput = document.createElement('input');
        idInput.type = 'hidden';
        idInput.name = 'Id';
        form.appendChild(idInput);
    }
    idInput.value = id;

    // Diğer alanları doldur
    form.querySelector('input[name="Title"]').value = title;
    form.querySelector('input[name="SubTitle"]').value = subTitle; // Yeni alan
    form.querySelector('input[name="Price"]').value = price.replace(',', '.');
    form.querySelector('input[name="DuraitonInMonths"]').value = duration;
    form.querySelector('textarea[name="Description"]').value = desc;
    form.querySelector('input[name="IsPopular"]').checked = (isPopular === 'True');
    
    // OrderIndex'i de saklayalım ki düzenleyince sırası bozulmasın
    let orderInput = form.querySelector('input[name="OrderIndex"]');
    if (!orderInput) {
        orderInput = document.createElement('input');
        orderInput.type = 'hidden';
        orderInput.name = 'OrderIndex';
        form.appendChild(orderInput);
    }
    orderInput.value = orderIndex;

    openPlanModal();
}

let currentDeleteForm = null;

// Silme butonuna basınca bu çalışacak
window.askConfirm = function(button, title, message) {
    currentDeleteForm = button.closest('form'); // Hangi formun silme yapacağını yakala
    
    document.getElementById('confirmTitle').innerText = title;
    document.getElementById('confirmMessage').innerText = message;
    document.getElementById('confirmModal').style.display = 'flex';
}

window.closeConfirmModal = function() {
    document.getElementById('confirmModal').style.display = 'none';
}

// "Evet, Sil" butonuna basınca gerçek formu gönder
document.getElementById('confirmBtn').onclick = function() {
    if (currentDeleteForm) {
        currentDeleteForm.submit();
    }
}

function askConfirm(button, title, message) {
    const modal = document.getElementById('confirmModal');
    if (!modal) return;

    // Başlık ve mesajı doldur
    document.getElementById('confirmTitle').innerText = title;
    document.getElementById('confirmMessage').innerText = message;

    // Modal'ı göster
    modal.style.display = 'flex';

    // Onay butonuna basınca ne olacağını ayarla
    const confirmBtn = document.getElementById('confirmBtn');
    
    // Önceki eventleri temizle (üst üste binmesin usta)
    confirmBtn.onclick = null; 

    confirmBtn.onclick = function() {
        // Butonun içindeki formu bul ve gönder
        const form = button.closest('form');
        if (form) {
            form.submit();
        }
        closeConfirmModal();
    };
}

function closeConfirmModal() {
    document.getElementById('confirmModal').style.display = 'none';
}