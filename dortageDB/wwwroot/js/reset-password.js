// wwwroot/js/reset-password.js

document.addEventListener('DOMContentLoaded', function () {
    initializePasswordToggles();
    initializePasswordStrength();
    initializePasswordMatch();
    initializeFormSubmission();
});

function initializePasswordToggles() {
    const toggleButtons = document.querySelectorAll('.toggle-password');

    toggleButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const eyeOpen = this.querySelector('.eye-open');
            const eyeClosed = this.querySelector('.eye-closed');

            if (input.type === 'password') {
                input.type = 'text';
                eyeOpen.style.display = 'none';
                eyeClosed.style.display = 'block';
            } else {
                input.type = 'password';
                eyeOpen.style.display = 'block';
                eyeClosed.style.display = 'none';
            }
        });
    });
}

function initializePasswordStrength() {
    const newPasswordInput = document.getElementById('newPasswordInput');
    if (!newPasswordInput) return;

    newPasswordInput.addEventListener('input', function () {
        const password = this.value;
        const strengthBars = document.querySelectorAll('.strength-bar');
        const strengthText = document.getElementById('strengthText');

        let strength = 0;
        let strengthLevel = '';

        // Check requirements
        const hasLength = password.length >= 6;
        const hasUppercase = /[A-Z]/.test(password);
        const hasNumber = /[0-9]/.test(password);
        const hasSpecial = /[!@#$%^&*(),.?":{}|<>+\-_=\[\]\\\/;'`~]/.test(password);

        // Update requirement indicators
        updateRequirement('req-length', hasLength);
        updateRequirement('req-uppercase', hasUppercase);
        updateRequirement('req-number', hasNumber);
        updateRequirement('req-special', hasSpecial);

        // Calculate strength
        if (hasLength) strength++;
        if (hasUppercase) strength++;
        if (hasNumber) strength++;
        if (hasSpecial) strength++;

        // Update strength bars
        strengthBars.forEach((bar, index) => {
            bar.classList.remove('active', 'weak', 'medium', 'strong');
            if (index < strength) {
                bar.classList.add('active');
                if (strength <= 1) {
                    bar.classList.add('weak');
                    strengthLevel = 'Zayıf';
                } else if (strength <= 2) {
                    bar.classList.add('medium');
                    strengthLevel = 'Orta';
                } else {
                    bar.classList.add('strong');
                    strengthLevel = strength === 3 ? 'İyi' : 'Güçlü';
                }
            }
        });

        // Update strength text
        if (password.length === 0) {
            strengthText.textContent = '-';
        } else {
            strengthText.textContent = strengthLevel;
        }
    });
}

function updateRequirement(id, met) {
    const element = document.getElementById(id);
    if (element) {
        if (met) {
            element.classList.add('met');
        } else {
            element.classList.remove('met');
        }
    }
}

function initializePasswordMatch() {
    const newPasswordInput = document.getElementById('newPasswordInput');
    const confirmPasswordInput = document.getElementById('confirmPasswordInput');
    const matchIndicator = document.getElementById('matchIndicator');

    if (!confirmPasswordInput || !newPasswordInput) return;

    confirmPasswordInput.addEventListener('input', function () {
        const newPassword = newPasswordInput.value;
        const confirmPassword = this.value;

        if (confirmPassword.length > 0) {
            if (newPassword === confirmPassword) {
                matchIndicator.style.display = 'block';
                this.style.borderColor = 'var(--success)';
            } else {
                matchIndicator.style.display = 'none';
                this.style.borderColor = 'var(--error)';
            }
        } else {
            matchIndicator.style.display = 'none';
            this.style.borderColor = 'var(--gray-200)';
        }
    });
}

function initializeFormSubmission() {
    const form = document.getElementById('resetPasswordForm');
    if (!form) return;

    form.addEventListener('submit', function () {
        const submitBtn = document.getElementById('submitBtn');
        const btnText = submitBtn.querySelector('.btn-text');
        const btnLoader = submitBtn.querySelector('.btn-loader');

        submitBtn.disabled = true;
        btnText.style.display = 'none';
        btnLoader.style.display = 'inline-block';
    });
}

console.log('✅ Reset Password JavaScript loaded successfully');