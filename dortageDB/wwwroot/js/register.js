document.addEventListener('DOMContentLoaded', function () {
    initializeForm();
});

function initializeForm() {
    attachEventListeners();
}

function attachEventListeners() {
    const form = document.getElementById('registrationForm');

    // TC No validation
    const tcNoInput = document.getElementById('TcNo');
    if (tcNoInput) {
        tcNoInput.addEventListener('input', handleTcNoInput);
        tcNoInput.addEventListener('blur', handleTcNoBlur);
    }

    // Email validation
    const emailInput = document.getElementById('Email');
    if (emailInput) {
        emailInput.addEventListener('blur', handleEmailBlur);
        emailInput.addEventListener('input', handleEmailInput);
    }

    // Phone formatting
    const phoneInput = document.getElementById('PhoneNumber');
    if (phoneInput) {
        phoneInput.addEventListener('input', handlePhoneInput);
        phoneInput.addEventListener('blur', handlePhoneBlur);
    }

    // Password strength
    const passwordInput = document.getElementById('Password');
    if (passwordInput) {
        passwordInput.addEventListener('input', handlePasswordInput);
    }

    // Password toggle
    const passwordToggle = document.getElementById('passwordToggle');
    if (passwordToggle && passwordInput) {
        passwordToggle.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            passwordInput.type = passwordInput.type === 'password' ? 'text' : 'password';
        });
    }

    // Confirm password
    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', handleConfirmPasswordInput);
        confirmPasswordInput.addEventListener('blur', handleConfirmPasswordBlur);
    }

    // Referral code
    const referralInput = document.getElementById('Code');
    if (referralInput) {
        referralInput.addEventListener('blur', handleReferralBlur);
        referralInput.addEventListener('input', function () {
            if (this.value.trim()) {
                hideError('Code', 'referralError');
            }
        });
    }

    // Submit button
    const submitBtn = document.getElementById('submitBtn');
    if (submitBtn) {
        submitBtn.addEventListener('click', handleSubmit);
    }

    // Field validations
    ['Ad', 'Soyad', 'Sehir'].forEach(field => {
        const input = document.getElementById(field);
        if (input) {
            input.addEventListener('blur', () => {
                const errorId = field === 'Ad' ? 'AdError' :
                    field === 'Soyad' ? 'SoyadError' : 'cityError';
                validateField(field, errorId);
            });
        }
    });

    // Checkboxes
    const pazarlamaCheckbox = document.getElementById('Pazarlama');
    if (pazarlamaCheckbox) {
        pazarlamaCheckbox.addEventListener('change', () => {
            if (pazarlamaCheckbox.checked) {
                document.getElementById('termsError').style.display = 'none';
            }
        });
    }

    const kvkkCheckbox = document.getElementById('Kvkk');
    if (kvkkCheckbox) {
        kvkkCheckbox.addEventListener('change', () => {
            if (kvkkCheckbox.checked) {
                document.getElementById('kvkkError').style.display = 'none';
            }
        });
    }

    // Focus first input
    const firstInput = document.getElementById('Ad');
    if (firstInput) firstInput.focus();
}

function validateField(fieldId, errorId) {
    const input = document.getElementById(fieldId);
    if (!input.value.trim()) {
        showError(fieldId, errorId);
    } else {
        hideError(fieldId, errorId);
    }
}

function handleEmailBlur() {
    const emailInput = document.getElementById('Email');
    if (isValidEmail(emailInput.value)) {
        hideError('Email', 'emailError');
    } else {
        showError('Email', 'emailError');
    }
}

function handleEmailInput() {
    const emailInput = document.getElementById('Email');
    const emailValidIcon = document.getElementById('emailValidIcon');

    if (isValidEmail(emailInput.value)) {
        if (emailValidIcon) {
            emailValidIcon.style.display = 'block';
        }
        emailInput.classList.add('success');
        emailInput.classList.remove('error');
        hideError('Email', 'emailError');
    } else {
        if (emailValidIcon) {
            emailValidIcon.style.display = 'none';
        }
        emailInput.classList.remove('success');
    }
}

function handleTcNoInput(e) {
    let value = e.target.value.replace(/\D/g, '');
    if (value.length > 11) value = value.slice(0, 11);
    e.target.value = value;
}

function handlePhoneInput(e) {
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
}

function handlePhoneBlur() {
    const phoneInput = document.getElementById('PhoneNumber');
    if (!isValidPhone(phoneInput.value)) {
        showError('PhoneNumber', 'phoneError');
    } else {
        hideError('PhoneNumber', 'phoneError');
    }
}

function handleTcNoBlur() {
    const tcNoInput = document.getElementById('TcNo');
    if (tcNoInput.value.trim() && !isValidTcNo(tcNoInput.value)) {
        showError('TcNo', 'tcNoError');
    } else {
        hideError('TcNo', 'tcNoError');
    }
}

function handlePasswordInput() {
    const passwordInput = document.getElementById('Password');
    const password = passwordInput.value;
    let strength = 0;

    // Length requirement
    const lengthReq = document.getElementById('req-length');
    if (lengthReq) {
        if (password.length >= 8) {
            lengthReq.classList.add('met');
            strength += 25;
        } else {
            lengthReq.classList.remove('met');
        }
    }

    // Uppercase requirement
    const uppercaseReq = document.getElementById('req-uppercase');
    if (uppercaseReq) {
        if (/[A-Z]/.test(password)) {
            uppercaseReq.classList.add('met');
            strength += 25;
        } else {
            uppercaseReq.classList.remove('met');
        }
    }

    // Number requirement
    const numberReq = document.getElementById('req-number');
    if (numberReq) {
        if (/[0-9]/.test(password)) {
            numberReq.classList.add('met');
            strength += 25;
        } else {
            numberReq.classList.remove('met');
        }
    }

    // Special character requirement
    const specialReq = document.getElementById('req-special');
    if (specialReq) {
        if (/[+!@#$%^&*(),.?":{}|<>]/.test(password)) {
            specialReq.classList.add('met');
            strength += 25;
        } else {
            specialReq.classList.remove('met');
        }
    }

    // Update strength bar
    const passwordStrengthBar = document.getElementById('passwordStrengthBar');
    if (passwordStrengthBar) {
        passwordStrengthBar.classList.remove('weak', 'medium', 'strong');

        if (password.length === 0) {
            passwordStrengthBar.style.width = '0%';
        } else if (strength <= 50) {
            passwordStrengthBar.classList.add('weak');
            passwordStrengthBar.style.width = '';
        } else if (strength <= 75) {
            passwordStrengthBar.classList.add('medium');
            passwordStrengthBar.style.width = '';
        } else {
            passwordStrengthBar.classList.add('strong');
            passwordStrengthBar.style.width = '';
        }
    }
}

function handleConfirmPasswordInput() {
    const passwordInput = document.getElementById('Password');
    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    const passwordMatchIcon = document.getElementById('passwordMatchIcon');

    if (confirmPasswordInput.value === passwordInput.value && confirmPasswordInput.value !== '') {
        if (passwordMatchIcon) {
            passwordMatchIcon.style.display = 'block';
        }
        confirmPasswordInput.classList.add('success');
        confirmPasswordInput.classList.remove('error');
        hideError('ConfirmPassword', 'confirmPasswordError');
    } else {
        if (passwordMatchIcon) {
            passwordMatchIcon.style.display = 'none';
        }
        confirmPasswordInput.classList.remove('success');
    }
}

function handleConfirmPasswordBlur() {
    const passwordInput = document.getElementById('Password');
    const confirmPasswordInput = document.getElementById('ConfirmPassword');

    if (confirmPasswordInput.value !== passwordInput.value && confirmPasswordInput.value !== '') {
        showError('ConfirmPassword', 'confirmPasswordError');
    }
}

function handleReferralBlur() {
    const referralInput = document.getElementById('Code');
    if (!referralInput.value.trim()) {
        showError('Code', 'referralError');
    } else if (referralInput.value.length < 6) {
        document.getElementById('referralError').innerHTML = '<span>⚠</span> Referans kodu en az 6 karakter olmalıdır';
        showError('Code', 'referralError');
    } else {
        hideError('Code', 'referralError');
    }
}

function handleSubmit(e) {
    e.preventDefault();

    console.log('=== FORM GÖNDERİLİYOR ===');

    if (validateForm()) {
        console.log('✅ Validation başarılı, form gönderiliyor...');
        submitForm();
    } else {
        console.log('❌ Validation başarısız');
    }
}

function validateForm() {
    let valid = true;

    // Ad
    const firstName = document.getElementById('Ad');
    if (!firstName.value.trim()) {
        showError('Ad', 'AdError');
        valid = false;
    } else {
        hideError('Ad', 'AdError');
    }

    // Soyad
    const lastName = document.getElementById('Soyad');
    if (!lastName.value.trim()) {
        showError('Soyad', 'SoyadError');
        valid = false;
    } else {
        hideError('Soyad', 'SoyadError');
    }

    // Email
    const email = document.getElementById('Email');
    if (!isValidEmail(email.value)) {
        showError('Email', 'emailError');
        valid = false;
    } else {
        hideError('Email', 'emailError');
    }

    // Phone
    const phone = document.getElementById('PhoneNumber');
    if (!isValidPhone(phone.value)) {
        showError('PhoneNumber', 'phoneError');
        valid = false;
    } else {
        hideError('PhoneNumber', 'phoneError');
    }

    // TC No (opsiyonel ama doldurulduysa geçerli olmalı)
    const tcNo = document.getElementById('TcNo');
    if (tcNo.value.trim() && !isValidTcNo(tcNo.value)) {
        showError('TcNo', 'tcNoError');
        valid = false;
    } else {
        hideError('TcNo', 'tcNoError');
    }

    // Şehir
    const city = document.getElementById('Sehir');
    if (!city.value) {
        showError('Sehir', 'cityError');
        valid = false;
    } else {
        hideError('Sehir', 'cityError');
    }

    // Şifre
    const password = document.getElementById('Password');
    if (!isStrongPassword(password.value)) {
        valid = false;
    }

    // Şifre tekrar
    const confirmPassword = document.getElementById('ConfirmPassword');
    if (password.value !== confirmPassword.value) {
        showError('ConfirmPassword', 'confirmPasswordError');
        valid = false;
    } else {
        hideError('ConfirmPassword', 'confirmPasswordError');
    }

    // Referans kodu
    const referralCode = document.getElementById('Code');
    if (!referralCode.value.trim()) {
        document.getElementById('referralError').innerHTML = '<span>⚠</span> Referans kodu gereklidir';
        showError('Code', 'referralError');
        valid = false;
    } else if (referralCode.value.length < 6) {
        document.getElementById('referralError').innerHTML = '<span>⚠</span> Referans kodu en az 6 karakter olmalıdır';
        showError('Code', 'referralError');
        valid = false;
    } else {
        hideError('Code', 'referralError');
    }

    // Kullanım koşulları (Pazarlama checkbox)
    const terms = document.getElementById('Pazarlama');
    if (!terms.checked) {
        document.getElementById('termsError').style.display = 'flex';
        valid = false;
    } else {
        document.getElementById('termsError').style.display = 'none';
    }

    // KVKK
    const kvkk = document.getElementById('Kvkk');
    if (!kvkk.checked) {
        document.getElementById('kvkkError').style.display = 'flex';
        valid = false;
    } else {
        document.getElementById('kvkkError').style.display = 'none';
    }

    return valid;
}

function submitForm() {
    const submitBtn = document.getElementById('submitBtn');
    submitBtn.disabled = true;
    submitBtn.classList.add('loading');
    submitBtn.textContent = 'Gönderiliyor...';

    const form = document.getElementById('registrationForm');

    // Form verilerini logla
    const formData = new FormData(form);
    console.log('Form verileri:');
    for (let [key, value] of formData.entries()) {
        console.log(`${key}: ${value}`);
    }

    form.submit();
}

function showError(inputId, errorId) {
    const input = document.getElementById(inputId);
    const error = document.getElementById(errorId);
    if (input && error) {
        input.classList.add('error');
        error.style.display = 'flex';
    }
}

function hideError(inputId, errorId) {
    const input = document.getElementById(inputId);
    const error = document.getElementById(errorId);
    if (input && error) {
        input.classList.remove('error');
        error.style.display = 'none';
    }
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function isValidPhone(phone) {
    const digits = phone.replace(/\D/g, '');
    return digits.length === 10 && digits[0] === '5';
}

function isValidTcNo(tcNo) {
    if (!/^\d{11}$/.test(tcNo)) return false;
    if (tcNo[0] === '0') return false;

    const digits = tcNo.split('').map(Number);
    const sum10 = digits.slice(0, 10).reduce((a, b) => a + b, 0);
    if (sum10 % 10 !== digits[10]) return false;

    const oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
    const evenSum = digits[1] + digits[3] + digits[5] + digits[7];
    if ((oddSum * 7 - evenSum) % 10 !== digits[9]) return false;

    return true;
}

function isStrongPassword(password) {
    return password.length >= 6 &&
        /[A-Z]/.test(password) &&
        /[0-9]/.test(password) &&
        /[+!@#$%^&*(),.?":{}|<>]/.test(password);
}

console.log('✅ DORTAGE Registration Form initialized successfully');