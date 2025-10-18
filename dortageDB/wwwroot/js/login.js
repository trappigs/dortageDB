
        // Form elements
    const form = document.getElementById('loginForm');
    const emailInput = document.querySelector('input[name="Email"]');
    const passwordInput = document.querySelector('input[name="Password"]');
    const togglePassword = document.getElementById('togglePassword');
    const eyeIcon = document.getElementById('eyeIcon');
    const loginButton = document.getElementById('loginButton');
    const buttonText = document.getElementById('buttonText');
    const loadingOverlay = document.getElementById('loadingOverlay');
    const lockWarning = document.getElementById('lockWarning');

    // Error tracking
    let failedAttempts = parseInt(sessionStorage.getItem('failedLoginAttempts') || '0');
    const MAX_ATTEMPTS = 3;
    let isLocked = sessionStorage.getItem('accountLocked') === 'true';
    let lockTimer = null;

    // Check if account is locked on page load
    if (isLocked) {
            const lockUntil = parseInt(sessionStorage.getItem('lockUntil'));
    const now = Date.now();

    if (now < lockUntil) {
                const remainingSeconds = Math.floor((lockUntil - now) / 1000);
    lockWarning.classList.add('active');
    loginButton.disabled = true;
    startLockCountdown(Math.ceil(remainingSeconds / 60));
            } else {
        // Lock expired
        sessionStorage.removeItem('accountLocked');
    sessionStorage.removeItem('lockUntil');
    sessionStorage.removeItem('failedLoginAttempts');
    isLocked = false;
    failedAttempts = 0;
            }
        }

    // Toggle password visibility
    togglePassword?.addEventListener('click', function() {
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
    passwordInput.setAttribute('type', type);

    if (type === 'password') {
        eyeIcon.innerHTML = '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>';
            } else {
        eyeIcon.innerHTML = '<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/>';
            }
        });

    // Email validation
    function validateEmail(email) {
            const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
        }

    // Real-time email validation
    emailInput?.addEventListener('blur', function() {
            const emailError = document.getElementById('emailError');
    if (this.value && !validateEmail(this.value)) {
        this.classList.add('error');
    emailError.style.display = 'flex';
            } else if (this.value) {
        this.classList.remove('error');
    this.classList.add('success');
    emailError.style.display = 'none';
            }
        });

    emailInput?.addEventListener('input', function() {
            if (this.classList.contains('error')) {
        this.classList.remove('error');
    document.getElementById('emailError').style.display = 'none';
            }
        });

    // Real-time password validation
    passwordInput?.addEventListener('blur', function() {
            const passwordError = document.getElementById('passwordError');
    if (this.value && this.value.length < 8) {
        this.classList.add('error');
    passwordError.style.display = 'flex';
            } else if (this.value) {
        this.classList.remove('error');
    this.classList.add('success');
    passwordError.style.display = 'none';
            }
        });

    passwordInput?.addEventListener('input', function() {
            if (this.classList.contains('error')) {
        this.classList.remove('error');
    document.getElementById('passwordError').style.display = 'none';
            }
        });

    // Account lock countdown
    function startLockCountdown(minutes) {
        let timeLeft = minutes * 60;
    const countdownEl = document.getElementById('countdown');

    lockTimer = setInterval(function() {
                const mins = Math.floor(timeLeft / 60);
    const secs = timeLeft % 60;
    countdownEl.textContent = `${mins}:${secs.toString().padStart(2, '0')}`;

    timeLeft--;

    if (timeLeft < 0) {
        clearInterval(lockTimer);
    isLocked = false;
    failedAttempts = 0;
    lockWarning.classList.remove('active');
    loginButton.disabled = false;
    sessionStorage.removeItem('accountLocked');
    sessionStorage.removeItem('lockUntil');
    sessionStorage.removeItem('failedLoginAttempts');
                }
            }, 1000);
        }

    // Form submission
    form?.addEventListener('submit', function(e) {
            if (isLocked) {
        e.preventDefault();
    return;
            }

    // Show loading state
    loginButton.disabled = true;
    buttonText.innerHTML = '<div class="login-spinner"></div> Giriş yapılıyor...';
    loadingOverlay.classList.add('active');
        });

    // Handle failed login attempts (call this from server response)
    window.handleLoginFailure = function() {
        failedAttempts++;
    sessionStorage.setItem('failedLoginAttempts', failedAttempts.toString());

    loadingOverlay.classList.remove('active');
    loginButton.disabled = false;
    buttonText.textContent = 'Giriş Yap';
            
            if (failedAttempts >= MAX_ATTEMPTS) {
        // Lock account for 5 minutes
        isLocked = true;
    const lockUntil = Date.now() + (5 * 60 * 1000);
    sessionStorage.setItem('accountLocked', 'true');
    sessionStorage.setItem('lockUntil', lockUntil.toString());
    lockWarning.classList.add('active');
    loginButton.disabled = true;
    startLockCountdown(5);
            }
        };

    // Social login functions
    function loginWithGoogle() {
        window.location.href = '@Url.Action("ExternalLogin", "Account", new { provider = "Google", returnUrl = ViewData["ReturnUrl"] })';
        }

    function loginWithLinkedIn() {
        window.location.href = '@Url.Action("ExternalLogin", "Account", new { provider = "LinkedIn", returnUrl = ViewData["ReturnUrl"] })';
        }

    // Clean up on page unload
    window.addEventListener('beforeunload', function() {
            if (lockTimer) {
        clearInterval(lockTimer);
            }
        });