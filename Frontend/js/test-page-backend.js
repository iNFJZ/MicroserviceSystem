// test-page-backend.js
$(function () {
    // Initialize page
    console.log('Test Page page loaded');
    
    // Load page data
    loadPageData();
});

async function loadPageData() {
    try {
        const token = window.adminAuth.getAuthToken();
        const response = await fetch('http://localhost:5050/api/your-endpoint', {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            const data = await response.json();
            // Handle your data here
            console.log('Data loaded:', data);
        }
    } catch (error) {
        console.error('Error loading data:', error);
        if (typeof toastr !== 'undefined') {
            toastr.error('Failed to load data');
        }
    }
}

// Add your page-specific functions here
function handlePageAction() {
    // Your page logic here
    console.log('Page action triggered');
}

// Example: Handle form submission
$(document).on('submit', '#your-form', function(e) {
    e.preventDefault();
    // Handle form submission
    console.log('Form submitted');
});

// Example: Handle button clicks
$(document).on('click', '#your-button', function() {
    handlePageAction();
});
