const API_BASE_URL = 'http://localhost:5001';
const GOOGLE_CLIENT_ID = '157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com';

function handleCredentialResponse(response) {
    showResult('info', 'Processing Google authentication...');
    
    google.accounts.oauth2.initTokenClient({
        client_id: GOOGLE_CLIENT_ID,
        scope: 'openid email profile',
        callback: function(tokenResponse) {
            console.log('Google Access Token:', tokenResponse.access_token);
            loginWithGoogle(tokenResponse.access_token);
        }
    }).requestAccessToken();
}

function loginWithGoogle(accessToken) {
    showResult('info', '<span class="loading"></span>Logging in with Google...');
    
    fetch(`${API_BASE_URL}/api/Auth/login/google`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            accessToken: accessToken
        })
    })
    .then(response => {
        if (!response.ok) {
            return response.json().then(err => {
                throw new Error(err.message || `HTTP error! status: ${response.status}`);
            });
        }
        return response.json();
    })
    .then(data => {
        if (data.token) {
            localStorage.setItem('authToken', data.token);
            showResult('success', 
                '<span class="status-indicator success"></span>Login with Google successful!<br>' +
                '<strong>JWT Token:</strong><br>' +
                '<div class="token-display">' + data.token.substring(0, 100) + '...</div>' +
                '<br><button onclick="copyToken()" class="btn btn-secondary">Copy Token</button>'
            );
        } else {
            showResult('error', 
                '<span class="status-indicator error"></span>Login failed: ' + (data.message || 'Unknown error')
            );
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showResult('error', 
            '<span class="status-indicator error"></span>Connection error: ' + error.message
        );
    });
}

document.getElementById('loginForm').addEventListener('submit', function(e) {
    e.preventDefault();
    
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    
    showResult('info', '<span class="loading"></span>Logging in...');
    
    fetch(`${API_BASE_URL}/api/Auth/login`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            email: email,
            password: password
        })
    })
    .then(response => {
        if (!response.ok) {
            return response.json().then(err => {
                throw new Error(err.message || `HTTP error! status: ${response.status}`);
            });
        }
        return response.json();
    })
    .then(data => {
        if (data.token) {
            localStorage.setItem('authToken', data.token);
            showResult('success', 
                '<span class="status-indicator success"></span>Login successful!<br>' +
                '<strong>JWT Token:</strong><br>' +
                '<div class="token-display">' + data.token.substring(0, 100) + '...</div>' +
                '<br><button onclick="copyToken()" class="btn btn-secondary">Copy Token</button>'
            );
        } else {
            showResult('error', 
                '<span class="status-indicator error"></span>Login failed: ' + (data.message || 'Unknown error')
            );
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showResult('error', 
            '<span class="status-indicator error"></span>Connection error: ' + error.message
        );
    });
});

function showResult(type, message) {
    const resultDiv = document.getElementById('result');
    resultDiv.className = `result ${type}`;
    resultDiv.innerHTML = message;
    resultDiv.style.display = 'block';
    
    if (type === 'info') {
        setTimeout(() => {
            if (resultDiv.innerHTML.includes('loading')) {
                resultDiv.style.display = 'none';
            }
        }, 5000);
    }
}

function copyToken() {
    const token = localStorage.getItem('authToken');
    if (token) {
        navigator.clipboard.writeText(token).then(() => {
            showResult('success', '<span class="status-indicator success"></span>Token copied to clipboard!');
        }).catch(() => {
            showResult('error', '<span class="status-indicator error"></span>Cannot copy token');
        });
    }
}

function clearToken() {
    localStorage.removeItem('authToken');
    showResult('info', '<span class="status-indicator info"></span>Token deleted');
}

function testApiConnection() {
    showResult('info', '<span class="loading"></span>Testing API connection...');
    
    fetch(`${API_BASE_URL}/api/Auth/validate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            token: 'test-token'
        })
    })
    .then(response => {
        if (response.ok) {
            showResult('success', '<span class="status-indicator success"></span>API connection successful!');
        } else {
            showResult('error', '<span class="status-indicator error"></span>API not available');
        }
    })
    .catch(error => {
        showResult('error', '<span class="status-indicator error"></span>Cannot connect to API: ' + error.message);
    });
}

window.addEventListener('load', function() {
    const token = localStorage.getItem('authToken');
    if (token) {
        showResult('info', 
            '<span class="status-indicator info"></span>You are logged in!<br>' +
            '<div class="token-display">' + token.substring(0, 100) + '...</div>' +
            '<br><button onclick="copyToken()" class="btn btn-secondary">Copy Token</button> ' +
            '<button onclick="clearToken()" class="btn btn-secondary">Delete Token</button>'
        );
    }
    
    setTimeout(testApiConnection, 1000);
});

console.log('Frontend loaded successfully!');
console.log('API Base URL:', API_BASE_URL);
console.log('Google Client ID:', GOOGLE_CLIENT_ID); 