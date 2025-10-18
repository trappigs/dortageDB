// DOMContentLoaded event
document.addEventListener('DOMContentLoaded', function () {
    initializeForm();
});

function initializeForm() {
    attachEventListeners();
}

function attachEventListeners() {
    const form = document.getElementById('registrationForm');
    const inputs = form.querySelectorAll('input, select');


    // TC No validation
    const tcNoInput = document.getElementById('TcNo');
    tcNoInput.addEventListener('input', handleTcNoInput);
    tcNoInput.addEventListener('blur', handleTcNoBlur);

    // Email validation
    const emailInput = document.getElementById('Email');
    emailInput.addEventListener('blur', handleEmailBlur);
    emailInput.addEventListener('input', handleEmailInput);

    // Phone formatting
    const phoneInput = document.getElementById('PhoneNumber');
    phoneInput.addEventListener('input', handlePhoneInput);
    phoneInput.addEventListener('blur', handlePhoneBlur);

    // Password strength
    const passwordInput = document.getElementById('Password');
    passwordInput.addEventListener('input', handlePasswordInput);

    // Password toggle
    const passwordToggle = document.getElementById('passwordToggle');
    if (passwordToggle && passwordInput) {
        passwordToggle.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
            } else {
                passwordInput.type = 'password';
            }
        });
    }


    // Confirm password
    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    confirmPasswordInput.addEventListener('input', handleConfirmPasswordInput);
    confirmPasswordInput.addEventListener('blur', handleConfirmPasswordBlur);

    // Referral code
    const referralInput = document.getElementById('Code');
    referralInput.addEventListener('blur', handleReferralBlur);

    // Submit button
    const submitBtn = document.getElementById('submitBtn');
    submitBtn.addEventListener('click', handleSubmit);

    // Field validations
    document.getElementById('Ad').addEventListener('blur', () => validateField('Ad', 'AdError'));
    document.getElementById('Soyad').addEventListener('blur', () => validateField('Soyad', 'SoyadError'));
    document.getElementById('Sehir').addEventListener('blur', () => validateField('Sehir', 'cityError'));

    // Checkboxes
    document.getElementById('Pazarlama').addEventListener('change', () => {
        if (document.getElementById('Pazarlama').checked) {
            document.getElementById('termsError').style.display = 'none';
        }
    });

    document.getElementById('Kvkk').addEventListener('change', () => {
        if (document.getElementById('Kvkk').checked) {
            document.getElementById('kvkkError').style.display = 'none';
        }
    });

    // Focus first input
    document.getElementById('Ad').focus();
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
        checkEmailAvailability(emailInput.value);
    }
}

function handleEmailInput() {
    const emailInput = document.getElementById('Email');
    const emailValidIcon = document.getElementById('emailValidIcon');

    if (isValidEmail(emailInput.value)) {
        emailValidIcon.style.display = 'block';
        emailInput.classList.add('success');
        emailInput.classList.remove('error');
        hideError('Email', 'emailError');
    } else {
        emailValidIcon.style.display = 'none';
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
    if (!isValidTcNo(tcNoInput.value)) {
        showError('TcNo', 'tcNoError');
    } else {
        hideError('TcNo', 'tcNoError');
    }
}

function handlePasswordInput() {
    const passwordInput = document.getElementById('Password');
    const password = passwordInput.value;
    let strength = 0;

    const lengthReq = document.getElementById('req-length');
    if (password.length >= 8) {
        lengthReq.classList.add('met');
        strength += 25;
    } else {
        lengthReq.classList.remove('met');
    }

    const uppercaseReq = document.getElementById('req-uppercase');
    if (/[A-Z]/.test(password)) {
        uppercaseReq.classList.add('met');
        strength += 25;
    } else {
        uppercaseReq.classList.remove('met');
    }

    const numberReq = document.getElementById('req-number');
    if (/[0-9]/.test(password)) {
        numberReq.classList.add('met');
        strength += 25;
    } else {
        numberReq.classList.remove('met');
    }

    const specialReq = document.getElementById('req-special');
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
        specialReq.classList.add('met');
        strength += 25;
    } else {
        specialReq.classList.remove('met');
    }

    const passwordStrengthBar = document.getElementById('passwordStrengthBar');
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

function handleConfirmPasswordInput() {
    const passwordInput = document.getElementById('Password');
    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    const passwordMatchIcon = document.getElementById('passwordMatchIcon');

    if (confirmPasswordInput.value === passwordInput.value && confirmPasswordInput.value !== '') {
        passwordMatchIcon.style.display = 'block';
        confirmPasswordInput.classList.add('success');
        confirmPasswordInput.classList.remove('error');
        hideError('ConfirmPassword', 'confirmPasswordError');
    } else {
        passwordMatchIcon.style.display = 'none';
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
    if (referralInput.value.trim() && referralInput.value.length >= 6) {
        hideError('Code', 'referralError');
    } else if (referralInput.value.trim()) {
        showError('Code', 'referralError');
    }
}

function handleSubmit() {
    if (validateForm()) {
        submitForm();
    }
}

function validateForm() {
    let valid = true;

    // ✅ YENİ
    const firstName = document.getElementById('Ad');
    if (!firstName.value.trim()) {
        showError('Ad', 'AdError');  // ✅
        valid = false;
    } else {
        hideError('Ad', 'AdError');  // ✅
    }

    const lastName = document.getElementById('Soyad');
    if (!lastName.value.trim()) {
        showError('Soyad', 'SoyadError');  // ✅
        valid = false;
    } else {
        hideError('Soyad', 'SoyadError');  // ✅
    }

    const email = document.getElementById('Email');
    if (!isValidEmail(email.value)) {
        showError('Email', 'emailError');
        valid = false;
    } else {
        hideError('Email', 'emailError');
    }

    const phone = document.getElementById('PhoneNumber');
    if (!isValidPhone(phone.value)) {
        showError('PhoneNumber', 'phoneError');
        valid = false;
    } else {
        hideError('PhoneNumber', 'phoneError');
    }

    const tcNo = document.getElementById('TcNo');
    if (!isValidTcNo(tcNo.value)) {
        showError('TcNo', 'tcNoError');
        valid = false;
    } else {
        hideError('TcNo', 'tcNoError');
    }


    const city = document.getElementById('Sehir');
    if (!city.value) {
        showError('Sehir', 'cityError');
        valid = false;
    } else {
        hideError('Sehir', 'cityError');
    }

    const password = document.getElementById('Password');
    if (!isStrongPassword(password.value)) {
        valid = false;
    }

    const confirmPassword = document.getElementById('ConfirmPassword');
    if (password.value !== confirmPassword.value) {
        showError('ConfirmPassword', 'confirmPasswordError');
        valid = false;
    } else {
        hideError('ConfirmPassword', 'confirmPasswordError');
    }

    const referralCode = document.getElementById('Code');
    if (!referralCode.value.trim() || referralCode.value.length < 6) {
        showError('Code', 'referralError');
        valid = false;
    } else {
        hideError('Code', 'referralError');
    }

    const terms = document.getElementById('Pazarlama');
    if (!terms.checked) {
        document.getElementById('termsError').style.display = 'flex';
        valid = false;
    } else {
        document.getElementById('termsError').style.display = 'none';
    }

    const kvkk = document.getElementById('Kvkk');
    if (!kvkk.checked) {
        document.getElementById('kvkkError').style.display = 'flex';
        valid = false;
    } else {
        document.getElementById('kvkkError').style.display = 'none';
    }

    return valid;
}

// ✅ YENİ submitForm
function submitForm() {
    const submitBtn = document.getElementById('submitBtn');
    submitBtn.disabled = true;
    submitBtn.classList.add('loading');

    const form = document.getElementById('registrationForm');
    form.submit(); // Direkt form submit
}

function checkEmailAvailability(email) {
    fetch('/Account/CheckEmailAvailability', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(email)
    })
        .then(response => response.json())
        .then(data => {
            if (!data.available) {
                const emailInput = document.getElementById('Email');
                document.getElementById('emailError').innerHTML = '<span>⚠</span> Bu e-posta adresi zaten kayıtlı. <a href="/Account/Login" style="color: #000a68; text-decoration: underline;">Giriş Yap</a>';
                document.getElementById('emailError').style.display = 'flex';
                emailInput.classList.add('error');
                emailInput.classList.remove('success');
            }
        });
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
    return password.length >= 8 &&
        /[A-Z]/.test(password) &&
        /[0-9]/.test(password) &&
        /[!@#$%^&*(),.?":{}|<>]/.test(password);
}

console.log('DORTAGE Registration Form initialized successfully');