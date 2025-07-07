const API_BASE_URL = 'http://localhost:5050';

function sanitizeInput(input) {
    if (typeof input !== 'string') return '';
    return input.trim().replace(/[<>]/g, '');
}

function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function isValidPassword(password) {
    return password.length >= 6 && 
           /[A-Z]/.test(password) && 
           /[a-z]/.test(password) && 
           /\d/.test(password);
}

function isValidUsername(username) {
    return username.length >= 3 && 
           username.length <= 50 && 
           /^[a-zA-Z0-9]+$/.test(username);
}

function showToast(message, isError = false) {
    if (!message || (typeof message === 'string' && message.trim() === '')) return;
    if (typeof toastr !== 'undefined') {
        if (isError) {
            toastr.error(message);
        } else {
            toastr.success(message);
        }
    } else {
        alert(message);
    }
}

if (document.getElementById('login-form')) {
    const loginForm = document.getElementById('login-form');
    loginForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const email = sanitizeInput(document.getElementById('login-email').value);
        const password = document.getElementById('login-password').value;
        
        const errors = [];
        
        if (!email) {
            errors.push('Email is required');
        } else if (!isValidEmail(email)) {
            errors.push('Please enter a valid email address');
        }
        
        if (!password) {
            errors.push('Password is required');
        } else if (password.length < 6) {
            errors.push('Password must be at least 6 characters');
        }
        
        if (errors.length > 0) {
            showToast(errors.filter(Boolean).join(', '), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });
            const data = await res.json();
            
            if (res.ok && data.token) {
                localStorage.setItem('authToken', data.token);
                showToast('Login successful! Redirecting...', false);
                setTimeout(() => {
                    window.location.href = '/admin-dashboard';
                }, 1000);
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showToast(data.errors.join(', '), true);
                } else {
                    showToast(data.message || 'Login failed!', true);
                }
            }
        } catch (err) {
            console.error('Login error:', err);
            showToast('Login failed! Please try again.', true);
        }
    });
}

if (document.getElementById('register-form')) {
    const registerForm = document.getElementById('register-form');
    registerForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const username = sanitizeInput(document.getElementById('register-username').value);
        const fullName = sanitizeInput(document.getElementById('register-fullname').value);
        const email = sanitizeInput(document.getElementById('register-email').value);
        const password = document.getElementById('register-password').value;
        const termsChecked = document.getElementById('terms-conditions')?.checked;
        
        const errors = [];
        
        if (!username) {
            errors.push('Username is required');
        } else if (!isValidUsername(username)) {
            errors.push('Username must be between 3 and 50 characters and contain only letters and numbers');
        }
        
        if (!email) {
            errors.push('Email is required');
        } else if (!isValidEmail(email)) {
            errors.push('Please enter a valid email address');
        }
        
        if (!password) {
            errors.push('Password is required');
        } else if (!isValidPassword(password)) {
            errors.push('Password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, and one number');
        }
        
        if (fullName && !/^[a-zA-ZÀ-ỹ\s]+$/.test(fullName)) {
            errors.push('Full name can only contain letters, spaces, and Vietnamese characters');
        }
        
        if (!termsChecked) {
            errors.push('You must agree to the privacy policy & terms to register.');
        }
        
        if (errors.length > 0) {
            showToast(errors.filter(Boolean).join(', '), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    username, 
                    fullName: fullName || null, 
                    email, 
                    password 
                })
            });
            const data = await res.json();
            
            if (res.ok) {
                localStorage.setItem('pendingVerificationEmail', email);
                showToast('Registration successful! Please check your email to verify your account.', false);
                setTimeout(() => {
                    window.location.href = '/verify-email';
                }, 1000);
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showToast(data.errors.join(', '), true);
                } else {
                    showToast(data.message || 'Registration failed!', true);
                }
            }
        } catch (err) {
            console.error('Register error:', err);
            showToast('Registration failed! Please try again.', true);
        }
    });
}

if (document.getElementById('forgot-password-form')) {
    const forgotPasswordForm = document.getElementById('forgot-password-form');
    forgotPasswordForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const email = sanitizeInput(document.getElementById('forgot-password-email').value);
        
        if (!email) {
            showToast('Email is required', true);
            return;
        }
        
        if (!isValidEmail(email)) {
            showToast('Please enter a valid email address', true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/forgot-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });
            const data = await res.json();
            
            if (res.ok) {
                localStorage.setItem('pendingResetEmail', email);
                showToast('Password reset email sent! Please check your email.', false);
                setTimeout(() => {
                    window.location.href = '/login';
                }, 1500);
                forgotPasswordForm.reset();
            } else {
                showToast(data.message || 'Failed to send reset email!', true);
            }
        } catch (err) {
            console.error('Forgot password error:', err);
            showToast('Failed to send reset email! Please try again.', true);
        }
    });
}

if (document.getElementById('reset-password-form')) {
    const resetPasswordForm = document.getElementById('reset-password-form');
    resetPasswordForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const password = document.getElementById('reset-password-password').value;
        const confirmPassword = document.getElementById('reset-password-confirm-password').value;
        
        if (!password) {
            showToast('Password is required', true);
            return;
        }
        
        if (!isValidPassword(password)) {
            showToast('Password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, and one number', true);
            return;
        }
        
        if (password !== confirmPassword) {
            showToast('Passwords do not match', true);
            return;
        }
        
        const urlParams = new URLSearchParams(window.location.search);
        const token = urlParams.get('token');
        
        if (!token) {
            showToast('Invalid reset token', true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/reset-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    token: token,
                    newPassword: password,
                    confirmPassword: confirmPassword
                })
            });
            const data = await res.json();
            
            if (res.ok) {
                showToast('Password reset successful! Redirecting to login...', false);
                setTimeout(() => {
                    window.location.href = '/login';
                }, 2000);
            } else {
                showToast(data.message || 'Password reset failed!', true);
            }
        } catch (err) { 
            showToast('Password reset failed! Please try again.', true);
        }
    });
}

if (document.getElementById('change-password-form')) {
    const changePasswordForm = document.getElementById('change-password-form');
    changePasswordForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const currentPassword = document.getElementById('change-password-current').value;
        const newPassword = document.getElementById('change-password-new').value;
        const confirmPassword = document.getElementById('change-password-confirm').value;
        
        if (!currentPassword) {
            showToast('Current password is required', true);
            return;
        }
        
        if (!newPassword) {
            showToast('New password is required', true);
            return;
        }
        
        if (!isValidPassword(newPassword)) {
            showToast('New password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, and one number', true);
            return;
        }
        
        if (newPassword !== confirmPassword) {
            showToast('New passwords do not match', true);
            return;
        }
        
        const token = localStorage.getItem('authToken');
        if (!token) {
            showToast('You must be logged in to change password', true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/change-password`, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ 
                    currentPassword,
                    newPassword 
                })
            });
            const data = await res.json();
            
            if (res.ok) {
                showToast('Password changed successfully!', false);
                changePasswordForm.reset();
            } else {
                showToast(data.message || 'Password change failed!', true);
            }
        } catch (err) {
            showToast('Password change failed! Please try again.', true);
        }
    });
}
