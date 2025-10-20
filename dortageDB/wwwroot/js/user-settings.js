// dortageDB/wwwroot/js/user-settings.js

document.addEventListener('DOMContentLoaded', function () {
    console.log('✅ User Settings initialized');

    initializeTabs();
    initializePasswordToggles();
    initializePasswordStrength();
    initializePhoneFormatting();
    initializeTcNoFormatting();
    initializeFormSubmission();
    initializeToastAutoHide();
});

function initializeTabs() {
    const tabButtons = document.querySelectorAll('.tabs li');
    const tabContents = document.querySelectorAll('.tab-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            const tabId = this.getAttribute('data-tab');

            console.log('Tab değiştiriliyor:', tabId);

            // Remove active class from all tabs
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabContents.forEach(content => content.classList.remove('active'));

            // Add active class to clicked tab
            this.classList.add('active');
            const targetTab = document.getElementById(tabId + '-tab');
            if (targetTab) {
                targetTab.classList.add('active');
            }
        });
    });
}

function initializePasswordToggles() {
    const toggleButtons = document.querySelectorAll('.show-hide');

    toggleButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);

            if (input) {
                const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
                input.setAttribute('type', type);
                this.textContent = type === 'password' ? '👁️' : '🙈';
            }
        });
    });
}

function initializePasswordStrength() {
    const newPasswordInput = document.getElementById('newPasswordInput');
    if (!newPasswordInput) return;

    newPasswordInput.addEventListener('input', function () {
        const password = this.value;
        const strengthBar = document.getElementById('strengthBar');
        const requirements = document.querySelectorAll('.requirements li');

        if (!strengthBar || requirements.length === 0) return;

        let strength = 0;

        // Check length (6+ characters)
        if (password.length >= 6) {
            requirements[0].classList.add('valid');
            strength += 25;
        } else {
            requirements[0].classList.remove('valid');
        }

        // Check uppercase
        if (/[A-Z]/.test(password)) {
            requirements[1].classList.add('valid');
            strength += 25;
        } else {
            requirements[1].classList.remove('valid');
        }

        // Check number
        if (/[0-9]/.test(password)) {
            requirements[2].classList.add('valid');
            strength += 25;
        } else {
            requirements[2].classList.remove('valid');
        }

        // Check special character
        if (/[!+@#$%^&*(),.?":{}|<>+\-_=\[\]\\\/;'`~]/.test(password)) {
            requirements[3].classList.add('valid');
            strength += 25;
        } else {
            requirements[3].classList.remove('valid');
        }

        // Update strength bar
        strengthBar.style.width = strength + '%';

        if (strength <= 25) {
            strengthBar.style.background = '#ef4444';
        } else if (strength <= 50) {
            strengthBar.style.background = '#f59e0b';
        } else if (strength <= 75) {
            strengthBar.style.background = '#10b981';
        } else {
            strengthBar.style.background = '#059669';
        }
    });
}

function initializePhoneFormatting() {
    const phoneInput = document.getElementById('phoneInput');
    if (!phoneInput) return;

    phoneInput.addEventListener('input', function (e) {
        let value = e.target.value.replace(/\D/g, '');
        if (value.length > 10) value = value.slice(0, 10);

        if (value.length > 0) {
            if (value.length <= 3) {
                value = '(' + value;
            } else if (value.length <= 6) {
                value = '(' + value.slice(0, 3) + ') ' + value.slice(3);
            } else if (value.length <= 8) {
                value = '(' + value.slice(0, 3) + ') ' + value.slice(3, 6) + ' ' + value.slice(6);
            } else {
                value = '(' + value.slice(0, 3) + ') ' + value.slice(3, 6) + ' ' + value.slice(6, 8) + ' ' + value.slice(8);
            }
        }

        e.target.value = value;
    });
}

function initializeTcNoFormatting() {
    const tcInput = document.getElementById('tcInput');
    if (!tcInput) return;

    tcInput.addEventListener('input', function (e) {
        let value = e.target.value.replace(/\D/g, '');
        if (value.length > 11) value = value.slice(0, 11);
        e.target.value = value;
    });
}

function initializeFormSubmission() {
    const profileForm = document.getElementById('profileForm');
    const securityForm = document.getElementById('securityForm');

    if (profileForm) {
        profileForm.addEventListener('submit', function (e) {
            console.log('📝 Profil formu gönderiliyor...');
            const saveBtn = document.getElementById('saveProfileBtn');
            if (saveBtn) {
                saveBtn.disabled = true;
                saveBtn.textContent = 'Kaydediliyor...';
            }
        });
    }

    if (securityForm) {
        securityForm.addEventListener('submit', function (e) {
            console.log('🔒 Şifre formu gönderiliyor...');

            const currentPassword = document.getElementById('currentPasswordInput').value;
            const newPassword = document.getElementById('newPasswordInput').value;
            const confirmPassword = document.getElementById('confirmPasswordInput').value;

            if (!currentPassword || !newPassword || !confirmPassword) {
                e.preventDefault();
                showToast('Lütfen tüm şifre alanlarını doldurun', 'error');
                return false;
            }

            if (newPassword !== confirmPassword) {
                e.preventDefault();
                showToast('Yeni şifreler eşleşmiyor', 'error');
                return false;
            }

            const saveBtn = document.getElementById('savePasswordBtn');
            if (saveBtn) {
                saveBtn.disabled = true;
                saveBtn.textContent = 'Değiştiriliyor...';
            }
        });
    }
}

function initializeToastAutoHide() {
    const toasts = document.querySelectorAll('.toast.show');
    toasts.forEach(toast => {
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                toast.style.display = 'none';
            }, 300);
        }, 3000);
    });
}

function showToast(message, type = 'success') {
    const toast = document.getElementById('toast');
    if (!toast) return;

    toast.textContent = message;
    toast.className = 'toast ' + type;
    toast.classList.add('show');
    toast.style.display = 'block';

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            toast.style.display = 'none';
        }, 300);
    }, 3000);
}

console.log('✅ User Settings JavaScript loaded successfully');